using CueApp.Core.Audio;
using ManagedBass;

namespace CueApp.Audio;

/// <summary>
/// Один играющий файл на BASS: trim in/out, fade in/out, громкость. Играет один раз.
/// Конец фрагмента задается средствами самого BASS (BASS_POS_END) — точно до сэмпла.
/// </summary>
internal sealed class BassPlayback : IAudioPlayback
{
    // Режим BASS_ChannelSetPosition, которого нет в перечислении ManagedBass (см. bass.h)
    private const PositionFlags PosEnd = (PositionFlags)0x10;   // BASS_POS_END: где поток заканчивается

    /// <summary>Минимальный фейд при остановке, чтобы не было щелчка.</summary>
    private const double MicroFadeSeconds = 0.01;

    private readonly int _stream;
    private readonly AudioPlayRequest _request;

    // Делегаты синхронизаций храним в полях: иначе сборщик мусора их удалит, а BASS будет их вызывать
    private readonly SyncProcedure _onHeardEnd;
    private readonly SyncProcedure _onFadeOutPoint;

    private int _finished; // 0/1, меняется атомарно — Finish вызывается ровно один раз

    public event EventHandler? Finished;

    public BassPlayback(AudioPlayRequest request)
    {
        _request = request;
        float volume = (float)Math.Pow(10, request.VolumeDb / 20);

        _stream = Bass.CreateStream(request.FilePath, 0, 0, BassFlags.Float | BassFlags.Prescan);
        if (_stream == 0)
            throw new InvalidOperationException(BassErrors.Describe(Bass.LastError));

        double fileLength = Bass.ChannelBytes2Seconds(_stream, Bass.ChannelGetLength(_stream));
        double start = Math.Clamp(request.TrimIn, 0, fileLength);
        double end = Math.Clamp(request.TrimOut ?? fileLength, start, fileLength);

        Bass.ChannelSetPosition(_stream, Bass.ChannelSeconds2Bytes(_stream, end), PosEnd);
        Bass.ChannelSetPosition(_stream, Bass.ChannelSeconds2Bytes(_stream, start));

        // Конец в момент, когда его СЛЫШНО: трек доиграл
        _onHeardEnd = OnHeardEnd;
        Bass.ChannelSetSync(_stream, SyncFlags.End, 0, _onHeardEnd);

        // Fade out — так, чтобы закончился ровно на trim out
        _onFadeOutPoint = OnFadeOutPoint;
        if (request.FadeOut > 0)
        {
            double fadeStart = Math.Max(start, end - request.FadeOut);
            Bass.ChannelSetSync(_stream, SyncFlags.Position, Bass.ChannelSeconds2Bytes(_stream, fadeStart), _onFadeOutPoint);
        }

        if (request.FadeIn > 0)
        {
            Bass.ChannelSetAttribute(_stream, ChannelAttribute.Volume, 0);
            Bass.ChannelPlay(_stream);
            Bass.ChannelSlideAttribute(_stream, ChannelAttribute.Volume, volume, (int)(request.FadeIn * 1000));
        }
        else
        {
            Bass.ChannelSetAttribute(_stream, ChannelAttribute.Volume, volume);
            Bass.ChannelPlay(_stream);
        }
    }

    public double Position => IsFinished
        ? 0
        : Bass.ChannelBytes2Seconds(_stream, Bass.ChannelGetPosition(_stream));

    public bool IsFinished => Volatile.Read(ref _finished) == 1;

    public void Stop(double fadeSeconds)
    {
        if (IsFinished)
            return;

        int ms = (int)(Math.Max(fadeSeconds, MicroFadeSeconds) * 1000);
        Bass.ChannelSlideAttribute(_stream, ChannelAttribute.Volume, 0, ms);

        // Когда фейд дошел до нуля — освобождаем поток (не из потока BASS, а из пула)
        Task.Delay(ms + 20).ContinueWith(_ => Finish());
    }

    // Из колбэка BASS поток освобождать нельзя — переходим в пул потоков
    private void OnHeardEnd(int handle, int channel, int data, IntPtr user) => Task.Run(Finish);

    private void OnFadeOutPoint(int handle, int channel, int data, IntPtr user) =>
        Bass.ChannelSlideAttribute(_stream, ChannelAttribute.Volume, 0, (int)(_request.FadeOut * 1000));

    private void Finish()
    {
        if (Interlocked.Exchange(ref _finished, 1) == 1)
            return;

        Bass.ChannelStop(_stream);
        Bass.StreamFree(_stream);
        Finished?.Invoke(this, EventArgs.Empty);
    }
}
