using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using CueApp.Core;
using CueApp.Core.Audio;
using CueApp.Core.Model;

namespace CueApp.App.ViewModels;

/// <summary>Проигрывание: GO, STOP, PANIC, играющие Audio-кью и их отсчет.</summary>
public partial class MainViewModel
{
    /// <summary>Как часто обновлять отсчет на экране. Сам звук от этого не зависит.</summary>
    private static readonly TimeSpan LiveRefresh = TimeSpan.FromMilliseconds(50);

    private readonly DispatcherTimer _liveTimer;

    // Играющие кью и их звук
    private readonly Dictionary<AudioCue, IAudioPlayback> _playing = new();

    // Длины файлов (сек) и файлы, которые не открылись
    private readonly Dictionary<AudioCue, double> _fileDurations = new();
    private readonly HashSet<AudioCue> _missingFiles = new();

    [RelayCommand]
    private void Go()
    {
        if (!_goGuard.TryAccept(_clock.Elapsed, Show.Settings.DoubleGoProtectionMs))
        {
            Status = $"Повторный GO проигнорирован (защита {Show.Settings.DoubleGoProtectionMs} мс)";
            return;
        }

        Cue? selected = _playhead.Current;
        Cue? started = _playhead.Go();

        // Выбор в списке переезжает туда, куда ушел плейхед (или пусто в конце списка)
        SelectedRow = _playhead.Current is { } next ? _rowByCue.GetValueOrDefault(next) : null;

        if (started is null)
        {
            Status = selected is GroupCue ? "Вход в группу " + Describe(selected) : "Ничего не выбрано — GO пропущен";
            return;
        }

        if (started is AudioCue audio)
            StartAudio(audio);
        else
            Status = "GO → " + Describe(started) + " (запуск этого типа кью — в следующем шаге)";
    }

    [RelayCommand]
    private void Stop()
    {
        if (_audio.IsInitialized)
            _audio.StopAll(Show.Settings.StopFadeSeconds);
        Status = $"STOP — всё гаснет за {Show.Settings.StopFadeSeconds:0.#} с";
    }

    [RelayCommand]
    private void Panic()
    {
        if (_audio.IsInitialized)
            _audio.StopAll(Show.Settings.PanicFadeSeconds);
        Status = $"PANIC — всё гаснет за {Show.Settings.PanicFadeSeconds:0.#} с";
    }

    private void StartAudio(AudioCue cue)
    {
        if (!_audio.IsInitialized)
        {
            Status = "Аудио недоступно — " + Describe(cue) + " не запущена";
            return;
        }
        if (cue.FilePath == "")
        {
            Status = Describe(cue) + ": не выбран файл";
            return;
        }

        // Повторный GO на играющей кью — перезапуск с начала (старый звук гасим микрофейдом)
        if (_playing.Remove(cue, out var previous))
            previous.Stop(0);

        IAudioPlayback playback;
        try
        {
            playback = _audio.Play(new AudioPlayRequest(MediaPaths.Resolve(cue.FilePath, ShowDirectory))
            {
                TrimIn = cue.TrimIn,
                TrimOut = cue.TrimOut,
                FadeIn = cue.FadeIn,
                FadeOut = cue.FadeOut,
                VolumeDb = cue.VolumeDb,
                PlayCount = cue.PlayCount,
                LoopForever = cue.LoopForever,
            });
        }
        catch (Exception ex)
        {
            // Нет файла / битый файл: кью не стартует, всё остальное продолжает работать
            _missingFiles.Add(cue);
            if (_rowByCue.TryGetValue(cue, out var missingRow))
                missingRow.IsMissingFile = true;
            Status = "Не удалось запустить " + Describe(cue) + ": " + ex.Message;
            return;
        }

        _playing[cue] = playback;
        // Событие приходит из потока аудио — в интерфейс переходим через Dispatcher
        playback.Finished += (_, _) => Dispatcher.UIThread.Post(() => OnPlaybackFinished(cue, playback));

        if (_rowByCue.TryGetValue(cue, out var row))
            ApplyPlaybackState(row);
        _liveTimer.Start();
        Status = "GO → " + Describe(cue);
    }

    private void OnPlaybackFinished(AudioCue cue, IAudioPlayback playback)
    {
        // Кью могли уже перезапустить — тогда в словаре другой звук, его не трогаем
        if (_playing.TryGetValue(cue, out var current) && current == playback)
            _playing.Remove(cue);

        if (_rowByCue.TryGetValue(cue, out var row))
            ApplyPlaybackState(row);
        if (_playing.Count == 0)
            _liveTimer.Stop();
    }

    /// <summary>Переносит состояние звука на строку: длительность, «нет файла», «играет», отсчет.</summary>
    private void ApplyPlaybackState(CueRowViewModel row)
    {
        if (row.Cue is not AudioCue cue)
            return;

        row.FileDuration = _fileDurations.TryGetValue(cue, out double duration) ? duration : null;
        row.IsMissingFile = _missingFiles.Contains(cue);
        row.IsPlaying = _playing.ContainsKey(cue);
        row.LiveTime = _playing.TryGetValue(cue, out var playback) ? LiveTimeText(cue, playback) : null;
    }

    /// <summary>«−0:21.0» — сколько осталось; «∞ 12:41.3» — сколько уже играет бесконечная петля.</summary>
    private string LiveTimeText(AudioCue cue, IAudioPlayback playback)
    {
        if (!_fileDurations.TryGetValue(cue, out double fileDuration))
            return "…";

        double? remaining = AudioTiming.Remaining(cue, fileDuration, playback.Position, playback.CurrentPass);
        return remaining is { } left
            ? "−" + TimeFormat.MinSecTenths(left)
            : "∞ " + TimeFormat.MinSecTenths(AudioTiming.Elapsed(cue, fileDuration, playback.Position, playback.CurrentPass));
    }

    private DispatcherTimer CreateLiveTimer()
    {
        var timer = new DispatcherTimer { Interval = LiveRefresh };
        timer.Tick += (_, _) =>
        {
            foreach (var (cue, playback) in _playing)
                if (_rowByCue.TryGetValue(cue, out var row))
                    row.LiveTime = LiveTimeText(cue, playback);
        };
        return timer;
    }

    /// <summary>
    /// Узнать длины аудиофайлов в фоне (чтение больших MP3 может занять время — интерфейс не ждет).
    /// </summary>
    private void LoadDurations(IEnumerable<AudioCue> cues)
    {
        if (!_audio.IsInitialized)
            return;

        foreach (var cue in cues.Where(c => c.FilePath != "").ToList())
        {
            string path = MediaPaths.Resolve(cue.FilePath, ShowDirectory);
            Task.Run(() =>
            {
                double? duration = null;
                try { duration = _audio.GetDuration(path); }
                catch { /* нет файла / не читается — покажем «нет файла» */ }

                Dispatcher.UIThread.Post(() =>
                {
                    if (duration is { } d)
                    {
                        _fileDurations[cue] = d;
                        _missingFiles.Remove(cue);
                    }
                    else
                    {
                        _missingFiles.Add(cue);
                    }

                    if (_rowByCue.TryGetValue(cue, out var row))
                        ApplyPlaybackState(row);
                });
            });
        }
    }

    /// <summary>Удалили кью (или группу) — ее звук гасим, чтобы не остался «невидимый» играющий.</summary>
    private void StopRemovedCues()
    {
        var stillInShow = Show.AllCues().ToHashSet();
        foreach (var cue in _playing.Keys.Where(c => !stillInShow.Contains(c)).ToList())
        {
            _playing.Remove(cue, out var playback);
            playback!.Stop(0);
        }
    }

    /// <summary>Перед «Новое» / «Открыть»: всё гасим и забываем про файлы старого шоу.</summary>
    private void StopEverythingForShowChange()
    {
        if (_audio.IsInitialized)
            _audio.StopAll(Show.Settings.StopFadeSeconds);
        _playing.Clear();
        _fileDurations.Clear();
        _missingFiles.Clear();
        _liveTimer.Stop();
    }
}
