using CueApp.Core.Model;

namespace CueApp.Tests;

public class ShowFileTests
{
    private static Show CreateSampleShow()
    {
        var music = new AudioCue
        {
            Number = "1",
            Name = "Увертюра",
            Notes = "Ждать кивка дирижера",
            Tags = { "акт 1" },
            Color = CueColor.Blue,
            PreWait = 1.5,
            Continue = ContinueMode.AutoFollow,
            FilePath = "audio/overture.wav",
            TrimIn = 2.25,
            TrimOut = 120,
            FadeIn = 0.5,
            FadeOut = 3,
            VolumeDb = -6,
            Outputs = { "Main L/R", "Sub" },
            PlayCount = 2,
        };

        return new Show
        {
            Name = "Тестовое шоу",
            Settings = { BlockGoWhileTimelinePlaying = true, DoubleGoProtectionMs = 500 },
            Cues =
            {
                music,
                new FadeCue { Number = "2", TargetCueId = music.Id, Duration = 4, TargetVolumeDb = -60, Curve = FadeCurve.SCurve },
                new GroupCue
                {
                    Number = "3",
                    Children =
                    {
                        new WaitCue { Number = "3.1", Duration = 2 },
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

        var music = Assert.IsType<AudioCue>(loaded.Cues[0]);
        Assert.Equal("Увертюра", music.Name);
        Assert.Equal(120, music.TrimOut);
        Assert.Equal(ContinueMode.AutoFollow, music.Continue);

        var fade = Assert.IsType<FadeCue>(loaded.Cues[1]);
        Assert.Equal(music.Id, fade.TargetCueId);

        var group = Assert.IsType<GroupCue>(loaded.Cues[2]);
        Assert.IsType<WaitCue>(group.Children[0]);
        var stop = Assert.IsType<StopCue>(group.Children[1]);
        Assert.Null(stop.TargetCueId); // null = остановить все

        Assert.True(loaded.Settings.BlockGoWhileTimelinePlaying);
        Assert.Equal(500, loaded.Settings.DoubleGoProtectionMs);
    }

    [Fact]
    public void Json_IsHumanReadable()
    {
        string json = ShowFile.ToJson(CreateSampleShow());

        Assert.Contains("\"type\": \"audio\"", json);
        Assert.Contains("\"continue\": \"autoFollow\"", json);
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
