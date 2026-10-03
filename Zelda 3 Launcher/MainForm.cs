using System.Diagnostics;
using System.Text;

namespace Zelda_3_Launcher
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            foreach (var g in Game.All) this.gamePicker.Items.Add(g.Name);
            this.gamePicker.SelectedIndex = Array.FindIndex(Game.All, g => g.Id == Program.game.Id);
            this.gamePicker.SelectedIndexChanged += new System.EventHandler(this.gamePicker_Changed);
            UpdateRomFolderLabel();

            UpdateMainForm();
        }

        private void launch_click(object sender, EventArgs e)
        {
            this.launch.Text = "Launching...";
            this.Enabled = false;

            this.build.Enabled= false;
            this.settings.Enabled = false;

            this.launch.Text = "Running...";
            this.WindowState = FormWindowState.Minimized;
            if (runProcess("cmd.exe", "/C .\\" + Program.game.Exe + (Program.game.LaunchArgs.Length > 0 ? " " + Program.game.LaunchArgs : "")))
            {
                MessageBox.Show("Error occurred while launching " + Program.game.Name + ".\n\nPlease refer to " + Program.logFile + " for further details.");
            }

            this.launch.Text = "Launch";
            this.Enabled = true;

            this.build.Enabled = true;
            this.settings.Enabled = true;

            this.WindowState = FormWindowState.Normal;
        }

        private void build_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(Program.settings.RomFolder)) ChooseRomFolder();

            var game = Program.game;
            this.build.Text = "Downloading...";
            this.build.Enabled = false;
            this.gamePicker.Enabled = false;
            this.romFolder.Enabled = false;

            this.launch.Enabled = false;
            this.settings.Enabled = false;

            using (progressForm getGame = new progressForm("Game Download", "Downloading " + game.Name + " build kit..."))
            {
                if (getGame.ShowDialog() == DialogResult.OK)
                {
                    getGame.Dispose();
                }
            }

            if (!File.Exists(Path.Combine(Program.repoDir, "build.cmd")))
            {
                ExitBuild();
                return;
            }

            using (progressForm copyROM = new progressForm("Copying ROM File", "Copying ROM file to proper directory..."))
            {
                if (copyROM.ShowDialog() == DialogResult.OK)
                {
                    copyROM.Dispose();
                }
            }

            if (!File.Exists(Path.Combine(Program.repoDir, game.Rom)))
            {
                MessageBox.Show("No ROM provided. Process cancelled.", "No ROM", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateMainForm();
                return;
            }

            this.build.Text = "Preparing...";

            progressCompile.Visible = true;
            labelCompileStatus.Visible = true;
            progressCompile.Value = 0;

            if (game.ExtractAssets)
            {
                // The kit carries its own python\ with the packages the step needs already installed.
                progressCompile.Value++;
                labelCompileStatus.Text = "Extracting assets from the ROM...";
                Program.Log("Extracting assets...");
                if (runProcess("cmd.exe", "/C " + game.AssetCommand))
                {
                    MessageBox.Show("Error occurred while extracting resources.\n\nPlease refer to " + Program.logFile + " for further details.");
                    return;
                }
            }

            // The kit's build.cmd: the port's own compile line, with the TCC and SDL2 the kit carries.
            // Explicit .\ because Windows with NoDefaultCurrentDirectoryInExePath set (the owner's PC) makes
            // cmd refuse a bare "build.cmd"; CI runners do not set it, which is why CI never saw it.
            // Same bytes CI compiled when it verified the kit.
            this.build.Text = "Building...";
            progressCompile.Value++;
            labelCompileStatus.Text = "Building " + game.Exe + "...";
            if (runProcess("cmd.exe", "/C .\build.cmd"))
            {
                MessageBox.Show("Error occurred while building " + game.Exe + ".\n\nPlease refer to " + Program.logFile + " for further details.");
                return;
            }

            labelCompileStatus.Text = "Backing up " + game.Ini + "...";
            var iniFile = Path.Combine(Program.repoDir, game.Ini);
            var iniCopy = Path.Combine(Program.repoDir, "saves", game.Ini);

            Directory.CreateDirectory(Path.Combine(Program.repoDir, "saves"));
            // Only zelda3.ini gets the tweaks below; other games keep their .ini and get a backup.
            if (!File.Exists(iniFile) || !game.FullSettings)
            {
                if (File.Exists(iniFile)) File.Copy(iniFile, iniCopy, true);
                ExitBuild();
                return;
            }
            if (File.Exists(iniCopy)) File.Delete(iniCopy);
            File.Move(iniFile, iniCopy);

            labelCompileStatus.Text = "Modifying " + game.Ini + "...";
            using (var modifiedFile = File.AppendText(iniFile))
            {
                foreach (var line in File.ReadLines(iniCopy))
                {
                    if (!line.Equals("# Change the appearance of Link by loading a ZSPR file") &&
                        !line.Equals("# See all sprites here: https://snesrev.github.io/sprites-gfx/snes/zelda3/link/") &&
                        !line.Equals("# Download the files with \"git clone https://github.com/snesrev/sprites-gfx.git\"") &&
                        !line.Equals("# LinkGraphics = sprites-gfx/snes/zelda3/link/sheets/megaman-x.2.zspr") &&
                        !line.Equals("# This default is suitable for QWERTZ keyboards.") &&
                        !line.Equals("#Controls = Up, Down, Left, Right, Right Shift, Return, x, y, s, a, c, v") &&
                        !line.Equals("# This one is suitable for AZERTY keyboards.") &&
                        !line.Equals("#Controls = Up, Down, Left, Right, Right Shift, Return, x, w, s, q, c, v") &&
                        !line.Equals("# Language = de"))
                    {
                        modifiedFile.WriteLine(line);
                    }
                    else if (line.Equals("# Language = de"))
                    {
                        modifiedFile.WriteLine("Language = us");
                    }
                }
            }

            ExitBuild();
        }

        private void ExitBuild()
        {
            progressCompile.Text = "Done";
            progressCompile.Visible = false;
            labelCompileStatus.Visible = false;

            Program.Log("\n\n\n\n");
            UpdateMainForm();
        }

        private void UpdateMainForm()
        {
            var game = Program.game;
            this.gamePicker.Enabled = true;
            this.romFolder.Enabled = true;
            this.launch.Enabled = false;
            this.settings.Enabled = false;
            this.launch.Text = "Launch";
            this.build.Text = "Download";
            var marker = Path.Combine(Program.repoDir, Game.PackageMarker);
            if (File.Exists(marker))
            {
                // This launcher's package on disk: a press redoes the ROM and asset steps. Another
                // version's package, or a folder from the old clone-and-build launcher: updates.
                this.build.Text = File.ReadAllText(marker).Trim() == Game.LauncherVersion ? "Re-build" : "Update";
            }
            else if (Directory.Exists(Program.repoDir))
            {
                this.build.Text = "Update";
            }
            this.build.Enabled = true;

            if (File.Exists(Path.Combine(Program.repoDir, game.Exe)))
            {
                this.launch.Enabled = true;
                this.settings.Enabled = true;

                if (!game.FullSettings)
                {
                    this.settings.Text = "Edit " + game.Ini;
                }
                else if (File.Exists(Path.Combine(Program.repoDir, game.Ini)))
                {
                    this.settings.Text = "Settings";
                }
                else
                {
                    this.settings.Text = "Restore INI";
                }
            }
        }

        private void settings_click(object sender, EventArgs e)
        {
            // The settings and keymapper forms are written for zelda3.ini; other games open their .ini.
            if (!Program.game.FullSettings)
            {
                var ini = Path.Combine(Program.repoDir, Program.game.Ini);
                if (File.Exists(ini)) Process.Start(new ProcessStartInfo("notepad.exe", "\"" + ini + "\"") { UseShellExecute = true });
                else MessageBox.Show(Program.game.Ini + " was not found. Re-build to restore it.", "No INI");
                return;
            }

            using (settingsForm settings = new settingsForm())
            {
                if (!settings.IsDisposed && settings.ShowDialog() == DialogResult.OK)
                {
                    settings.Dispose();
                }
            }

            UpdateMainForm();
        }

        private void gamePicker_Changed(object? sender, EventArgs e)
        {
            Program.settings.GameId = Game.All[this.gamePicker.SelectedIndex].Id;
            Program.settings.Save();
            UpdateMainForm();
        }

        private void romFolder_Click(object? sender, EventArgs e)
        {
            ChooseRomFolder();
        }

        private void ChooseRomFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Where are your SNES ROMs? Subfolders are searched too.";
                dialog.UseDescriptionForTitle = true;
                if (Directory.Exists(Program.settings.RomFolder)) dialog.InitialDirectory = Program.settings.RomFolder;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Program.settings.RomFolder = dialog.SelectedPath;
                    Program.settings.Save();
                }
            }
            UpdateRomFolderLabel();
        }

        private void UpdateRomFolderLabel()
        {
            var folder = Program.settings.RomFolder;
            this.romFolder.Text = string.IsNullOrEmpty(folder) ? "Choose ROM folder..." : "ROMs: " + Path.GetFileName(folder.TrimEnd('\\'));
            this.toolTip.SetToolTip(this.romFolder, string.IsNullOrEmpty(folder) ? "Where your SNES ROMs are" : folder);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.gamePicker = new System.Windows.Forms.ComboBox();
            this.romFolder = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip();
            this.build = new System.Windows.Forms.Button();
            this.launch = new System.Windows.Forms.Button();
            this.settings = new System.Windows.Forms.Button();
            this.labelCompileStatus = new System.Windows.Forms.Label();
            this.progressCompile = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // build
            // 
            this.gamePicker.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.gamePicker.Location = new System.Drawing.Point(8, 8);
            this.gamePicker.Name = "gamePicker";
            this.gamePicker.Size = new System.Drawing.Size(175, 23);
            this.gamePicker.TabIndex = 3;
            this.romFolder.Location = new System.Drawing.Point(8, 37);
            this.romFolder.Name = "romFolder";
            this.romFolder.Size = new System.Drawing.Size(175, 27);
            this.romFolder.TabIndex = 4;
            this.romFolder.UseVisualStyleBackColor = true;
            this.romFolder.Click += new System.EventHandler(this.romFolder_Click);
            this.build.AutoSize = true;
            this.build.Location = new System.Drawing.Point(8, 72);
            this.build.Name = "build";
            this.build.Size = new System.Drawing.Size(175, 50);
            this.build.TabIndex = 1;
            this.build.Text = "Download";
            this.build.UseVisualStyleBackColor = true;
            this.build.Click += new System.EventHandler(this.build_Click);
            // 
            // launch
            // 
            this.launch.AutoSize = true;
            this.launch.Enabled = false;
            this.launch.Location = new System.Drawing.Point(8, 128);
            this.launch.Name = "launch";
            this.launch.Size = new System.Drawing.Size(175, 50);
            this.launch.TabIndex = 0;
            this.launch.Text = "Launch";
            this.launch.UseVisualStyleBackColor = true;
            this.launch.Click += new System.EventHandler(this.launch_click);
            // 
            // settings
            // 
            this.settings.AutoSize = true;
            this.settings.Enabled = false;
            this.settings.Location = new System.Drawing.Point(8, 184);
            this.settings.Name = "settings";
            this.settings.Size = new System.Drawing.Size(175, 50);
            this.settings.TabIndex = 2;
            this.settings.Text = "Settings";
            this.settings.UseVisualStyleBackColor = true;
            this.settings.Click += new System.EventHandler(this.settings_click);
            // 
            // labelCompileStatus
            // 
            this.labelCompileStatus.Location = new System.Drawing.Point(8, 237);
            this.labelCompileStatus.Name = "labelCompileStatus";
            this.labelCompileStatus.Size = new System.Drawing.Size(175, 23);
            this.labelCompileStatus.TabIndex = 1;
            this.labelCompileStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelCompileStatus.Visible = false;
            // 
            // progressCompile
            // 
            this.progressCompile.Location = new System.Drawing.Point(8, 263);
            this.progressCompile.MarqueeAnimationSpeed = 10;
            this.progressCompile.Maximum = 3;
            this.progressCompile.Name = "progressCompile";
            this.progressCompile.Size = new System.Drawing.Size(175, 23);
            this.progressCompile.Step = 1;
            this.progressCompile.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressCompile.TabIndex = 2;
            this.progressCompile.UseWaitCursor = true;
            this.progressCompile.Visible = false;
            // 
            // MainForm
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.progressCompile);
            this.Controls.Add(this.labelCompileStatus);
            this.Controls.Add(this.settings);
            this.Controls.Add(this.launch);
            this.Controls.Add(this.build);
            this.Controls.Add(this.romFolder);
            this.Controls.Add(this.gamePicker);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        public Boolean runProcess(string filename, string arguments)
        {
            var logFile = Program.logFile;

            bool createdNew;
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "console process", out createdNew);
            var signaled = false;

            if(!createdNew)
            {
                waitHandle.Set();
            }

            Program.Log("\n" + DateTime.Now.ToString() + "\n----------------------------\n");

            Program.Log("Executing via " + filename + ":\n " + arguments + "\n");

            ProcessStartInfo sInfo = new ProcessStartInfo();
            sInfo.FileName = filename;
            sInfo.Arguments = arguments;
            sInfo.WorkingDirectory = Program.repoDir;
            sInfo.RedirectStandardOutput = true;
            sInfo.RedirectStandardError = true;
            sInfo.UseShellExecute = false;
            sInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = sInfo;
            processes.Add(process);

            while (!IsHandleCreated)
            {
                this.CreateHandle();
            }

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    Program.Log(e.Data + "\n");
                }));
            });

            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    Program.Log(e.Data + "\n");
                }));
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            do
            {
                Application.DoEvents();

                signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(1));
            } while (!process.HasExited);

            processes.Remove(process);

            if (process.ExitCode > 0)
            {
                return true;
            }

            process.Close();

            Program.Log("\n");

            if (File.Exists(logFile))
            {
                var fileInfo = new FileInfo(logFile);

                while (fileInfo.Length > (51200))
                {
                    var lines = File.ReadLines(logFile).ToArray();

                    File.WriteAllLines(logFile, lines.Skip(1).ToArray());

                    fileInfo = new FileInfo(logFile);
                }
            }

            return false;
        }

        List<Process> processes = new List<Process>();
    }
}