# snesrev launcher

One launcher for the [snesrev](https://github.com/snesrev) PC ports of SNES games: **Zelda 3 (A Link
to the Past)**, **Super Metroid** and **Super Mario World**. Pick the game, point it at your ROM folder,
press Download, press Launch.

A fork of [RadzPrower's Zelda 3 Launcher](https://github.com/RadzPrower/Zelda-3-Launcher) (and its
[Super Metroid conversion](https://github.com/RadzPrower/Super-Metroid-Launcher)). His UI, his settings
and keymapper windows; this fork makes it one launcher for all three games and makes it stop breaking.

## What you need

- Windows 10 or later, 64-bit.
- Your own ROM of the game. Any file name is fine: the launcher finds it by hash in the folder you pick
  (subfolders too), and copies it in under the name the port expects.

That's it. No .NET to install, no Python, no compiler to set up.

## Install

Download `snesrev-launcher-x.y.z-win-x64.exe` from the
[latest release](https://github.com/yabo-san/snesrev-launcher/releases/latest), put it in a folder of
its own, and run it. Windows SmartScreen will warn because the exe isn't signed: **More info, then Run
anyway**. Each game gets its own subfolder next to the exe.

Every release carries a build provenance attestation. To check a download came from this repo's
pipeline:

    gh attestation verify snesrev-launcher-x.y.z-win-x64.exe --repo yabo-san/snesrev-launcher

## Using it

1. Pick the game at the top.
2. **Choose ROM folder** once. It's remembered in `launcher.json` next to the exe.
3. **Download**: fetches the port's source, your ROM, the compiler and SDL, builds the game. A few
   minutes the first time. Zelda 3 also extracts its assets from the ROM (this is the one step that
   uses a bundled Python).
4. **Launch**.
5. **Settings** (Zelda 3): RadzPrower's full settings and keymapper windows, below. Super Metroid and
   Super Mario World open their `.ini` in Notepad; see each port's README for the keys.

The Download button becomes **Update** when the port has new commits, **Re-build** otherwise.
Each game writes a log next to the exe (`zelda3.log`, `sm.log`, `smw.log`); look there first if
something fails.

## What changed from RadzPrower's launcher

- One launcher, three games. The game list is `Game.cs`; adding a port is one entry.
- Finds your ROM by hash in a folder you choose, instead of asking for the file every time. Headered
  ROMs are accepted. A mismatched hash warns but lets you continue.
- Does not freeze. Downloads and the git clone run off the UI thread; the old "is the internet up?"
  ping, which many networks drop, is one quick HTTPS request.
- Runs on a supported .NET (10, LTS) as one self-contained exe. The original targeted .NET 7, out of
  support since May 2024, and would not start without that exact runtime.
- Pinned everything: the .NET SDK (`global.json`), NuGet packages (`packages.lock.json`), the Python
  packages used for Zelda 3's asset extraction. The original installed whatever was newest that day.
- Built and released by CI, not by hand. See `.github/workflows/`.

## Settings menu (Zelda 3)

RadzPrower's documentation of the settings and keymapper windows, unchanged, is in
[docs/RADZPROWER-SETTINGS.md](docs/RADZPROWER-SETTINGS.md).

## Credits and license

RadzPrower for the launcher this is forked from. snesrev for the ports. MIT, as upstream; see
[LICENSE](LICENSE). Not affiliated with Nintendo. No ROMs are included or downloaded; you bring your own.
