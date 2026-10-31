using System.Threading.Tasks;

namespace CueApp.App.Services;

/// <summary>Ответ на «Сохранить изменения?».</summary>
public enum SaveChangesAnswer
{
    // Cancel — первым: окно, закрытое крестиком, возвращает значение по умолчанию,
    // и это должно быть «Отмена», а не «Сохранить»
    Cancel,
    Save,
    DontSave,
}

/// <summary>
/// Диалоги, которые нужны ViewModel. Интерфейс — чтобы ViewModel не знала про окна
/// (и ее можно было проверить без экрана).
/// </summary>
public interface IDialogService
{
    /// <summary>Выбрать файл шоу для открытия; null — пользователь отменил.</summary>
    Task<string?> PickShowToOpenAsync();

    /// <summary>Выбрать, куда сохранить шоу; null — пользователь отменил.</summary>
    Task<string?> PickShowToSaveAsync(string suggestedName);

    Task<SaveChangesAnswer> AskSaveChangesAsync(string showName);
}
