namespace CueApp.Core.Audio;

/// <summary>
/// Аудиодвижок. Ядро и UI работают только с этим интерфейсом,
/// конкретная библиотека (сейчас BASS) спрятана в проекте CueApp.Audio.
/// </summary>
public interface IAudioEngine : IDisposable
{
    /// <summary>Готов ли движок играть звук.</summary>
    bool IsInitialized { get; }

    /// <summary>Открывает аудиоустройство. Бросает исключение, если не удалось.</summary>
    void Initialize();

    /// <summary>Длина файла в секундах. Бросает исключение, если файла нет или формат не поддерживается.</summary>
    double GetDuration(string filePath);

    /// <summary>Запустить файл. Бросает исключение, если файл не открылся.</summary>
    IAudioPlayback Play(AudioPlayRequest request);

    /// <summary>Остановить всё, что играет, с фейдом.</summary>
    void StopAll(double fadeSeconds);
}

/// <summary>Что и как проиграть. Все времена — в секундах во времени файла.</summary>
public sealed record AudioPlayRequest(string FilePath)
{
    public double TrimIn { get; init; }

    /// <summary>null — до конца файла.</summary>
    public double? TrimOut { get; init; }

    public double FadeIn { get; init; }

    /// <summary>Фейд в конце (заканчивается ровно на trim out).</summary>
    public double FadeOut { get; init; }

    public double VolumeDb { get; init; }
}

/// <summary>Один играющий звук.</summary>
public interface IAudioPlayback
{
    /// <summary>Текущая (слышимая) позиция во времени файла, сек.</summary>
    double Position { get; }

    bool IsFinished { get; }

    /// <summary>Остановить с фейдом (0 — мгновенно, с микрофейдом против щелчка).</summary>
    void Stop(double fadeSeconds);

    /// <summary>
    /// Звук закончился (доиграл или остановлен). ВНИМАНИЕ: вызывается из потока аудио,
    /// в UI нужно переходить через Dispatcher.
    /// </summary>
    event EventHandler? Finished;
}
