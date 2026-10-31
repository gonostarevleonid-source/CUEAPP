using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CueApp.Core.Audio;

namespace CueApp.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAudioEngine _audio;

    [ObservableProperty]
    public partial string Status { get; set; } = "";

    public MainViewModel(IAudioEngine audio)
    {
        _audio = audio;

        // Ошибка звука не должна ронять программу — показываем её в статусе
        try
        {
            _audio.Initialize();
            Status = "Аудио готово. Нажмите GO.";
        }
        catch (Exception ex)
        {
            Status = "Аудио недоступно: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Go()
    {
        if (!_audio.IsInitialized)
            return;

        try
        {
            _audio.PlayTestTone();
            Status = "Играет тестовый тон 1 кГц";
        }
        catch (Exception ex)
        {
            Status = "Ошибка воспроизведения: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (!_audio.IsInitialized)
            return;

        _audio.StopAll();
        Status = "Остановлено";
    }
}
