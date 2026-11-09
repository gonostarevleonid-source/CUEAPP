using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CueApp.App.Themes;
using CueApp.Core;
using CueApp.Core.Model;

namespace CueApp.App.ViewModels.Inspector;

/// <summary>Вариант для выпадающего списка: значение + подпись.</summary>
public sealed record Choice<T>(T Value, string Label);

/// <summary>Образец цвета кью в инспекторе.</summary>
public sealed partial class SwatchViewModel(CueColor color, string name, IBrush brush) : ObservableObject
{
    public CueColor Color { get; } = color;
    public string Name { get; } = name;
    public IBrush Brush { get; } = brush;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// Инспектор выбранной кью: заметки + вкладка «Основное».
/// Поля, которые есть у любой кью, — здесь; поля конкретного типа — в Details.
/// </summary>
public sealed partial class InspectorViewModel : EditorViewModelBase
{
    private readonly Cue _cue;
    private readonly Show _show;
    private readonly Action<string> _report;

    public InspectorViewModel(Cue cue, Show show, Action changed, Action<string> report, object? details)
        : base(changed)
    {
        _cue = cue;
        _show = show;
        _report = report;
        Details = details;
        Swatches = CreateSwatches();
        UpdateSwatches();
    }

    public Cue Cue => _cue;

    /// <summary>Поля конкретного типа кью (Трек, Stop); у группы — null.</summary>
    public object? Details { get; }

    public string NotesHeader => "Заметки · кью " + (_cue.Number == "" ? "без номера" : _cue.Number);

    /// <summary>Заголовок вкладки: «Трек · 2», «Stop · 5».</summary>
    public string MainHeader => TypeLabel + (_cue.Number == "" ? "" : " · " + _cue.Number);

    public string TypeLabel => CueRowViewModel.TypeLabelOf(_cue);

    /// <summary>Номер. Номера закреплены: занятый номер не принимаем.</summary>
    public string Number
    {
        get => _cue.Number;
        set
        {
            string number = (value ?? "").Trim();
            if (number == _cue.Number)
                return;

            if (number != "" && _show.AllCues().Any(c => c != _cue && c.Number == number))
            {
                _report($"Номер «{number}» уже занят другой кью");
            }
            else
            {
                _cue.Number = number;
                Changed();
                OnPropertyChanged(nameof(NotesHeader));
                OnPropertyChanged(nameof(MainHeader));
            }
            Refresh(nameof(Number));
        }
    }

    public string Name
    {
        get => _cue.Name;
        set
        {
            if (value == _cue.Name)
                return;
            _cue.Name = value ?? "";
            Changed();
        }
    }

    public IReadOnlyList<SwatchViewModel> Swatches { get; }

    [RelayCommand]
    private void SetColor(CueColor color)
    {
        if (color == _cue.Color)
            return;
        _cue.Color = color;
        UpdateSwatches();
        Changed();
    }

    /// <summary>Заметки (многострочный текст).</summary>
    public string Notes
    {
        get => _cue.Notes;
        set
        {
            if (value == _cue.Notes)
                return;
            _cue.Notes = value ?? "";
            Changed();
        }
    }

    private void UpdateSwatches()
    {
        foreach (var swatch in Swatches)
            swatch.IsSelected = swatch.Color == _cue.Color;
    }

    private static IReadOnlyList<SwatchViewModel> CreateSwatches() =>
    [
        new(CueColor.None, "Нет", Brushes.Transparent),
        new(CueColor.Red, "Красный", ThemeBrushes.Get("FillRedBrush")),
        new(CueColor.Orange, "Оранжевый", ThemeBrushes.Get("FillOrangeBrush")),
        new(CueColor.Yellow, "Желтый", ThemeBrushes.Get("FillYellowBrush")),
        new(CueColor.Green, "Зеленый", ThemeBrushes.Get("FillGreenBrush")),
        new(CueColor.Blue, "Синий", ThemeBrushes.Get("FillBlueBrush")),
        new(CueColor.Purple, "Фиолетовый", ThemeBrushes.Get("FillPurpleBrush")),
        new(CueColor.Pink, "Розовый", ThemeBrushes.Get("FillPinkBrush")),
    ];
}
