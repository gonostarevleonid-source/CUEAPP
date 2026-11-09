namespace CueApp.Core.Audio;

/// <summary>
/// Какие аудиофайлы можно добавлять как трек. BASS читает их сам
/// или через системные кодеки (Media Foundation на Windows, CoreAudio на Mac).
/// </summary>
public static class AudioFormats
{
    public static IReadOnlyList<string> Extensions { get; } = [".wav", ".aif", ".aiff", ".mp3", ".flac", ".ogg", ".m4a"];

    public static bool IsAudioFile(string path) =>
        Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
