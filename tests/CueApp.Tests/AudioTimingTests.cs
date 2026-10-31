using CueApp.Core.Audio;
using CueApp.Core.Model;

namespace CueApp.Tests;

public class AudioTimingTests
{
    // Файл 60 с, играем фрагмент 10…40 с (проход = 30 с)
    private const double File = 60;
    private readonly AudioCue _cue = new() { TrimIn = 10, TrimOut = 40 };

    [Fact]
    public void PassLength_RespectsTrim()
    {
        Assert.Equal(30, AudioTiming.PassLength(_cue, File));
    }

    [Fact]
    public void PassLength_NoTrimOut_PlaysToEndOfFile()
    {
        Assert.Equal(50, AudioTiming.PassLength(new AudioCue { TrimIn = 10 }, File));
    }

    [Fact]
    public void PassLength_TrimOutBeyondFile_IsClamped()
    {
        Assert.Equal(60, AudioTiming.PassLength(new AudioCue { TrimOut = 999 }, File));
    }

    [Fact]
    public void TotalLength_MultipliesByPlayCount_InfiniteIsNull()
    {
        _cue.PlayCount = 3;
        Assert.Equal(90, AudioTiming.TotalLength(_cue, File));

        _cue.LoopForever = true;
        Assert.Null(AudioTiming.TotalLength(_cue, File));
    }

    [Fact]
    public void Remaining_CountsRestOfPassAndRemainingPasses()
    {
        // 3 прохода, сейчас 2-й, позиция 25 с в файле → 15 с до конца прохода + 30 с третьего прохода
        _cue.PlayCount = 3;

        Assert.Equal(45, AudioTiming.Remaining(_cue, File, position: 25, currentPass: 2));
    }

    [Fact]
    public void Remaining_LastPass_OnlyRestOfPass()
    {
        Assert.Equal(5, AudioTiming.Remaining(_cue, File, position: 35, currentPass: 1));
    }

    [Fact]
    public void Remaining_InfiniteLoop_IsNull()
    {
        _cue.LoopForever = true;

        Assert.Null(AudioTiming.Remaining(_cue, File, position: 25, currentPass: 7));
    }

    [Fact]
    public void Elapsed_CountsAllPasses()
    {
        // 3-й проход, позиция 20 с → 2 полных прохода по 30 с + 10 с
        _cue.LoopForever = true;

        Assert.Equal(70, AudioTiming.Elapsed(_cue, File, position: 20, currentPass: 3));
    }
}
