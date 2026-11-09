namespace CueApp.Core.Time;

/// <summary>
/// Один кадр LTC (SMPTE 12M): 80 бит, младший бит каждого поля — первым.
/// Числа — в двоично-десятичном виде (BCD): единицы и десятки отдельно.
/// </summary>
public static class LtcFrame
{
    public const int BitCount = 80;

    /// <summary>Синхрослово (биты 64…79): по нему декодер находит конец кадра и направление.</summary>
    public static readonly bool[] SyncWord =
        [false, false, true, true, true, true, true, true, true, true, true, true, true, true, false, true];

    /// <summary>Биты кадра для таймкода tc. User bits и флаги BGF — нули.</summary>
    public static bool[] Encode(Timecode tc, FrameRate rate)
    {
        var bits = new bool[BitCount];

        Put(bits, 0, 4, tc.Frames % 10);
        Put(bits, 8, 2, tc.Frames / 10);
        bits[10] = rate.IsDropFrame();          // флаг drop-frame
        Put(bits, 16, 4, tc.Seconds % 10);
        Put(bits, 24, 3, tc.Seconds / 10);
        Put(bits, 32, 4, tc.Minutes % 10);
        Put(bits, 40, 3, tc.Minutes / 10);
        Put(bits, 48, 4, tc.Hours % 10);
        Put(bits, 56, 2, tc.Hours / 10);
        SyncWord.CopyTo(bits, 64);

        // Бит коррекции полярности: число единиц в кадре должно быть четным,
        // тогда каждый кадр начинается с той же полярности сигнала
        int polarityBit = PolarityBitIndex(rate);
        bits[polarityBit] = bits.Count(b => b) % 2 == 1;
        return bits;
    }

    /// <summary>Для 25 fps бит коррекции полярности — 59, для 24/30 — 27 (так в стандарте).</summary>
    public static int PolarityBitIndex(FrameRate rate) => rate == FrameRate.Fps25 ? 59 : 27;

    /// <summary>Разобрать 80 бит обратно в таймкод (нужно для проверки; позже — для LTC-входа).</summary>
    public static Timecode Decode(bool[] bits) => new(
        Hours: Get(bits, 48, 4) + 10 * Get(bits, 56, 2),
        Minutes: Get(bits, 32, 4) + 10 * Get(bits, 40, 3),
        Seconds: Get(bits, 16, 4) + 10 * Get(bits, 24, 3),
        Frames: Get(bits, 0, 4) + 10 * Get(bits, 8, 2));

    private static void Put(bool[] bits, int start, int count, int value)
    {
        for (int i = 0; i < count; i++)
            bits[start + i] = ((value >> i) & 1) == 1;
    }

    private static int Get(bool[] bits, int start, int count)
    {
        int value = 0;
        for (int i = 0; i < count; i++)
            if (bits[start + i])
                value |= 1 << i;
        return value;
    }
}
