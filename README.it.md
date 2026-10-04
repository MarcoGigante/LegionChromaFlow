# LegionChromaFlow

Luci dinamiche per la tastiera **Lenovo Legion 7 16IRX9** (Spectrum per-key RGB), pensate come alternativa a Razer Chroma Generate.

* **Motivo principale: lo sfondo del desktop.** L'immagine scorre e ondeggia lentamente e in modo casuale sui tasti, con piccole onde di luce ogni tanto.
* **Influenza lieve della finestra attiva.** Quando cambi finestra, i suoi colori si propagano **dal centro della tastiera verso l'esterno** e restano come leggera tinta (di default 28%).
* **Solo colori veri.** Pixel neri, grigi o bianchi della finestra vengono ignorati: se lo sfondo della finestra e' nero, la tastiera resta quella del desktop.

## Installazione

1. Copia questa cartella in `C:\LegionChromaFlow` (deve contenere `bin`, `config`, `src`, `logs`).
2. Serve il runtime **.NET 8 o successivo** (se manca: `winget install Microsoft.DotNet.Runtime.8`).
3. In **Lenovo Vantage** seleziona un profilo con l'effetto **Sinc Legion Aurora** e premi **APPLICA**, poi **chiudi Vantage** (anche dall'area di notifica). Vantage e questo programma non devono scrivere insieme sulla tastiera.
4. Esegui in quest'ordine, controllando che ogni passo funzioni:
   * `probe.bat`: trova la tastiera, legge mappa tasti e profilo. **Non cambia le luci.**
   * `test.bat`: tutti i tasti rosso, verde, blu per qualche secondo, poi ripristina.
   * `run.bat`: avvia l'effetto con finestra visibile (Ctrl+C per fermare).
5. Quando e' tutto ok: `run-hidden.vbs` lo avvia in background, `stop.bat` lo ferma.
   Opzionale: `install-startup.bat` lo avvia a ogni accesso (`uninstall-startup.bat` lo toglie).

Se `probe.bat` non trova la tastiera, riprova da un prompt avviato **come amministratore** e leggi `logs\legionchromaflow.log`.

## Pannello grafico e icona nell'area di notifica

* `OpenPanel.vbs` apre il pannello (se il programma e' gia' avviato lo porta in primo piano). `run-hidden.vbs` (e l'avvio automatico) parte nascosto con la sola icona vicino all'orologio.
* Dal pannello: anteprima dal vivo dei colori dei tasti, scelta dello stile onda (**Smooth** / **Barrier**), tutti i parametri con spiegazione (passa il mouse sull'icona tonda "i"), "Anteprima onda" e "Avvia con Windows". Le modifiche sono immediate e si salvano in `config\config.json` (commenti compresi).
* Icona: clic sinistro = mostra/nascondi il pannello, clic destro = menu rapido (stile, anteprima, Esci). Chiudere la finestra con la X la nasconde soltanto; per uscire usa **Esci** dal menu dell'icona o `stop.bat`.
* Windows 11 nasconde le icone nuove nel menu ^: trascinala sulla barra per averla sempre visibile.

## Personalizzazione

Modifica `config\config.json` (i commenti spiegano ogni voce) e riavvia. Le piu' utili:

| Voce | Effetto |
|---|---|
| `WindowInfluence` | 0 = ignora la finestra, 0.28 = lieve, 1 = totale |
| `DriftSpeed` | velocita' con cui scorre lo sfondo (0 = fermo) |
| `Saturation`, `Gamma`, `Brightness` | resa dei colori sui LED |
| `WaveSeconds`, `WaveBand`, `WaveGlow` | durata, ampiezza e bagliore dell'onda dal centro |
| `ChromaThreshold` | alzalo per ignorare anche colori poco saturi |
| `WallpaperOverride` | usa un'altra immagine al posto dello sfondo di Windows |

## Se qualcosa non va

* **Tastiera ferma su un colore dopo aver chiuso male il programma:** cambia profilo con Fn+barra spaziatrice, oppure riavvia e usa `stop.bat`.
* **Colori che sfarfallano o non cambiano:** Vantage (o un altro programma Legion) e' ancora attivo. `probe.bat` elenca quelli che rileva.
* **Dopo sospensione/ripresa** il programma si ricollega da solo.
* **Ricompilare:** `build.bat` (serve .NET SDK 8+). Il controllo della logica senza tastiera: `dotnet bin\LegionChromaFlow.dll selftest`.

## Note tecniche e limiti

* Nessuna dipendenza esterna: HID tramite `hid.dll`/`setupapi.dll`, sfondo tramite GDI+, finestra tramite GDI.
* Il protocollo della tastiera (report da 960 byte, modalita' "Aurora" con bitmap per tasto) e' ricavato dal codice di **Lenovo Legion Toolkit** (LenovoLegionToolkit-Team, GPL-3.0). Se ridistribuisci questo programma, rispetta quella licenza.
* Non e' compatibile con Razer Chroma: non c'e' modo ufficiale di collegarsi a Chroma senza un App Id Razer.
* L'effetto non e' stato provato sull'hardware reale prima della consegna: la prima esecuzione serve proprio a verificarlo (`probe`, poi `test`).
