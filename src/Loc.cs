using System.Runtime.InteropServices;

namespace LegionChromaFlow;

/// <summary>User-interface translations. Order of every row: en, it, es, fr, de, pt, zh.</summary>
internal static class L
{
    public static readonly (string Code, string Native)[] Languages =
    {
        ("en", "English"), ("it", "Italiano"), ("es", "Español"), ("fr", "Français"),
        ("de", "Deutsch"), ("pt", "Português"), ("zh", "简体中文"),
    };

    public static string Current { get; private set; } = "en";
    public static event Action? Changed;

    [DllImport("kernel32.dll")] private static extern ushort GetUserDefaultUILanguage();

    /// <summary>Resolves "auto" (or an unknown code) to a supported language using the Windows display language.</summary>
    public static string Resolve(string? setting)
    {
        var s = (setting ?? "auto").Trim().ToLowerInvariant();
        foreach (var (code, _) in Languages) if (code == s) return code;
        try
        {
            return (GetUserDefaultUILanguage() & 0x3FF) switch
            {
                0x10 => "it", 0x0A => "es", 0x0C => "fr", 0x07 => "de", 0x16 => "pt", 0x04 => "zh", _ => "en"
            };
        }
        catch { return "en"; }
    }

    public static void Set(string setting)
    {
        var code = Resolve(setting);
        if (code == Current) return;
        Current = code;
        Changed?.Invoke();
    }

    public static void Init(string setting) => Current = Resolve(setting);

    public static string T(string key, params object[] args)
    {
        var idx = Array.FindIndex(Languages, l => l.Code == Current);
        if (!Table.TryGetValue(key, out var row)) return key;
        var text = idx >= 0 && idx < row.Length && row[idx].Length > 0 ? row[idx] : row[0];
        return args.Length == 0 ? text : string.Format(text, args);
    }

    private static readonly Dictionary<string, string[]> Table = new()
    {
        // ---- pages ----
        ["p.live.t"] = new[] { "Lights", "Luci", "Luces", "Lumières", "Licht", "Luzes", "灯光" },
        ["p.live.s"] = new[] { "Preview and style", "Anteprima e stile", "Vista previa y estilo", "Aperçu et style", "Vorschau und Stil", "Pré-visualização e estilo", "预览与样式" },
        ["p.live.h"] = new[] { "Live lights", "Luci dal vivo", "Luces en directo", "Éclairage en direct", "Live-Beleuchtung", "Luzes ao vivo", "实时灯光" },
        ["p.live.l"] = new[] {
            "What you see here is what is going to the keyboard. Choose the wave style.",
            "Quello che vedi qui è quello che sta andando sulla tastiera. Scegli lo stile dell'onda.",
            "Lo que ves aquí es lo que se envía al teclado. Elige el estilo de la onda.",
            "Ce que vous voyez ici est ce qui est envoyé au clavier. Choisissez le style de l'onde.",
            "Was Sie hier sehen, geht gerade an die Tastatur. Wählen Sie den Wellenstil.",
            "O que vê aqui é o que está a ser enviado para o teclado. Escolha o estilo da onda.",
            "这里显示的就是正在发送到键盘的灯光。请选择波浪样式。" },

        ["p.wave.t"] = new[] { "Wave", "Onda", "Onda", "Onde", "Welle", "Onda", "波浪" },
        ["p.wave.s"] = new[] { "Window change", "Cambio finestra", "Cambio de ventana", "Changement de fenêtre", "Fensterwechsel", "Mudança de janela", "切换窗口" },
        ["p.wave.h"] = new[] { "Wave on window change", "Onda al cambio finestra", "Onda al cambiar de ventana", "Onde au changement de fenêtre", "Welle beim Fensterwechsel", "Onda ao mudar de janela", "切换窗口时的波浪" },
        ["p.wave.l"] = new[] {
            "How the effect spreads from the center of the keyboard when you switch window.",
            "Come si propaga l'effetto dal centro della tastiera quando cambi finestra.",
            "Cómo se propaga el efecto desde el centro del teclado al cambiar de ventana.",
            "Comment l'effet se propage depuis le centre du clavier quand vous changez de fenêtre.",
            "Wie sich der Effekt beim Fensterwechsel von der Tastaturmitte aus ausbreitet.",
            "Como o efeito se propaga a partir do centro do teclado ao mudar de janela.",
            "切换窗口时，效果如何从键盘中心向外扩散。" },

        ["p.window.t"] = new[] { "Window", "Finestra", "Ventana", "Fenêtre", "Fenster", "Janela", "窗口" },
        ["p.window.s"] = new[] { "Colors and smoothness", "Colori e fluidità", "Colores y fluidez", "Couleurs et fluidité", "Farben und Flüssigkeit", "Cores e fluidez", "颜色与流畅度" },
        ["p.window.h"] = new[] { "Active window colors", "Colori della finestra attiva", "Colores de la ventana activa", "Couleurs de la fenêtre active", "Farben des aktiven Fensters", "Cores da janela ativa", "活动窗口颜色" },
        ["p.window.l"] = new[] {
            "How much and how the colors of the open window influence the keyboard.",
            "Quanto e come i colori della finestra aperta influenzano la tastiera.",
            "Cuánto y cómo influyen en el teclado los colores de la ventana abierta.",
            "Dans quelle mesure les couleurs de la fenêtre ouverte influencent le clavier.",
            "Wie stark und auf welche Weise die Farben des geöffneten Fensters die Tastatur beeinflussen.",
            "Quanto e como as cores da janela aberta influenciam o teclado.",
            "当前窗口的颜色对键盘的影响程度与方式。" },

        ["p.look.t"] = new[] { "Look", "Aspetto", "Aspecto", "Apparence", "Aussehen", "Aspeto", "外观" },
        ["p.look.s"] = new[] { "Color rendition", "Resa dei colori", "Reproducción del color", "Rendu des couleurs", "Farbwiedergabe", "Reprodução de cor", "色彩表现" },
        ["p.look.h"] = new[] { "Look of the lights", "Aspetto delle luci", "Aspecto de las luces", "Apparence des lumières", "Aussehen der Beleuchtung", "Aspeto das luzes", "灯光外观" },
        ["p.look.l"] = new[] {
            "Brightness, intensity and depth of the colors on the LEDs.",
            "Luminosità, intensità e profondità dei colori sui LED.",
            "Brillo, intensidad y profundidad de los colores en los LED.",
            "Luminosité, intensité et profondeur des couleurs sur les LED.",
            "Helligkeit, Intensität und Tiefe der Farben auf den LEDs.",
            "Brilho, intensidade e profundidade das cores nos LED.",
            "LED 上颜色的亮度、强度与深度。" },

        ["p.desk.t"] = new[] { "Desktop", "Desktop", "Escritorio", "Bureau", "Desktop", "Ambiente de trabalho", "桌面" },
        ["p.desk.s"] = new[] { "Motion and speed", "Movimento e velocità", "Movimiento y velocidad", "Mouvement et vitesse", "Bewegung und Tempo", "Movimento e velocidade", "动态与速度" },
        ["p.desk.h"] = new[] { "Wallpaper motion", "Movimento dello sfondo", "Movimiento del fondo", "Mouvement du fond d'écran", "Bewegung des Hintergrunds", "Movimento do fundo", "壁纸动态" },
        ["p.desk.l"] = new[] {
            "How the desktop wallpaper drifts across the keyboard and how smooth the output is.",
            "Come scorre lo sfondo del desktop sulla tastiera e quanto è fluido l'invio.",
            "Cómo se desplaza el fondo de escritorio por el teclado y lo fluido que es el envío.",
            "Comment le fond d'écran défile sur le clavier et la fluidité de l'envoi.",
            "Wie der Desktop-Hintergrund über die Tastatur wandert und wie flüssig die Ausgabe ist.",
            "Como o fundo do ambiente de trabalho se desloca no teclado e quão fluido é o envio.",
            "桌面壁纸如何在键盘上漂移，以及输出的流畅程度。" },

        // ---- options ----
        ["o.WaveSeconds.c"] = new[] { "Wave duration", "Durata dell'onda", "Duración de la onda", "Durée de l'onde", "Wellendauer", "Duração da onda", "波浪时长" },
        ["o.WaveSeconds.h"] = new[] {
            "How many seconds the wave takes to travel from the center to the edges of the keyboard when you switch window.\n\nLow = quick and snappy.\nHigh = slow and cinematic.",
            "Quanti secondi impiega l'onda a viaggiare dal centro ai bordi della tastiera quando cambi finestra.\n\nBasso = scatto rapido e vivace.\nAlto = onda lenta e scenografica.",
            "Segundos que tarda la onda en viajar desde el centro hasta los bordes del teclado al cambiar de ventana.\n\nBajo = rápida y viva.\nAlto = lenta y espectacular.",
            "Nombre de secondes que met l'onde pour aller du centre aux bords du clavier quand vous changez de fenêtre.\n\nFaible = rapide et nerveuse.\nÉlevé = lente et spectaculaire.",
            "Wie viele Sekunden die Welle beim Fensterwechsel von der Mitte bis zu den Rändern der Tastatur braucht.\n\nNiedrig = schnell und knackig.\nHoch = langsam und eindrucksvoll.",
            "Quantos segundos a onda demora a ir do centro às extremidades do teclado ao mudar de janela.\n\nBaixo = rápida e viva.\nAlto = lenta e cinematográfica.",
            "切换窗口时，波浪从键盘中心扩散到边缘所需的秒数。\n\n低 = 快速利落。\n高 = 缓慢而有电影感。" },

        ["o.BarrierWidth.c"] = new[] { "Barrier thickness", "Spessore della barriera", "Grosor de la barrera", "Épaisseur de la barrière", "Barrierendicke", "Espessura da barreira", "屏障厚度" },
        ["o.BarrierWidth.h"] = new[] {
            "Width of the band of switched-off keys that precedes the new colors, as a percentage of the keyboard.\n\nLow = thin, crisp line.\nHigh = wide dark band.\n\nBarrier style only.",
            "Larghezza della fascia di tasti spenti che precede i nuovi colori, in percentuale della tastiera.\n\nBasso = linea sottile e netta.\nAlto = larga fascia scura.\n\nVale solo per lo stile Barrier.",
            "Anchura de la banda de teclas apagadas que precede a los nuevos colores, en porcentaje del teclado.\n\nBajo = línea fina y nítida.\nAlto = banda oscura ancha.\n\nSolo para el estilo Barrier.",
            "Largeur de la bande de touches éteintes qui précède les nouvelles couleurs, en pourcentage du clavier.\n\nFaible = ligne fine et nette.\nÉlevé = large bande sombre.\n\nStyle Barrier uniquement.",
            "Breite des Bands aus ausgeschalteten Tasten, das den neuen Farben vorausgeht, in Prozent der Tastatur.\n\nNiedrig = dünne, scharfe Linie.\nHoch = breites dunkles Band.\n\nNur für den Stil Barrier.",
            "Largura da faixa de teclas apagadas que antecede as novas cores, em percentagem do teclado.\n\nBaixo = linha fina e nítida.\nAlto = faixa escura larga.\n\nApenas para o estilo Barrier.",
            "新颜色前方那条熄灭按键带的宽度，以键盘宽度的百分比表示。\n\n低 = 细而锐利的线。\n高 = 宽的暗带。\n\n仅适用于 Barrier 样式。" },

        ["o.WaveBand.c"] = new[] { "Front softness", "Morbidezza del fronte", "Suavidad del frente", "Douceur du front", "Weichheit der Front", "Suavidade da frente", "前沿柔和度" },
        ["o.WaveBand.h"] = new[] {
            "How blurred the transition between old and new colors is.\n\nLow = hard edge.\nHigh = very soft dissolve.\n\nSmooth style only.",
            "Quanto è sfumato il passaggio tra vecchi e nuovi colori.\n\nBasso = passaggio netto.\nAlto = dissolvenza molto morbida.\n\nVale solo per lo stile Smooth.",
            "Cuán difuminada es la transición entre los colores antiguos y los nuevos.\n\nBajo = borde duro.\nAlto = fundido muy suave.\n\nSolo para el estilo Smooth.",
            "Degré de flou de la transition entre anciennes et nouvelles couleurs.\n\nFaible = bord net.\nÉlevé = fondu très doux.\n\nStyle Smooth uniquement.",
            "Wie weich der Übergang zwischen alten und neuen Farben ist.\n\nNiedrig = harte Kante.\nHoch = sehr weiches Überblenden.\n\nNur für den Stil Smooth.",
            "Quão esbatida é a transição entre as cores antigas e as novas.\n\nBaixo = limite nítido.\nAlto = esbatimento muito suave.\n\nApenas para o estilo Smooth.",
            "新旧颜色之间过渡的模糊程度。\n\n低 = 边缘锐利。\n高 = 非常柔和的渐变。\n\n仅适用于 Smooth 样式。" },

        ["o.WaveGlow.c"] = new[] { "Front glow", "Bagliore del fronte", "Brillo del frente", "Lueur du front", "Leuchten der Front", "Brilho da frente", "前沿光晕" },
        ["o.WaveGlow.h"] = new[] {
            "How much the wave front lights up white as it advances. 0 = no glow.\n\nSmooth style only.",
            "Quanto si illumina di bianco il fronte dell'onda mentre avanza. 0 = nessun bagliore.\n\nVale solo per lo stile Smooth.",
            "Cuánto se ilumina de blanco el frente de la onda al avanzar. 0 = sin brillo.\n\nSolo para el estilo Smooth.",
            "Intensité de la lueur blanche du front de l'onde pendant sa progression. 0 = aucune lueur.\n\nStyle Smooth uniquement.",
            "Wie stark die Wellenfront beim Vorrücken weiß aufleuchtet. 0 = kein Leuchten.\n\nNur für den Stil Smooth.",
            "O quanto a frente da onda se ilumina de branco ao avançar. 0 = sem brilho.\n\nApenas para o estilo Smooth.",
            "波浪前沿推进时泛起的白色光晕强度。0 = 无光晕。\n\n仅适用于 Smooth 样式。" },

        ["o.WindowInfluence.c"] = new[] { "Window influence", "Influenza della finestra", "Influencia de la ventana", "Influence de la fenêtre", "Fenstereinfluss", "Influência da janela", "窗口影响度" },
        ["o.WindowInfluence.h"] = new[] {
            "How much the active window's colors cover the desktop's, for as long as the window stays open.\n\n0% = always ignored.\n30% = subtle tint.\n100% = the keyboard takes the window's colors.",
            "Quanto i colori della finestra attiva coprono quelli del desktop, finché la finestra resta aperta.\n\n0% = li ignora sempre.\n30% = leggera tinta.\n100% = la tastiera prende i colori della finestra.",
            "Cuánto cubren los colores de la ventana activa a los del escritorio mientras la ventana siga abierta.\n\n0% = siempre ignorados.\n30% = tinte sutil.\n100% = el teclado toma los colores de la ventana.",
            "Dans quelle mesure les couleurs de la fenêtre active recouvrent celles du bureau tant que la fenêtre reste ouverte.\n\n0 % = toujours ignorées.\n30 % = légère teinte.\n100 % = le clavier prend les couleurs de la fenêtre.",
            "Wie stark die Farben des aktiven Fensters die des Desktops überdecken, solange das Fenster geöffnet bleibt.\n\n0 % = immer ignoriert.\n30 % = dezenter Farbton.\n100 % = die Tastatur übernimmt die Fensterfarben.",
            "Quanto as cores da janela ativa cobrem as do ambiente de trabalho enquanto a janela estiver aberta.\n\n0% = sempre ignoradas.\n30% = tom subtil.\n100% = o teclado assume as cores da janela.",
            "只要窗口保持打开，其颜色会在多大程度上覆盖桌面颜色。\n\n0% = 始终忽略。\n30% = 淡淡的色调。\n100% = 键盘完全采用窗口颜色。" },

        ["o.WindowFollowSeconds.c"] = new[] { "Color smoothness", "Fluidità dei colori", "Fluidez del color", "Fluidité des couleurs", "Farbglättung", "Fluidez das cores", "颜色平滑度" },
        ["o.WindowFollowSeconds.h"] = new[] {
            "How gently the lights chase the window's colors when its content changes (scrolling, video, pages).\n\n0 = reacts instantly but can look steppy.\nHigh = very smooth transitions, with a little delay when colors change.",
            "Quanto dolcemente le luci inseguono i colori della finestra quando il suo contenuto cambia (scorrimento, video, pagine).\n\n0 = reagisce subito ma può risultare a scatti.\nAlto = transizioni molto fluide, con un po' di ritardo nel cambio colore.",
            "Con qué suavidad las luces siguen los colores de la ventana cuando cambia su contenido (desplazamiento, vídeo, páginas).\n\n0 = reacciona al instante pero puede verse entrecortado.\nAlto = transiciones muy fluidas, con algo de retraso al cambiar de color.",
            "Douceur avec laquelle les lumières suivent les couleurs de la fenêtre quand son contenu change (défilement, vidéo, pages).\n\n0 = réagit instantanément mais peut paraître saccadé.\nÉlevé = transitions très fluides, avec un léger retard au changement de couleur.",
            "Wie sanft die Beleuchtung den Fensterfarben folgt, wenn sich der Inhalt ändert (Scrollen, Video, Seiten).\n\n0 = reagiert sofort, kann aber ruckelig wirken.\nHoch = sehr weiche Übergänge, mit leichter Verzögerung beim Farbwechsel.",
            "Com que suavidade as luzes seguem as cores da janela quando o conteúdo muda (deslocamento, vídeo, páginas).\n\n0 = reage de imediato, mas pode parecer entrecortado.\nAlto = transições muito fluidas, com um pequeno atraso na mudança de cor.",
            "窗口内容变化（滚动、视频、翻页）时，灯光跟随窗口颜色的柔和程度。\n\n0 = 立即响应，但可能显得生硬。\n高 = 过渡非常平滑，换色时略有延迟。" },

        ["o.ChromaThreshold.c"] = new[] { "Color threshold", "Soglia colore", "Umbral de color", "Seuil de couleur", "Farbschwelle", "Limiar de cor", "颜色阈值" },
        ["o.ChromaThreshold.h"] = new[] {
            "Below this vividness, window pixels count as colorless (grays, whites) and are ignored.\n\nRaise it to also ignore pastel colors.",
            "Sotto questa vivacità i pixel della finestra sono considerati senza colore (grigi, bianchi) e ignorati.\n\nAlzala per ignorare anche i colori pastello.",
            "Por debajo de esta viveza, los píxeles de la ventana se consideran sin color (grises, blancos) y se ignoran.\n\nSúbelo para ignorar también los colores pastel.",
            "En dessous de cette vivacité, les pixels de la fenêtre sont considérés comme sans couleur (gris, blancs) et ignorés.\n\nAugmentez-la pour ignorer aussi les couleurs pastel.",
            "Unterhalb dieser Farbintensität gelten Fensterpixel als farblos (Grau, Weiß) und werden ignoriert.\n\nErhöhen, um auch Pastellfarben zu ignorieren.",
            "Abaixo desta vivacidade, os píxeis da janela são considerados sem cor (cinzentos, brancos) e ignorados.\n\nAumente para ignorar também as cores pastel.",
            "低于此鲜艳度的窗口像素被视为无色（灰、白）并被忽略。\n\n调高可同时忽略淡色。" },

        ["o.ValueThreshold.c"] = new[] { "Brightness threshold", "Soglia luminosità", "Umbral de brillo", "Seuil de luminosité", "Helligkeitsschwelle", "Limiar de brilho", "亮度阈值" },
        ["o.ValueThreshold.h"] = new[] {
            "Below this brightness, window pixels (black, dark backgrounds) are ignored.\n\nIf the window is black, the keyboard stays on the desktop colors.",
            "Sotto questa luminosità i pixel della finestra (nero, sfondi scuri) sono ignorati.\n\nSe la finestra è nera, la tastiera resta sui colori del desktop.",
            "Por debajo de este brillo, los píxeles de la ventana (negro, fondos oscuros) se ignoran.\n\nSi la ventana es negra, el teclado se queda con los colores del escritorio.",
            "En dessous de cette luminosité, les pixels de la fenêtre (noir, fonds sombres) sont ignorés.\n\nSi la fenêtre est noire, le clavier garde les couleurs du bureau.",
            "Unterhalb dieser Helligkeit werden Fensterpixel (Schwarz, dunkle Hintergründe) ignoriert.\n\nIst das Fenster schwarz, bleibt die Tastatur bei den Desktop-Farben.",
            "Abaixo deste brilho, os píxeis da janela (preto, fundos escuros) são ignorados.\n\nSe a janela for preta, o teclado mantém as cores do ambiente de trabalho.",
            "低于此亮度的窗口像素（黑色、深色背景）会被忽略。\n\n如果窗口是黑色的，键盘将保持桌面颜色。" },

        ["o.WindowSampleMs.c"] = new[] { "Read frequency", "Frequenza di lettura", "Frecuencia de lectura", "Fréquence de lecture", "Leseintervall", "Frequência de leitura", "读取频率" },
        ["o.WindowSampleMs.h"] = new[] {
            "Milliseconds between reads of the active window.\n\nLow = reacts sooner but uses more CPU.\nHigh = lighter.",
            "Ogni quanti millisecondi viene letta la finestra attiva.\n\nBasso = reagisce prima ma usa più CPU.\nAlto = più leggero.",
            "Milisegundos entre lecturas de la ventana activa.\n\nBajo = reacciona antes pero usa más CPU.\nAlto = más ligero.",
            "Millisecondes entre deux lectures de la fenêtre active.\n\nFaible = réagit plus vite mais utilise plus de CPU.\nÉlevé = plus léger.",
            "Millisekunden zwischen zwei Abfragen des aktiven Fensters.\n\nNiedrig = reagiert früher, benötigt aber mehr CPU.\nHoch = schonender.",
            "Milissegundos entre leituras da janela ativa.\n\nBaixo = reage mais cedo mas usa mais CPU.\nAlto = mais leve.",
            "读取活动窗口的时间间隔（毫秒）。\n\n低 = 反应更快，但占用更多 CPU。\n高 = 更省资源。" },

        ["o.Brightness.c"] = new[] { "Brightness", "Luminosità", "Brillo", "Luminosité", "Helligkeit", "Brilho", "亮度" },
        ["o.Brightness.h"] = new[] {
            "Overall brightness of all keys.", "Luminosità generale di tutti i tasti.", "Brillo general de todas las teclas.",
            "Luminosité générale de toutes les touches.", "Gesamthelligkeit aller Tasten.", "Brilho geral de todas as teclas.", "所有按键的整体亮度。" },

        ["o.Saturation.c"] = new[] { "Saturation", "Saturazione", "Saturación", "Saturation", "Sättigung", "Saturação", "饱和度" },
        ["o.Saturation.h"] = new[] {
            "Color intensity.\n\n0 = black and white.\n1 = original colors.\nAbove 1 = more vivid colors.",
            "Intensità dei colori.\n\n0 = bianco e nero.\n1 = colori originali.\nPiù di 1 = colori più accesi.",
            "Intensidad de los colores.\n\n0 = blanco y negro.\n1 = colores originales.\nMás de 1 = colores más vivos.",
            "Intensité des couleurs.\n\n0 = noir et blanc.\n1 = couleurs d'origine.\nAu-delà de 1 = couleurs plus vives.",
            "Farbintensität.\n\n0 = Schwarzweiß.\n1 = Originalfarben.\nÜber 1 = lebhaftere Farben.",
            "Intensidade das cores.\n\n0 = preto e branco.\n1 = cores originais.\nMais de 1 = cores mais vivas.",
            "颜色的浓淡程度。\n\n0 = 黑白。\n1 = 原始颜色。\n大于 1 = 颜色更鲜艳。" },

        ["o.Gamma.c"] = new[] { "Depth (gamma)", "Profondità (gamma)", "Profundidad (gamma)", "Profondeur (gamma)", "Tiefe (Gamma)", "Profundidade (gama)", "深度（伽马）" },
        ["o.Gamma.h"] = new[] {
            "High = deeper, less washed-out colors on the LEDs, but darker.\nLow = lighter, pastel colors.",
            "Alto = colori più profondi e meno slavati sui LED, ma più scuri.\nBasso = colori più chiari e pastello.",
            "Alto = colores más profundos y menos lavados en los LED, pero más oscuros.\nBajo = colores más claros y pastel.",
            "Élevé = couleurs plus profondes et moins délavées sur les LED, mais plus sombres.\nFaible = couleurs plus claires et pastel.",
            "Hoch = tiefere, weniger blasse Farben auf den LEDs, aber dunkler.\nNiedrig = hellere Pastellfarben.",
            "Alto = cores mais profundas e menos desbotadas nos LED, mas mais escuras.\nBaixo = cores mais claras e pastel.",
            "高 = LED 上的颜色更深、更不发白，但更暗。\n低 = 颜色更浅、偏粉彩。" },

        ["o.DriftSpeed.c"] = new[] { "Wallpaper speed", "Velocità dello sfondo", "Velocidad del fondo", "Vitesse du fond", "Hintergrundtempo", "Velocidade do fundo", "壁纸速度" },
        ["o.DriftSpeed.h"] = new[] {
            "How fast the desktop image drifts and sways across the keys.\n\n0 = still.\n1 = slow (default).\n4 = fast.",
            "Quanto velocemente l'immagine del desktop scorre e ondeggia sui tasti.\n\n0 = ferma.\n1 = lenta (predefinito).\n4 = veloce.",
            "Con qué rapidez la imagen del escritorio se desplaza y ondula por las teclas.\n\n0 = quieta.\n1 = lenta (predeterminado).\n4 = rápida.",
            "Vitesse à laquelle l'image du bureau défile et ondule sur les touches.\n\n0 = fixe.\n1 = lente (par défaut).\n4 = rapide.",
            "Wie schnell das Desktop-Bild über die Tasten wandert und schwingt.\n\n0 = still.\n1 = langsam (Standard).\n4 = schnell.",
            "A rapidez com que a imagem do ambiente de trabalho se desloca e ondula pelas teclas.\n\n0 = parada.\n1 = lenta (predefinição).\n4 = rápida.",
            "桌面图像在按键上漂移、摆动的速度。\n\n0 = 静止。\n1 = 缓慢（默认）。\n4 = 快速。" },

        ["o.Shimmer.c"] = new[] { "Key shimmer", "Sfarfallio tra tasti", "Destello entre teclas", "Scintillement des touches", "Tastenflimmern", "Cintilação das teclas", "按键闪烁" },
        ["o.Shimmer.h"] = new[] {
            "Small random brightness variations from key to key, for a livelier look.",
            "Piccole variazioni casuali di luminosità da un tasto all'altro, per un aspetto più vivo.",
            "Pequeñas variaciones aleatorias de brillo entre teclas, para un aspecto más vivo.",
            "Petites variations aléatoires de luminosité d'une touche à l'autre, pour un rendu plus vivant.",
            "Kleine zufällige Helligkeitsschwankungen von Taste zu Taste für einen lebendigeren Look.",
            "Pequenas variações aleatórias de brilho entre teclas, para um aspeto mais vivo.",
            "按键之间随机的细微亮度变化，让效果更生动。" },

        ["o.RandomRippleEverySeconds.c"] = new[] { "Random light ripples", "Onde casuali di luce", "Ondas de luz aleatorias", "Ondulations lumineuses aléatoires", "Zufällige Lichtwellen", "Ondas de luz aleatórias", "随机光波" },
        ["o.RandomRippleEverySeconds.h"] = new[] {
            "Average seconds between small random ripples of light.\n\n0 = never.",
            "Ogni quanti secondi (in media) compare una piccola onda di luce casuale.\n\n0 = mai.",
            "Segundos de media entre pequeñas ondas de luz aleatorias.\n\n0 = nunca.",
            "Nombre moyen de secondes entre deux petites ondulations lumineuses aléatoires.\n\n0 = jamais.",
            "Durchschnittliche Sekunden zwischen kleinen zufälligen Lichtwellen.\n\n0 = nie.",
            "Segundos, em média, entre pequenas ondas de luz aleatórias.\n\n0 = nunca.",
            "两次随机小光波之间的平均间隔（秒）。\n\n0 = 从不。" },

        ["o.Fps.c"] = new[] { "Frames per second", "Fotogrammi al secondo", "Fotogramas por segundo", "Images par seconde", "Bilder pro Sekunde", "Fotogramas por segundo", "每秒帧数" },
        ["o.Fps.h"] = new[] {
            "How many times per second colors are sent to the keyboard.\n\nHigh = smoother motion.\nLow = lighter load, but steppy.",
            "Quante volte al secondo vengono inviati i colori alla tastiera.\n\nAlto = movimento più fluido.\nBasso = meno carico, ma a scatti.",
            "Cuántas veces por segundo se envían los colores al teclado.\n\nAlto = movimiento más fluido.\nBajo = menos carga, pero entrecortado.",
            "Nombre de fois par seconde où les couleurs sont envoyées au clavier.\n\nÉlevé = mouvement plus fluide.\nFaible = moins de charge, mais saccadé.",
            "Wie oft pro Sekunde Farben an die Tastatur gesendet werden.\n\nHoch = flüssigere Bewegung.\nNiedrig = geringere Last, aber ruckelig.",
            "Quantas vezes por segundo as cores são enviadas para o teclado.\n\nAlto = movimento mais fluido.\nBaixo = menos carga, mas entrecortado.",
            "每秒向键盘发送颜色的次数。\n\n高 = 动画更流畅。\n低 = 负载更低，但会显得卡顿。" },

        // ---- value formats / notes ----
        ["f.instant"] = new[] { "instant", "immediato", "inmediato", "immédiat", "sofort", "imediato", "即时" },
        ["f.still"] = new[] { "still", "fermo", "quieto", "fixe", "still", "parado", "静止" },
        ["f.never"] = new[] { "never", "mai", "nunca", "jamais", "nie", "nunca", "从不" },
        ["f.every"] = new[] { "every {0} s", "ogni {0} s", "cada {0} s", "toutes les {0} s", "alle {0} s", "a cada {0} s", "每 {0} 秒" },
        ["n.only"] = new[] { "{0} style only", "solo stile {0}", "solo estilo {0}", "style {0} uniquement", "nur Stil {0}", "apenas estilo {0}", "仅 {0} 样式" },

        // ---- interface ----
        ["live.style"] = new[] { "WAVE STYLE", "STILE DELL'ONDA", "ESTILO DE ONDA", "STYLE D'ONDE", "WELLENSTIL", "ESTILO DA ONDA", "波浪样式" },
        ["live.style.tip"] = new[] {
            "SMOOTH: new colors melt in gently from the center outwards, with a glow on the front.\n\nBARRIER: a thin band of switched-off keys sweeps across the keyboard from the center. Behind the band the new colors appear at once, ahead of it the old ones remain.",
            "SMOOTH: i nuovi colori si sciolgono dolcemente dal centro verso l'esterno, con un bagliore sul fronte.\n\nBARRIER: una fascia sottile di tasti spenti attraversa la tastiera dal centro. Dietro la fascia compaiono subito i nuovi colori, davanti restano i vecchi.",
            "SMOOTH: los nuevos colores se funden suavemente del centro hacia fuera, con un brillo en el frente.\n\nBARRIER: una banda fina de teclas apagadas recorre el teclado desde el centro. Detrás de la banda aparecen enseguida los nuevos colores; delante se mantienen los antiguos.",
            "SMOOTH : les nouvelles couleurs se fondent doucement du centre vers l'extérieur, avec une lueur sur le front.\n\nBARRIER : une fine bande de touches éteintes traverse le clavier depuis le centre. Derrière la bande apparaissent aussitôt les nouvelles couleurs, devant elle restent les anciennes.",
            "SMOOTH: Die neuen Farben blenden sanft von der Mitte nach außen über, mit einem Leuchten an der Front.\n\nBARRIER: Ein dünnes Band ausgeschalteter Tasten wandert von der Mitte über die Tastatur. Hinter dem Band erscheinen sofort die neuen Farben, davor bleiben die alten.",
            "SMOOTH: as novas cores esbatem-se suavemente do centro para fora, com um brilho na frente.\n\nBARRIER: uma faixa fina de teclas apagadas percorre o teclado a partir do centro. Atrás da faixa surgem logo as novas cores; à frente mantêm-se as antigas.",
            "SMOOTH：新颜色从中心向外柔和渐变，前沿带有光晕。\n\nBARRIER：一条细细的熄灭按键带从中心扫过键盘。按键带后方立即出现新颜色，前方仍保持旧颜色。" },
        ["card.smooth.sub"] = new[] { "Soft dissolve with glow", "Dissolvenza morbida con bagliore", "Fundido suave con brillo", "Fondu doux avec lueur", "Weiches Überblenden mit Leuchten", "Esbatimento suave com brilho", "柔和渐变，带光晕" },
        ["card.barrier.sub"] = new[] { "Crisp front with a band of dark keys", "Fronte netto con fascia di tasti spenti", "Frente nítido con banda de teclas apagadas", "Front net avec bande de touches éteintes", "Scharfe Front mit Band dunkler Tasten", "Frente nítida com faixa de teclas apagadas", "锐利前沿，带一圈熄灭的按键" },
        ["btn.wave"] = new[] { "PREVIEW WAVE", "ANTEPRIMA ONDA", "VISTA PREVIA DE ONDA", "APERÇU DE L'ONDE", "WELLE TESTEN", "PRÉ-VISUALIZAR ONDA", "预览波浪" },
        ["btn.wave.tip"] = new[] {
            "Starts the wave right away with the colors of the last active window, without having to switch window. Handy to try out your settings.",
            "Lancia subito l'onda con i colori dell'ultima finestra attiva, senza dover cambiare finestra. Utile per provare le impostazioni.",
            "Lanza la onda al instante con los colores de la última ventana activa, sin tener que cambiar de ventana. Útil para probar tus ajustes.",
            "Lance l'onde immédiatement avec les couleurs de la dernière fenêtre active, sans avoir à changer de fenêtre. Pratique pour tester vos réglages.",
            "Startet die Welle sofort mit den Farben des zuletzt aktiven Fensters, ohne das Fenster wechseln zu müssen. Praktisch zum Ausprobieren der Einstellungen.",
            "Inicia a onda de imediato com as cores da última janela ativa, sem ter de mudar de janela. Útil para experimentar as definições.",
            "立即使用最近一个活动窗口的颜色触发波浪，无需切换窗口，方便试验各项设置。" },
        ["autostart"] = new[] { "Start with Windows", "Avvia con Windows", "Iniciar con Windows", "Démarrer avec Windows", "Mit Windows starten", "Iniciar com o Windows", "开机自动启动" },
        ["hint"] = new[] {
            "Drag the icon from the ^ area near the clock onto the taskbar to keep it always visible.",
            "Trascina l'icona dall'area ^ vicino all'orologio sulla barra per averla sempre visibile.",
            "Arrastra el icono desde el área ^ junto al reloj a la barra de tareas para tenerlo siempre visible.",
            "Faites glisser l'icône depuis la zone ^ près de l'horloge vers la barre des tâches pour la garder toujours visible.",
            "Ziehen Sie das Symbol aus dem ^-Bereich neben der Uhr auf die Taskleiste, damit es immer sichtbar bleibt.",
            "Arraste o ícone da área ^ junto ao relógio para a barra de tarefas para o manter sempre visível.",
            "将图标从时钟旁的 ^ 区域拖到任务栏上，即可始终显示。" },
        ["tray.open"] = new[] { "Open settings", "Apri impostazioni", "Abrir ajustes", "Ouvrir les paramètres", "Einstellungen öffnen", "Abrir definições", "打开设置" },
        ["tray.style"] = new[] { "Wave style", "Stile onda", "Estilo de onda", "Style d'onde", "Wellenstil", "Estilo da onda", "波浪样式" },
        ["tray.smooth"] = new[] { "Smooth - soft dissolve", "Smooth - dissolvenza morbida", "Smooth - fundido suave", "Smooth - fondu doux", "Smooth - weiches Überblenden", "Smooth - esbatimento suave", "Smooth - 柔和渐变" },
        ["tray.barrier"] = new[] { "Barrier - band of dark keys", "Barrier - fascia di tasti spenti", "Barrier - banda de teclas apagadas", "Barrier - bande de touches éteintes", "Barrier - Band dunkler Tasten", "Barrier - faixa de teclas apagadas", "Barrier - 熄灭按键带" },
        ["tray.wave"] = new[] { "Preview wave", "Anteprima onda", "Vista previa de onda", "Aperçu de l'onde", "Welle testen", "Pré-visualizar onda", "预览波浪" },
        ["tray.exit"] = new[] { "Exit (restores the lights)", "Esci (ripristina le luci)", "Salir (restaura las luces)", "Quitter (restaure les lumières)", "Beenden (stellt die Beleuchtung wieder her)", "Sair (restaura as luzes)", "退出（恢复灯光）" },
        ["balloon"] = new[] {
            "It keeps running here: click the icon to reopen the panel.",
            "Continua a lavorare qui: clic sull'icona per riaprire il pannello.",
            "Sigue funcionando aquí: haz clic en el icono para reabrir el panel.",
            "Il continue de tourner ici : cliquez sur l'icône pour rouvrir le panneau.",
            "Läuft hier weiter: Klicken Sie auf das Symbol, um das Fenster wieder zu öffnen.",
            "Continua a funcionar aqui: clique no ícone para reabrir o painel.",
            "程序仍在此处运行：点击图标即可重新打开面板。" },
        ["status.starting"] = new[] { "Starting...", "Avvio...", "Iniciando...", "Démarrage...", "Start...", "A iniciar...", "正在启动…" },
        ["status.waiting"] = new[] { "Waiting for the keyboard...", "In attesa della tastiera...", "Esperando el teclado...", "En attente du clavier...", "Warte auf die Tastatur...", "A aguardar o teclado...", "正在等待键盘…" },
        ["status.connected"] = new[] { "Keyboard connected", "Tastiera collegata", "Teclado conectado", "Clavier connecté", "Tastatur verbunden", "Teclado ligado", "键盘已连接" },
        ["status.active"] = new[] { "Effect active - {0} keys", "Effetto attivo - {0} tasti", "Efecto activo - {0} teclas", "Effet actif - {0} touches", "Effekt aktiv - {0} Tasten", "Efeito ativo - {0} teclas", "效果运行中 - {0} 个按键" },
        ["lang.auto"] = new[] { "Automatic (system)", "Automatica (sistema)", "Automático (sistema)", "Automatique (système)", "Automatisch (System)", "Automático (sistema)", "自动（跟随系统）" },
        ["lang.tip"] = new[] { "Interface language", "Lingua dell'interfaccia", "Idioma de la interfaz", "Langue de l'interface", "Sprache der Oberfläche", "Idioma da interface", "界面语言" },
    };
}
