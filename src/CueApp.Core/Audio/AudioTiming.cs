using CueApp.Core.Model;

namespace CueApp.Core.Audio;

/// <summary>Расчет длительности трека с учетом trim (раздел 7 ТЗ).</summary>
public static class AudioTiming
{
    /// <summary>Сколько играет трек: от trim in до trim out (или конца файла).</summary>
    public static double Length(TrackCue cue, double fileDuration) =>
        Math.Max(0, SegmentEnd(cue, fileDuration) - SegmentStart(cue, fileDuration));

    /// <summary>Сколько осталось играть при текущей позиции во времени файла.</summary>
    public static double Remaining(TrackCue cue, double fileDuration, double position)
    {
        double end = SegmentEnd(cue, fileDuration);
        return Math.Max(0, end - Math.Clamp(position, SegmentStart(cue, fileDuration), end));
    }

    private static double SegmentStart(TrackCue cue, double fileDuration) =>
        Math.Clamp(cue.TrimIn, 0, fileDuration);

    private static double SegmentEnd(TrackCue cue, double fileDuration) =>
        Math.Clamp(cue.TrimOut ?? fileDuration, 0, fileDuration);
}
