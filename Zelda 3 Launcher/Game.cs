using System.Text.Json;

namespace Zelda_3_Launcher
{
    // One snesrev port. Everything the launcher used to hardcode for Zelda 3 lives here, so the
    // same download/build/launch flow works for each game.
    public sealed class Game
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string RepoUrl { get; init; } = "";
        public string Dir { get; init; } = "";            // folder next to the launcher
        public string Rom { get; init; } = "";            // file name the port expects
        public string Exe { get; init; } = "";
        public string Ini { get; init; } = "";
        public string[] Sha1 { get; init; } = Array.Empty<string>(); // accepted ROM hashes, upper case, no copier header
        public string SdlVersion { get; init; } = "";     // the version the game's run_with_tcc.bat expects
        public string TccUrl { get; init; } = "";
        public bool ExtractAssets { get; init; }          // Zelda 3 builds zelda3_assets.dat from the ROM with Python
        public bool FullSettings { get; init; }           // the settings and keymapper forms are written for zelda3.ini

        public string SdlUrl => $"https://github.com/libsdl-org/SDL/releases/download/release-{SdlVersion}/SDL2-devel-{SdlVersion}-VC.zip";

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
                ExtractAssets = true, FullSettings = true,
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
                RepoUrl = "https://github.com/snesrev/smw.git", Dir = "smw",
                Rom = "smw.sfc", Exe = "smw.exe", Ini = "smw.ini",
                Sha1 = new[] { "6B47BB75D16514B6A476AA0C73A683A2A4C18765" },
                SdlVersion = "2.28.1",
                TccUrl = "https://github.com/FitzRoyX/tinycc/releases/download/tcc_20230519/tcc_20230519.zip",
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
