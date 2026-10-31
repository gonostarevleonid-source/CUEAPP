using System.Globalization;

namespace CueApp.Core;

/// <summary>Форматирование времени для показа (раздел 6 ТЗ).</summary>
public static class TimeFormat
{
    /// <summary>
    /// Формат «м:сс.д» (по умолчанию): 5.0, 12.5, 1:05.0, 12:41.3.
    /// До минуты — только секунды, как в макете.
    /// </summary>
    public static string MinSecTenths(double seconds)
    {
        // Округляем до десятых один раз, чтобы 59.96 стало 1:00.0, а не 0:60.0
        long tenths = (long)Math.Round(Math.Abs(seconds) * 10, MidpointRounding.AwayFromZero);
        string sign = seconds < 0 && tenths > 0 ? "−" : "";
        long minutes = tenths / 600;
        double rest = (tenths % 600) / 10.0;

        return minutes == 0
            ? sign + rest.ToString("0.0", CultureInfo.InvariantCulture)
            : sign + minutes + ":" + rest.ToString("00.0", CultureInfo.InvariantCulture);
    }
}
