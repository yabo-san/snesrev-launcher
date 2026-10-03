namespace Zelda_3_Launcher
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        public static string currentDirectory = Directory.GetCurrentDirectory();

        public static LauncherSettings settings = LauncherSettings.Load();
        public static Game game => Game.ById(settings.GameId);

        // Per game: each port gets its own folder next to the launcher.
        public static string repoDir => Path.Combine(currentDirectory, game.Dir);
        public static string third_partyDir => Path.Combine(repoDir, "third_party");
        public static string logFile => Path.Combine(currentDirectory, game.Dir + ".log");

        // The only way the log gets written. Opens with ReadWrite sharing so a viewer holding the
        // file (an editor, a second launcher window, a tail) cannot make a log line throw, retries
        // briefly if something holds it exclusively, and gives up silently: the log exists to explain
        // a failure, it must never be one (v1.6.2 crashed in runProcess on exactly this, 2026-10-03).
        public static void Log(string text)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    using var fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    using var w = new StreamWriter(fs);
                    w.Write(text);
                    return;
                }
                catch (IOException) { Thread.Sleep(100); }
                catch (UnauthorizedAccessException) { return; }
            }
        }
    }
}
