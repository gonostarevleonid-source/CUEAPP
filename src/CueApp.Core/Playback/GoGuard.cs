namespace CueApp.Core.Playback;

/// <summary>
/// Защита от двойного GO: второе нажатие раньше чем через N мс после принятого игнорируется
/// (дребезг кнопки, нервный палец).
/// </summary>
public sealed class GoGuard
{
    private TimeSpan? _lastAccepted;

    /// <param name="now">Текущее время по любым монотонным часам (например, Stopwatch.Elapsed).</param>
    /// <param name="protectionMs">Окно защиты из настроек; 0 — защита выключена.</param>
    public bool TryAccept(TimeSpan now, int protectionMs)
    {
        if (_lastAccepted is { } last && now - last < TimeSpan.FromMilliseconds(protectionMs))
            return false;

        _lastAccepted = now;
        return true;
    }
}
