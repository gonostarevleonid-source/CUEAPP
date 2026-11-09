using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CueApp.Core;
using CueApp.Core.Model;

namespace CueApp.App.ViewModels.Inspector;

/// <summary>Поля трека: файл, trim, фейды, громкость.</summary>
public sealed partial class TrackDetailsViewModel : EditorViewModelBase
{
    /// <summary>Громче +12 дБ не даем: опечатка «60» вместо «-60» не должна оглушить зал.</summary>
    private const double MaxVolumeDb = 12;

    private readonly TrackCue _cue;
    private readonly Func<Task<string?>> _pickFile;
    private readonly Action _fileChanged;

    /// <param name="pickFile">Выбрать файл; возвращает путь для записи в шоу (уже относительный) или null.</param>
    /// <param name="fileChanged">Файл сменили — перечитать длительность.</param>
    public TrackDetailsViewModel(TrackCue cue, Action changed, Func<Task<string?>> pickFile, Action fileChanged)
        : base(changed)
    {
        _cue = cue;
        _pickFile = pickFile;
        _fileChanged = fileChanged;
    }

    public string FileLabel => _cue.FilePath == "" ? "файл не выбран" : _cue.FilePath;

    [RelayCommand]
    private async Task PickFile()
    {
        string? path = await _pickFile();
        if (path is null || path == _cue.FilePath)
            return;

        _cue.FilePath = path;
        OnPropertyChanged(nameof(FileLabel));
        Changed();
        _fileChanged();
    }

    public string TrimIn
    {
        get => TimeFormat.Editable(_cue.TrimIn);
        set => EditTime(value, _cue.TrimIn, v => _cue.TrimIn = v);
    }

    /// <summary>Пусто — до конца файла.</summary>
    public string TrimOut
    {
        get => _cue.TrimOut is { } t ? TimeFormat.Editable(t) : "";
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (_cue.TrimOut is null)
                    return;
                _cue.TrimOut = null;
                Changed();
                Refresh(nameof(TrimOut));
                return;
            }
            EditTime(value, _cue.TrimOut ?? -1, v => _cue.TrimOut = v);
        }
    }

    public string FadeIn
    {
        get => TimeFormat.Editable(_cue.FadeIn);
        set => EditTime(value, _cue.FadeIn, v => _cue.FadeIn = v);
    }

    public string FadeOut
    {
        get => TimeFormat.Editable(_cue.FadeOut);
        set => EditTime(value, _cue.FadeOut, v => _cue.FadeOut = v);
    }

    public string VolumeDb
    {
        get => FormatNumber(_cue.VolumeDb);
        set => EditNumber(value, _cue.VolumeDb, v => _cue.VolumeDb = Math.Min(v, MaxVolumeDb));
    }
}

/// <summary>Поля Stop-кью.</summary>
public sealed partial class StopDetailsViewModel : EditorViewModelBase
{
    private readonly StopCue _cue;

    public StopDetailsViewModel(StopCue cue, Show show, Action changed) : base(changed)
    {
        _cue = cue;
        Targets = [new(null, "Все кью"), .. show.AllCues().Where(c => c != cue).Select(c => new Choice<Cue?>(c, Describe(c)))];
    }

    public IReadOnlyList<Choice<Cue?>> Targets { get; }

    /// <summary>null — остановить все.</summary>
    public Choice<Cue?> Target
    {
        get => Targets.FirstOrDefault(t => t.Value?.Id == _cue.TargetCueId) ?? Targets[0];
        set
        {
            Guid? id = value?.Value?.Id;
            if (id == _cue.TargetCueId)
                return;
            _cue.TargetCueId = id;
            Changed();
        }
    }

    public string FadeOut
    {
        get => TimeFormat.Editable(_cue.FadeOut);
        set => EditTime(value, _cue.FadeOut, v => _cue.FadeOut = v);
    }

    private static string Describe(Cue cue) => (cue.Number == "" ? "" : cue.Number + " · ") + cue.Name;
}
