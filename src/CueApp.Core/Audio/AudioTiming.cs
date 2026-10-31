using CueApp.Core.Model;

namespace CueApp.Core.Audio;

/// <summary>Расчет длительностей Audio-кью с учетом trim и повторов (раздел 6 ТЗ).</summary>
public static class AudioTiming
{
    /// <summary>Длина одного прохода: от trim in до trim out (или конца файла).</summary>
    public static double PassLength(AudioCue cue, double fileDuration) =>
        Math.Max(0, SegmentEnd(cue, fileDuration) - SegmentStart(cue, fileDuration));

    /// <summary>Полная длительность со всеми повторами; null — бесконечная петля.</summary>
    public static double? TotalLength(AudioCue cue, double fileDuration) =>
        cue.LoopForever ? null : PassLength(cue, fileDuration) * Math.Max(1, cue.PlayCount);

    /// <summary>
    /// Сколько осталось играть: остаток текущего прохода + все оставшиеся проходы.
    /// null — бесконечная петля («∞»).
    /// </summary>
    public static double? Remaining(AudioCue cue, double fileDuration, double position, int currentPass)
    {
        if (cue.LoopForever)
            return null;

        double end = SegmentEnd(cue, fileDuration);
        double inThisPass = Math.Max(0, end - Math.Clamp(position, SegmentStart(cue, fileDuration), end));
        int passesAfter = Math.Max(0, Math.Max(1, cue.PlayCount) - currentPass);
        return inThisPass + passesAfter * PassLength(cue, fileDuration);
    }

    /// <summary>Сколько уже играет (для «∞ 12:41»): все прошедшие проходы + текущий.</summary>
    public static double Elapsed(AudioCue cue, double fileDuration, double position, int currentPass)
    {
        double start = SegmentStart(cue, fileDuration);
        double inThisPass = Math.Clamp(position, start, SegmentEnd(cue, fileDuration)) - start;
        return (Math.Max(1, currentPass) - 1) * PassLength(cue, fileDuration) + inThisPass;
    }

    private static double SegmentStart(AudioCue cue, double fileDuration) =>
        Math.Clamp(cue.TrimIn, 0, fileDuration);

    private static double SegmentEnd(AudioCue cue, double fileDuration) =>
        Math.Clamp(cue.TrimOut ?? fileDuration, 0, fileDuration);
}
