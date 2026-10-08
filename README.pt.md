<div align="center">

# LegionChromaFlow

**O fundo do seu ambiente de trabalho a fluir pelo teclado do seu Lenovo Legion.**<br>
Iluminação RGB dinâmica tecla a tecla com um painel de controlo ao estilo Razer Chroma — sem Lenovo Vantage, sem nuvem, sem dependências.

[![build](https://img.shields.io/github/actions/workflow/status/MarcoGigante/LegionChromaFlow/build.yml?branch=main&style=flat-square&label=build)](https://github.com/MarcoGigante/LegionChromaFlow/actions)
[![release](https://img.shields.io/github/v/release/MarcoGigante/LegionChromaFlow?style=flat-square)](https://github.com/MarcoGigante/LegionChromaFlow/releases)
[![license](https://img.shields.io/badge/license-GPL--3.0-44d62c?style=flat-square)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8%2B-512bd4?style=flat-square)

[English](README.md) · [Italiano](README.it.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · **Português** · [简体中文](README.zh.md)

<img src="docs/screenshot-live.png" alt="Painel de controlo do LegionChromaFlow com pré-visualização ao vivo do teclado" width="860">

</div>

---

## Funcionalidades

- 🖼️ **Luzes guiadas pelo fundo** — a imagem do ambiente de trabalho desloca-se e ondula lentamente pelas teclas, com pequenas ondas de luz aleatórias.
- 🪟 **Tom da janela ativa** — ao mudar de janela, as suas cores propagam-se **do centro do teclado para as extremidades** e mantêm-se enquanto essa janela estiver em foco.
- 🎯 **Só contam as cores reais** — os píxeis pretos, cinzentos e brancos são ignorados. Uma janela preta deixa o teclado com as cores do fundo.
- ⚡ **Dois estilos de onda** — *Smooth*, um esbatimento suave com brilho, ou *Barrier*, uma faixa fina de teclas apagadas que atravessa o teclado com as novas cores logo atrás.
- 🧈 **Transições fluidas** — um tempo de «acompanhamento» ajustável, para que as luzes deslizem até às cores da janela em vez de saltarem.
- 🎛️ **Painel ao estilo Razer** — interface escura, pré-visualização ao vivo do teclado e explicação de cada opção; as alterações aplicam-se de imediato e guardam-se sozinhas.
- 🌍 **7 idiomas** — inglês, italiano, espanhol, francês, alemão, português e chinês simplificado, à escolha no painel.
- 🔔 **Ícone na área de notificação** — clique esquerdo para mostrar/ocultar o painel, clique direito para o menu rápido.
- 🔌 **Zero dependências** — usa `hid.dll`, GDI+ e GDI. Só precisa de .NET.
- 🤖 **Cenas com IA opcionais** — o Claude pode criar uma cena de luzes (paleta, padrão, velocidade) para a janela que está a ver, para além dos outros efeitos. Desligadas por predefinição; requerem a sua própria chave de API.
- 🔒 **Offline e privado por predefinição** — nada sai do seu PC a menos que ative as cenas com IA. Os píxeis das janelas são lidos em memória e nunca guardados.

<div align="center">
<img src="docs/screenshot-window.png" alt="Página de definições das cores da janela" width="760">
</div>

## Início rápido

**Requisitos:** Windows 10/11 · [.NET 8 Desktop Runtime ou posterior](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.DesktopRuntime.8`) · um portátil Lenovo Legion com teclado **Spectrum RGB por tecla**.

1. **Transfira** o zip mais recente em [Releases](../../releases) e extraia-o (por exemplo para `C:\LegionChromaFlow`). Prefere compilar? Instale o SDK do .NET 8 e execute `build.bat`.
2. **No Lenovo Vantage**, escolha um perfil com o efeito *Legion Aurora Sync*, clique em **Aplicar** e depois **feche o Vantage por completo** (também na área de notificação). O Vantage e este programa não devem escrever no teclado ao mesmo tempo.
3. **Verifique o hardware**, por esta ordem:

   | Passo | Comando | O que faz |
   |---|---|---|
   | 1 | `probe.bat` | Encontra o teclado e lê o mapa de teclas e o perfil. **Não altera as luzes.** |
   | 2 | `test.bat` | Todas as teclas a vermelho → verde → azul durante alguns segundos e depois restaura o seu perfil. |
   | 3 | `OpenPanel.vbs` | Inicia o efeito e abre o painel de controlo. |

4. Gostou? Assinale **Iniciar com o Windows** no painel. O `run-hidden.vbs` inicia-o em silêncio na área de notificação; o `stop.bat` (ou *Sair* no menu do ícone) para-o e restaura o seu perfil de iluminação.

> **Dica:** o Windows 11 esconde os ícones novos atrás da seta `^`. Arraste o ícone do LegionChromaFlow para a barra de tarefas para o manter sempre visível.

## Painel de controlo

| Secção | O que pode ajustar |
|---|---|
| **Luzes** | Pré-visualização ao vivo do teclado, cartões de estilo, botão **Pré-visualizar onda** |
| **Onda** | Duração (segundos), espessura da barreira, suavidade da frente, brilho |
| **Janela** | Influência, **fluidez das cores**, limiares de cor e de brilho, frequência de leitura |
| **Aspeto** | Brilho, saturação, gama |
| **Ambiente de trabalho** | Velocidade do fundo, cintilação, ondas aleatórias, fotogramas por segundo |

Passe o rato sobre o ícone redondo **ⓘ** junto a cada opção para ver o que faz e o que significam os valores baixos e altos. O idioma escolhe-se no canto superior direito. Tudo é guardado em `config/config.json` (com os comentários) enquanto arrasta.

## Estilos de onda

| | **Smooth** | **Barrier** |
|---|---|---|
| Aspeto | As novas cores esbatem-se a partir do centro com um brilho suave | Uma faixa fina de teclas apagadas expande-se; as novas cores surgem logo atrás |
| Sensação | Fluida, ambiente | Nítida, tipo «scanner» |
| Opções próprias | Suavidade da frente, brilho | Espessura da barreira |

## Resolução de problemas

- **Teclado não encontrado** — execute `probe.bat` numa linha de comandos aberta **como administrador** e leia `logs\legionchromaflow.log`.
- **Cintilação ou cores que não mudam** — o Lenovo Vantage (ou outra ferramenta Legion) ainda está ativo. O `probe` lista os que deteta.
- **Teclado preso numa cor após um encerramento inesperado** — mude de perfil com `Fn + Espaço`, ou inicie a aplicação uma vez e use *Sair*.
- **Pouco fluido** — aumente *Fluidez das cores* na secção **Janela**.
- **Após suspensão/retoma** o programa volta a ligar-se sozinho.

Detalhes técnicos, referência da linha de comandos e tabela completa de configuração: consulte o [README em inglês](README.md).

## Luzes desde que liga

Por predefinição o efeito arranca assim que inicia sessão (tarefa ao iniciar sessão, sem o atraso da pasta Arranque) e **reativa-se sozinho após suspensão/retoma e desbloqueio**: ao acordar o portátil com o botão de ligar, volta o efeito em vez do modo próprio do teclado.

Para iluminar o teclado **ainda mais cedo — no ecrã de arranque/bloqueio do Windows, antes de alguém iniciar sessão —** execute uma vez `install-boot.bat` (pede permissões de administrador e instala uma tarefa de arranque que corre como SYSTEM). A instância de arranque usa uma pequena cópia em cache do seu fundo (guardada sempre que o painel corre) e passa o controlo ao painel no início de sessão sem cintilações. Remove-se com `uninstall-boot.bat`. A fase de firmware/BIOS antes de o Windows carregar não pode ser alterada por software.

## Cenas com IA (opcionais)

Ative **IA** no painel e o Claude cria uma cena de luzes — paleta, padrão (`aurora`, `pulse`, `wave`, `sparkle`, `rain`, `fire`, `breathe`), velocidade e intensidade — para a janela que está a ver. A cena sobrepõe-se aos efeitos do fundo e das cores da janela e esbate-se ao mudar de janela.

- **Desligadas por predefinição.** Requerem a sua própria chave de API da Anthropic: cole-a no painel (guardada cifrada para o seu utilizador do Windows com DPAPI em `config/ai.key`, excluído do git) ou defina a variável de ambiente `ANTHROPIC_API_KEY`.
- **O que é enviado:** ao mudar de janela, uma pequena captura JPEG dessa janela (máx. 768 px de largura) segue para `api.anthropic.com`, no máximo uma vez por *Intervalo mínimo* (12 s por predefinição). Com a função desligada não é enviado nada. As janelas cujo título contenha palavras como "password" ou "bank" são ignoradas (`AiSkipTitles` em `config.json`).
- **Custo:** os pedidos são faturados na sua própria conta Anthropic. O modelo predefinido é o pequeno e rápido `claude-haiku-4-5-20251001`; altere-o com `AiModel`.
- **Ajustes:** *Intensidade da IA*, *Intervalo mínimo* e *Esbatimento de cena* na secção **IA**.

## Compatibilidade e aviso legal

- Desenvolvido e testado no **Legion 7 16IRX9**. Outros Legion com o mesmo teclado Spectrum *deverão* funcionar, mas não foram testados: [abra uma issue](../../issues) com o resultado do `probe`.
- Projeto **não oficial**, sem qualquer ligação à Lenovo ou à Razer nem por elas apoiado. As marcas pertencem aos respetivos proprietários.
- Envia ao teclado os mesmos comandos que o software da Lenovo. **Utilize por sua conta e risco**; consulte a [licença](LICENSE) quanto à exclusão de garantia.
- Não é compatível com o Razer Chroma (não existe forma oficial de o integrar sem um App Id da Razer).

## Segurança e confiança

- Sem telemetria nem atualizações automáticas. A única função de rede é a opcional, desligada por predefinição, das [cenas com IA](#cenas-com-ia-opcionais), que só comunica com `api.anthropic.com`.
- As compilações oficiais são geradas apenas pelo [workflow de release](.github/workflows/release.yml) a partir de uma etiqueta e incluem um `SHA256SUMS.txt`. Binários de qualquer outra origem não são oficiais — consulte [SECURITY.md](SECURITY.md).
- Os forks são bem-vindos ao abrigo da GPL; só o mantenedor pode alterar este repositório.

## Créditos e licença

- O protocolo do teclado foi derivado do **Lenovo Legion Toolkit** da LenovoLegionToolkit-Team (GPL-3.0); por isso este projeto usa a mesma licença.
- Interface inspirada no estilo escuro e verde néon do Razer Synapse / Chroma Studio.
- Licença: [GPL-3.0](LICENSE). Se redistribuir o programa, tem de manter a mesma licença e disponibilizar o código-fonte.
