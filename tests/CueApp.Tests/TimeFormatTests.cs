using CueApp.Core;

namespace CueApp.Tests;

public class TimeFormatTests
{
    [Theory]
    [InlineData("65", 65)]
    [InlineData("65.5", 65.5)]
    [InlineData("65,5", 65.5)]
    [InlineData(" 2.25 ", 2.25)]
    [InlineData("1:05", 65)]
    [InlineData("1:05.5", 65.5)]
    [InlineData("12:41,3", 761.3)]
    [InlineData("0", 0)]
    public void TryParse_Accepts(string text, double expected)
    {
        Assert.True(TimeFormat.TryParse(text, out double seconds));
        Assert.Equal(expected, seconds, precision: 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("1:75")]
    [InlineData("1:2:3")]
    [InlineData("-1:05")]
    public void TryParse_Rejects(string text)
    {
        Assert.False(TimeFormat.TryParse(text, out _));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(2.25, "2.25")]
    [InlineData(5, "5")]
    [InlineData(65.5, "1:05.5")]
    [InlineData(0.1234, "0.123")]
    public void Editable_ShowsUpToMilliseconds(double seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.Editable(seconds));
    }

    [Theory]
    [InlineData(2.25)]
    [InlineData(65.5)]
    [InlineData(761.3)]
    public void Editable_RoundTripsThroughTryParse(double seconds)
    {
        Assert.True(TimeFormat.TryParse(TimeFormat.Editable(seconds), out double back));
        Assert.Equal(seconds, back, precision: 6);
    }
}
