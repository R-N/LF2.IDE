<div align="center">

![LF2 IDE Icon](icon.png)  
LF2 IDE  
[![Platforms](https://img.shields.io/badge/platform-windows-blue)](https://github.com/ahmetsait/LF2.IDE/releases) [![Latest Release](https://img.shields.io/github/v/release/ahmetsait/LF2.IDE)](https://github.com/ahmetsait/LF2.IDE/releases/latest) [![Downloads](https://img.shields.io/github/downloads/ahmetsait/LF2.IDE/total)](https://github.com/ahmetsait/LF2.IDE/releases) [![License](https://img.shields.io/github/license/ahmetsait/LF2.IDE)](LICENSE) [![Sponsor](https://img.shields.io/badge/Sponsor-black?logo=githubsponsors)](https://github.com/sponsors/ahmetsait)
=======
Little Figther 2 Integrated Development Environment
</div>

LF2 IDE is a graphical game modding toolkit for Little Fighter 2, also known as a "Data Changer". It allows you to edit data files (`*.dat`) of the game with wide range of convenient features to make it easier for LF2 data changers / modders. It was originally announced on Little Fighter Empire forums in this thread: https://lf-empire.de/forum/showthread.php?tid=9064

![Screenshot](screenshot.png)

Features
--------
- Code editor with syntax highlighting, code folding (also by level) and auto-complete support
- Integrated visual designer tools
- Instant Data Loader (IDL): Loads data files into LF2's memory allowing you to see your changes instantly
- Solution Explorer window for navigating the LF2 directory inside LF2 IDE
- Open any game folder (File > Open Game Folder…, or pass the folder on the command line); its data key is detected automatically, so mods with a non-standard key such as Combined Effort open directly
- Errors window that checks the active data file (unclosed blocks, duplicate frames, bad numbers, missing `next`/`hit_*` targets)
- Sprite Animator (plays frames following `next`/`wait`, exports GIF) and Object Browser (objects from `data.txt` with their sprite sheets)
- Tools: Data Key Finder, Frame Generator, Multi-line Find & Replace, Memory Reader, configurable external tools
- Edit > Advanced: delete empty lines, trim trailing spaces
- Integrated media player (Windows)
- Sprite tools such as transparency tool, image pixel formatter and sprite mirrorer
- Plugin support

Several of these come from [LFStudio](https://github.com/wirwl/lfs).

Linux
-----
LF2 IDE runs natively on Linux with Mono. The original LF2 runs under Wine; OpenLF2 can run natively without Wine.

```sh
sudo pacman -S mono                      # or your distro's mono / mono-complete package
xbuild /p:Configuration=Release /p:Platform=x86 LF2.IDE.sln
mono "LF2.IDE/bin/x86/Release/LF2 IDE.exe" ~/games/LF2   # opens the game folder as the project
```

- Games are started with `wine`; set `WINEPREFIX` before launching LF2 IDE to use a specific prefix.
- In Settings, set **Game command** to e.g. `/path/to/openlf2 --game "$(GameDir)" --reload` to start [OpenLF2](https://github.com/R-N/OpenLF2) instead; with `--reload` it picks up saved `.dat` changes live, no Wine or memory access needed. `$(GameExe)` is also available; leave the command empty for the default.
- Media files open in the desktop's default player instead of the integrated one.
- Instant Data Loader and Memory Reader write/read the game's memory through `/proc/<pid>/mem`. Wine starts the game outside LF2 IDE's process tree, so this needs `sudo sysctl kernel.yama.ptrace_scope=0` on distros that default to 1.
- Settings live in `~/.config/LF2.IDE`.

The code editor is [FastColoredTextBox](https://github.com/PavelTorgashov/FastColoredTextBox) (LGPLv3, source in `FastColoredTextBox/`, patched to run on Mono).

Downloads
---------
Head to [Releases](https://github.com/ahmetsait/LF2.IDE/releases) section for downloads.

License
-------
LF2 IDE is licensed under the [MIT License](LICENSE).

About Little Fighter 2
----------------------
LF2 is a 2D beat 'em up fighting game made by Marti Wong & Starsky Wong. It is extremely customizable with its data file architecture allowing modders to create characters, weapons, levels and even stories from scratch.

![LF2](lf2.png)  
See the official website: https://lf2.net
