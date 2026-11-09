namespace CueApp.Core.Time;

/// <summary>
/// Генератор аудиосигнала LTC: кадр за кадром, бифазное кодирование (biphase mark).
/// В начале каждого бита сигнал меняет знак; у единицы — еще один перепад посередине бита.
/// Работает с любой частотой дискретизации: длина бита может быть дробной (29.97 fps),
/// дробная часть накапливается, поэтому сигнал не уплывает от реального времени.
/// </summary>
public sealed class LtcGenerator
{
    private readonly FrameRate _rate;
    private readonly double _samplesPerBit;
    private readonly float _amplitude;

    private long _frameNumber;
    private bool[] _bits = new bool[LtcFrame.BitCount];
    private int _bitIndex;
    private double _phase;        // сколько сэмплов прошло от начала текущего бита
    private bool _midDone;        // перепад в середине «единицы» уже сделан
    private float _level = 1;     // текущая полярность сигнала: +1 или −1

    /// <param name="amplitude">Пиковый уровень 0…1 (1 = 0 dBFS).</param>
    public LtcGenerator(FrameRate rate, int sampleRate, float amplitude)
    {
        _rate = rate;
        _samplesPerBit = sampleRate / (rate.Actual() * LtcFrame.BitCount);
        _amplitude = amplitude;
    }

    /// <summary>Таймкод кадра, который сейчас генерируется.</summary>
    public Timecode Current => Timecode.FromFrameNumber(_frameNumber, _rate);

    /// <summary>Начать заново с кадра tc (запуск трека, перемотка).</summary>
    public void Start(Timecode tc)
    {
        _frameNumber = tc.ToFrameNumber(_rate);
        _bits = LtcFrame.Encode(tc, _rate);
        _bitIndex = 0;
        _phase = 0;
        _midDone = false;
        _level = -_level; // перепад в самом начале первого бита
    }

    /// <summary>Заполнить буфер следующими сэмплами LTC (моно).</summary>
    public void Fill(Span<float> buffer)
    {
        double half = _samplesPerBit / 2;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (_phase >= _samplesPerBit)
            {
                _phase -= _samplesPerBit;
                NextBit();
                _level = -_level; // перепад в начале каждого бита
            }
            else if (!_midDone && _bits[_bitIndex] && _phase >= half)
            {
                _level = -_level; // у «единицы» — перепад посередине
                _midDone = true;
            }

            buffer[i] = _level * _amplitude;
            _phase += 1;
        }
    }

    private void NextBit()
    {
        _midDone = false;
        if (++_bitIndex < LtcFrame.BitCount)
            return;

        // Кадр кончился — следующий таймкод
        _bitIndex = 0;
        _frameNumber++;
        _bits = LtcFrame.Encode(Timecode.FromFrameNumber(_frameNumber, _rate), _rate);
    }
}
