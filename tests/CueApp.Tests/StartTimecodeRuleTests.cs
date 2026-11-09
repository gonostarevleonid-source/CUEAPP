using CueApp.Core.Model;
using CueApp.Core.Time;
using TC = CueApp.Core.Time.Timecode;

namespace CueApp.Tests;

public class StartTimecodeRuleTests
{
    private readonly Dictionary<TrackCue, double> _durations = new();

    private TC Suggest(Show show, TrackCue track) =>
        StartTimecodeRule.Suggest(show, track, t => _durations.TryGetValue(t, out double d) ? d : null);

    [Fact]
    public void FirstTrack_StartsAtFirstTrackTimecode()
    {
        var track = new TrackCue();
        var show = new Show { Cues = { new StopCue(), track } };

        Assert.Equal(new TC(1, 0, 0, 0), Suggest(show, track));
    }

    [Fact]
    public void NextTrack_EndPlusFiveMinutes_RoundedUpToFiveMinutes()
    {
        // Предыдущий 01:00:00:00, длина 3:47 → конец 01:03:47 → +5 = 01:08:47 → 01:10:00:00
        var first = new TrackCue { StartTimecode = new TC(1, 0, 0, 0) };
        var second = new TrackCue();
        _durations[first] = 227;
        var show = new Show { Cues = { first, second } };

        Assert.Equal(new TC(1, 10, 0, 0), Suggest(show, second));
    }

    [Fact]
    public void ExactBoundary_IsNotPushedFurther()
    {
        // Длина ровно 5:00 → конец 01:05:00 → +5 = 01:10:00 — уже кратно 5, не 01:15
        var first = new TrackCue { StartTimecode = new TC(1, 0, 0, 0) };
        var second = new TrackCue();
        _durations[first] = 300;
        var show = new Show { Cues = { first, second } };

        Assert.Equal(new TC(1, 10, 0, 0), Suggest(show, second));
    }

    [Fact]
    public void UnknownDuration_CountsAsZero()
    {
        var first = new TrackCue { StartTimecode = new TC(1, 0, 0, 0) };
        var second = new TrackCue();
        var show = new Show { Cues = { first, second } };

        Assert.Equal(new TC(1, 5, 0, 0), Suggest(show, second));
    }

    [Fact]
    public void UsesPreviousTrackInListOrder_IncludingInsideGroups()
    {
        // Трек в группе после трека 2:00:00:00 — считаем от него, Stop между ними не мешает
        var a = new TrackCue { StartTimecode = new TC(1, 0, 0, 0) };
        var b = new TrackCue { StartTimecode = new TC(2, 0, 0, 0) };
        var inGroup = new TrackCue();
        var after = new TrackCue { StartTimecode = new TC(5, 0, 0, 0) };
        _durations[b] = 60;
        var show = new Show { Cues = { a, b, new GroupCue { Children = { new StopCue(), inGroup } }, after } };

        Assert.Equal(new TC(2, 10, 0, 0), Suggest(show, inGroup));
    }

    [Fact]
    public void RuleFromSettings_IsUsed()
    {
        var first = new TrackCue { StartTimecode = new TC(10, 0, 0, 0) };
        var second = new TrackCue();
        _durations[first] = 61;
        var show = new Show
        {
            Settings = { StartTimecodeGapMinutes = 1, StartTimecodeRoundMinutes = 1 },
            Cues = { first, second },
        };

        // 10:01:01 + 1 мин = 10:02:01 → до целой минуты вверх = 10:03:00
        Assert.Equal(new TC(10, 3, 0, 0), Suggest(show, second));
    }

    [Fact]
    public void StartTimecodeAndRate_SurviveSaveAndLoad()
    {
        var track = new TrackCue { StartTimecode = new TC(1, 12, 0, 2), FrameRate = FrameRate.Fps2997Df };
        var show = new Show { Cues = { track } };

        string json = ShowFile.ToJson(show);
        var loaded = Assert.IsType<TrackCue>(ShowFile.FromJson(json).Cues[0]);

        Assert.Contains("\"startTimecode\": \"01:12:00:02\"", json);
        Assert.Contains("\"frameRate\": \"fps2997Df\"", json);
        Assert.Equal(new TC(1, 12, 0, 2), loaded.StartTimecode);
        Assert.Equal(FrameRate.Fps2997Df, loaded.FrameRate);
    }
}
