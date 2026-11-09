using CueApp.Core.Model;

namespace CueApp.Core.Time;

/// <summary>
/// Стартовый таймкод нового трека (раздел 5 ТЗ): треки не пересекаются по TC.
/// Первый трек — с FirstTrackTimecode; каждый следующий — через N минут после конца
/// предыдущего трека в списке, с округлением вверх до целых M минут.
/// Назначается только при добавлении: существующие треки сами не сдвигаются,
/// иначе съехал бы уже прописанный свет.
/// </summary>
public static class StartTimecodeRule
{
    /// <param name="fileDuration">Длина файла трека в секундах; null — неизвестна (считаем 0).</param>
    public static Timecode Suggest(Show show, TrackCue newTrack, Func<TrackCue, double?> fileDuration)
    {
        var settings = show.Settings;
        TrackCue? previous = show.AllCues()
            .TakeWhile(c => c != newTrack)
            .OfType<TrackCue>()
            .LastOrDefault();

        if (previous is null)
            return settings.FirstTrackTimecode;

        // Конец предыдущего — по времени «на подписи» таймкода: старт + длина ФАЙЛА (TC привязан к файлу)
        double end = previous.StartTimecode.LabelSeconds + (fileDuration(previous) ?? 0);
        double candidate = end + settings.StartTimecodeGapMinutes * 60;

        int step = Math.Max(1, settings.StartTimecodeRoundMinutes) * 60;
        int rounded = (int)Math.Ceiling(candidate / step) * step;
        return Timecode.FromLabelSeconds(rounded);
    }
}
