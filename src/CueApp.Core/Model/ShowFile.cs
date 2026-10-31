using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace CueApp.Core.Model;

/// <summary>Сохранение и загрузка шоу в файл .cue (человекочитаемый JSON).</summary>
public static class ShowFile
{
    public const string Extension = ".cue";
    public const string BackupExtension = ".bak";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // Кириллица пишется буквами, а не кодами У... (файл должен читаться глазами)
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Перечисления пишем словами («autoFollow»), а не числами
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // Прощаем мелочи при ручной правке файла
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string ToJson(Show show) => JsonSerializer.Serialize(show, Options);

    public static Show FromJson(string json) =>
        JsonSerializer.Deserialize<Show>(json, Options)
        ?? throw new InvalidDataException("Файл шоу пустой.");

    /// <summary>
    /// Безопасное сохранение: пишем во временный файл и подменяем им старый.
    /// Предыдущая версия остается рядом как «имя.cue.bak».
    /// Если запись оборвется на середине, старый файл не пострадает.
    /// </summary>
    public static void Save(Show show, string path)
    {
        string tempPath = path + ".tmp";
        File.WriteAllText(tempPath, ToJson(show));

        if (File.Exists(path))
            File.Replace(tempPath, path, path + BackupExtension);
        else
            File.Move(tempPath, path);
    }

    public static Show Load(string path) => FromJson(File.ReadAllText(path));
}
