using CueApp.Core.Audio;
using CueApp.Core.Model;

namespace CueApp.Tests;

public class AudioTimingTests
{
    // Файл 60 с, играем фрагмент 10…40 с
    private const double File = 60;
    private readonly TrackCue _cue = new() { TrimIn = 10, TrimOut = 40 };

    [Fact]
    public void Length_RespectsTrim()
    {
        Assert.Equal(30, AudioTiming.Length(_cue, File));
    }

    [Fact]
    public void Length_NoTrimOut_PlaysToEndOfFile()
    {
        Assert.Equal(50, AudioTiming.Length(new TrackCue { TrimIn = 10 }, File));
    }

    [Fact]
    public void Length_TrimOutBeyondFile_IsClamped()
    {
        Assert.Equal(60, AudioTiming.Length(new TrackCue { TrimOut = 999 }, File));
    }

    [Fact]
    public void Remaining_CountsToTrimOut()
    {
        Assert.Equal(15, AudioTiming.Remaining(_cue, File, position: 25));
    }

    [Fact]
    public void Remaining_PositionOutsideTrim_IsClamped()
    {
        // До trim in — осталось всё; после trim out — ноль
        Assert.Equal(30, AudioTiming.Remaining(_cue, File, position: 2));
        Assert.Equal(0, AudioTiming.Remaining(_cue, File, position: 55));
    }
}
