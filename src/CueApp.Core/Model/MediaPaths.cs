namespace CueApp.Core.Model;

/// <summary>
/// Пути к медиафайлам в шоу. Храним их относительно папки шоу и через «/»,
/// чтобы папку шоу со звуками можно было перенести между Windows и Mac.
/// </summary>
public static class MediaPaths
{
    /// <summary>Путь из файла шоу → полный путь на этом компьютере.</summary>
    public static string Resolve(string stored, string? showDirectory)
    {
        if (stored == "" || Path.IsPathRooted(stored) || showDirectory is null)
            return stored;

        return Path.GetFullPath(Path.Combine(showDirectory, stored.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>
    /// Полный путь → как записать в шоу: относительно папки шоу через «/».
    /// Если шоу еще не сохранено или файл на другом диске — остается полный путь.
    /// </summary>
    public static string ToStored(string fullPath, string? showDirectory)
    {
        if (fullPath == "" || showDirectory is null)
            return fullPath;

        string relative = Path.GetRelativePath(showDirectory, fullPath);
        return Path.IsPathRooted(relative) ? fullPath : relative.Replace('\\', '/');
    }

    /// <summary>
    /// Шоу сохраняется в другую папку («Сохранить как…»): пересчитать пути всех Audio-кью,
    /// чтобы они по-прежнему указывали на те же файлы. newShowDirectory = null — сделать пути полными.
    /// </summary>
    public static void Relocate(Show show, string? oldShowDirectory, string? newShowDirectory)
    {
        foreach (var audio in show.AllCues().OfType<AudioCue>())
            audio.FilePath = ToStored(Resolve(audio.FilePath, oldShowDirectory), newShowDirectory);
    }
}
