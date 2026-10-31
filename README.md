# CUEAPP

Show control для театра и концертов (Windows + macOS): кью-лист в духе QLab,
Timeline cue с маркерами MIDI/OSC/DMX и генерация таймкода LTC/MTC.
Полное ТЗ — в [CLAUDE.md](CLAUDE.md).

## Что нужно установить

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Git

## Сборка и запуск

```bash
dotnet build
dotnet run --project src/CueApp.App
```

Тесты:

```bash
dotnet test
```

## Структура

| Проект | Что внутри |
|---|---|
| `src/CueApp.Core` | Модель шоу, кью, GO, тайминг. Без UI и без конкретных библиотек |
| `src/CueApp.Audio` | Аудио на BASS (реализация `IAudioEngine`) |
| `src/CueApp.Control` | MIDI, MTC, OSC, Art-Net/sACN |
| `src/CueApp.App` | Интерфейс на Avalonia (MVVM) |
| `tests/CueApp.Tests` | Тесты ядра |

## BASS

Нативные библиотеки BASS 2.4.18 (www.un4seen.com) лежат в `src/CueApp.Audio/native/`.
BASS бесплатен для некоммерческого использования; для коммерческого нужна лицензия un4seen.
