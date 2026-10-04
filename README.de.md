<div align="center">

# LegionChromaFlow

**Ihr Desktop-Hintergrund, der über die Tastatur Ihres Lenovo Legion fließt.**<br>
Dynamische RGB-Beleuchtung pro Taste mit einem Bedienfeld im Stil von Razer Chroma — ohne Lenovo Vantage, ohne Cloud, ohne Abhängigkeiten.

[![build](https://img.shields.io/github/actions/workflow/status/MarcoGigante/LegionChromaFlow/build.yml?branch=main&style=flat-square&label=build)](https://github.com/MarcoGigante/LegionChromaFlow/actions)
[![release](https://img.shields.io/github/v/release/MarcoGigante/LegionChromaFlow?style=flat-square)](https://github.com/MarcoGigante/LegionChromaFlow/releases)
[![license](https://img.shields.io/badge/license-GPL--3.0-44d62c?style=flat-square)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8%2B-512bd4?style=flat-square)

[English](README.md) · [Italiano](README.it.md) · [Español](README.es.md) · [Français](README.fr.md) · **Deutsch** · [Português](README.pt.md) · [简体中文](README.zh.md)

<img src="docs/screenshot-live.png" alt="LegionChromaFlow-Bedienfeld mit Live-Vorschau der Tastatur" width="860">

</div>

---

## Funktionen

- 🖼️ **Beleuchtung aus dem Hintergrundbild** — das Desktop-Bild wandert und schwingt langsam über die Tasten, ergänzt durch kleine zufällige Lichtwellen.
- 🪟 **Farbton des aktiven Fensters** — beim Fensterwechsel breiten sich dessen Farben **von der Tastaturmitte nach außen** aus und bleiben, solange das Fenster im Fokus ist.
- 🎯 **Nur echte Farben zählen** — schwarze, graue und weiße Pixel werden ignoriert. Ein schwarzes Fenster lässt die Tastatur bei den Hintergrundfarben.
- ⚡ **Zwei Wellenstile** — *Smooth*, ein weiches Überblenden mit Leuchten, oder *Barrier*, ein dünnes Band ausgeschalteter Tasten, das über die Tastatur wandert, mit den neuen Farben direkt dahinter.
- 🧈 **Weiche Übergänge** — eine einstellbare „Folgezeit“, damit die Lichter zu den Fensterfarben gleiten statt zu springen.
- 🎛️ **Bedienfeld im Razer-Stil** — dunkle Oberfläche, Live-Vorschau der Tastatur, Erklärung zu jeder Option; Änderungen wirken sofort und werden automatisch gespeichert.
- 🌍 **7 Sprachen** — Englisch, Italienisch, Spanisch, Französisch, Deutsch, Portugiesisch und vereinfachtes Chinesisch, im Bedienfeld wählbar.
- 🔔 **Symbol im Infobereich** — Linksklick blendet das Bedienfeld ein/aus, Rechtsklick öffnet das Schnellmenü.
- 🔌 **Keine Abhängigkeiten** — nutzt `hid.dll`, GDI+ und GDI. Nur .NET wird benötigt.
- 🔒 **Offline und privat** — kein Netzwerkcode. Fensterpixel werden im Speicher gelesen und nie gespeichert.

<div align="center">
<img src="docs/screenshot-window.png" alt="Einstellungsseite für die Fensterfarben" width="760">
</div>

## Schnellstart

**Voraussetzungen:** Windows 10/11 · [.NET 8 Desktop Runtime oder neuer](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.DesktopRuntime.8`) · ein Lenovo-Legion-Laptop mit **Spectrum-RGB-Tastatur (pro Taste)**.

1. **Laden Sie** das neueste Zip von [Releases](../../releases) herunter und entpacken Sie es (z. B. nach `C:\LegionChromaFlow`). Lieber selbst kompilieren? Installieren Sie das .NET-8-SDK und führen Sie `build.bat` aus.
2. **In Lenovo Vantage** ein Profil mit dem Effekt *Legion Aurora Sync* wählen, auf **Übernehmen** klicken und Vantage dann **vollständig beenden** (auch im Infobereich). Vantage und dieses Programm dürfen nicht gleichzeitig auf die Tastatur schreiben.
3. **Hardware prüfen**, in dieser Reihenfolge:

   | Schritt | Befehl | Was er tut |
   |---|---|---|
   | 1 | `probe.bat` | Findet die Tastatur, liest Tastenbelegung und Profil. **Ändert die Beleuchtung nicht.** |
   | 2 | `test.bat` | Alle Tasten werden einige Sekunden rot → grün → blau, danach wird Ihr Profil wiederhergestellt. |
   | 3 | `OpenPanel.vbs` | Startet den Effekt und öffnet das Bedienfeld. |

4. Zufrieden? Aktivieren Sie im Bedienfeld **Mit Windows starten**. `run-hidden.vbs` startet es still im Infobereich; `stop.bat` (oder *Beenden* im Symbolmenü) stoppt es und stellt Ihr Beleuchtungsprofil wieder her.

> **Tipp:** Windows 11 versteckt neue Symbole hinter dem Pfeil `^`. Ziehen Sie das LegionChromaFlow-Symbol auf die Taskleiste, damit es immer sichtbar bleibt.

## Bedienfeld

| Bereich | Was sich einstellen lässt |
|---|---|
| **Licht** | Live-Vorschau der Tastatur, Stilkarten, Schaltfläche **Welle testen** |
| **Welle** | Dauer (Sekunden), Barrierendicke, Weichheit der Front, Leuchten |
| **Fenster** | Einfluss, **Farbglättung**, Farb- und Helligkeitsschwellen, Leseintervall |
| **Aussehen** | Helligkeit, Sättigung, Gamma |
| **Desktop** | Hintergrundtempo, Tastenflimmern, zufällige Lichtwellen, Bilder pro Sekunde |

Fahren Sie mit der Maus über das runde **ⓘ** neben einer Option, um zu sehen, was sie bewirkt und was niedrige bzw. hohe Werte bedeuten. Die Sprache wählen Sie oben rechts. Alles wird beim Ziehen in `config/config.json` zurückgeschrieben (Kommentare bleiben erhalten).

## Wellenstile

| | **Smooth** | **Barrier** |
|---|---|---|
| Optik | Neue Farben blenden von der Mitte aus mit weichem Leuchten über | Ein dünnes Band ausgeschalteter Tasten wandert nach außen; dahinter erscheinen sofort die neuen Farben |
| Eindruck | Fließend, ambient | Scharf, „Scanner“ |
| Eigene Optionen | Weichheit der Front, Leuchten | Barrierendicke |

## Fehlerbehebung

- **Tastatur nicht gefunden** — führen Sie `probe.bat` in einer Eingabeaufforderung **als Administrator** aus und lesen Sie `logs\legionchromaflow.log`.
- **Flackern oder Farben ändern sich nicht** — Lenovo Vantage (oder ein anderes Legion-Tool) läuft noch. `probe` listet erkannte Programme auf.
- **Tastatur hängt nach einem Absturz auf einer Farbe** — Profil mit `Fn + Leertaste` wechseln oder die App einmal starten und *Beenden* wählen.
- **Nicht flüssig genug** — erhöhen Sie *Farbglättung* im Bereich **Fenster**.
- **Nach Standby/Fortsetzen** verbindet sich das Programm selbstständig neu.

Technische Details, Kommandozeilen-Referenz und die vollständige Konfigurationstabelle: siehe die [englische README](README.md).

## Kompatibilität und Haftungsausschluss

- Entwickelt und getestet auf dem **Legion 7 16IRX9**. Andere Legion-Modelle mit derselben Spectrum-Tastatur *sollten* funktionieren, sind aber ungetestet — bitte [öffnen Sie ein Issue](../../issues) mit der Ausgabe von `probe`.
- **Inoffizielles** Projekt, weder mit Lenovo oder Razer verbunden noch von ihnen unterstützt. Alle Marken gehören ihren jeweiligen Inhabern.
- Es sendet dieselben Befehle an die Tastatur wie Lenovos eigene Software. **Nutzung auf eigene Gefahr**; den Gewährleistungsausschluss finden Sie in der [Lizenz](LICENSE).
- Nicht mit Razer Chroma kompatibel (ohne Razer-App-ID gibt es keinen offiziellen Weg zur Anbindung).

## Sicherheit und Vertrauen

- Kein Netzwerkzugriff, keine Telemetrie, keine automatischen Updates.
- Offizielle Builds entstehen ausschließlich im [Release-Workflow](.github/workflows/release.yml) aus einem Tag und enthalten eine `SHA256SUMS.txt`. Binärdateien aus anderen Quellen sind nicht offiziell — siehe [SECURITY.md](SECURITY.md).
- Forks sind unter der GPL willkommen; nur der Maintainer kann dieses Repository ändern.

## Danksagung und Lizenz

- Das Tastaturprotokoll wurde aus **Lenovo Legion Toolkit** des LenovoLegionToolkit-Teams (GPL-3.0) abgeleitet; deshalb steht dieses Projekt unter derselben Lizenz.
- Oberfläche inspiriert vom dunklen, neongrünen Look von Razer Synapse / Chroma Studio.
- Lizenz: [GPL-3.0](LICENSE). Wenn Sie das Programm weitergeben, müssen Sie dieselbe Lizenz beibehalten und den Quellcode verfügbar machen.
