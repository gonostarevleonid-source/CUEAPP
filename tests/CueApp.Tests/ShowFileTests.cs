using CueApp.Core.Model;

namespace CueApp.Tests;

public class ShowFileTests
{
    private static Show CreateSampleShow()
    {
        var music = new TrackCue
        {
            Number = "1",
            Name = "Увертюра",
            Notes = "Ждать кивка дирижера",
            Color = CueColor.Blue,
            FilePath = "audio/overture.wav",
            TrimIn = 2.25,
            TrimOut = 120,
            FadeIn = 0.5,
            FadeOut = 3,
            VolumeDb = -6,
            Outputs = { "Main L/R", "Sub" },
        };

        return new Show
        {
            Name = "Тестовое шоу",
            Settings = { BlockGoWhileTrackPlaying = true, DoubleGoProtectionMs = 500 },
            Cues =
            {
                music,
                new StopCue { Number = "2", TargetCueId = music.Id, FadeOut = 4 },
                new GroupCue
                {
                    Number = "3",
                    Children =
                    {
                        new TrackCue { Number = "3.1", FilePath = "audio/rain.wav" },
                        new StopCue { Number = "3.2", FadeOut = 1 },
                    },
                },
            },
        };
    }

    [Fact]
    public void SaveAndLoad_KeepsEverything()
    {
        var original = CreateSampleShow();

        var loaded = ShowFile.FromJson(ShowFile.ToJson(original));

        // Сравниваем через JSON: если что-то потерялось, строки не совпадут
        Assert.Equal(ShowFile.ToJson(original), ShowFile.ToJson(loaded));

        var music = Assert.IsType<TrackCue>(loaded.Cues[0]);
        Assert.Equal("Увертюра", music.Name);
        Assert.Equal(120, music.TrimOut);

        var stopOne = Assert.IsType<StopCue>(loaded.Cues[1]);
        Assert.Equal(music.Id, stopOne.TargetCueId);

        var group = Assert.IsType<GroupCue>(loaded.Cues[2]);
        Assert.IsType<TrackCue>(group.Children[0]);
        var stopAll = Assert.IsType<StopCue>(group.Children[1]);
        Assert.Null(stopAll.TargetCueId); // null = остановить все

        Assert.True(loaded.Settings.BlockGoWhileTrackPlaying);
        Assert.Equal(500, loaded.Settings.DoubleGoProtectionMs);
    }

    [Fact]
    public void Json_IsHumanReadable()
    {
        string json = ShowFile.ToJson(CreateSampleShow());

        Assert.Contains("\"type\": \"track\"", json);
        Assert.Contains("\"color\": \"blue\"", json);
        Assert.Contains("Увертюра", json); // кириллица без \u-кодов
    }

    [Fact]
    public void Save_SecondTime_KeepsBackupOfPreviousVersion()
    {
        string dir = Directory.CreateTempSubdirectory("cueapp-test").FullName;
        string path = Path.Combine(dir, "show" + ShowFile.Extension);
        try
        {
            ShowFile.Save(new Show { Name = "Версия 1" }, path);
            ShowFile.Save(new Show { Name = "Версия 2" }, path);

            Assert.Equal("Версия 2", ShowFile.Load(path).Name);
            Assert.Equal("Версия 1", ShowFile.Load(path + ShowFile.BackupExtension).Name);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
