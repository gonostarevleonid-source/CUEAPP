using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CueApp.App.Views;
using CueApp.Core.Model;

namespace CueApp.App.Services;

/// <summary>Системные окна выбора файла + наш вопрос «Сохранить изменения?».</summary>
public sealed class DialogService : IDialogService
{
    private static readonly FilePickerFileType ShowFileType = new("Шоу CUEAPP")
    {
        Patterns = ["*" + ShowFile.Extension],
    };

    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> PickShowToOpenAsync()
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Открыть шоу",
            AllowMultiple = false,
            FileTypeFilter = [ShowFileType],
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> PickShowToSaveAsync(string suggestedName)
    {
        var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить шоу",
            SuggestedFileName = suggestedName,
            DefaultExtension = ShowFile.Extension.TrimStart('.'),
            FileTypeChoices = [ShowFileType],
            ShowOverwritePrompt = true,
        });

        return file?.TryGetLocalPath();
    }

    public Task<SaveChangesAnswer> AskSaveChangesAsync(string showName) =>
        new SaveChangesDialog(showName).ShowDialog<SaveChangesAnswer>(_owner);
}
