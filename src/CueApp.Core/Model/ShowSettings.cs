namespace CueApp.Core.Model;

/// <summary>
/// Настройки проекта (раздел 9 ТЗ), хранятся в шоу-файле.
/// Пока только то, что нужно на этапе 1; остальные разделы добавятся вместе с функциями.
/// </summary>
public sealed class ShowSettings
{
    // --- Воспроизведение ---

    /// <summary>Блокировать запуск кью, пока играет таймлайн (Stop и Panic работают всегда).</summary>
    public bool BlockGoWhileTimelinePlaying { get; set; }

    public double PanicFadeSeconds { get; set; } = 0.5;

    // --- Отсчет ---

    /// <summary>Ниже этого порога отсчет желтый, сек.</summary>
    public double CountdownWarningSeconds { get; set; } = 30;

    /// <summary>Ниже этого порога отсчет красный и мигает, сек.</summary>
    public double CountdownCriticalSeconds { get; set; } = 10;

    // --- Show mode ---

    /// <summary>Защита от двойного GO, мс.</summary>
    public int DoubleGoProtectionMs { get; set; } = 300;

    /// <summary>Клавиша GO (имя клавиши Avalonia).</summary>
    public string GoKey { get; set; } = "Space";
}
