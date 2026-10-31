namespace CueApp.Core.Model;

/// <summary>Шоу целиком — то, что лежит в файле .cue.</summary>
public sealed class Show
{
    /// <summary>Версия формата файла — пригодится, когда формат будет меняться.</summary>
    public int FormatVersion { get; set; } = 1;

    public string Name { get; set; } = "";

    public ShowSettings Settings { get; set; } = new();

    /// <summary>Основные кью сверху вниз. Подкью лежат внутри GroupCue.</summary>
    public List<Cue> Cues { get; set; } = new();

    /// <summary>Все кью по порядку сверху вниз, включая подкью (как они видны в списке).</summary>
    public IEnumerable<Cue> AllCues() => Flatten(Cues);

    public Cue? FindCue(Guid id) => AllCues().FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Вставляет новую кью сразу после anchor, на том же уровне (подкью остается в своей группе).
    /// anchor = null — в конец списка. Номер новой кью подбирается автоматически (см. SuggestNumber).
    /// </summary>
    public void InsertAfter(Cue? anchor, Cue newCue)
    {
        List<Cue> list = anchor is null ? Cues : FindParentList(anchor) ?? Cues;
        int index = anchor is null ? list.Count : list.IndexOf(anchor) + 1;
        Cue? previous = index > 0 ? list[index - 1] : null;

        newCue.Number = SuggestNumber(previous, ParentPrefix(list));
        list.Insert(index, newCue);
    }

    /// <summary>Удаляет кью (группу — вместе с подкью). false — такой кью в шоу нет.</summary>
    public bool Remove(Cue cue) => FindParentList(cue)?.Remove(cue) ?? false;

    /// <summary>Список, в котором лежит кью: Cues или Children какой-то группы.</summary>
    public List<Cue>? FindParentList(Cue cue)
    {
        if (Cues.Contains(cue))
            return Cues;
        return AllCues().OfType<GroupCue>().FirstOrDefault(g => g.Children.Contains(cue))?.Children;
    }

    /// <summary>
    /// Номер для новой кью: «предыдущий + 1» (3 → 4, 4.1 → 4.2).
    /// Если номер уже занят или предыдущий не число («12a») — пустой номер:
    /// номера закреплены, чужой номер не трогаем.
    /// </summary>
    private string SuggestNumber(Cue? previous, string prefix)
    {
        string candidate;
        if (previous is null)
        {
            candidate = prefix + "1";
        }
        else
        {
            string number = previous.Number;
            int dot = number.LastIndexOf('.');
            string head = number[..(dot + 1)];
            if (!int.TryParse(number[(dot + 1)..], out int last))
                return "";
            candidate = head + (last + 1);
        }

        return AllCues().Any(c => c.Number == candidate) ? "" : candidate;
    }

    /// <summary>Префикс номеров внутри группы («4.»), для основного списка — пусто.</summary>
    private string ParentPrefix(List<Cue> list)
    {
        if (list == Cues)
            return "";
        var group = AllCues().OfType<GroupCue>().First(g => g.Children == list);
        return group.Number == "" ? "" : group.Number + ".";
    }

    /// <summary>
    /// Команда «Перенумеровать»: основные кью 1, 2, 3...; подкью — 4.1, 4.2 (и глубже 4.2.1).
    /// </summary>
    public void Renumber() => Renumber(Cues, prefix: "");

    private static void Renumber(List<Cue> cues, string prefix)
    {
        for (int i = 0; i < cues.Count; i++)
        {
            cues[i].Number = prefix + (i + 1);
            if (cues[i] is GroupCue group)
                Renumber(group.Children, cues[i].Number + ".");
        }
    }

    private static IEnumerable<Cue> Flatten(IEnumerable<Cue> cues)
    {
        foreach (var cue in cues)
        {
            yield return cue;
            if (cue is GroupCue group)
                foreach (var child in Flatten(group.Children))
                    yield return child;
        }
    }
}
