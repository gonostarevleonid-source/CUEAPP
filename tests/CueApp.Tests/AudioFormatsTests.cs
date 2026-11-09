using CueApp.Core.Audio;

namespace CueApp.Tests;

public class AudioFormatsTests
{
    [Theory]
    [InlineData("song.wav")]
    [InlineData("SONG.WAV")]
    [InlineData("Финал.mp3")]
    [InlineData("/Users/leonid/show/intro.aiff")]
    [InlineData(@"C:\show\audio\rain.flac")]
    public void AudioFiles_AreAccepted(string path)
    {
        Assert.True(AudioFormats.IsAudioFile(path));
    }

    [Theory]
    [InlineData("show.cue")]
    [InlineData("notes.txt")]
    [InlineData("video.mp4")]
    [InlineData("папка")]
    public void OtherFiles_AreRejected(string path)
    {
        Assert.False(AudioFormats.IsAudioFile(path));
    }
}
