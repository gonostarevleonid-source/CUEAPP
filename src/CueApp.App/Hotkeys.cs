using System;
using Avalonia.Input;

namespace CueApp.App;

/// <summary>Горячие клавиши меню: на Windows — Ctrl, на Mac — Cmd.</summary>
public static class Hotkeys
{
    private static readonly KeyModifiers Command =
        OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;

    public static KeyGesture NewShow { get; } = new(Key.N, Command);
    public static KeyGesture OpenShow { get; } = new(Key.O, Command);
    public static KeyGesture SaveShow { get; } = new(Key.S, Command);
    public static KeyGesture SaveShowAs { get; } = new(Key.S, Command | KeyModifiers.Shift);
}
