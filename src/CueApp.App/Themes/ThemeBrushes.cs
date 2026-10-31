using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace CueApp.App.Themes;

/// <summary>Достает кисти из Colors.axaml по имени — для ViewModel, которым цвет зависит от данных.</summary>
public static class ThemeBrushes
{
    public static IBrush Get(string key) =>
        Application.Current!.TryGetResource(key, ThemeVariant.Dark, out var value) && value is IBrush brush
            ? brush
            : Brushes.Magenta; // ярко-розовый сразу покажет опечатку в имени ресурса
}
