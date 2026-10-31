using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CueApp.App.Services;
using CueApp.Core.Audio;
using CueApp.Core.Model;
using CueApp.Core.Playback;

namespace CueApp.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const string UntitledName = "Без названия";

    private readonly IAudioEngine _audio;
    private readonly IDialogService _dialogs;
    private readonly GoGuard _goGuard = new();
    private Playhead _playhead;

    // Монотонные часы для защиты от двойного GO (не зависят от перевода системного времени)
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public Show Show { get; private set; }

    /// <summary>Файл, в котором лежит шоу; null — новое шоу, еще ни разу не сохранено.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTitle), nameof(FileLabel), nameof(WindowTitle))]
    public partial string? FilePath { get; private set; }

    /// <summary>Есть несохраненные изменения.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    public partial bool IsDirty { get; private set; }

    /// <summary>Строки кью-листа сверху вниз (подкью идут сразу за своей группой).</summary>
    public ObservableCollection<CueRowViewModel> Rows { get; } = new();

    /// <summary>Выбранная кью — та, что уйдет по GO.</summary>
    [ObservableProperty]
    public partial CueRowViewModel? SelectedRow { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "";

    /// <summary>Название шоу = имя файла без «.cue».</summary>
    public string ShowTitle => FilePath is null ? UntitledName : Path.GetFileNameWithoutExtension(FilePath);

    public string FileLabel => FilePath is null ? "не сохранено" : Path.GetFileName(FilePath);

    /// <summary>Заголовок окна; «•» — есть несохраненные изменения.</summary>
    public string WindowTitle => "CUEAPP — " + ShowTitle + (IsDirty ? " •" : "");

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

    public MainViewModel(IAudioEngine audio, IDialogService dialogs)
    {
        _audio = audio;
        _dialogs = dialogs;
        Show = new Show();
        _playhead = new Playhead(Show);

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
        IsDirty = true;
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
        IsDirty = true;
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

    // ---------- Файл ----------

    [RelayCommand]
    private async Task NewShow()
    {
        if (!await ConfirmDiscardChangesAsync())
            return;

        LoadShow(new Show(), path: null);
        Status = "Новое шоу";
    }

    [RelayCommand]
    private async Task OpenShow()
    {
        if (!await ConfirmDiscardChangesAsync())
            return;

        string? path = await _dialogs.PickShowToOpenAsync();
        if (path is null)
            return;

        // Битый файл не роняет программу: текущее шоу остается открытым
        try
        {
            LoadShow(ShowFile.Load(path), path);
            Status = "Открыто: " + path;
        }
        catch (Exception ex)
        {
            Status = "Не удалось открыть " + Path.GetFileName(path) + ": " + ex.Message;
        }
    }

    [RelayCommand]
    private Task SaveShow() => SaveAsync();

    [RelayCommand]
    private Task SaveShowAs() => SaveAsAsync();

    /// <summary>
    /// Перед «Новое», «Открыть» и закрытием окна: если есть несохраненные изменения — спросить.
    /// true — можно продолжать (сохранили или решили не сохранять), false — отмена.
    /// </summary>
    public async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!IsDirty)
            return true;

        return await _dialogs.AskSaveChangesAsync(ShowTitle) switch
        {
            SaveChangesAnswer.Save => await SaveAsync(),
            SaveChangesAnswer.DontSave => true,
            _ => false,
        };
    }

    /// <summary>Сохранить в текущий файл (новое шоу — спросить куда). true — сохранено.</summary>
    private Task<bool> SaveAsync() =>
        FilePath is null ? SaveAsAsync() : Task.FromResult(SaveTo(FilePath));

    private async Task<bool> SaveAsAsync()
    {
        string? path = await _dialogs.PickShowToSaveAsync(ShowTitle + ShowFile.Extension);
        return path is not null && SaveTo(path);
    }

    private bool SaveTo(string path)
    {
        try
        {
            ShowFile.Save(Show, path);
            FilePath = path;
            IsDirty = false;
            Status = "Сохранено: " + path;
            return true;
        }
        catch (Exception ex)
        {
            // Нет прав, диск полон, флешку вынули — шоу остается в памяти, можно сохранить в другое место
            Status = "Не удалось сохранить: " + ex.Message;
            return false;
        }
    }

    private void LoadShow(Show show, string? path)
    {
        Show = show;
        _playhead = new Playhead(show);
        FilePath = path;
        IsDirty = false;
        RebuildRows();
        SelectedRow = Rows.FirstOrDefault();
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
