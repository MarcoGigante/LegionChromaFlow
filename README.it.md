<div align="center">

# LegionChromaFlow

**Lo sfondo del tuo desktop che scorre sulla tastiera del tuo Lenovo Legion.**<br>
Illuminazione RGB dinamica per singolo tasto con un pannello di controllo in stile Razer Chroma — niente Lenovo Vantage, niente cloud, nessuna dipendenza.

[![build](https://img.shields.io/github/actions/workflow/status/MarcoGigante/LegionChromaFlow/build.yml?branch=main&style=flat-square&label=build)](https://github.com/MarcoGigante/LegionChromaFlow/actions)
[![release](https://img.shields.io/github/v/release/MarcoGigante/LegionChromaFlow?style=flat-square)](https://github.com/MarcoGigante/LegionChromaFlow/releases)
[![license](https://img.shields.io/badge/license-GPL--3.0-44d62c?style=flat-square)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8%2B-512bd4?style=flat-square)

[English](README.md) · **Italiano** · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [简体中文](README.zh.md)

<img src="docs/screenshot-live.png" alt="Pannello di controllo di LegionChromaFlow con anteprima dal vivo della tastiera" width="860">

</div>

---

## Caratteristiche

- 🖼️ **Luci guidate dallo sfondo** — l'immagine del desktop scorre e ondeggia lentamente sui tasti, con piccole onde di luce casuali.
- 🪟 **Tinta della finestra attiva** — quando cambi finestra, i suoi colori si propagano **dal centro della tastiera verso i bordi** e restano finché quella finestra è in primo piano.
- 🎯 **Contano solo i colori veri** — pixel neri, grigi e bianchi vengono ignorati. Una finestra nera lascia la tastiera sui colori dello sfondo.
- ⚡ **Due stili di onda** — *Smooth*, una dissolvenza morbida con bagliore, oppure *Barrier*, una sottile fascia di tasti spenti che attraversa la tastiera con i nuovi colori subito dietro.
- 🧈 **Transizioni fluide** — un tempo di "inseguimento" regolabile, così le luci scivolano verso i colori della finestra invece di andare a scatti.
- 🎛️ **Pannello in stile Razer** — interfaccia scura, anteprima dal vivo della tastiera, spiegazioni su ogni opzione; le modifiche si applicano subito e si salvano da sole.
- 🌍 **7 lingue** — inglese, italiano, spagnolo, francese, tedesco, portoghese e cinese semplificato, selezionabili dal pannello.
- 🔔 **Icona nell'area di notifica** — clic sinistro per mostrare/nascondere il pannello, clic destro per il menu rapido.
- 🔌 **Zero dipendenze** — usa `hid.dll`, GDI+ e GDI. Serve solo .NET.
- 🔒 **Offline e privato** — nessun codice di rete. I pixel delle finestre vengono letti in memoria e mai salvati.

<div align="center">
<img src="docs/screenshot-window.png" alt="Pagina delle impostazioni dei colori della finestra" width="760">
</div>

## Avvio rapido

**Requisiti:** Windows 10/11 · [.NET 8 Desktop Runtime o successivo](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.DesktopRuntime.8`) · un portatile Lenovo Legion con tastiera **Spectrum RGB per tasto**.

1. **Scarica** l'ultimo zip da [Releases](../../releases) ed estrailo (per esempio in `C:\LegionChromaFlow`). Preferisci compilarlo? Installa l'SDK .NET 8 ed esegui `build.bat`.
2. **In Lenovo Vantage** seleziona un profilo con l'effetto *Sinc Legion Aurora*, premi **Applica**, poi **chiudi completamente Vantage** (anche dall'area di notifica). Vantage e questo programma non devono scrivere insieme sulla tastiera.
3. **Controlla l'hardware**, in quest'ordine:

   | Passo | Comando | Cosa fa |
   |---|---|---|
   | 1 | `probe.bat` | Trova la tastiera, legge mappa tasti e profilo. **Non cambia le luci.** |
   | 2 | `test.bat` | Tutti i tasti rosso → verde → blu per qualche secondo, poi ripristina il profilo. |
   | 3 | `OpenPanel.vbs` | Avvia l'effetto e apre il pannello di controllo. |

4. Ti piace? Spunta **Avvia con Windows** nel pannello. `run-hidden.vbs` lo avvia in silenzio nell'area di notifica; `stop.bat` (o *Esci* dal menu dell'icona) lo ferma e ripristina il profilo delle luci.

> **Suggerimento:** Windows 11 nasconde le icone nuove dietro la freccia `^`. Trascina l'icona di LegionChromaFlow sulla barra delle applicazioni per averla sempre visibile.

## Pannello di controllo

| Sezione | Cosa puoi regolare |
|---|---|
| **Luci** | Anteprima dal vivo della tastiera, schede degli stili, pulsante **Anteprima onda** |
| **Onda** | Durata (secondi), spessore della barriera, morbidezza del fronte, bagliore |
| **Finestra** | Influenza, **fluidità dei colori**, soglie di colore e luminosità, frequenza di lettura |
| **Aspetto** | Luminosità, saturazione, gamma |
| **Desktop** | Velocità dello sfondo, sfarfallio, onde casuali, fotogrammi al secondo |

Passa il mouse sull'icona tonda **ⓘ** accanto a ogni opzione per vedere cosa fa e cosa significano i valori bassi e alti. In alto a destra scegli la lingua. Tutto viene salvato in `config/config.json` (commenti compresi) mentre trascini.

## Stili di onda

| | **Smooth** | **Barrier** |
|---|---|---|
| Aspetto | I nuovi colori si sciolgono dal centro con un bagliore morbido | Una sottile fascia di tasti spenti si espande; i nuovi colori compaiono subito dietro |
| Sensazione | Fluida, d'atmosfera | Netta, "scanner" |
| Opzioni specifiche | Morbidezza del fronte, bagliore | Spessore della barriera |

## Se qualcosa non va

- **Tastiera non trovata** — esegui `probe.bat` da un prompt avviato **come amministratore** e leggi `logs\legionchromaflow.log`.
- **Colori che sfarfallano o non cambiano** — Lenovo Vantage (o un altro programma Legion) è ancora attivo. `probe` elenca quelli che rileva.
- **Tastiera ferma su un colore dopo un arresto anomalo** — cambia profilo con `Fn + barra spaziatrice`, oppure avvia l'app una volta e usa *Esci*.
- **Non abbastanza fluido** — aumenta *Fluidità dei colori* nella sezione **Finestra**.
- **Dopo sospensione/ripresa** il programma si ricollega da solo.

Dettagli tecnici, riferimento della riga di comando e tabella completa della configurazione: vedi il [README in inglese](README.md).

## Compatibilità e avvertenze

- Sviluppato e provato sul **Legion 7 16IRX9**. Altri Legion con la stessa tastiera Spectrum *dovrebbero* funzionare ma non sono stati provati: [apri una issue](../../issues) con l'output di `probe`.
- Progetto **non ufficiale**, non affiliato né approvato da Lenovo o Razer. I marchi appartengono ai rispettivi proprietari.
- Invia alla tastiera gli stessi comandi del software Lenovo. **Usalo a tuo rischio**; vedi la [licenza](LICENSE) per l'esclusione di garanzia.
- Non è compatibile con Razer Chroma (non esiste un modo ufficiale per collegarsi senza un App Id Razer).

## Sicurezza e fiducia

- Nessun accesso alla rete, nessuna telemetria, nessun aggiornamento automatico.
- Le build ufficiali sono prodotte solo dal [workflow di release](.github/workflows/release.yml) a partire da un tag e includono un `SHA256SUMS.txt`. Binari da altre fonti non sono ufficiali — vedi [SECURITY.md](SECURITY.md).
- I fork sono benvenuti con licenza GPL; solo il maintainer può modificare questo repository.

## Crediti e licenza

- Il protocollo della tastiera è ricavato da **Lenovo Legion Toolkit** del LenovoLegionToolkit-Team (GPL-3.0); per questo il progetto usa la stessa licenza.
- Interfaccia ispirata allo stile scuro e verde neon di Razer Synapse / Chroma Studio.
- Licenza: [GPL-3.0](LICENSE). Se ridistribuisci il programma devi mantenere la stessa licenza e rendere disponibile il sorgente.
