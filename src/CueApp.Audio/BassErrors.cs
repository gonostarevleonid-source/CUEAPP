using ManagedBass;

namespace CueApp.Audio;

/// <summary>Понятные сообщения для частых ошибок BASS.</summary>
internal static class BassErrors
{
    public static string Describe(Errors error) => error switch
    {
        Errors.FileOpen => "файл не найден или не открывается",
        Errors.FileFormat => "формат файла не поддерживается",
        Errors.Codec => "нет кодека для этого файла",
        Errors.Memory => "не хватает памяти",
        Errors.Init => "аудиоустройство не инициализировано",
        _ => "ошибка BASS: " + error,
    };
}
