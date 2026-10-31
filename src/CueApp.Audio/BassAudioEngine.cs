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
    private const int Channels = 2;

    private readonly List<int> _streams = new();

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

    public void PlayTestTone()
    {
        float[] samples = GenerateTone(frequencyHz: 1000, seconds: 2.0, levelDb: -12);

        // Push-поток: BASS копирует данные к себе, после этого массив можно не хранить
        int stream = Bass.CreateStream(SampleRate, Channels, BassFlags.Float | BassFlags.AutoFree,
            StreamProcedureType.Push);
        if (stream == 0)
            throw new InvalidOperationException("Не удалось создать поток: " + Bass.LastError);

        // Флаг End говорит BASS, что больше данных не будет — поток сам освободится в конце
        Bass.StreamPutData(stream, samples, (samples.Length * sizeof(float)) | (int)StreamProcedureType.End);
        Bass.ChannelPlay(stream);

        lock (_streams)
            _streams.Add(stream);
    }

    public void StopAll()
    {
        lock (_streams)
        {
            foreach (int stream in _streams)
                Bass.StreamFree(stream); // уже освобожденный поток просто вернет false
            _streams.Clear();
        }
    }

    public void Dispose()
    {
        if (!IsInitialized)
            return;

        StopAll();
        Bass.Free();
        IsInitialized = false;
    }

    /// <summary>Синус в стерео (float), с фейдами 10 мс на краях, чтобы не было щелчков.</summary>
    private static float[] GenerateTone(double frequencyHz, double seconds, double levelDb)
    {
        int frames = (int)(SampleRate * seconds);
        int fadeFrames = SampleRate / 100;
        double amplitude = Math.Pow(10, levelDb / 20);
        var data = new float[frames * Channels];

        for (int i = 0; i < frames; i++)
        {
            double fade = Math.Min(1.0, Math.Min(i, frames - 1 - i) / (double)fadeFrames);
            float value = (float)(amplitude * fade * Math.Sin(2 * Math.PI * frequencyHz * i / SampleRate));
            data[i * Channels] = value;
            data[i * Channels + 1] = value;
        }

        return data;
    }
}
