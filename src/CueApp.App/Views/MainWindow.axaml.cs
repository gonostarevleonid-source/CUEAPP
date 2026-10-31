using System;
using Avalonia.Controls;
using Avalonia.Input;
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

    /// <summary>Закрытие окна при несохраненных изменениях — сначала спросить.</summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
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
