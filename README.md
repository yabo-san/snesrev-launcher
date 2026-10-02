# snesrev launcher

One launcher for the [snesrev](https://github.com/snesrev) PC ports: **Zelda 3 (A Link to the Past)**,
**Super Metroid** and **Super Mario World**. Pick the game, point it at your ROM folder, press Download,
press Launch.

A fork of [RadzPrower's Zelda 3 Launcher](https://github.com/RadzPrower/Zelda-3-Launcher) (and its
[Super Metroid conversion](https://github.com/RadzPrower/Super-Metroid-Launcher)). His UI, his settings
and keymapper windows; this fork makes it one launcher for all three games and makes it stop breaking.

## What you need

- Windows 10 or later, 64-bit.
- Your own ROM of the game. Any file name is fine: the launcher finds it by hash in the folder you pick
  (subfolders too), and copies it in under the name the port expects.

| game | ROM it looks for |
|---|---|
| Zelda 3 | A Link to the Past, USA (other languages via Settings) |
| Super Metroid | Super Metroid (Japan, USA) |
| Super Mario World | Super Mario World, USA |

**Super Mario Bros. and The Lost Levels (All-Stars): not implemented.** snesrev's smw can run those two
from the Super Mario All-Stars ROM. I purposely didn't bother implementing them. Feel free to wire it
in; the hook is there (an extra ROM per game, `ExtraRom` in `Game.cs`).

That's it. Nothing to install: no .NET, no Python, no compiler, no git. Everything the game needs
to build comes down in one zip.

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
3. **Download**: fetches the game's build kit (one zip per game, attached to the same release as the
   launcher), finds your ROM and copies it in, extracts the assets from the ROM where the port needs
   that (Zelda 3, Super Mario World; with the Python inside the kit), then runs the kit's `build.cmd`
   to compile the game.
4. **Launch**.
5. **Settings** (Zelda 3): RadzPrower's full settings and keymapper windows, below. Super Metroid and
   Super Mario World open their `.ini` in Notepad; see each port's README for the keys.

The Download button becomes **Update** when the game on disk came from another launcher version,
**Re-build** otherwise (it redoes the ROM, asset and build steps on the kit already there).
Each game writes a log next to the exe (`zelda3.log`, `sm.log`, `smw.log`); look there first if
something fails.

## What changed from RadzPrower's launcher

- One launcher, three games. The game list is `Game.cs`; adding a port is one entry.
- Finds your ROM by hash in a folder you choose, instead of asking for the file every time. Headered
  ROMs are accepted. A mismatched hash warns but lets you continue.
- Downloads and the ROM search run on a worker thread instead of the UI thread. The connectivity
  check is one HTTPS request to github.com instead of pings.
- Runs on a supported .NET (10, LTS) as one self-contained exe. The original targeted .NET 7, out of
  support since May 2024, and would not start without that exact runtime.
- The build inputs are packaged, not fetched piecemeal. The original cloned the source with git and
  downloaded a compiler, SDL, Python and pip separately on every user's machine, so what got built
  depended on that machine (its git library failed on a global git setting it did not know, for
  one). Now `.github/workflows/games.yml` assembles one **build kit** per game in CI: the source at
  a pinned snesrev commit, the same TCC and SDL2 the port's own `run_with_tcc.bat` names, an
  embedded Python with pinned packages where the asset step needs one, and a `build.cmd` with the
  port's exact compile line. CI compiles a copy of the kit to prove it builds, then attaches the
  kit (not the compiled game) to the release. Your PC runs `build.cmd` on the same bytes everyone
  else gets. No game binaries are hosted here; `KIT.txt` inside each kit says what it was made from.
- Pinned everything else too: the .NET SDK (`global.json`), NuGet packages (`packages.lock.json`).
- Built and released by CI, not by hand. See `.github/workflows/`.

## Settings menu (Zelda 3)

RadzPrower's documentation of the settings and keymapper windows, unchanged, is in
[docs/RADZPROWER-SETTINGS.md](docs/RADZPROWER-SETTINGS.md).

## Credits and license

RadzPrower for the launcher this is forked from. snesrev for the ports. MIT, as upstream; see
[LICENSE](LICENSE). Not affiliated with Nintendo. No ROMs are included or downloaded; you bring your own.
