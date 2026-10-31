using CueApp.Core.Audio;
using ManagedBass;

namespace CueApp.Audio;

/// <summary>
/// Один играющий файл на BASS: trim in/out, fade in/out, громкость, повтор N раз или ∞.
/// Trim и петля делаются средствами самого BASS (BASS_POS_END / BASS_POS_LOOP) —
/// конец фрагмента точный до сэмпла, повтор без щелчка на стыке.
/// </summary>
internal sealed class BassPlayback : IAudioPlayback
{
    // Режимы BASS_ChannelSetPosition, которых нет в перечислении ManagedBass (см. bass.h)
    private const PositionFlags PosEnd = (PositionFlags)0x10;   // BASS_POS_END: где поток заканчивается
    private const PositionFlags PosLoop = (PositionFlags)0x11;  // BASS_POS_LOOP: куда прыгать при петле

    /// <summary>Минимальный фейд при остановке, чтобы не было щелчка.</summary>
    private const double MicroFadeSeconds = 0.01;

    private readonly int _stream;
    private readonly AudioPlayRequest _request;
    private readonly int _totalPasses;
    private readonly float _volume;

    // Делегаты синхронизаций храним в полях: иначе сборщик мусора их удалит, а BASS будет их вызывать
    private readonly SyncProcedure _onDecodedEnd;
    private readonly SyncProcedure _onHeardEnd;
    private readonly SyncProcedure _onFadeOutPoint;

    private int _decodedPasses;   // сколько проходов уже ДЕКОДИРОВАНО (BASS декодирует с опережением)
    private int _heardPasses;     // сколько проходов уже ПРОЗВУЧАЛО
    private int _finished;        // 0/1, меняется атомарно — Finish вызывается ровно один раз

    public event EventHandler? Finished;

    public BassPlayback(AudioPlayRequest request)
    {
        _request = request;
        _totalPasses = Math.Max(1, request.PlayCount);
        _volume = (float)Math.Pow(10, request.VolumeDb / 20);

        _stream = Bass.CreateStream(request.FilePath, 0, 0, BassFlags.Float | BassFlags.Prescan);
        if (_stream == 0)
            throw new InvalidOperationException(BassErrors.Describe(Bass.LastError));

        double fileLength = Bass.ChannelBytes2Seconds(_stream, Bass.ChannelGetLength(_stream));
        double start = Math.Clamp(request.TrimIn, 0, fileLength);
        double end = Math.Clamp(request.TrimOut ?? fileLength, start, fileLength);

        Bass.ChannelSetPosition(_stream, Bass.ChannelSeconds2Bytes(_stream, end), PosEnd);
        Bass.ChannelSetPosition(_stream, Bass.ChannelSeconds2Bytes(_stream, start), PosLoop);
        Bass.ChannelSetPosition(_stream, Bass.ChannelSeconds2Bytes(_stream, start));

        bool loops = request.LoopForever || _totalPasses > 1;
        if (loops)
            Bass.ChannelFlags(_stream, BassFlags.Loop, BassFlags.Loop);

        // Конец прохода в момент ДЕКОДИРОВАНИЯ: решаем, нужна ли еще петля
        _onDecodedEnd = OnDecodedEnd;
        Bass.ChannelSetSync(_stream, SyncFlags.End | SyncFlags.Mixtime, 0, _onDecodedEnd);

        // Конец прохода в момент, когда его СЛЫШНО: считаем проходы и ловим окончание
        _onHeardEnd = OnHeardEnd;
        Bass.ChannelSetSync(_stream, SyncFlags.End, 0, _onHeardEnd);

        // Fade out — только в последнем проходе
        _onFadeOutPoint = OnFadeOutPoint;
        if (request.FadeOut > 0 && !request.LoopForever)
        {
            double fadeStart = Math.Max(start, end - request.FadeOut);
            Bass.ChannelSetSync(_stream, SyncFlags.Position, Bass.ChannelSeconds2Bytes(_stream, fadeStart), _onFadeOutPoint);
        }

        // Fade in — только в начале первого прохода
        if (request.FadeIn > 0)
        {
            Bass.ChannelSetAttribute(_stream, ChannelAttribute.Volume, 0);
            Bass.ChannelPlay(_stream);
            Bass.ChannelSlideAttribute(_stream, ChannelAttribute.Volume, _volume, (int)(request.FadeIn * 1000));
        }
        else
        {
            Bass.ChannelSetAttribute(_stream, ChannelAttribute.Volume, _volume);
            Bass.ChannelPlay(_stream);
        }
    }

    public double Position => IsFinished
        ? 0
        : Bass.ChannelBytes2Seconds(_stream, Bass.ChannelGetPosition(_stream));

    public int CurrentPass => Math.Min(_heardPasses + 1, _request.LoopForever ? int.MaxValue : _totalPasses);

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

    private void OnDecodedEnd(int handle, int channel, int data, IntPtr user)
    {
        _decodedPasses++;
        // BASS решает «петля или конец» ПОСЛЕ этого вызова: если это конец последнего прохода —
        // снимаем флаг петли, и поток заканчивается здесь же
        if (!_request.LoopForever && _decodedPasses >= _totalPasses)
            Bass.ChannelFlags(_stream, BassFlags.Default, BassFlags.Loop);
    }

    private void OnHeardEnd(int handle, int channel, int data, IntPtr user)
    {
        _heardPasses++;
        if (!_request.LoopForever && _heardPasses >= _totalPasses)
            Task.Run(Finish); // из колбэка BASS поток освобождать нельзя
    }

    private void OnFadeOutPoint(int handle, int channel, int data, IntPtr user)
    {
        if (_heardPasses == _totalPasses - 1)
            Bass.ChannelSlideAttribute(_stream, ChannelAttribute.Volume, 0, (int)(_request.FadeOut * 1000));
    }

    private void Finish()
    {
        if (Interlocked.Exchange(ref _finished, 1) == 1)
            return;

        Bass.ChannelStop(_stream);
        Bass.StreamFree(_stream);
        Finished?.Invoke(this, EventArgs.Empty);
    }
}
