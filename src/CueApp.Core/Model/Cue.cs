using System.Text.Json.Serialization;

namespace CueApp.Core.Model;

/// <summary>
/// Базовая кью: всё, что есть у любой кью независимо от типа.
/// Все времена — в секундах.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TrackCue), "track")]
[JsonDerivedType(typeof(StopCue), "stop")]
[JsonDerivedType(typeof(GroupCue), "group")]
public abstract class Cue
{
    /// <summary>Внутренний идентификатор. Не меняется никогда — на него ссылается Stop-кью.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Номер, видимый пользователю («4», «4.1», «12a»). Закреплен за кью, может быть пустым.</summary>
    public string Number { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Заметки (многострочный текст).</summary>
    public string Notes { get; set; } = "";

    public CueColor Color { get; set; } = CueColor.None;
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
