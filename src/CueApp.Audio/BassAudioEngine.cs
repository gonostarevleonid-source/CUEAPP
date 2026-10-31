using CueApp.Core.Audio;
using ManagedBass;

namespace CueApp.Audio;

/// <summary>
/// Реализация IAudioEngine на библиотеке BASS (через ManagedBass).
/// Нужен нативный файл рядом с программой: bass.dll (Windows) / libbass.dylib (macOS).
/// </summary>
public sealed class BassAudioEngine : IAudioEngine
{
    private const int SampleRate = 48000;

    private readonly List<BassPlayback> _playing = new();

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        // -1 = устройство вывода по умолчанию
        if (!Bass.Init(-1, SampleRate, DeviceInitFlags.Default))
            throw new InvalidOperationException("BASS не инициализирован: " + Bass.LastError);

        IsInitialized = true;
    }

    public double GetDuration(string filePath)
    {
        // Decode — только читаем файл, ничего не играем; Prescan — точная длина для MP3
        int stream = Bass.CreateStream(filePath, 0, 0, BassFlags.Decode | BassFlags.Prescan | BassFlags.Float);
        if (stream == 0)
            throw new InvalidOperationException(BassErrors.Describe(Bass.LastError));

        try
        {
            return Bass.ChannelBytes2Seconds(stream, Bass.ChannelGetLength(stream));
        }
        finally
        {
            Bass.StreamFree(stream);
        }
    }

    public IAudioPlayback Play(AudioPlayRequest request)
    {
        var playback = new BassPlayback(request);
        lock (_playing)
            _playing.Add(playback);

        playback.Finished += (_, _) =>
        {
            lock (_playing)
                _playing.Remove(playback);
        };
        return playback;
    }

    public void StopAll(double fadeSeconds)
    {
        BassPlayback[] snapshot;
        lock (_playing)
            snapshot = _playing.ToArray();

        foreach (var playback in snapshot)
            playback.Stop(fadeSeconds);
    }

    public void Dispose()
    {
        if (!IsInitialized)
            return;

        Bass.Free();
        IsInitialized = false;
    }
}
