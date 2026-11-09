using CueApp.Core;
using CueApp.Core.Model;

namespace CueApp.Tests;

public class CueEditingTests
{
    [Fact]
    public void InsertAfter_Null_AppendsWithNextNumber()
    {
        var show = new Show();

        show.InsertAfter(null, new StopCue());
        show.InsertAfter(null, new StopCue());

        Assert.Equal(["1", "2"], show.Cues.Select(c => c.Number));
    }

    [Fact]
    public void InsertAfter_InMiddle_TakenNumberStaysEmpty()
    {
        // 3 и 4 уже есть: новая после 3 не может стать «4» — номера закреплены
        var three = new StopCue { Number = "3" };
        var four = new StopCue { Number = "4" };
        var show = new Show { Cues = { three, four } };
        var added = new StopCue();

        show.InsertAfter(three, added);

        Assert.Equal(1, show.Cues.IndexOf(added));
        Assert.Equal("", added.Number);
        Assert.Equal("4", four.Number);
    }

    [Fact]
    public void InsertAfter_SubCue_StaysInGroupWithSubNumber()
    {
        var sub = new StopCue { Number = "4.1" };
        var group = new GroupCue { Number = "4", Children = { sub } };
        var show = new Show { Cues = { group } };
        var added = new StopCue();

        show.InsertAfter(sub, added);

        Assert.Same(added, group.Children[1]);
        Assert.Equal("4.2", added.Number);
    }

    [Fact]
    public void InsertAfter_NonNumericPrevious_GivesEmptyNumber()
    {
        var odd = new StopCue { Number = "12a" };
        var show = new Show { Cues = { odd } };
        var added = new StopCue();

        show.InsertAfter(odd, added);

        Assert.Equal("", added.Number);
    }

    [Fact]
    public void Remove_DeletesGroupWithChildren_AndSubCueFromGroup()
    {
        var sub1 = new StopCue();
        var sub2 = new StopCue();
        var group = new GroupCue { Children = { sub1, sub2 } };
        var other = new StopCue();
        var show = new Show { Cues = { group, other } };

        Assert.True(show.Remove(sub1));
        Assert.Equal([sub2], group.Children);

        Assert.True(show.Remove(group));
        Assert.Equal([other], show.AllCues());

        Assert.False(show.Remove(sub1)); // уже удалена
    }

    [Theory]
    [InlineData(0, "0.0")]
    [InlineData(5, "5.0")]
    [InlineData(12.04, "12.0")]
    [InlineData(65, "1:05.0")]
    [InlineData(59.96, "1:00.0")]
    [InlineData(761.3, "12:41.3")]
    [InlineData(-7.8, "−7.8")]
    public void TimeFormat_MinSecTenths(double seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.MinSecTenths(seconds));
    }
}
