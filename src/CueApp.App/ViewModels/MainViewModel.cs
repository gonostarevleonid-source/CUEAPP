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
using CueApp.App.ViewModels.Inspector;
using CueApp.Core.Audio;
using CueApp.Core.Model;
using CueApp.Core.Playback;

namespace CueApp.App.ViewModels;

/// <summary>
/// Главное окно: шоу, кью-лист, файл. Всё про проигрывание — в MainViewModel.Playback.cs.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private const string UntitledName = "Без названия";

    private readonly IAudioEngine _audio;
    private readonly IDialogService _dialogs;
    private readonly GoGuard _goGuard = new();
    private Playhead _playhead;

    // Монотонные часы для защиты от двойного GO (не зависят от перевода системного времени)
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    // Строка списка для каждой кью — чтобы быстро обновлять отсчет играющих
    private readonly Dictionary<Cue, CueRowViewModel> _rowByCue = new();

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

    /// <summary>Заметки и инспектор выбранной кью; null — ничего не выбрано.</summary>
    [ObservableProperty]
    public partial InspectorViewModel? Inspector { get; private set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "";

    /// <summary>Название шоу = имя файла без «.cue».</summary>
    public string ShowTitle => FilePath is null ? UntitledName : Path.GetFileNameWithoutExtension(FilePath);

    public string FileLabel => FilePath is null ? "не сохранено" : Path.GetFileName(FilePath);

    /// <summary>Заголовок окна; «•» — есть несохраненные изменения.</summary>
    public string WindowTitle => "CUEAPP — " + ShowTitle + (IsDirty ? " •" : "");

    public string CueCountText => Rows.Count + " кью";

    /// <summary>Папка шоу — от нее считаются относительные пути к звукам; null — шоу не сохранено.</summary>
    private string? ShowDirectory => FilePath is null ? null : Path.GetDirectoryName(FilePath);

    partial void OnSelectedRowChanged(CueRowViewModel? oldValue, CueRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;
        if (newValue is not null)
            newValue.IsSelected = true;

        // Клик по кью делает ее выбранной — следующий GO запустит её
        _playhead.Current = newValue?.Cue;

        // Инспектор и заметки показывают выбранную кью
        if (Inspector?.Cue != newValue?.Cue)
            Inspector = newValue is null ? null : CreateInspector(newValue.Cue);
    }

    private InspectorViewModel CreateInspector(Cue cue)
    {
        // После каждой правки: шоу изменено, строка списка перечитывает колонки
        void Changed()
        {
            IsDirty = true;
            if (_rowByCue.TryGetValue(cue, out var row))
                row.Refresh();
        }

        object? details = cue switch
        {
            TrackCue track => new TrackDetailsViewModel(track, Changed, PickAudioFileForCueAsync, () => ReloadDuration(track)),
            StopCue stop => new StopDetailsViewModel(stop, Show, Changed),
            _ => null,
        };
        return new InspectorViewModel(cue, Show, Changed, message => Status = message, details);
    }

    /// <summary>«Выбрать…» в инспекторе трека: один файл, путь сразу в виде для шоу.</summary>
    private async Task<string?> PickAudioFileForCueAsync()
    {
        var files = await _dialogs.PickAudioFilesAsync();
        return files.Count == 0 ? null : MediaPaths.ToStored(files[0], ShowDirectory);
    }

    public MainViewModel(IAudioEngine audio, IDialogService dialogs)
    {
        _audio = audio;
        _dialogs = dialogs;
        Show = new Show();
        _playhead = new Playhead(Show);
        _liveTimer = CreateLiveTimer();

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

    // ---------- Кью-лист ----------

    /// <summary>
    /// Добавить кью нужного типа сразу после выбранной.
    /// Трек — сначала выбрать файлы (можно несколько: будет несколько треков подряд); отмена — ничего не добавляем.
    /// </summary>
    [RelayCommand]
    private async Task AddCue(string type)
    {
        List<Cue> added;
        if (type == "Track")
        {
            var files = await _dialogs.PickAudioFilesAsync();
            added = files.Select(file => (Cue)new TrackCue
            {
                FilePath = MediaPaths.ToStored(file, ShowDirectory),
                Name = Path.GetFileNameWithoutExtension(file),
            }).ToList();
        }
        else
        {
            Cue cue = type switch
            {
                "Stop" => new StopCue { Name = "Стоп всё" },
                "Group" => new GroupCue { Name = "Новая группа" },
                _ => throw new ArgumentException("Неизвестный тип кью: " + type),
            };
            added = [cue];
        }

        if (added.Count == 0)
            return;

        Cue? anchor = SelectedRow?.Cue;
        foreach (var cue in added)
        {
            Show.InsertAfter(anchor, cue);
            anchor = cue;
        }

        IsDirty = true;
        RebuildRows();
        SelectedRow = _rowByCue[added[^1]];
        LoadDurations(added.OfType<TrackCue>());
    }

    /// <summary>Удалить выбранную кью (группу — вместе с подкью) и выбрать следующую строку.</summary>
    [RelayCommand]
    private void DeleteCue()
    {
        if (SelectedRow is null)
            return;

        int index = Rows.IndexOf(SelectedRow);
        Show.Remove(SelectedRow.Cue);
        StopRemovedCues();
        IsDirty = true;
        RebuildRows();
        SelectedRow = Rows.Count == 0 ? null : Rows[Math.Min(index, Rows.Count - 1)];
    }

    private static string Describe(Cue cue) =>
        cue.Number == "" ? cue.Name : cue.Number + " · " + cue.Name;

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
        // Пути к звукам пересчитываем относительно новой папки шоу
        string? oldDirectory = ShowDirectory;
        string newDirectory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        MediaPaths.Relocate(Show, oldDirectory, newDirectory);

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
            // Нет прав, диск полон, флешку вынули — шоу остается в памяти, можно сохранить в другое место.
            // Пути возвращаем как были, чтобы они по-прежнему находили файлы
            MediaPaths.Relocate(Show, newDirectory, oldDirectory);
            Status = "Не удалось сохранить: " + ex.Message;
            return false;
        }
    }

    private void LoadShow(Show show, string? path)
    {
        StopEverythingForShowChange();
        Show = show;
        _playhead = new Playhead(show);
        FilePath = path;
        IsDirty = false;
        RebuildRows();
        SelectedRow = Rows.FirstOrDefault();
        LoadDurations(show.AllCues().OfType<TrackCue>());
    }

    /// <summary>Пересобирает плоский список строк из дерева кью шоу.</summary>
    private void RebuildRows()
    {
        Rows.Clear();
        _rowByCue.Clear();
        AddRows(Show.Cues, depth: 0);
        OnPropertyChanged(nameof(CueCountText));
    }

    private void AddRows(IEnumerable<Cue> cues, int depth)
    {
        foreach (var cue in cues)
        {
            var row = new CueRowViewModel(cue, depth);
            ApplyPlaybackState(row); // длительность файла, «играет», «нет файла» переживают пересборку
            Rows.Add(row);
            _rowByCue[cue] = row;
            if (cue is GroupCue group)
                AddRows(group.Children, depth + 1);
        }
    }
}
