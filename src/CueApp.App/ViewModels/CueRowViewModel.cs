using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CueApp.App.Themes;
using CueApp.Core;
using CueApp.Core.Audio;
using CueApp.Core.Model;

namespace CueApp.App.ViewModels;

/// <summary>Одна строка кью-листа: что и каким цветом показать для кью.</summary>
public partial class CueRowViewModel : ViewModelBase
{
    /// <summary>Смещение подкью вправо на каждый уровень вложенности, px (из макета).</summary>
    private const double IndentPerLevel = 32;

    private const string NoValue = "—";

    public Cue Cue { get; }

    /// <summary>0 — основная кью, 1 — подкью (4.1), 2 — 4.2.1.</summary>
    public int Depth { get; }

    public CueRowViewModel(Cue cue, int depth)
    {
        Cue = cue;
        Depth = depth;
    }

    /// <summary>Выбранная кью (уйдет по GO): белая обводка + зеленая полоска.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Играет сейчас: синий фон + синяя полоска.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    public partial bool IsPlaying { get; set; }

    /// <summary>Длина аудиофайла, сек (null — еще не известна или это не трек).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    public partial double? FileDuration { get; set; }

    /// <summary>Файл не найден или не читается.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    public partial bool IsMissingFile { get; set; }

    /// <summary>Живой отсчет во время игры («−0:21.0»); null — не играет.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Duration))]
    public partial string? LiveTime { get; set; }

    /// <summary>Над списком тащат файлы, и они встанут сразу после этой строки (синяя линия снизу).</summary>
    [ObservableProperty]
    public partial bool IsDropTarget { get; set; }

    public bool IsSubCue => Depth > 0;

    /// <summary>Кью поменяли в инспекторе — перечитать все колонки строки.</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);

    public Thickness RowMargin => new(Depth * IndentPerLevel, 0, 0, 0);

    public string Number => Cue.Number;

    public FontWeight NumberWeight => IsSubCue ? FontWeight.Normal : FontWeight.Bold;

    public string Name => Cue.Name;

    public string TypeLabel => TypeLabelOf(Cue);

    public static string TypeLabelOf(Cue cue) => cue switch
    {
        TrackCue => "Трек",
        StopCue => "Stop",
        GroupCue => "Group",
        _ => "?",
    };

    public IBrush TypeBrush => ThemeBrushes.Get(Cue switch
    {
        TrackCue => "CueTrackBrush",
        StopCue => "CueStopBrush",
        _ => "CueGroupBrush",
    });

    /// <summary>Колонка «Длительность»: во время игры — отсчет «осталось», иначе длина с учетом trim.</summary>
    public string Duration => LiveTime ?? (IsMissingFile ? "нет файла" : StaticDuration);

    private string StaticDuration => Cue switch
    {
        TrackCue track when FileDuration is { } file => TimeFormat.MinSecTenths(AudioTiming.Length(track, file)),
        StopCue stop => TimeFormat.MinSecTenths(stop.FadeOut),
        _ => NoValue,
    };

    /// <summary>
    /// Заливка строки: играющая — синий фон; иначе цвет кью; иначе чуть более светлый фон подкью; иначе прозрачно.
    /// </summary>
    public IBrush RowBackground => IsPlaying ? ThemeBrushes.Get("PlayingBackgroundBrush") : Cue.Color switch
    {
        CueColor.Red => ThemeBrushes.Get("FillRedBrush"),
        CueColor.Orange => ThemeBrushes.Get("FillOrangeBrush"),
        CueColor.Yellow => ThemeBrushes.Get("FillYellowBrush"),
        CueColor.Green => ThemeBrushes.Get("FillGreenBrush"),
        CueColor.Blue => ThemeBrushes.Get("FillBlueBrush"),
        CueColor.Purple => ThemeBrushes.Get("FillPurpleBrush"),
        CueColor.Pink => ThemeBrushes.Get("FillPinkBrush"),
        _ => IsSubCue ? ThemeBrushes.Get("SubCueBackgroundBrush") : Brushes.Transparent,
    };
}
