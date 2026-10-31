using CueApp.Core.Model;

namespace CueApp.Tests;

public class MediaPathsTests
{
    // Пути строим через Path.Combine, чтобы тесты работали и на Windows, и на Mac
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "cueapp-paths");
    private static readonly string ShowDir = Path.Combine(Root, "Show");
    private static readonly string AudioFile = Path.Combine(ShowDir, "audio", "overture.wav");

    [Fact]
    public void FileInsideShowFolder_StoredRelativeWithForwardSlash()
    {
        Assert.Equal("audio/overture.wav", MediaPaths.ToStored(AudioFile, ShowDir));
    }

    [Fact]
    public void FileNextToShowFolder_StoredWithDotDot()
    {
        string sibling = Path.Combine(Root, "Sounds", "rain.wav");

        Assert.Equal("../Sounds/rain.wav", MediaPaths.ToStored(sibling, ShowDir));
    }

    [Fact]
    public void UnsavedShow_KeepsFullPath()
    {
        Assert.Equal(AudioFile, MediaPaths.ToStored(AudioFile, showDirectory: null));
    }

    [Fact]
    public void Resolve_RelativeWithForwardSlash_GivesFullPath()
    {
        Assert.Equal(AudioFile, MediaPaths.Resolve("audio/overture.wav", ShowDir));
    }

    [Fact]
    public void Resolve_FullPath_StaysAsIs()
    {
        Assert.Equal(AudioFile, MediaPaths.Resolve(AudioFile, ShowDir));
    }

    [Fact]
    public void Relocate_SaveAsToOtherFolder_StillPointsToSameFile()
    {
        var cue = new AudioCue { FilePath = "audio/overture.wav" };
        var show = new Show { Cues = { new GroupCue { Children = { cue } } } };
        string newDir = Path.Combine(Root, "Backup");

        MediaPaths.Relocate(show, ShowDir, newDir);

        Assert.Equal("../Show/audio/overture.wav", cue.FilePath);
        Assert.Equal(AudioFile, MediaPaths.Resolve(cue.FilePath, newDir));
    }

    [Fact]
    public void Relocate_FirstSave_TurnsFullPathsIntoRelative()
    {
        var cue = new AudioCue { FilePath = AudioFile };
        var show = new Show { Cues = { cue } };

        MediaPaths.Relocate(show, oldShowDirectory: null, ShowDir);

        Assert.Equal("audio/overture.wav", cue.FilePath);
    }
}
