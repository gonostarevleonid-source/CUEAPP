using CueApp.Core.Model;
using CueApp.Core.Playback;

namespace CueApp.Tests;

public class PlayheadTests
{
    // Шоу для тестов:
    // 1
    // 2
    // 4 Группа
    //   4.1
    //   4.2
    // 5
    private readonly TrackCue _c1 = new() { Number = "1" };
    private readonly TrackCue _c2 = new() { Number = "2" };
    private readonly TrackCue _c41 = new() { Number = "4.1" };
    private readonly TrackCue _c42 = new() { Number = "4.2" };
    private readonly GroupCue _g4;
    private readonly TrackCue _c5 = new() { Number = "5" };
    private readonly Show _show;

    public PlayheadTests()
    {
        _g4 = new GroupCue { Number = "4", Children = { _c41, _c42 } };
        _show = new Show { Cues = { _c1, _c2, _g4, _c5 } };
    }

    [Fact]
    public void Go_StartsSelected_AndMovesToNext()
    {
        var playhead = new Playhead(_show) { Current = _c1 };

        Assert.Same(_c1, playhead.Go());
        Assert.Same(_c2, playhead.Current);
    }

    [Fact]
    public void Go_OnGroup_StartsNothing_AndEntersGroup()
    {
        var playhead = new Playhead(_show) { Current = _g4 };

        Assert.Null(playhead.Go());
        Assert.Same(_c41, playhead.Current);
    }

    [Fact]
    public void Go_OnEmptyGroup_MovesPastIt()
    {
        _g4.Children.Clear();
        var playhead = new Playhead(_show) { Current = _g4 };

        Assert.Null(playhead.Go());
        Assert.Same(_c5, playhead.Current);
    }

    [Fact]
    public void Go_ThroughGroup_SubCuesOneByOne_ThenOutOfGroup()
    {
        var playhead = new Playhead(_show) { Current = _g4 };

        playhead.Go();
        Assert.Same(_c41, playhead.Go());
        Assert.Same(_c42, playhead.Current);

        Assert.Same(_c42, playhead.Go());
        Assert.Same(_c5, playhead.Current);
    }

    [Fact]
    public void Go_OnLastCue_LeavesNothingSelected_AndNextGoDoesNothing()
    {
        var playhead = new Playhead(_show) { Current = _c5 };

        Assert.Same(_c5, playhead.Go());
        Assert.Null(playhead.Current);
        Assert.Null(playhead.Go());
    }
}

public class GoGuardTests
{
    [Fact]
    public void SecondGoInsideWindow_IsIgnored()
    {
        var guard = new GoGuard();

        Assert.True(guard.TryAccept(TimeSpan.FromMilliseconds(1000), 300));
        Assert.False(guard.TryAccept(TimeSpan.FromMilliseconds(1299), 300));
        Assert.True(guard.TryAccept(TimeSpan.FromMilliseconds(1300), 300));
    }

    [Fact]
    public void IgnoredGo_DoesNotExtendWindow()
    {
        // Окно считается от последнего ПРИНЯТОГО GO, а не от любого нажатия
        var guard = new GoGuard();

        guard.TryAccept(TimeSpan.FromMilliseconds(0), 300);
        guard.TryAccept(TimeSpan.FromMilliseconds(200), 300);

        Assert.True(guard.TryAccept(TimeSpan.FromMilliseconds(300), 300));
    }

    [Fact]
    public void ZeroProtection_AcceptsEverything()
    {
        var guard = new GoGuard();

        Assert.True(guard.TryAccept(TimeSpan.Zero, 0));
        Assert.True(guard.TryAccept(TimeSpan.Zero, 0));
    }
}
