using CueApp.Core.Time;
using TC = CueApp.Core.Time.Timecode;

namespace CueApp.Tests;

public class TimecodeTests
{
    [Fact]
    public void Fps25_FrameNumberAndSeconds()
    {
        var tc = new TC(1, 0, 0, 0);

        Assert.Equal(90000, tc.ToFrameNumber(FrameRate.Fps25));
        Assert.Equal(3600, tc.ToSeconds(FrameRate.Fps25), precision: 9);
    }

    [Theory]
    [InlineData(FrameRate.Fps24)]
    [InlineData(FrameRate.Fps25)]
    [InlineData(FrameRate.Fps2997Df)]
    [InlineData(FrameRate.Fps2997Ndf)]
    [InlineData(FrameRate.Fps30)]
    public void EveryFrame_RoundTripsThroughFrameNumber(FrameRate rate)
    {
        // Первые 11 минут — захватываем и пропуски DF, и «каждую десятую минуту»
        long limit = (long)(rate.Actual() * 60 * 11);
        for (long n = 0; n < limit; n++)
            Assert.Equal(n, TC.FromFrameNumber(n, rate).ToFrameNumber(rate));
    }

    [Fact]
    public void DropFrame_SkipsFrames0And1_ExceptEveryTenthMinute()
    {
        // 00:00:59;29 → следующий кадр 00:01:00;02 (номера 00 и 01 пропущены)
        Assert.Equal(new TC(0, 0, 59, 29), TC.FromFrameNumber(1799, FrameRate.Fps2997Df));
        Assert.Equal(new TC(0, 1, 0, 2), TC.FromFrameNumber(1800, FrameRate.Fps2997Df));

        // На 10-й минуте пропуска нет
        Assert.Equal(new TC(0, 10, 0, 0), TC.FromFrameNumber(17982, FrameRate.Fps2997Df));
    }

    [Fact]
    public void DropFrame_OneHourOfLabels_IsAboutOneRealHour()
    {
        // В этом и смысл drop-frame: 01:00:00;00 почти точно = 3600 реальных секунд
        Assert.Equal(107892, new TC(1, 0, 0, 0).ToFrameNumber(FrameRate.Fps2997Df));
        Assert.Equal(3600, new TC(1, 0, 0, 0).ToSeconds(FrameRate.Fps2997Df), precision: 0);
    }

    [Fact]
    public void NonDrop2997_RunsSlowerThanRealTime()
    {
        // 01:00:00:00 в 29.97 NDF наступает через 3603.6 реальных секунд
        Assert.Equal(3603.6, new TC(1, 0, 0, 0).ToSeconds(FrameRate.Fps2997Ndf), precision: 3);
    }

    [Fact]
    public void FromSeconds_GivesFrameThatIsPlaying()
    {
        // 3600.039 с при 25 fps — это еще кадр 0 (следующий начнется на 3600.04)
        Assert.Equal(new TC(1, 0, 0, 0), TC.FromSeconds(3600.039, FrameRate.Fps25));
        Assert.Equal(new TC(1, 0, 0, 1), TC.FromSeconds(3600.04, FrameRate.Fps25));
    }

    [Fact]
    public void PastMidnight_WrapsAround()
    {
        Assert.Equal(new TC(0, 0, 0, 5), TC.FromFrameNumber(25 * 86400 + 5, FrameRate.Fps25));
    }

    [Theory]
    [InlineData("01:12:00:00", FrameRate.Fps25, 1, 12, 0, 0)]
    [InlineData("01:12:00;02", FrameRate.Fps2997Df, 1, 12, 0, 2)]
    [InlineData(" 23.59.59.24 ", FrameRate.Fps25, 23, 59, 59, 24)]
    public void TryParse_Accepts(string text, FrameRate rate, int h, int m, int s, int f)
    {
        Assert.True(TC.TryParse(text, rate, out var tc));
        Assert.Equal(new TC(h, m, s, f), tc);
    }

    [Theory]
    [InlineData("01:00:00:25", FrameRate.Fps25)]    // кадр 25 при 25 fps не бывает
    [InlineData("24:00:00:00", FrameRate.Fps25)]
    [InlineData("01:60:00:00", FrameRate.Fps25)]
    [InlineData("01:00:00", FrameRate.Fps25)]
    [InlineData("abc", FrameRate.Fps25)]
    [InlineData("00:01:00;00", FrameRate.Fps2997Df)] // пропущенный номер drop-frame
    public void TryParse_Rejects(string text, FrameRate rate)
    {
        Assert.False(TC.TryParse(text, rate, out _));
    }

    [Fact]
    public void ToString_DropFrameUsesSemicolon()
    {
        var tc = new TC(1, 2, 3, 4);

        Assert.Equal("01:02:03:04", tc.ToString(FrameRate.Fps25));
        Assert.Equal("01:02:03;04", tc.ToString(FrameRate.Fps2997Df));
    }

    [Fact]
    public void Normalize_FitsFramesToRate()
    {
        Assert.Equal(new TC(1, 0, 0, 24), new TC(1, 0, 0, 29).Normalize(FrameRate.Fps25));
        Assert.Equal(new TC(0, 1, 0, 2), new TC(0, 1, 0, 0).Normalize(FrameRate.Fps2997Df));
    }
}
