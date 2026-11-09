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

    /// <summary>
    /// Для полей ввода: точность до миллисекунд, без лишних нулей — «0», «2.25», «1:05.5».
    /// </summary>
    public static string Editable(double seconds)
    {
        long ms = (long)Math.Round(Math.Max(0, seconds) * 1000, MidpointRounding.AwayFromZero);
        long minutes = ms / 60000;
        double rest = (ms % 60000) / 1000.0;

        return minutes == 0
            ? rest.ToString("0.###", CultureInfo.InvariantCulture)
            : minutes + ":" + rest.ToString("00.###", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Разбор времени, введенного руками: «65», «65.5», «65,5», «1:05», «1:05.5».
    /// Отрицательное время и мусор не принимаются.
    /// </summary>
    public static bool TryParse(string? text, out double seconds)
    {
        seconds = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string[] parts = text.Trim().Replace(',', '.').Split(':');
        if (parts.Length > 2)
            return false;

        if (!double.TryParse(parts[^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double secondsPart))
            return false;

        int minutes = 0;
        if (parts.Length == 2)
        {
            // В записи «м:сс» секунд меньше минуты
            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out minutes) || secondsPart >= 60)
                return false;
        }

        seconds = minutes * 60 + secondsPart;
        return true;
    }
}
