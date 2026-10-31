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
