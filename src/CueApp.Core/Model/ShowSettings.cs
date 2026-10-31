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

    /// <summary>Что делает GO на группе и куда после него переходит выбор.</summary>
    public GroupGoMode GroupGo { get; set; } = GroupGoMode.RunWholeGroup;

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

public enum GroupGoMode
{
    /// <summary>GO запускает группу целиком, выбор переходит на следующую основную кью.</summary>
    RunWholeGroup,

    /// <summary>Группа как папка: GO ничего не запускает, выбор переходит на первую подкью.</summary>
    EnterGroup,
}
