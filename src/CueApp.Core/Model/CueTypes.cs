namespace CueApp.Core.Model;

/// <summary>Проигрывание аудиофайла.</summary>
public sealed class AudioCue : Cue
{
    /// <summary>Путь к файлу (лучше относительный от папки шоу).</summary>
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

    /// <summary>Сколько раз проиграть (1, 2, 3...). Игнорируется, если LoopForever.</summary>
    public int PlayCount { get; set; } = 1;

    /// <summary>Повторять бесконечно (∞).</summary>
    public bool LoopForever { get; set; }
}

/// <summary>Плавное изменение громкости другой кью.</summary>
public sealed class FadeCue : Cue
{
    /// <summary>Id целевой кью; null — цель не выбрана.</summary>
    public Guid? TargetCueId { get; set; }

    public double Duration { get; set; } = 3;

    /// <summary>До какой громкости фейдить, дБ.</summary>
    public double TargetVolumeDb { get; set; }

    public FadeCurve Curve { get; set; } = FadeCurve.Linear;
}

public enum FadeCurve
{
    Linear,
    SCurve,
}

/// <summary>Остановка одной кью или всех.</summary>
public sealed class StopCue : Cue
{
    /// <summary>Id целевой кью; null — остановить все.</summary>
    public Guid? TargetCueId { get; set; }

    public double FadeOut { get; set; }
}

/// <summary>Пауза.</summary>
public sealed class WaitCue : Cue
{
    public double Duration { get; set; } = 5;
}

/// <summary>Группа подкью (4 → 4.1, 4.2...).</summary>
public sealed class GroupCue : Cue
{
    public List<Cue> Children { get; set; } = new();
}
