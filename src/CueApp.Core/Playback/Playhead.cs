using CueApp.Core.Model;

namespace CueApp.Core.Playback;

/// <summary>
/// Плейхед: какая кью выбрана («уйдет по GO») и куда переходит выбор после GO.
/// Сам ничего не проигрывает — только решает, ЧТО запустить.
/// </summary>
public sealed class Playhead
{
    private readonly Show _show;

    public Playhead(Show show)
    {
        _show = show;
    }

    /// <summary>Выбранная кью; null — ничего не выбрано, GO ничего не делает.</summary>
    public Cue? Current { get; set; }

    /// <summary>
    /// Нажали GO: возвращает кью, которую надо запустить (null — запускать нечего),
    /// и переводит выбор на следующую кью.
    /// </summary>
    public Cue? Go()
    {
        Cue? cue = Current;
        if (cue is null)
            return null;

        // Группа как папка: GO только «входит» в нее
        if (cue is GroupCue group && _show.Settings.GroupGo == GroupGoMode.EnterGroup)
        {
            Current = group.Children.FirstOrDefault() ?? NextOutside(group);
            return null;
        }

        Current = NextOutside(EndOfContinueChain(cue));
        return cue;
    }

    /// <summary>
    /// Если у кью стоит auto-continue / auto-follow, следующая за ней запустится сама —
    /// плейхед перепрыгивает всю такую цепочку (в пределах своего уровня).
    /// </summary>
    private Cue EndOfContinueChain(Cue cue)
    {
        while (cue.Continue != ContinueMode.None && NextSibling(cue) is { } next)
            cue = next;
        return cue;
    }

    /// <summary>Следующая строка списка после кью, не заходя внутрь нее самой (подкью группы пропускаются).</summary>
    private Cue? NextOutside(Cue cue)
    {
        if (NextSibling(cue) is { } sibling)
            return sibling;

        // Последняя в группе — выходим на уровень выше
        GroupCue? parent = ParentGroup(cue);
        return parent is null ? null : NextOutside(parent);
    }

    private Cue? NextSibling(Cue cue)
    {
        List<Cue>? list = _show.FindParentList(cue);
        if (list is null)
            return null;
        int index = list.IndexOf(cue);
        return index + 1 < list.Count ? list[index + 1] : null;
    }

    private GroupCue? ParentGroup(Cue cue) =>
        _show.AllCues().OfType<GroupCue>().FirstOrDefault(g => g.Children.Contains(cue));
}
