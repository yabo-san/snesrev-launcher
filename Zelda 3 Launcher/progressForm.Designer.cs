using System.Net.Http;
using System.Text;

using LibGit2Sharp;
using System.IO.Compression;

namespace Zelda_3_Launcher
{
    partial class progressForm : Form
    {
        public progressForm(string title, string message)
        {
            InitializeComponent();

            this.Text = title;
            this.updateLabel.Text = message;

            switch (title)
            {
                case "Repository Download":
                    this.Shown += new System.EventHandler(this.cloneRepo);
                    break;
                case "Copying ROM File":
                    this.Shown += new System.EventHandler(this.copyROM);
                    break;
                case "Downloading TCC":
                    this.Shown += new System.EventHandler(this.downloadTCC);
                    break;
                case "Downloading SDL2":
                    this.Shown += new System.EventHandler(this.downloadSDL2);
                    break;
                case "Downloading Python":
                    this.Shown += new System.EventHandler(this.downloadPython);
                    break;
                case "Downloading pip":
                    this.Shown += new System.EventHandler(this.downloadPip);
                    break;
            }
        }

        private async void copyROM(object sender, EventArgs e)
        {
            this.Refresh();
            var game = Program.game;
            var target = Path.Combine(Program.repoDir, game.Rom);

            if (File.Exists(target))
            {
                this.Close();
                return;
            }

            if (File.Exists(Path.Combine(Program.currentDirectory, game.Rom)))
            {
                File.Move(Path.Combine(Program.currentDirectory, game.Rom), target);
                this.Close();
                return;
            }

            // Look in the user's ROM folder first: any .sfc/.smc whose hash matches this game.
            // Hashing a whole ROM set, especially over a network share, takes seconds, so it runs
            // off the UI thread with the file being checked shown in the label.
            var found = await FindRomAsync(Program.settings.RomFolder, game.Sha1, game.Rom);
            if (found != null)
            {
                WriteRom(found, target);
                if (await CopyExtraRoms(game)) { this.Close(); return; }
                File.Delete(target);   // an extra ROM is missing: cancel, so the build does not start
                this.Close();
                return;
            }

            Boolean exit = false;
            var result = new OpenFileDialog();
            result.Filter = game.Name + " ROM (*.sfc;*.smc)|*.sfc;*.smc";
            if (Directory.Exists(Program.settings.RomFolder)) result.InitialDirectory = Program.settings.RomFolder;
            while (!exit)
            {
                if (result.ShowDialog() == DialogResult.OK)
                {
                    var hashCheck = checkHash(result.FileName);
                    if (hashCheck.success)
                    {
                        WriteRom(result.FileName, target);
                        if (!await CopyExtraRoms(game)) File.Delete(target);
                        exit = true;
                    }
                    else
                    {
                        var answer = MessageBox.Show("This ROM's hash doesn't match the version " + game.Name + " expects.\n\n" +
                            "The hash of the file selected is " + hashCheck.yourHash + ".\n\n" +
                            "The expected hash is " + hashCheck.hash + ".\n\n" +
                            "Use it anyway? Choose No to pick another file.", "ROM Hash Mismatch", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                        if (answer == DialogResult.Yes)
                        {
                            WriteRom(result.FileName, target);
                            exit = true;
                        }
                        else if (answer == DialogResult.Cancel) exit = true;
                    }
                }
                else exit = true;
            }

            this.Close();
        }

        // Each extra ROM the game needs (All-Stars): found by hash in the ROM folder, else asked for.
        // Returns false if one is missing, which cancels the build.
        private async Task<bool> CopyExtraRoms(Game game)
        {
            foreach (var extra in game.ExtraRoms)
            {
                var dest = Path.Combine(Program.repoDir, extra.SubDir, extra.Name);
                if (File.Exists(dest)) continue;
                var file = await FindRomAsync(Program.settings.RomFolder, extra.Sha1, extra.Name);
                if (file == null)
                {
                    var dlg = new OpenFileDialog { Filter = extra.Name + " (*.sfc;*.smc)|*.sfc;*.smc", Title = game.Name + " also needs " + extra.Name };
                    if (Directory.Exists(Program.settings.RomFolder)) dlg.InitialDirectory = Program.settings.RomFolder;
                    if (dlg.ShowDialog() != DialogResult.OK) { MessageBox.Show(game.Name + " needs " + extra.Name + " too. Process cancelled.", "ROM missing"); return false; }
                    var yours = Sha1Of(RomBytes(dlg.FileName));
                    if (!extra.Sha1.Contains(yours) &&
                        MessageBox.Show("This file's hash (" + yours + ") is not the one the port expects (" + string.Join(" or ", extra.Sha1) + ").\n\nUse it anyway?", "ROM Hash Mismatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return false;
                    file = dlg.FileName;
                }
                WriteRom(file, dest);
            }
            return true;
        }

        // Scans the ROM folder (and its subfolders) for a file matching one of the hashes, on a
        // worker thread; the label shows what is being checked.
        private async Task<string?> FindRomAsync(string folder, string[] sha1s, string wanted)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return null;
            progBar.Style = ProgressBarStyle.Marquee;
            var progress = new Progress<string>(name => updateLabel.Text = "Looking for " + wanted + ": " + name);
            return await Task.Run(() => FindRom(folder, sha1s, progress));
        }

        public static string? FindRom(string folder, string[] sha1s, IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return null;
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            foreach (var file in Directory.EnumerateFiles(folder, "*.*", options))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".sfc" && ext != ".smc") continue;
                try
                {
                    if (new FileInfo(file).Length > 8 * 1024 * 1024) continue;
                    progress?.Report(Path.GetFileName(file));
                    if (sha1s.Contains(Sha1Of(RomBytes(file)))) return file;
                }
                catch { }
            }
            return null;
        }

        // A ROM's bytes without the 512-byte copier header some dumps carry.
        public static byte[] RomBytes(string file)
        {
            var bytes = File.ReadAllBytes(file);
            return bytes.Length % 1024 == 512 ? bytes[512..] : bytes;
        }

        public static string Sha1Of(byte[] bytes) =>
            Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes));

        private static void WriteRom(string source, string target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, RomBytes(source));
        }

        // Every long step runs off the UI thread and reports back through the progress bar, so the
        // window never shows "Not Responding" and a stalled step cannot hang the launcher.
        private async void cloneRepo(object sender, EventArgs e)
        {
            this.Refresh();

            if (!await IsConnectedToInternet())
            {
                MessageBox.Show("Unable to reach github.com.\n\nCheck your internet connection and try again.", "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Dispose();
                return;
            }

            var game = Program.game;
            var repoDir = Program.repoDir;
            progBar.Style = ProgressBarStyle.Marquee;

            try
            {
                await Task.Run(() =>
                {
                    if (Repository.IsValid(repoDir))
                    {
                        using var repo = new Repository(repoDir);
                        var iniFile = Path.Combine(repoDir, game.Ini);
                        var iniBackup = Path.Combine(repoDir, "saves", game.Ini);
                        Directory.CreateDirectory(Path.Combine(repoDir, "saves"));
                        if (File.Exists(iniFile)) File.Copy(iniFile, iniBackup, true);

                        // Fetch, then reset to the remote's tip: a clean copy of upstream, keeping the ini.
                        var remote = repo.Network.Remotes["origin"];
                        Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(r => r.Specification), new FetchOptions(), null);
                        var tracked = repo.Head.TrackedBranch ?? repo.Branches["origin/" + repo.Head.FriendlyName] ?? repo.Branches["origin/main"] ?? repo.Branches["origin/master"];
                        if (tracked != null)
                            repo.Reset(ResetMode.Hard, tracked.Tip, new CheckoutOptions { OnCheckoutProgress = CheckoutProgress });

                        if (File.Exists(iniBackup)) File.Copy(iniBackup, iniFile, true);
                    }
                    else
                    {
                        if (Directory.Exists(repoDir)) Directory.Delete(repoDir, true);
                        Repository.Clone(game.RepoUrl, repoDir, new CloneOptions { OnCheckoutProgress = CheckoutProgress });
                    }
                });
            }
            catch (Exception ex)
            {
                File.AppendAllText(Program.logFile, "\n" + DateTime.Now + " repository: " + ex + "\n");
                MessageBox.Show("Could not download the " + game.Dir + " repository.\n\n" + ex.Message + "\n\nSee " + Program.logFile + ".", "Download failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.Close();
        }

        public void CheckoutProgress(string path, int completed, int total)
        {
            if (total <= 0) return;
            try
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    progBar.Style = ProgressBarStyle.Continuous;
                    progBar.Maximum = total;
                    progBar.Value = Math.Min(completed, total);
                }));
            }
            catch { }
        }

        private async void downloadTCC(object sender, EventArgs e)
        {
            this.Refresh();
            await downloadZip("tcc", "TCC.zip", new Uri(Program.game.TccUrl));
            this.Close();
        }

        private async void downloadSDL2(object sender, EventArgs e)
        {
            this.Refresh();
            await downloadZip("SDL2-" + Program.game.SdlVersion, "SDL2.zip", new Uri(Program.game.SdlUrl));
            this.Close();
        }

        private async void downloadPython(object sender, EventArgs e)
        {
            this.Refresh();
            // Pinned: the embeddable build the asset extraction was tested with.
            await downloadZip("assets", "Python.zip", new Uri("https://www.python.org/ftp/python/3.11.1/python-3.11.1-embed-amd64.zip"));
            this.Close();
        }

        static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        // Downloads to a file, reporting progress on the UI thread. Returns false on failure (already reported).
        private async Task<bool> downloadFile(Uri uri, string destination)
        {
            if (!await IsConnectedToInternet())
            {
                MessageBox.Show("Unable to reach github.com.\n\nCheck your internet connection and try again.", "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            progBar.Style = ProgressBarStyle.Marquee;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var tmp = destination + ".part";
            try
            {
                using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? -1;
                if (total > 0) { progBar.Style = ProgressBarStyle.Continuous; progBar.Maximum = 100; }

                await using var input = await response.Content.ReadAsStreamAsync();
                await using (var output = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, true))
                {
                    var buffer = new byte[1 << 16];
                    long done = 0; int read;
                    while ((read = await input.ReadAsync(buffer)) > 0)
                    {
                        await output.WriteAsync(buffer.AsMemory(0, read));
                        done += read;
                        if (total > 0) progBar.Value = (int)(done * 100 / total);
                    }
                }
                if (File.Exists(destination)) File.Delete(destination);
                File.Move(tmp, destination);
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                File.AppendAllText(Program.logFile, "\n" + DateTime.Now + " download " + uri + ": " + ex + "\n");
                MessageBox.Show("Download failed: " + uri + "\n\n" + ex.Message, "Download failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task downloadZip(string folder, string filename, Uri uri)
        {
            var directory = Path.Combine(Program.third_partyDir, folder);
            var zip = Path.Combine(Program.third_partyDir, filename);

            if (File.Exists(zip)) File.Delete(zip);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);

            if (!await downloadFile(uri, zip)) return;

            this.updateLabel.Text = "Extracting " + filename + " to " + folder + "...";
            progBar.Style = ProgressBarStyle.Marquee;
            try
            {
                await Task.Run(() =>
                {
                    if (filename.Equals("Python.zip")) ZipFile.ExtractToDirectory(zip, Path.Combine(Program.repoDir, "assets"), true);
                    else ZipFile.ExtractToDirectory(zip, Program.third_partyDir, true);
                });
            }
            catch (Exception ex)
            {
                File.AppendAllText(Program.logFile, "\n" + DateTime.Now + " extract " + filename + ": " + ex + "\n");
                MessageBox.Show("Could not extract " + filename + ".\n\n" + ex.Message, "Extract failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try { File.Delete(zip); } catch { }
            }
        }

        private async void downloadPip(object sender, EventArgs e)
        {
            this.Refresh();
            var destination = Path.Combine(Program.repoDir, "assets", "get-pip.py");
            await downloadFile(new Uri("https://bootstrap.pypa.io/get-pip.py"), destination);
            this.Close();
        }

        // One quick HTTPS request to the host the downloads come from. Pings are dropped by many
        // networks and VPNs, which made the old check report "offline" after a 30 second freeze.
        public static async Task<bool> IsConnectedToInternet()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var request = new HttpRequestMessage(HttpMethod.Head, "https://github.com/");
                using var response = await http.SendAsync(request, cts.Token);
                return true;
            }
            catch { return false; }
        }

        // Zelda 3's language ROMs (settings > Language). The main ROM check uses Game.Sha1.
        static readonly Dictionary<string, string[]> ZeldaLanguageHashes = new()
        {
            ["us"] = new[] { "6D4F10A8B10E10DBE624CB23CF03B88BB8252973" },
            ["de"] = new[] { "2E62494967FB0AFDF5DA1635607F9641DF7C6559" },
            ["fr"] = new[] { "229364A1B92A05167CD38609B1AA98F7041987CC" },
            ["fr-c"] = new[] { "C1C6C7F76FFF936C534FF11F87A54162FC0AA100" },
            ["en"] = new[] { "7C073A222569B9B8E8CA5FCB5DFEC3B5E31DA895" },
            ["es"] = new[] { "461FCBD700D1332009C0E85A7A136E2A8E4B111E" },
            ["pl"] = new[] { "3C4D605EEFDA1D76F101965138F238476655B11D" },
            ["pt"] = new[] { "D0D09ED41F9C373FE6AFDCCAFBF0DA8C88D3D90D" },
            ["redux"] = new[] { "B2A07A59E64C498BC1B2F28728F9BF4014C8D582", "9325C22EB0A2A1F0017157C8B620BC3A605CEDE1" },
            ["nl"] = new[] { "FA8ADFDBA2697C9A54D583A1284A22AC764C7637" },
            ["sv"] = new[] { "43CD3438469B2C3FE879EA2F410B3EF3CB3F1CA4" },
        };

        public (Boolean success, string hash, string yourHash) checkHash(string file, string version)
        {
            var yourHash = Sha1Of(RomBytes(file));
            if (!ZeldaLanguageHashes.TryGetValue(version, out var hashes)) return (false, "NULL", yourHash);
            return (hashes.Contains(yourHash), string.Join(" or ", hashes), yourHash);
        }

        public (Boolean success, string hash, string yourHash) checkHash(string file)
        {
            var game = Program.game;
            var yourHash = Sha1Of(RomBytes(file));
            return (game.Sha1.Contains(yourHash), string.Join(" or ", game.Sha1), yourHash);
        }

        private ProgressBar progBar;
        private Label updateLabel;
    }
}