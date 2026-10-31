using Avalonia.Controls;
using Avalonia.Interactivity;
using CueApp.App.Services;

namespace CueApp.App.Views;

/// <summary>Вопрос «Сохранить изменения в шоу?» с тремя ответами.</summary>
public partial class SaveChangesDialog : Window
{
    // Конструктор без параметров нужен дизайнеру Avalonia
    public SaveChangesDialog() : this("") { }

    public SaveChangesDialog(string showName)
    {
        InitializeComponent();
        QuestionText.Text = $"Сохранить изменения в шоу «{showName}»?";
    }

    private void OnSave(object? sender, RoutedEventArgs e) => Close(SaveChangesAnswer.Save);

    private void OnDontSave(object? sender, RoutedEventArgs e) => Close(SaveChangesAnswer.DontSave);

    // Закрытие крестиком вернет значение по умолчанию — это тоже Cancel (см. SaveChangesAnswer)
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(SaveChangesAnswer.Cancel);
}
