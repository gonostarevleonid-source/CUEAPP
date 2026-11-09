namespace CueApp.Core.Model;

/// <summary>
/// Трек: аудиофайл, который играет один раз — от GO до конца (или до Stop).
/// Позже у трека появятся таймлайн с маркерами и таймкод.
/// </summary>
public sealed class TrackCue : Cue
{
    /// <summary>Путь к файлу (относительно папки шоу, через «/»).</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Начало проигрывания во времени файла.</summary>
    public double TrimIn { get; set; }

    /// <summary>Конец проигрывания во времени файла; null — до конца файла.</summary>
    public double? TrimOut { get; set; }

    public double FadeIn { get; set; }

    public double FadeOut { get; set; }

    /// <summary>Громкость в дБ (0 = без изменений).</summary>
    public double VolumeDb { get; set; }

    /// <summary>Имена выходов из таблицы выходов проекта («Main L/R», «Sub»...).</summary>
    public List<string> Outputs { get; set; } = new();
}

/// <summary>Остановка одной кью или всех.</summary>
public sealed class StopCue : Cue
{
    /// <summary>Id целевой кью; null — остановить все.</summary>
    public Guid? TargetCueId { get; set; }

    public double FadeOut { get; set; }
}

/// <summary>Группа (1 → 1.1, 1.2…) для структуры шоу: блоки, акты.</summary>
public sealed class GroupCue : Cue
{
    public List<Cue> Children { get; set; } = new();
}
