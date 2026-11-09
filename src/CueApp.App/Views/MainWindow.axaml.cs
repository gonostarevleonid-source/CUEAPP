using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using CueApp.App.ViewModels;

namespace CueApp.App.Views;

public partial class MainWindow : Window
{
    // Пользователь уже ответил на «Сохранить изменения?» — второй раз не спрашиваем
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        // На Mac Cmd+S и т.п. ловит системное меню; на Windows меню — обычная полоска,
        // поэтому горячие клавиши вешаем на окно (иначе сработало бы дважды на Mac)
        if (!OperatingSystem.IsMacOS())
        {
            AddHotkey(Hotkeys.NewShow, vm => vm.NewShowCommand);
            AddHotkey(Hotkeys.OpenShow, vm => vm.OpenShowCommand);
            AddHotkey(Hotkeys.SaveShow, vm => vm.SaveShowCommand);
            AddHotkey(Hotkeys.SaveShowAs, vm => vm.SaveShowAsCommand);
        }

        // Tunnel — видим клавишу раньше, чем список или поле ввода успеют ее обработать
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void AddHotkey(KeyGesture gesture, Func<MainViewModel, System.Windows.Input.ICommand> command)
    {
        var binding = new KeyBinding { Gesture = gesture };
        // Команда берется из ViewModel, когда она появится в DataContext
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                binding.Command = command(vm);
        };
        KeyBindings.Add(binding);
    }

    /// <summary>
    /// Пробел = GO, Delete (на Mac Cmd+delete) = удалить кью — но НЕ когда печатаем в поле инспектора:
    /// иначе название «Финальная песня» запускало бы кью на каждом пробеле.
    /// </summary>
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        var commandKey = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

        if (FocusManager?.GetFocusedElement() is TextBox textBox)
        {
            // Enter в однострочном поле — применить значение и вернуться к списку (пробел снова GO)
            if (e.Key == Key.Enter && !textBox.AcceptsReturn)
            {
                CommitAndLeave(textBox);
                e.Handled = true;
            }
            // Ctrl/Cmd+S и т.п. — сначала применить набранное, иначе сохранится старое значение
            else if (e.KeyModifiers.HasFlag(commandKey))
            {
                CommitAndLeave(textBox);
            }
            return;
        }

        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.None)
        {
            vm.GoCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete || (e.Key == Key.Back && e.KeyModifiers == KeyModifiers.Meta))
        {
            vm.DeleteCueCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Применить набранное в поле и увести фокус в кью-лист (пробел снова = GO).
    /// Применяем явно: при программном переводе фокуса Avalonia сама значение не отправляет.
    /// </summary>
    private void CommitAndLeave(TextBox textBox)
    {
        BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty)?.UpdateSource();
        FocusCueList();
    }

    /// <summary>В Avalonia фокус принимают строки списка, поэтому фокусируем выбранную строку.</summary>
    private void FocusCueList()
    {
        var selected = CueList.SelectedIndex >= 0 ? CueList.ContainerFromIndex(CueList.SelectedIndex) : null;
        if (selected?.Focus() != true)
            CueList.Focus();
    }

    /// <summary>Закрытие окна при несохраненных изменениях — сначала спросить.</summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        // Набранное в поле, но еще не примененное — применить до вопроса о сохранении
        if (FocusManager?.GetFocusedElement() is TextBox textBox)
            CommitAndLeave(textBox);

        if (_closeConfirmed || DataContext is not MainViewModel { IsDirty: true } vm)
            return;

        e.Cancel = true;
        if (await vm.ConfirmDiscardChangesAsync())
        {
            _closeConfirmed = true;
            Close();
        }
    }
}
