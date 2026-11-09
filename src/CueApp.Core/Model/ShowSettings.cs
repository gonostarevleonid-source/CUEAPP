using CueApp.Core.Time;

namespace CueApp.Core.Model;

/// <summary>
/// Настройки проекта (раздел 9 ТЗ), хранятся в шоу-файле.
/// Пока только то, что нужно на этапе 1; остальные разделы добавятся вместе с функциями.
/// </summary>
public sealed class ShowSettings
{
    // --- Воспроизведение ---

    /// <summary>Блокировать запуск кью, пока играет трек (Stop и Panic работают всегда).</summary>
    public bool BlockGoWhileTrackPlaying { get; set; }

    /// <summary>Фейд кнопки STOP (остановить всё), сек.</summary>
    public double StopFadeSeconds { get; set; } = 1;

    public double PanicFadeSeconds { get; set; } = 0.5;

    // --- Таймкод ---

    /// <summary>Частота для новых треков.</summary>
    public FrameRate DefaultFrameRate { get; set; } = FrameRate.Fps25;

    /// <summary>Старт TC первого трека в шоу.</summary>
    public Timecode FirstTrackTimecode { get; set; } = new(1, 0, 0, 0);

    /// <summary>Следующий трек стартует через столько минут после конца предыдущего…</summary>
    public int StartTimecodeGapMinutes { get; set; } = 5;

    /// <summary>…с округлением вверх до целого числа таких минут (01:08:47 → 01:10:00:00).</summary>
    public int StartTimecodeRoundMinutes { get; set; } = 5;

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
