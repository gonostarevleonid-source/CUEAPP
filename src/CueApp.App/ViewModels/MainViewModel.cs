using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CueApp.Core.Audio;
using CueApp.Core.Model;

namespace CueApp.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAudioEngine _audio;

    public Show Show { get; }

    /// <summary>Строки кью-листа сверху вниз (подкью идут сразу за своей группой).</summary>
    public ObservableCollection<CueRowViewModel> Rows { get; } = new();

    /// <summary>Выбранная кью — та, что уйдет по GO.</summary>
    [ObservableProperty]
    public partial CueRowViewModel? SelectedRow { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "";

    public string ShowTitle => Show.Name;

    public string CueCountText => Rows.Count + " кью";

    partial void OnSelectedRowChanged(CueRowViewModel? oldValue, CueRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;
        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public MainViewModel(IAudioEngine audio, Show show)
    {
        _audio = audio;
        Show = show;
        RebuildRows();
        SelectedRow = Rows.FirstOrDefault();

        // Ошибка звука не должна ронять программу — показываем её в статусе
        try
        {
            _audio.Initialize();
            Status = "Аудио готово.";
        }
        catch (Exception ex)
        {
            Status = "Аудио недоступно: " + ex.Message;
        }
    }

    /// <summary>Добавить кью нужного типа сразу после выбранной.</summary>
    [RelayCommand]
    private void AddCue(string type)
    {
        Cue cue = type switch
        {
            "Audio" => new AudioCue(),
            "Fade" => new FadeCue(),
            "Stop" => new StopCue(),
            "Wait" => new WaitCue(),
            "Group" => new GroupCue(),
            _ => throw new ArgumentException("Неизвестный тип кью: " + type),
        };
        cue.Name = "Новая " + type;

        Show.InsertAfter(SelectedRow?.Cue, cue);
        RebuildRows();
        SelectedRow = Rows.First(r => r.Cue == cue);
    }

    /// <summary>Удалить выбранную кью (группу — вместе с подкью) и выбрать следующую строку.</summary>
    [RelayCommand]
    private void DeleteCue()
    {
        if (SelectedRow is null)
            return;

        int index = Rows.IndexOf(SelectedRow);
        Show.Remove(SelectedRow.Cue);
        RebuildRows();
        SelectedRow = Rows.Count == 0 ? null : Rows[Math.Min(index, Rows.Count - 1)];
    }

    [RelayCommand]
    private void Go()
    {
        if (!_audio.IsInitialized)
            return;

        // Пока GO только проверяет звук; настоящий запуск кью — следующим шагом
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

    [RelayCommand]
    private void Panic()
    {
        // Фейд Panic (Settings.PanicFadeSeconds) появится вместе с фейдами в аудиодвижке
        Stop();
        Status = "PANIC — всё остановлено";
    }

    /// <summary>Пересобирает плоский список строк из дерева кью шоу.</summary>
    private void RebuildRows()
    {
        Rows.Clear();
        AddRows(Show.Cues, depth: 0);
        OnPropertyChanged(nameof(CueCountText));
    }

    private void AddRows(IEnumerable<Cue> cues, int depth)
    {
        foreach (var cue in cues)
        {
            Rows.Add(new CueRowViewModel(cue, depth));
            if (cue is GroupCue group)
                AddRows(group.Children, depth + 1);
        }
    }
}
