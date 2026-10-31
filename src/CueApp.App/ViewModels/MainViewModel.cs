using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CueApp.Core.Audio;
using CueApp.Core.Model;
using CueApp.Core.Playback;

namespace CueApp.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAudioEngine _audio;
    private readonly Playhead _playhead;
    private readonly GoGuard _goGuard = new();

    // Монотонные часы для защиты от двойного GO (не зависят от перевода системного времени)
    private readonly Stopwatch _clock = Stopwatch.StartNew();

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

        // Клик по кью делает ее выбранной — следующий GO запустит её
        _playhead.Current = newValue?.Cue;
    }

    public MainViewModel(IAudioEngine audio, Show show)
    {
        _audio = audio;
        Show = show;
        _playhead = new Playhead(show);
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
        if (!_goGuard.TryAccept(_clock.Elapsed, Show.Settings.DoubleGoProtectionMs))
        {
            Status = $"Повторный GO проигнорирован (защита {Show.Settings.DoubleGoProtectionMs} мс)";
            return;
        }

        Cue? selected = _playhead.Current;
        Cue? started = _playhead.Go();

        // Выбор в списке переезжает туда, куда ушел плейхед (или пусто в конце списка)
        SelectedRow = Rows.FirstOrDefault(r => r.Cue == _playhead.Current);

        if (started is null)
        {
            Status = selected is GroupCue ? "Вход в группу " + Describe(selected) : "Ничего не выбрано — GO пропущен";
            return;
        }

        Status = "GO → " + Describe(started);
        PlayTestSound(); // пока вместо настоящего запуска кью
    }

    private static string Describe(Cue cue) =>
        cue.Number == "" ? cue.Name : cue.Number + " · " + cue.Name;

    private void PlayTestSound()
    {
        if (!_audio.IsInitialized)
            return;

        try
        {
            _audio.PlayTestTone();
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
