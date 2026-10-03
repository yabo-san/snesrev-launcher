using System.Text.RegularExpressions;
using IniParser.Model;

namespace Zelda_3_Launcher
{
    // The settings and keymapper forms were written for zelda3.ini. The three ports share most of
    // their ini (General, Graphics, Sound, KeyMap, GamepadMap); zelda3 adds Features, Language, MSU,
    // shaders and a few graphics toggles. Rather than three forms, one form asks the game's own ini
    // which keys it has: a control whose key is missing is hidden, and nothing is written back for a
    // key the ini never mentioned. A key counts as present when it appears in the file at all, even
    // commented out (zelda3.ini ships "# LinkGraphics = ..." and the game understands the key).
    internal static class IniShape
    {
        public static bool HasKey(string iniText, string key) =>
            Regex.IsMatch(iniText, @"(?m)^\s*#?\s*" + Regex.Escape(key) + @"\s*=");

        // A read that tolerates a missing section or key.
        public static string? Get(IniData data, string section, string key, string? fallback)
        {
            if (!data.Sections.ContainsSection(section)) return fallback;
            return data[section][key] ?? fallback;
        }

        // Drop every key the ini never had, and any section that is empty afterwards, so a form
        // written for zelda3 cannot teach sm.ini about MSU or Link's sprite sheet.
        // `keep` names keys the game parses even though its default ini never lists them (all three
        // ports read DisplayPerf and ToggleRenderer from [KeyMap]; none of the shipped inis mention them).
        public static void Prune(IniData data, string iniText, params string[] keep)
        {
            var drop = new List<(string section, string key)>();
            foreach (var section in data.Sections)
                foreach (var kd in section.Keys)
                    if (!HasKey(iniText, kd.KeyName) && !keep.Contains(kd.KeyName)) drop.Add((section.SectionName, kd.KeyName));
            foreach (var (section, key) in drop) data[section].RemoveKey(key);

            var empty = new List<string>();
            foreach (var section in data.Sections)
                if (section.Keys.Count == 0 && !Regex.IsMatch(iniText, @"(?m)^\s*\[" + Regex.Escape(section.SectionName) + @"\]"))
                    empty.Add(section.SectionName);
            foreach (var name in empty) data.Sections.RemoveSection(name);
        }

        // Hide a control whose key this game does not have. Hidden, not just disabled, so the
        // window reads as that game's settings rather than Zelda's with holes in it.
        public static void ShowIf(bool present, params Control[] controls)
        {
            foreach (var c in controls) { c.Visible = present; c.Enabled = present; }
        }
    }
}
