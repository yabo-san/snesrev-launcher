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

    // One snesrev port. Everything the launcher used to hardcode for Zelda 3 lives here, so the
    // same download/build/launch flow works for each game. Entries may share a Dir and launch the
    // same exe with different arguments (LaunchArgs).
    public sealed class Game
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string RepoUrl { get; init; } = "";
        public string Dir { get; init; } = "";            // folder next to the launcher
        public string Rom { get; init; } = "";            // file name the port expects
        public string Exe { get; init; } = "";
        public string LaunchArgs { get; init; } = "";     // passed to Exe on launch, if any
        public string Ini { get; init; } = "";
        public string[] Sha1 { get; init; } = Array.Empty<string>(); // accepted ROM hashes, upper case, no copier header
        public ExtraRom[] ExtraRoms { get; init; } = Array.Empty<ExtraRom>();
        public string SdlVersion { get; init; } = "";     // the version the game's run_with_tcc.bat expects
        public string TccUrl { get; init; } = "";
        // Asset step, run before the build with the bundled Python: PipPackages installed first
        // (pinned), then AssetCommand run by cmd in the game dir. Empty = no Python needed.
        public string[] PipPackages { get; init; } = Array.Empty<string>();
        public string AssetCommand { get; init; } = "";
        public bool FullSettings { get; init; }           // the settings and keymapper forms are written for zelda3.ini

        public bool ExtractAssets => AssetCommand.Length > 0;
        public string SdlUrl => $"https://github.com/libsdl-org/SDL/releases/download/release-{SdlVersion}/SDL2-devel-{SdlVersion}-VC.zip";

        const string SmwRepo = "https://github.com/snesrev/smw.git";
        const string SmwSha1 = "6B47BB75D16514B6A476AA0C73A683A2A4C18765";          // USA
        const string Tcc2023 = "https://github.com/FitzRoyX/tinycc/releases/download/tcc_20230519/tcc_20230519.zip";
        // smw builds smw_assets.dat from the ROM with assets/restool.py (standard library only).
        const string SmwAssets = "cd .\\assets && python restool.py";
        // Not set up on purpose: smw can also run Super Mario Bros. and The Lost Levels from the
        // Super Mario All-Stars ROM (smas.sfc in other/, sha1 C05817C5..., other/extract.py with
        // zstandard, then smw.exe smb1.sfc / smbll.sfc). The owner would rather people play the
        // original NES games than a port of a port. ExtraRoms + LaunchArgs are the hook if you want it.

        public static readonly Game[] All =
        {
            new Game
            {
                Id = "zelda3", Name = "Zelda 3 (A Link to the Past)",
                RepoUrl = "https://github.com/snesrev/zelda3.git", Dir = "zelda3",
                Rom = "zelda3.sfc", Exe = "zelda3.exe", Ini = "zelda3.ini",
                Sha1 = new[] { "6D4F10A8B10E10DBE624CB23CF03B88BB8252973" },
                SdlVersion = "2.26.3",
                TccUrl = "https://github.com/FitzRoyX/tinycc/releases/download/tcc_20221020/tcc_20221020.zip",
                PipPackages = new[] { "pillow==11.3.0", "pyyaml==6.0.2" },
                AssetCommand = "cd .\\assets && python restool.py --extract-from-rom",
                FullSettings = true,
            },
            new Game
            {
                Id = "sm", Name = "Super Metroid",
                RepoUrl = "https://github.com/snesrev/sm.git", Dir = "sm",
                Rom = "sm.smc", Exe = "sm.exe", Ini = "sm.ini",
                Sha1 = new[] { "DA957F0D63D14CB441D215462904C4FA8519C613" },
                SdlVersion = "2.24.1",
                TccUrl = "https://github.com/FitzRoyX/tinycc/releases/download/tcc_20221020/tcc_20221020.zip",
            },
            new Game
            {
                Id = "smw", Name = "Super Mario World",
                RepoUrl = SmwRepo, Dir = "smw",
                Rom = "smw.sfc", Exe = "smw.exe", Ini = "smw.ini",
                Sha1 = new[] { SmwSha1 },
                SdlVersion = "2.28.1", TccUrl = Tcc2023,
                AssetCommand = SmwAssets,
            },
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
