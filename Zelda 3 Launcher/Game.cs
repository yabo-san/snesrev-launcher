using System.Reflection;
using System.Text.Json;

namespace Zelda_3_Launcher
{
    // A ROM a game needs besides its main one. No shipped game uses it; see the note above Game.All.
    public sealed class ExtraRom
    {
        public string Name { get; init; } = "";       // file name the port expects
        public string SubDir { get; init; } = "";     // folder under the game dir it goes in ("" = root)
        public string[] Sha1 { get; init; } = Array.Empty<string>();
    }

    // One snesrev port. This repo's CI (.github/workflows/games.yml) assembles a build kit per game
    // from a pinned snesrev commit: the source, TCC, SDL2, an embedded Python where the asset step
    // needs one, and build.cmd with the port's own compile line. CI compiles a copy to prove the kit
    // builds, then attaches the kit (not the compiled game) to every release as <Dir>-win-x64.zip.
    // The launcher downloads that kit, finds the ROM by hash, runs the asset step, runs build.cmd.
    public sealed class Game
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Dir { get; init; } = "";            // folder next to the launcher; also the package name
        public string Rom { get; init; } = "";            // file name the port expects
        public string Exe { get; init; } = "";
        public string LaunchArgs { get; init; } = "";     // passed to Exe on launch, if any
        public string Ini { get; init; } = "";
        public string[] Sha1 { get; init; } = Array.Empty<string>(); // accepted ROM hashes, upper case, no copier header
        public ExtraRom[] ExtraRoms { get; init; } = Array.Empty<ExtraRom>();
        // Asset step, run by cmd in the game dir after the ROM is in place, using the kit's own
        // python\python.exe. Empty = the port reads the ROM directly.
        public string AssetCommand { get; init; } = "";
        public bool FullSettings { get; init; }           // the settings and keymapper forms are written for zelda3.ini

        public bool ExtractAssets => AssetCommand.Length > 0;

        // The kit for this game, from the same release as the running launcher, so the two always
        // match. Written next to the game after a download as PackageMarker.
        public static readonly string LauncherVersion = (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0)).ToString(3);
        public const string Releases = "https://github.com/yabo-san/snesrev-launcher/releases/download";
        public string PackageUrl => $"{Releases}/v{LauncherVersion}/{Dir}-win-x64.zip";
        public const string PackageMarker = ".snesrev-package";

        public static readonly Game[] All =
        {
            new Game
            {
                Id = "zelda3", Name = "Zelda 3 (A Link to the Past)", Dir = "zelda3",
                Rom = "zelda3.sfc", Exe = "zelda3.exe", Ini = "zelda3.ini",
                Sha1 = new[] { "6D4F10A8B10E10DBE624CB23CF03B88BB8252973" },
                AssetCommand = "cd .\\assets && ..\\python\\python.exe restool.py --extract-from-rom",
                FullSettings = true,
            },
            new Game
            {
                Id = "sm", Name = "Super Metroid", Dir = "sm",
                Rom = "sm.smc", Exe = "sm.exe", Ini = "sm.ini",
                Sha1 = new[] { "DA957F0D63D14CB441D215462904C4FA8519C613" },
            },
            new Game
            {
                Id = "smw", Name = "Super Mario World", Dir = "smw",
                Rom = "smw.sfc", Exe = "smw.exe", Ini = "smw.ini",
                Sha1 = new[] { "6B47BB75D16514B6A476AA0C73A683A2A4C18765" },
                AssetCommand = "cd .\\assets && ..\\python\\python.exe restool.py",
            },
            // Not set up on purpose: smw can also run Super Mario Bros. and The Lost Levels from the
            // Super Mario All-Stars ROM (smas.sfc in other/, sha1 C05817C5..., other/extract.py with
            // zstandard, then smw.exe smb1.sfc / smbll.sfc). Purposely not implemented; feel free to
            // wire it in. ExtraRoms + LaunchArgs are the hook; the kit would need other/ and zstandard.
        };

        public static Game ById(string? id) => All.FirstOrDefault(g => g.Id == id) ?? All[0];
    }

    // Launcher settings that are not part of a game's .ini: which game, and where the user's ROMs are.
    public sealed class LauncherSettings
    {
        public string GameId { get; set; } = "zelda3";
        public string RomFolder { get; set; } = "";

        static string PathOnDisk => Path.Combine(Program.currentDirectory, "launcher.json");

        public static LauncherSettings Load()
        {
            try
            {
                if (File.Exists(PathOnDisk))
                    return JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(PathOnDisk)) ?? new LauncherSettings();
            }
            catch { }
            return new LauncherSettings();
        }

        public void Save() =>
            File.WriteAllText(PathOnDisk, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
