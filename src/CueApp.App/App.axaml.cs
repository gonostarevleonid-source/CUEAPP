using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CueApp.App.ViewModels;
using CueApp.App.Views;
using CueApp.Audio;

namespace CueApp.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var audio = new BassAudioEngine();
            desktop.Exit += (_, _) => audio.Dispose();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(audio, DemoShow.Create()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
