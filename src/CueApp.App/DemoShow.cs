using CueApp.Core.Model;

namespace CueApp.App;

/// <summary>
/// Временное демо-шоу, пока нет открытия файлов: чтобы были видны подкью, цвета и колонки.
/// Убрать, когда появится «Открыть / Сохранить».
/// </summary>
public static class DemoShow
{
    public static Show Create()
    {
        var foyer = new AudioCue { Number = "2", Name = "Фон фойе", LoopForever = true };

        return new Show
        {
            Name = "Демо-шоу",
            Cues =
            {
                new AudioCue { Number = "1", Name = "Предзвонок 1", Color = CueColor.Blue },
                foyer,
                new FadeCue { Number = "3", Name = "Фон фойе → тишина", TargetCueId = foyer.Id, Duration = 5, TargetVolumeDb = -60 },
                new GroupCue
                {
                    Number = "4",
                    Name = "Пролог",
                    Color = CueColor.Purple,
                    Children =
                    {
                        new AudioCue { Number = "4.1", Name = "Ветер, море", PreWait = 0.5, Continue = ContinueMode.AutoContinue, Color = CueColor.Purple },
                        new WaitCue { Number = "4.2", Name = "Пауза перед чайками", Duration = 2, Continue = ContinueMode.AutoFollow },
                        new AudioCue { Number = "4.3", Name = "Чайки", PreWait = 2 },
                    },
                },
                new AudioCue { Number = "5", Name = "Поезд вдали", Color = CueColor.Yellow },
                new StopCue { Number = "6", Name = "Стоп всё", FadeOut = 3, Color = CueColor.Red },
                new AudioCue { Number = "7", Name = "Поклоны", Color = CueColor.Green },
                new WaitCue { Number = "8", Name = "Пауза 10 с", Duration = 10 },
            },
        };
    }
}
