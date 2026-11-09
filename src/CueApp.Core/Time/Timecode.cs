using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CueApp.Core.Time;

/// <summary>Частота таймкода (раздел 5 ТЗ).</summary>
public enum FrameRate
{
    Fps24,
    Fps25,

    /// <summary>29.97 drop-frame: номера кадров 0 и 1 пропускаются каждую минуту, кроме каждой десятой.</summary>
    Fps2997Df,

    /// <summary>29.97 non-drop: номера кадров подряд, но идут чуть медленнее реального времени.</summary>
    Fps2997Ndf,

    Fps30,
}

public static class FrameRates
{
    /// <summary>Сколько кадров в «секунде» таймкода (номер кадра 0…N-1).</summary>
    public static int Nominal(this FrameRate rate) => rate switch
    {
        FrameRate.Fps24 => 24,
        FrameRate.Fps25 => 25,
        _ => 30,
    };

    /// <summary>Сколько кадров в реальной секунде.</summary>
    public static double Actual(this FrameRate rate) => rate switch
    {
        FrameRate.Fps2997Df or FrameRate.Fps2997Ndf => 30000.0 / 1001.0,
        _ => rate.Nominal(),
    };

    public static bool IsDropFrame(this FrameRate rate) => rate == FrameRate.Fps2997Df;

    /// <summary>Подпись для интерфейса: «25», «29.97 DF».</summary>
    public static string Label(this FrameRate rate) => rate switch
    {
        FrameRate.Fps24 => "24",
        FrameRate.Fps25 => "25",
        FrameRate.Fps2997Df => "29.97 DF",
        FrameRate.Fps2997Ndf => "29.97 NDF",
        _ => "30",
    };

    public static IReadOnlyList<FrameRate> All { get; } =
        [FrameRate.Fps24, FrameRate.Fps25, FrameRate.Fps2997Df, FrameRate.Fps2997Ndf, FrameRate.Fps30];
}

/// <summary>
/// Значение таймкода ЧЧ:ММ:СС:КК. Само по себе не знает частоту — все пересчеты принимают FrameRate.
/// В файле шоу хранится строкой «01:00:00:00».
/// </summary>
[JsonConverter(typeof(TimecodeJsonConverter))]
public readonly record struct Timecode(int Hours, int Minutes, int Seconds, int Frames)
{
    private const int SecondsPerDay = 24 * 3600;

    // Drop-frame 29.97: кадров в 10 минутах и в «минуте с пропуском»
    private const long DfFramesPer10Min = 17982;
    private const long DfFramesPerMin = 1798;

    /// <summary>Номер кадра от 00:00:00:00 (для drop-frame — с учетом пропущенных номеров).</summary>
    public long ToFrameNumber(FrameRate rate)
    {
        long labelFrames = ((Hours * 3600L) + (Minutes * 60L) + Seconds) * rate.Nominal() + Frames;
        if (!rate.IsDropFrame())
            return labelFrames;

        long totalMinutes = Hours * 60L + Minutes;
        return labelFrames - 2 * (totalMinutes - totalMinutes / 10);
    }

    public static Timecode FromFrameNumber(long frameNumber, FrameRate rate)
    {
        int nominal = rate.Nominal();
        long framesPerDay = rate.IsDropFrame() ? DfFramesPer10Min * 6 * 24 : (long)nominal * SecondsPerDay;
        frameNumber = ((frameNumber % framesPerDay) + framesPerDay) % framesPerDay; // через полночь — по кругу

        if (rate.IsDropFrame())
        {
            // Вернуть пропущенные номера кадров, чтобы получить «номер по подписи»
            long tens = frameNumber / DfFramesPer10Min;
            long rest = frameNumber % DfFramesPer10Min;
            frameNumber += 18 * tens + (rest < 2 ? 0 : 2 * ((rest - 2) / DfFramesPerMin));
        }

        int frames = (int)(frameNumber % nominal);
        long totalSeconds = frameNumber / nominal;
        return new Timecode((int)(totalSeconds / 3600), (int)(totalSeconds / 60 % 60), (int)(totalSeconds % 60), frames);
    }

    /// <summary>Реальное время от 00:00:00:00 в секундах.</summary>
    public double ToSeconds(FrameRate rate) => ToFrameNumber(rate) / rate.Actual();

    /// <summary>Таймкод кадра, который идет в момент seconds (реальное время от 00:00:00:00).</summary>
    public static Timecode FromSeconds(double seconds, FrameRate rate) =>
        FromFrameNumber((long)Math.Floor(seconds * rate.Actual() + 1e-6), rate);

    /// <summary>Время «по подписи» в секундах (ЧЧ·3600 + ММ·60 + СС), без кадров — для округления стартов.</summary>
    public int LabelSeconds => Hours * 3600 + Minutes * 60 + Seconds;

    public static Timecode FromLabelSeconds(int seconds)
    {
        seconds = ((seconds % SecondsPerDay) + SecondsPerDay) % SecondsPerDay;
        return new Timecode(seconds / 3600, seconds / 60 % 60, seconds % 60, 0);
    }

    /// <summary>Подогнать под частоту: кадр не больше последнего; в drop-frame — не пропущенный номер.</summary>
    public Timecode Normalize(FrameRate rate)
    {
        int frames = Math.Min(Frames, rate.Nominal() - 1);
        if (IsDroppedLabel(Seconds, Minutes, frames, rate))
            frames = 2;
        return this with { Frames = frames };
    }

    /// <summary>«01:00:00:00»; для drop-frame перед кадрами «;» — так принято писать DF.</summary>
    public string ToString(FrameRate rate) =>
        $"{Hours:00}:{Minutes:00}:{Seconds:00}{(rate.IsDropFrame() ? ';' : ':')}{Frames:00}";

    public override string ToString() => $"{Hours:00}:{Minutes:00}:{Seconds:00}:{Frames:00}";

    /// <summary>
    /// Разбор «01:12:00:00» (разделители «:», «;» или «.»). Проверяет поля по частоте:
    /// кадр меньше номинала, в drop-frame нет пропущенных номеров.
    /// </summary>
    public static bool TryParse(string? text, FrameRate rate, out Timecode timecode)
    {
        timecode = default;
        string[] parts = (text ?? "").Trim().Split(':', ';', '.');
        if (parts.Length != 4)
            return false;

        var numbers = new int[4];
        for (int i = 0; i < 4; i++)
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                return false;

        var (h, m, s, f) = (numbers[0], numbers[1], numbers[2], numbers[3]);
        if (h > 23 || m > 59 || s > 59 || f >= rate.Nominal() || IsDroppedLabel(s, m, f, rate))
            return false;

        timecode = new Timecode(h, m, s, f);
        return true;
    }

    /// <summary>В drop-frame номера кадров 0 и 1 в начале каждой минуты (кроме 0, 10, 20…) не существуют.</summary>
    private static bool IsDroppedLabel(int seconds, int minutes, int frames, FrameRate rate) =>
        rate.IsDropFrame() && seconds == 0 && frames < 2 && minutes % 10 != 0;
}

/// <summary>Таймкод в файле шоу — строкой «01:00:00:00».</summary>
public sealed class TimecodeJsonConverter : JsonConverter<Timecode>
{
    public override Timecode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Без частоты проверяем только формат; 30 — самый «широкий» номинал кадров
        string? text = reader.GetString();
        return Timecode.TryParse(text, FrameRate.Fps30, out var tc)
            ? tc
            : throw new JsonException($"Неверный таймкод «{text}»");
    }

    public override void Write(Utf8JsonWriter writer, Timecode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
