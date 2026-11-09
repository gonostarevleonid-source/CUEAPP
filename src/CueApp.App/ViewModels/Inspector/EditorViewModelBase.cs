using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using CueApp.Core;

namespace CueApp.App.ViewModels.Inspector;

/// <summary>
/// Общее для всех частей инспектора: правка полей кью из текста.
/// Неверный ввод не применяется — поле просто показывает прежнее значение.
/// </summary>
public abstract class EditorViewModelBase : ViewModelBase
{
    private readonly Action _changed;

    /// <param name="changed">Вызывается после каждой правки: пометить шоу измененным, обновить строку списка.</param>
    protected EditorViewModelBase(Action changed)
    {
        _changed = changed;
    }

    protected void Changed() => _changed();

    /// <summary>Применить введенное время. Пусто или мусор — откат к прежнему значению.</summary>
    protected void EditTime(string? text, double current, Action<double> apply, [CallerMemberName] string? property = null)
    {
        // Текст не менялся (просто ушли из поля) — не трогаем модель, иначе округление «испортило» бы значение
        if (text == TimeFormat.Editable(current))
            return;

        if (TimeFormat.TryParse(text, out double value))
        {
            apply(value);
            Changed();
        }
        Refresh(property);
    }

    /// <summary>Применить введенное число (дБ и т.п.).</summary>
    protected void EditNumber(string? text, double current, Action<double> apply, [CallerMemberName] string? property = null)
    {
        if (text == FormatNumber(current))
            return;

        if (double.TryParse(text?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            && double.IsFinite(value))
        {
            apply(value);
            Changed();
        }
        Refresh(property);
    }

    protected static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Перечитать поле из модели (после отката или нормализации ввода).
    /// Откладываем на следующий такт: во время записи в модель поле еще занято привязкой.
    /// </summary>
    protected void Refresh(string? property) =>
        Dispatcher.UIThread.Post(() => OnPropertyChanged(property));
}
