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

    /// <summary>Временный метод этапа 0: проиграть тестовый тон 1 кГц.</summary>
    void PlayTestTone();

    /// <summary>Остановить всё, что играет.</summary>
    void StopAll();
}
