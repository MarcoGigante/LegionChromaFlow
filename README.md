<div align="center">

# LegionChromaFlow

**Your desktop wallpaper, flowing across your Lenovo Legion keyboard.**<br>
Dynamic per-key RGB lighting with a Razer-Chroma-style control panel — no Lenovo Vantage, no cloud, no dependencies.

[![build](https://img.shields.io/github/actions/workflow/status/ProgMarc/LegionChromaFlow/build.yml?branch=main&style=flat-square&label=build)](https://github.com/ProgMarc/LegionChromaFlow/actions)
[![release](https://img.shields.io/github/v/release/ProgMarc/LegionChromaFlow?style=flat-square)](https://github.com/ProgMarc/LegionChromaFlow/releases)
[![license](https://img.shields.io/badge/license-GPL--3.0-44d62c?style=flat-square)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8%2B-512bd4?style=flat-square)
![dependencies](https://img.shields.io/badge/dependencies-0-44d62c?style=flat-square)

[Features](#features) · [Quick start](#quick-start) · [Control panel](#control-panel) · [Wave styles](#wave-styles) · [Configuration](#configuration) · [Troubleshooting](#troubleshooting) · [How it works](#how-it-works)

**English** · [Italiano](README.it.md)

<img src="docs/screenshot-live.png" alt="LegionChromaFlow control panel with live keyboard preview" width="860">

</div>

---

## Features

- 🖼️ **Wallpaper-driven lighting** — your desktop image slowly drifts and sways across the keys, with small random ripples of light.
- 🪟 **Active-window tint** — switch window and its colors sweep out **from the center of the keyboard to the edges**, then stay as long as that window is in focus.
- 🎯 **Only real colors count** — black, gray and white pixels are ignored. A black window leaves the keyboard on your wallpaper.
- ⚡ **Two wave styles** — a soft, glowing *Smooth* dissolve, or a crisp *Barrier*: a thin band of dark keys sweeps across and the new colors appear right behind it.
- 🧈 **Buttery transitions** — a tunable “follow” time so the lights glide toward window colors instead of stepping.
- 🎛️ **Razer-style control panel** — dark UI, live keyboard preview, tooltips on every option; changes apply instantly and are saved for you.
- 🔔 **System-tray app** — left-click to show/hide the panel, right-click for quick style switch, wave preview and *Exit*.
- 🔌 **Zero dependencies** — talks to the keyboard through `hid.dll`, reads the wallpaper with GDI+, captures windows with GDI. Nothing to install except .NET.
- 🔁 **Self-healing** — reconnects automatically after sleep/resume or when the keyboard re-enumerates.
- 🔒 **Offline and private** — no network code at all. Window pixels are sampled in memory and never stored.

<div align="center">
<img src="docs/screenshot-window.png" alt="Window-color settings page" width="760">
</div>

## Quick start

**Requirements:** Windows 10/11 · [.NET 8 Desktop Runtime or newer](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.DesktopRuntime.8`) · a Lenovo Legion laptop with the **Spectrum per-key RGB** keyboard.

1. **Download** the latest zip from [Releases](../../releases) and extract it (for example to `C:\LegionChromaFlow`).
   Prefer building it yourself? Install the .NET 8 SDK and run `build.bat`.
2. **Set a profile in Lenovo Vantage** that uses the *Legion Aurora Sync* effect, press **Apply**, then **close Vantage completely** (also from the tray). Vantage and this app must not write to the keyboard at the same time.
3. **Check your hardware**, in this order:

   | Step | Command | What it does |
   |---|---|---|
   | 1 | `probe.bat` | Finds the keyboard, reads the key map and profile. **Does not change the lights.** |
   | 2 | `test.bat` | Every key goes red → green → blue for a few seconds, then restores your profile. |
   | 3 | `OpenPanel.vbs` | Starts the effect and opens the control panel. |

4. Happy with it? Tick **Start with Windows** in the panel. `run-hidden.vbs` launches it silently to the tray; `stop.bat` (or *Exit* in the tray menu) stops it and restores your lighting profile.

> **Tip:** Windows 11 hides new tray icons behind the `^` arrow. Drag the LegionChromaFlow icon onto the taskbar to keep it always visible.

## Control panel

| Section | What you can tune |
|---|---|
| **Lights** | Live preview of what is on the keyboard, wave style cards, **Preview wave** button |
| **Wave** | Duration (seconds), barrier width, front softness, glow |
| **Window** | Influence, **color follow time**, color/brightness thresholds, sampling rate |
| **Look** | Brightness, saturation, gamma |
| **Desktop** | Wallpaper drift speed, shimmer, random ripples, frames per second |

Hover the round **ⓘ** next to any option to see exactly what it does and what low/high values mean. Everything is written back to `config/config.json` (comments preserved) as you drag.

## Wave styles

| | **Smooth** | **Barrier** |
|---|---|---|
| Look | New colors melt in from the center with a soft glow | A thin band of switched-off keys sweeps outward; new colors appear immediately behind it |
| Feel | Fluid, ambient | Crisp, “scanner” |
| Specific options | Front softness, glow | Barrier width |

Switch any time from the panel or the tray menu — it applies instantly.

## Configuration

Edit `config/config.json` (it is commented) or use the panel. The most useful keys:

| Key | Effect |
|---|---|
| `WaveStyle` | `"smooth"` or `"barrier"` |
| `WindowInfluence` | `0` ignore the window · `0.28` subtle tint · `1` full takeover |
| `WindowFollowSeconds` | How smoothly lights chase window colors. `0` instant (can look steppy) · `1.2` default · `3+` very smooth, slower to react |
| `WaveSeconds` | Seconds for the wave to travel from the center to the edges |
| `BarrierWidth` | Thickness of the dark band (Barrier style) |
| `DriftSpeed` | Wallpaper drift speed (`0` = still) |
| `Saturation`, `Gamma`, `Brightness` | Color rendition on the LEDs |
| `ChromaThreshold`, `ValueThreshold` | Raise to ignore pastel / dark window pixels |
| `WallpaperOverride` | Use another image instead of the Windows wallpaper |
| `Profile` | Spectrum profile to drive (`0` = the one currently active) |

### Command line

```text
dotnet bin\LegionChromaFlow.dll <mode>

  gui [tray]   control panel + tray icon (tray = start hidden)
  run          effect only, console window, Ctrl+C to stop
  probe        diagnostics, never changes the lights
  test         red / green / blue test, then restores the profile
  selftest     logic checks, no keyboard needed
  stop         stop the running instance
```

## Troubleshooting

- **Keyboard not found** — run `probe.bat` from a prompt started **as administrator** and read `logs\legionchromaflow.log`.
- **Flickering or colors not changing** — Lenovo Vantage (or another Legion tool) is still running. `probe` lists the ones it detects.
- **Keyboard stuck on one color after a crash** — switch profile with `Fn + Space`, or start the app once and use *Exit*.
- **Not smooth enough** — raise *Color follow time* in the **Window** section.
- **After sleep/resume** the app reconnects by itself.

## How it works

1. **Protocol** — the Spectrum keyboard accepts 960-byte HID feature reports. The app switches the keyboard into “Aurora” mode and streams a per-key bitmap (`key code + R,G,B`) at 5–40 fps. When it exits it hands the lights back to your saved profile.
2. **Wallpaper field** — the wallpaper is downscaled to a small color grid and sampled with a slowly wandering offset, zoom and warp for the drifting look.
3. **Window field** — the foreground window is captured at tiny resolution; pixels pass a chroma/brightness filter so only real colors tint the keyboard.
4. **Wave** — on window change, the old and new influence are blended by a front expanding from the keyboard’s center (Smooth) or separated by a dark band (Barrier). Between changes the target is low-pass filtered by *Color follow time*.

The project is plain C# / .NET 8 (WinForms for the panel), with no NuGet packages. Source lives in [`src/`](src); `selftest` verifies the effect logic without hardware.

## Compatibility and disclaimer

- Developed and tested on the **Legion 7 16IRX9**. Other Legion models using the same Spectrum keyboard *should* work but are untested — please [open an issue](../../issues) with your `probe` output.
- This is an **unofficial** project. It is not affiliated with, endorsed by, or supported by Lenovo or Razer. All trademarks belong to their owners.
- It writes HID feature reports to your keyboard controller using the same commands as Lenovo’s own software. **Use at your own risk**; see the [license](LICENSE) for the warranty disclaimer.
- Not compatible with Razer Chroma (there is no supported way to hook into it without a Razer App Id).

## Security and trust

- No network access, no telemetry, no auto-update.
- Official builds are produced only by the [release workflow](.github/workflows/release.yml) from a tagged commit and ship with a `SHA256SUMS.txt`. Binaries from anywhere else are not official — see [SECURITY.md](SECURITY.md).
- Forks are welcome under the GPL; only the maintainer can change this repository.

## Credits

- The keyboard protocol was derived from **Lenovo Legion Toolkit** by the LenovoLegionToolkit-Team (GPL-3.0). This project is therefore released under the same license.
- UI inspired by the dark, neon-green look of Razer Synapse / Chroma Studio.

## License

[GPL-3.0](LICENSE). If you redistribute this program, you must keep the same license and make the source available.
