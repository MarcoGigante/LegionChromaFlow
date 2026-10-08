<div align="center">

# LegionChromaFlow

**El fondo de tu escritorio fluyendo por el teclado de tu Lenovo Legion.**<br>
Iluminación RGB dinámica tecla a tecla con un panel de control al estilo Razer Chroma — sin Lenovo Vantage, sin nube, sin dependencias.

[![build](https://img.shields.io/github/actions/workflow/status/MarcoGigante/LegionChromaFlow/build.yml?branch=main&style=flat-square&label=build)](https://github.com/MarcoGigante/LegionChromaFlow/actions)
[![release](https://img.shields.io/github/v/release/MarcoGigante/LegionChromaFlow?style=flat-square)](https://github.com/MarcoGigante/LegionChromaFlow/releases)
[![license](https://img.shields.io/badge/license-GPL--3.0-44d62c?style=flat-square)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8%2B-512bd4?style=flat-square)

[English](README.md) · [Italiano](README.it.md) · **Español** · [Français](README.fr.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · [简体中文](README.zh.md)

<img src="docs/screenshot-live.png" alt="Panel de control de LegionChromaFlow con vista previa en directo del teclado" width="860">

</div>

---

## Características

- 🖼️ **Luces guiadas por el fondo** — la imagen del escritorio se desplaza y ondula lentamente por las teclas, con pequeñas ondas de luz aleatorias.
- 🪟 **Tinte de la ventana activa** — al cambiar de ventana, sus colores se extienden **desde el centro del teclado hacia los bordes** y permanecen mientras esa ventana tenga el foco.
- 🎯 **Solo cuentan los colores reales** — los píxeles negros, grises y blancos se ignoran. Una ventana negra deja el teclado con los colores del fondo.
- ⚡ **Dos estilos de onda** — *Smooth*, un fundido suave con brillo, o *Barrier*, una fina banda de teclas apagadas que cruza el teclado con los nuevos colores justo detrás.
- 🧈 **Transiciones fluidas** — un tiempo de «seguimiento» ajustable para que las luces se deslicen hacia los colores de la ventana en lugar de dar saltos.
- 🎛️ **Panel al estilo Razer** — interfaz oscura, vista previa en directo del teclado y explicación de cada opción; los cambios se aplican al instante y se guardan solos.
- 🌍 **7 idiomas** — inglés, italiano, español, francés, alemán, portugués y chino simplificado, seleccionables desde el panel.
- 🔔 **Icono en la bandeja del sistema** — clic izquierdo para mostrar/ocultar el panel, clic derecho para el menú rápido.
- 🔌 **Cero dependencias** — usa `hid.dll`, GDI+ y GDI. Solo necesitas .NET.
- 🤖 **Escenas con IA opcionales** — Claude puede diseñar una escena de luces (paleta, patrón, velocidad) para la ventana que estás mirando, además de los otros efectos. Desactivadas por defecto; requieren tu propia clave de API.
- 🔒 **Sin conexión y privado por defecto** — nada sale de tu PC salvo que actives las escenas con IA. Los píxeles de las ventanas se leen en memoria y nunca se guardan.

<div align="center">
<img src="docs/screenshot-window.png" alt="Página de ajustes de color de la ventana" width="760">
</div>

## Inicio rápido

**Requisitos:** Windows 10/11 · [.NET 8 Desktop Runtime o posterior](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.DesktopRuntime.8`) · un portátil Lenovo Legion con teclado **Spectrum RGB por tecla**.

1. **Descarga** el último zip desde [Releases](../../releases) y extráelo (por ejemplo en `C:\LegionChromaFlow`). ¿Prefieres compilarlo? Instala el SDK de .NET 8 y ejecuta `build.bat`.
2. **En Lenovo Vantage** elige un perfil con el efecto *Legion Aurora Sync*, pulsa **Aplicar** y luego **cierra Vantage por completo** (también desde la bandeja). Vantage y este programa no deben escribir en el teclado a la vez.
3. **Comprueba tu hardware**, en este orden:

   | Paso | Comando | Qué hace |
   |---|---|---|
   | 1 | `probe.bat` | Encuentra el teclado y lee el mapa de teclas y el perfil. **No cambia las luces.** |
   | 2 | `test.bat` | Todas las teclas en rojo → verde → azul unos segundos y luego restaura tu perfil. |
   | 3 | `OpenPanel.vbs` | Inicia el efecto y abre el panel de control. |

4. ¿Te gusta? Marca **Iniciar con Windows** en el panel. `run-hidden.vbs` lo inicia en silencio en la bandeja; `stop.bat` (o *Salir* en el menú del icono) lo detiene y restaura tu perfil de iluminación.

> **Consejo:** Windows 11 oculta los iconos nuevos tras la flecha `^`. Arrastra el icono de LegionChromaFlow a la barra de tareas para tenerlo siempre visible.

## Panel de control

| Sección | Qué puedes ajustar |
|---|---|
| **Luces** | Vista previa en directo del teclado, tarjetas de estilo, botón **Vista previa de onda** |
| **Onda** | Duración (segundos), grosor de la barrera, suavidad del frente, brillo |
| **Ventana** | Influencia, **fluidez del color**, umbrales de color y brillo, frecuencia de lectura |
| **Aspecto** | Brillo, saturación, gamma |
| **Escritorio** | Velocidad del fondo, destello entre teclas, ondas aleatorias, fotogramas por segundo |

Pasa el ratón por el icono redondo **ⓘ** junto a cada opción para ver qué hace y qué significan los valores bajos y altos. El idioma se elige arriba a la derecha. Todo se guarda en `config/config.json` (con los comentarios) mientras arrastras.

## Estilos de onda

| | **Smooth** | **Barrier** |
|---|---|---|
| Aspecto | Los nuevos colores se funden desde el centro con un brillo suave | Una fina banda de teclas apagadas se expande; los nuevos colores aparecen justo detrás |
| Sensación | Fluida, ambiental | Nítida, tipo «escáner» |
| Opciones propias | Suavidad del frente, brillo | Grosor de la barrera |

## Solución de problemas

- **No se encuentra el teclado** — ejecuta `probe.bat` desde un símbolo del sistema abierto **como administrador** y lee `logs\legionchromaflow.log`.
- **Parpadeos o colores que no cambian** — Lenovo Vantage (u otra herramienta Legion) sigue activo. `probe` muestra los que detecta.
- **Teclado atascado en un color tras un cierre inesperado** — cambia de perfil con `Fn + Espacio`, o inicia la app una vez y usa *Salir*.
- **No es lo bastante fluido** — sube *Fluidez del color* en la sección **Ventana**.
- **Tras suspender/reanudar** el programa se reconecta solo.

Detalles técnicos, referencia de línea de comandos y tabla completa de configuración: consulta el [README en inglés](README.md).

## Luces desde el encendido

Por defecto el efecto arranca en cuanto inicias sesión (una tarea al iniciar sesión, sin el retraso de la carpeta Inicio) y **se reactiva solo tras suspender/reanudar y desbloquear**, de modo que al despertar el portátil con el botón de encendido vuelve el efecto en lugar del modo propio del teclado.

Para iluminar el teclado **aún antes —en la pantalla de inicio/bloqueo de Windows, antes de que nadie inicie sesión—** ejecuta una vez `install-boot.bat` (pide permisos de administrador e instala una tarea de inicio que se ejecuta como SYSTEM). La instancia de arranque usa una pequeña copia en caché de tu fondo (se guarda cada vez que el panel se ejecuta) y cede el control al panel al iniciar sesión sin parpadeos. Se quita con `uninstall-boot.bat`. La fase de firmware/BIOS previa a la carga de Windows no se puede cambiar por software.

## Escenas con IA (opcionales)

Activa **IA** en el panel y Claude diseña una escena de luces —paleta, patrón (`aurora`, `pulse`, `wave`, `sparkle`, `rain`, `fire`, `breathe`), velocidad e intensidad— para la ventana que estás mirando. La escena se superpone a los efectos del fondo y de los colores de la ventana, y se funde al cambiar de ventana.

- **Desactivadas por defecto.** Requieren tu propia clave de API de Anthropic: pégala en el panel (se guarda cifrada para tu usuario de Windows con DPAPI en `config/ai.key`, excluido de git) o define la variable de entorno `ANTHROPIC_API_KEY`.
- **Qué se envía:** al cambiar de ventana, una pequeña captura JPEG de esa ventana (máx. 768 px de ancho) va a `api.anthropic.com`, como mucho una vez por *Intervalo mínimo* (12 s por defecto). Con la función desactivada no se envía nada. Las ventanas cuyo título contenga palabras como "password" o "bank" se omiten (`AiSkipTitles` en `config.json`).
- **Coste:** las peticiones se facturan a tu propia cuenta de Anthropic. El modelo predeterminado es el pequeño y rápido `claude-haiku-4-5-20251001`; cámbialo con `AiModel`.
- **Ajustes:** *Intensidad de la IA*, *Intervalo mínimo* y *Fundido de escena* en la sección **IA**.

## Compatibilidad y aviso legal

- Desarrollado y probado en el **Legion 7 16IRX9**. Otros Legion con el mismo teclado Spectrum *deberían* funcionar, pero no se han probado: [abre una issue](../../issues) con la salida de `probe`.
- Proyecto **no oficial**, sin relación con Lenovo ni Razer ni respaldado por ellas. Las marcas pertenecen a sus propietarios.
- Envía al teclado los mismos comandos que el software de Lenovo. **Úsalo bajo tu responsabilidad**; consulta la [licencia](LICENSE) para la exención de garantía.
- No es compatible con Razer Chroma (no hay una forma oficial de engancharse sin un App Id de Razer).

## Seguridad y confianza

- Sin telemetría ni actualizaciones automáticas. La única función de red es la opcional, desactivada por defecto, de las [escenas con IA](#escenas-con-ia-opcionales), que solo habla con `api.anthropic.com`.
- Las compilaciones oficiales las genera únicamente el [workflow de release](.github/workflows/release.yml) a partir de una etiqueta e incluyen un `SHA256SUMS.txt`. Los binarios de cualquier otro origen no son oficiales — consulta [SECURITY.md](SECURITY.md).
- Los forks son bienvenidos bajo la GPL; solo el mantenedor puede modificar este repositorio.

## Créditos y licencia

- El protocolo del teclado se derivó de **Lenovo Legion Toolkit** del LenovoLegionToolkit-Team (GPL-3.0); por eso este proyecto usa la misma licencia.
- Interfaz inspirada en el estilo oscuro y verde neón de Razer Synapse / Chroma Studio.
- Licencia: [GPL-3.0](LICENSE). Si redistribuyes el programa, debes mantener la misma licencia y poner el código fuente a disposición.
