using System.Text.Json.Serialization;

namespace CueApp.Core.Model;

/// <summary>
/// Базовая кью: всё, что есть у любой кью независимо от типа.
/// Все времена — в секундах.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AudioCue), "audio")]
[JsonDerivedType(typeof(FadeCue), "fade")]
[JsonDerivedType(typeof(StopCue), "stop")]
[JsonDerivedType(typeof(WaitCue), "wait")]
[JsonDerivedType(typeof(GroupCue), "group")]
public abstract class Cue
{
    /// <summary>Внутренний идентификатор. Не меняется никогда — на него ссылаются Fade/Stop.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Номер, видимый пользователю («4», «4.1», «12a»). Закреплен за кью, может быть пустым.</summary>
    public string Number { get; set; } = "";

    public string Name { get; set; } = "";

    public string Notes { get; set; } = "";

    public List<string> Tags { get; set; } = new();

    public CueColor Color { get; set; } = CueColor.None;

    public double PreWait { get; set; }

    public double PostWait { get; set; }

    public ContinueMode Continue { get; set; } = ContinueMode.None;
}

/// <summary>Заливка строки кью в списке (раздел 3 ТЗ).</summary>
public enum CueColor
{
    None,
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple,
    Pink,
}

/// <summary>Что происходит со следующей кью после запуска этой.</summary>
public enum ContinueMode
{
    /// <summary>Ждем следующего GO.</summary>
    None,

    /// <summary>Следующая кью стартует сразу после post-wait.</summary>
    AutoContinue,

    /// <summary>Следующая кью стартует, когда эта закончилась (+ post-wait).</summary>
    AutoFollow,
}
