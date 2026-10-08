<div align="center">

# LegionChromaFlow

**让桌面壁纸在你的联想 Legion 键盘上流动起来。**<br>
逐键动态 RGB 灯效，配有 Razer Chroma 风格的控制面板 —— 无需 Lenovo Vantage，无需云端，零依赖。

[![build](https://img.shields.io/github/actions/workflow/status/MarcoGigante/LegionChromaFlow/build.yml?branch=main&style=flat-square&label=build)](https://github.com/MarcoGigante/LegionChromaFlow/actions)
[![release](https://img.shields.io/github/v/release/MarcoGigante/LegionChromaFlow?style=flat-square)](https://github.com/MarcoGigante/LegionChromaFlow/releases)
[![license](https://img.shields.io/badge/license-GPL--3.0-44d62c?style=flat-square)](LICENSE)
![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8%2B-512bd4?style=flat-square)

[English](README.md) · [Italiano](README.it.md) · [Español](README.es.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Português](README.pt.md) · **简体中文**

<img src="docs/screenshot-live.png" alt="带有键盘实时预览的 LegionChromaFlow 控制面板" width="860">

</div>

---

## 功能特点

- 🖼️ **壁纸驱动的灯光** —— 桌面图像在按键上缓缓漂移、摇曳，并不时泛起随机的小光波。
- 🪟 **活动窗口着色** —— 切换窗口时，其颜色会**从键盘中心向边缘扩散**，并在该窗口保持前台期间一直保留。
- 🎯 **只认真实颜色** —— 黑、灰、白像素会被忽略。窗口是黑色时，键盘保持壁纸颜色。
- ⚡ **两种波浪样式** —— *Smooth*：带光晕的柔和渐变；*Barrier*：一条细细的熄灭按键带扫过键盘，新颜色紧随其后出现。
- 🧈 **丝滑过渡** —— 可调的“跟随时间”，让灯光平滑地过渡到窗口颜色，而不是一格一格地跳。
- 🎛️ **Razer 风格面板** —— 深色界面、键盘实时预览、每个选项都有说明；修改立即生效并自动保存。
- 🌍 **7 种语言** —— 英语、意大利语、西班牙语、法语、德语、葡萄牙语和简体中文，可在面板中切换。
- 🔔 **系统托盘图标** —— 左键显示/隐藏面板，右键打开快捷菜单。
- 🔌 **零依赖** —— 使用 `hid.dll`、GDI+ 和 GDI，只需安装 .NET。
- 🤖 **可选的 AI 场景** —— Claude 可以为你正在看的窗口设计灯光场景（配色、图案、速度），叠加在其他效果之上。默认关闭；需要你自己的 API 密钥。
- 🔒 **默认离线且私密** —— 除非开启 AI 场景，否则不会有任何数据离开你的电脑。窗口像素仅在内存中读取，从不保存。

<div align="center">
<img src="docs/screenshot-window.png" alt="窗口颜色设置页面" width="760">
</div>

## 快速开始

**系统要求：** Windows 10/11 · [.NET 8 桌面运行时或更高版本](https://dotnet.microsoft.com/download)（`winget install Microsoft.DotNet.DesktopRuntime.8`）· 配有 **Spectrum 逐键 RGB** 键盘的联想 Legion 笔记本。

1. 从 [Releases](../../releases) **下载**最新的 zip 并解压（例如解压到 `C:\LegionChromaFlow`）。想自己编译？安装 .NET 8 SDK 后运行 `build.bat`。
2. **在 Lenovo Vantage 中**选择使用 *Legion Aurora Sync* 效果的配置文件，点击**应用**，然后**彻底关闭 Vantage**（包括托盘中的图标）。Vantage 与本程序不能同时写入键盘。
3. **按顺序检查硬件：**

   | 步骤 | 命令 | 作用 |
   |---|---|---|
   | 1 | `probe.bat` | 查找键盘，读取按键映射和配置文件。**不会改变灯光。** |
   | 2 | `test.bat` | 所有按键依次显示红 → 绿 → 蓝各数秒，然后恢复你的配置文件。 |
   | 3 | `OpenPanel.vbs` | 启动灯效并打开控制面板。 |

4. 满意吗？在面板中勾选**开机自动启动**。`run-hidden.vbs` 会静默启动并驻留托盘；`stop.bat`（或托盘菜单中的*退出*）会停止程序并恢复你的灯光配置文件。

> **提示：** Windows 11 会把新图标收进 `^` 箭头里。把 LegionChromaFlow 图标拖到任务栏上，即可始终显示。

## 控制面板

| 分区 | 可调内容 |
|---|---|
| **灯光** | 键盘实时预览、样式卡片、**预览波浪**按钮 |
| **波浪** | 时长（秒）、屏障厚度、前沿柔和度、光晕 |
| **窗口** | 影响度、**颜色平滑度**、颜色与亮度阈值、读取频率 |
| **外观** | 亮度、饱和度、伽马 |
| **桌面** | 壁纸速度、按键闪烁、随机光波、每秒帧数 |

把鼠标移到每个选项旁边的圆形 **ⓘ** 图标上，即可看到它的作用以及低值、高值的含义。语言在右上角选择。拖动滑块时，所有设置会实时写回 `config/config.json`（保留注释）。

## 波浪样式

| | **Smooth** | **Barrier** |
|---|---|---|
| 外观 | 新颜色从中心起柔和渐变，带光晕 | 一条细细的熄灭按键带向外扩展；新颜色紧随其后出现 |
| 感觉 | 流畅、氛围感 | 利落、“扫描仪”式 |
| 专属选项 | 前沿柔和度、光晕 | 屏障厚度 |

## 故障排除

- **找不到键盘** —— 以**管理员身份**打开命令提示符运行 `probe.bat`，并查看 `logs\legionchromaflow.log`。
- **闪烁或颜色不变** —— Lenovo Vantage（或其他 Legion 工具）仍在运行。`probe` 会列出它检测到的程序。
- **异常退出后键盘停在某一种颜色** —— 用 `Fn + 空格键` 切换配置文件，或启动一次本程序后选择*退出*。
- **不够流畅** —— 在**窗口**分区中调高*颜色平滑度*。
- **睡眠/唤醒后**程序会自动重新连接。

技术细节、命令行参考和完整配置表请参阅[英文 README](README.md)。

## 开机即亮

默认情况下，灯效在你登录时立即启动（使用登录时的计划任务，没有“启动”文件夹的延迟），并且会**在睡眠唤醒和解锁后自动重新接管键盘**，因此用电源键唤醒笔记本时，会恢复本程序的灯效，而不是键盘自带的模式。

若想让键盘**更早亮起 —— 在 Windows 启动/锁屏界面、任何人登录之前** —— 请运行一次 `install-boot.bat`（会请求管理员权限，并安装一个以 SYSTEM 身份运行的开机任务）。开机实例使用壁纸的小尺寸缓存（面板每次运行时都会更新），并在你登录时无闪烁地把控制权交给面板。用 `uninstall-boot.bat` 即可移除。Windows 加载之前的固件/BIOS 阶段无法通过软件更改。

## AI 场景（可选）

在面板中开启 **AI**，Claude 就会为你正在看的窗口设计灯光场景 —— 配色、图案（`aurora`、`pulse`、`wave`、`sparkle`、`rain`、`fire`、`breathe`）、速度和强度。该场景叠加在壁纸和窗口颜色效果之上，切换窗口时会平滑过渡。

- **默认关闭。** 需要你自己的 Anthropic API 密钥：在面板中粘贴（通过 DPAPI 为你的 Windows 用户加密保存在 `config/ai.key`，已被 git 忽略），或设置环境变量 `ANTHROPIC_API_KEY`。
- **发送的内容：** 切换窗口时，会把该窗口的一张小 JPEG 截图（宽度最大 768 像素）发送到 `api.anthropic.com`，每个*最小间隔*（默认 12 秒）至多一次。功能关闭时不会发送任何内容。标题包含 "password"、"bank" 等词的窗口会被跳过（见 `config.json` 中的 `AiSkipTitles`）。
- **费用：** 请求计入你自己的 Anthropic 账户。默认模型是小巧快速的 `claude-haiku-4-5-20251001`，可通过 `AiModel` 更改。
- **调节：** 在 **AI** 分区中调整 *AI 强度*、*最小间隔* 和 *场景渐变*。

## 兼容性与免责声明

- 开发并测试于 **Legion 7 16IRX9**。使用相同 Spectrum 键盘的其他 Legion 机型*应该*可用，但尚未测试：请[提交 issue](../../issues) 并附上 `probe` 的输出。
- 这是**非官方**项目，与联想或 Razer 无关，也未获其认可。所有商标归各自所有者所有。
- 本程序向键盘控制器发送的命令与联想自家软件相同。**使用风险自负**；免责条款见[许可证](LICENSE)。
- 与 Razer Chroma 不兼容（没有 Razer App Id 就无法官方接入）。

## 安全与信任

- 无遥测、无自动更新。唯一的网络功能是可选且默认关闭的 [AI 场景](#ai-场景可选)，它只与 `api.anthropic.com` 通信。
- 官方构建仅由[发布工作流](.github/workflows/release.yml)基于标签生成，并附带 `SHA256SUMS.txt`。来自其他任何渠道的二进制文件都不是官方版本 —— 见 [SECURITY.md](SECURITY.md)。
- 欢迎在 GPL 下 fork；只有维护者可以修改本仓库。

## 致谢与许可证

- 键盘协议源自 LenovoLegionToolkit-Team 的 **Lenovo Legion Toolkit**（GPL-3.0），因此本项目采用相同的许可证。
- 界面灵感来自 Razer Synapse / Chroma Studio 的深色霓虹绿风格。
- 许可证：[GPL-3.0](LICENSE)。如需再分发本程序，必须保持相同许可证并提供源代码。
