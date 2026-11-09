using CueApp.Core.Time;
using TC = CueApp.Core.Time.Timecode;

namespace CueApp.Tests;

public class LtcTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void Frame_EncodesAndDecodesFields()
    {
        var tc = new TC(23, 59, 45, 12);

        bool[] bits = LtcFrame.Encode(tc, FrameRate.Fps25);

        Assert.Equal(80, bits.Length);
        Assert.Equal(tc, LtcFrame.Decode(bits));
        Assert.Equal(LtcFrame.SyncWord, bits[64..80]);
    }

    [Theory]
    [InlineData(FrameRate.Fps24)]
    [InlineData(FrameRate.Fps25)]
    [InlineData(FrameRate.Fps2997Df)]
    [InlineData(FrameRate.Fps2997Ndf)]
    [InlineData(FrameRate.Fps30)]
    public void Frame_HasEvenNumberOfOnes(FrameRate rate)
    {
        // Иначе каждый второй кадр начинался бы с противоположной полярностью
        for (int f = 0; f < rate.Nominal(); f++)
        {
            var tc = new TC(1, 2, 3, f).Normalize(rate);
            Assert.Equal(0, LtcFrame.Encode(tc, rate).Count(b => b) % 2);
        }
    }

    [Fact]
    public void Frame_DropFrameFlag_OnlyForDf()
    {
        Assert.True(LtcFrame.Encode(new TC(1, 0, 0, 2), FrameRate.Fps2997Df)[10]);
        Assert.False(LtcFrame.Encode(new TC(1, 0, 0, 2), FrameRate.Fps2997Ndf)[10]);
        Assert.False(LtcFrame.Encode(new TC(1, 0, 0, 2), FrameRate.Fps25)[10]);
    }

    [Theory]
    [InlineData(FrameRate.Fps24)]
    [InlineData(FrameRate.Fps25)]
    [InlineData(FrameRate.Fps2997Df)]
    [InlineData(FrameRate.Fps2997Ndf)]
    [InlineData(FrameRate.Fps30)]
    public void Signal_DecodesBackToConsecutiveTimecodes(FrameRate rate)
    {
        // Старт за 1 секунду до минуты — в drop-frame проверим пропуск кадров 0 и 1
        var start = new TC(1, 0, 59, 0);
        float[] signal = Generate(rate, start, seconds: 3);

        var decoded = TestLtcDecoder.Decode(signal, SampleRate, rate);

        Assert.True(decoded.Count >= 3 * (int)rate.Actual() - 2, $"декодировано кадров: {decoded.Count}");
        long first = start.ToFrameNumber(rate);
        for (int i = 0; i < decoded.Count; i++)
            Assert.Equal(TC.FromFrameNumber(first + i, rate), decoded[i]);
    }

    [Fact]
    public void Signal_DropFrame_JumpsOverFrames0And1()
    {
        float[] signal = Generate(FrameRate.Fps2997Df, new TC(1, 0, 59, 28), seconds: 0.2);

        var decoded = TestLtcDecoder.Decode(signal, SampleRate, FrameRate.Fps2997Df);

        Assert.Equal([new TC(1, 0, 59, 28), new TC(1, 0, 59, 29), new TC(1, 1, 0, 2), new TC(1, 1, 0, 3)], decoded.Take(4));
    }

    [Theory]
    [InlineData(FrameRate.Fps25, 250)]
    [InlineData(FrameRate.Fps2997Ndf, 299)]   // 29.97 кадра в секунду — за 10 с ровно 299 целых кадров
    [InlineData(FrameRate.Fps30, 300)]
    public void Signal_KeepsRealTime(FrameRate rate, int framesIn10Seconds)
    {
        // Длина бита при 29.97 дробная — генератор не должен уплывать.
        // +1 мс: последний бит декодер «видит» только по перепаду, который начинает следующий бит;
        // следующий кадр за 1 мс закончиться не успеет (кадр длится 33–42 мс)
        float[] signal = Generate(rate, new TC(1, 0, 0, 0), seconds: 10.001);

        int complete = TestLtcDecoder.Decode(signal, SampleRate, rate).Count;

        Assert.Equal(framesIn10Seconds, complete);
    }

    [Fact]
    public void Signal_Amplitude_IsAsSet()
    {
        float[] signal = Generate(FrameRate.Fps25, new TC(1, 0, 0, 0), seconds: 0.1, amplitude: 0.25f);

        Assert.Equal(0.25f, signal.Max());
        Assert.Equal(-0.25f, signal.Min());
    }

    [Fact]
    public void Generator_FillInSmallChunks_SameAsOneBigFill()
    {
        // Звуковая карта просит сэмплы кусками разной длины — результат не должен от этого зависеть
        var whole = new LtcGenerator(FrameRate.Fps2997Df, SampleRate, 0.5f);
        whole.Start(new TC(1, 0, 0, 0));
        var a = new float[SampleRate];
        whole.Fill(a);

        var chunked = new LtcGenerator(FrameRate.Fps2997Df, SampleRate, 0.5f);
        chunked.Start(new TC(1, 0, 0, 0));
        var b = new float[SampleRate];
        for (int pos = 0, size = 1; pos < b.Length; pos += size, size = size % 997 + 13)
            chunked.Fill(b.AsSpan(pos, Math.Min(size, b.Length - pos)));

        Assert.Equal(a, b);
    }

    private static float[] Generate(FrameRate rate, TC start, double seconds, float amplitude = 0.5f)
    {
        var generator = new LtcGenerator(rate, SampleRate, amplitude);
        generator.Start(start);
        var buffer = new float[(int)(SampleRate * seconds)];
        generator.Fill(buffer);
        return buffer;
    }
}

/// <summary>
/// Простой декодер LTC — только для тестов: перепады сигнала → биты → кадры по синхрослову.
/// Короткий интервал между перепадами (полбита) — половина «единицы», длинный (целый бит) — «ноль».
/// </summary>
internal static class TestLtcDecoder
{
    public static List<TC> Decode(float[] signal, int sampleRate, FrameRate rate)
    {
        double samplesPerBit = sampleRate / (rate.Actual() * LtcFrame.BitCount);

        // Моменты перепадов; перед первым сэмплом сигнал считаем противоположным (генератор начинает с перепада)
        var edges = new List<int> { 0 };
        for (int i = 1; i < signal.Length; i++)
            if (Math.Sign(signal[i]) != Math.Sign(signal[i - 1]))
                edges.Add(i);

        var bits = new List<bool>();
        for (int i = 1; i < edges.Count; i++)
        {
            int interval = edges[i] - edges[i - 1];
            if (interval > samplesPerBit * 0.75)
            {
                bits.Add(false);
            }
            else if (i + 1 < edges.Count && edges[i + 1] - edges[i] <= samplesPerBit * 0.75)
            {
                bits.Add(true); // два коротких подряд = одна «единица»
                i++;
            }
        }

        var frames = new List<TC>();
        for (int end = 79; end < bits.Count; end++)
        {
            if (!bits.Skip(end - 15).Take(16).SequenceEqual(LtcFrame.SyncWord))
                continue;
            frames.Add(LtcFrame.Decode(bits.Skip(end - 79).Take(80).ToArray()));
        }
        return frames;
    }
}
