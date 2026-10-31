using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CueApp.App.Themes;
using CueApp.Core;
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

    public bool IsSubCue => Depth > 0;

    public Thickness RowMargin => new(Depth * IndentPerLevel, 0, 0, 0);

    public string Number => Cue.Number;

    public FontWeight NumberWeight => IsSubCue ? FontWeight.Normal : FontWeight.Bold;

    public string Name => Cue.Name;

    public string TypeLabel => Cue switch
    {
        AudioCue => "Audio",
        FadeCue => "Fade",
        StopCue => "Stop",
        WaitCue => "Wait",
        GroupCue => "Group",
        _ => "?",
    };

    public IBrush TypeBrush => ThemeBrushes.Get(Cue switch
    {
        AudioCue => "CueAudioBrush",
        FadeCue => "CueFadeBrush",
        StopCue => "CueStopBrush",
        _ => "CueGroupBrush", // Wait и Group — серые
    });

    public string PreWait => TimeFormat.MinSecTenths(Cue.PreWait);

    public string PostWait => TimeFormat.MinSecTenths(Cue.PostWait);

    /// <summary>Длительность. У Audio она станет известна, когда подключим чтение файла.</summary>
    public string Duration => Cue switch
    {
        AudioCue { LoopForever: true } => "∞",
        FadeCue fade => TimeFormat.MinSecTenths(fade.Duration),
        StopCue stop => TimeFormat.MinSecTenths(stop.FadeOut),
        WaitCue wait => TimeFormat.MinSecTenths(wait.Duration),
        _ => NoValue,
    };

    /// <summary>Значок ↓ — auto-continue (сразу).</summary>
    public bool IsAutoContinue => Cue.Continue == ContinueMode.AutoContinue;

    /// <summary>Значок ↓ с чертой — auto-follow (после окончания кью).</summary>
    public bool IsAutoFollow => Cue.Continue == ContinueMode.AutoFollow;

    /// <summary>Заливка строки: цвет кью, иначе чуть более светлый фон подкью, иначе прозрачно.</summary>
    public IBrush RowBackground => Cue.Color switch
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
