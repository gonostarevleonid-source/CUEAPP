using CueApp.Core.Model;

namespace CueApp.Tests;

public class ShowModelTests
{
    [Fact]
    public void Renumber_NumbersMainCuesAndSubCues()
    {
        var group = new GroupCue
        {
            Children =
            {
                new WaitCue(),
                new GroupCue { Children = { new WaitCue() } },
            },
        };
        var show = new Show { Cues = { new AudioCue(), new WaitCue(), group } };

        show.Renumber();

        Assert.Equal(["1", "2", "3", "3.1", "3.2", "3.2.1"], show.AllCues().Select(c => c.Number));
    }

    [Fact]
    public void AllCues_GoesTopToBottomIncludingSubCues()
    {
        var a = new WaitCue { Name = "A" };
        var sub = new WaitCue { Name = "B.1" };
        var b = new GroupCue { Name = "B", Children = { sub } };
        var c = new WaitCue { Name = "C" };
        var show = new Show { Cues = { a, b, c } };

        Assert.Equal(["A", "B", "B.1", "C"], show.AllCues().Select(x => x.Name));
        Assert.Same(sub, show.FindCue(sub.Id));
        Assert.Null(show.FindCue(Guid.NewGuid()));
    }
}
