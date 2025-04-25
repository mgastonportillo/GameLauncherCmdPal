using System;
using System.IO;
using GameLauncherCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GameLauncherCmdPal.Helpers
{
    public class SettingsManager : JsonSettingsManager
    {
        private static readonly string _namespace = "gamelauncher";
        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        // %LocalAppData%, when packaged, makes it hard for the user to find the folder
        // so, maybe, IsPackaged() can be leveraged to use that variable more efficiently.
        private static readonly string _defaultCustomShortcutsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Game Launcher Shortcuts"
        );

        private readonly TextSetting _defaultPath = new(
            Namespaced(nameof(DefaultPath)),
            Resources.custom_shortcuts_label,
            Resources.custom_shortcuts_desc,
            _defaultCustomShortcutsPath
        );

        private readonly ToggleSetting _toggleXbox = new(
            Namespaced(nameof(ToggleXbox)),
            Resources.toggle_xbox_label,
            Resources.toggle_xbox_desc,
            true
        );

        private readonly ToggleSetting _toggleSteam = new(
            Namespaced(nameof(ToggleSteam)),
            Resources.toggle_steam_label,
            Resources.toggle_steam_desc,
            true
        );

        private readonly ToggleSetting _toggleEpic = new(
            Namespaced(nameof(ToggleEpic)),
            Resources.toggle_epic_label,
            Resources.toggle_epic_desc,
            true
        );

        public string DefaultPath
        {
            // Expand variables so that user can use variables (needs to be polished to match virtualised paths behaviour)
            get => Environment.ExpandEnvironmentVariables(_defaultPath.Value ?? _defaultCustomShortcutsPath);
        }

        public bool ToggleXbox
        {
            get => _toggleXbox.Value;
            set => _toggleXbox.Value = value;
        }

        public bool ToggleSteam
        {
            get => _toggleSteam.Value;
            set => _toggleSteam.Value = value;
        }

        public bool ToggleEpic
        {
            get => _toggleEpic.Value;
            set => _toggleEpic.Value = value;
        }

        internal static string SettingsJsonPath()
        {
            var baseDir = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "gamelauncher.json");
        }

        public SettingsManager()
        {
            FilePath = SettingsJsonPath();
            // Settings.Add(_toggleEpic); // TBA
            // Settings.Add(_toggleSteam);
            // Settings.Add(_toggleXbox);
            Settings.Add(_defaultPath);

            LoadSettings();

            EnsureDirectoryExists(DefaultPath);

            // Event handler: Saves the setting when changed via PowerToys UI
            // Currently has a visual bug but a fix has been committed to the PowerToys repo
            Settings.SettingsChanged += (s, a) =>
            {
                SaveSettings();
            };
        }

        private static void EnsureDirectoryExists(string path)
        {
            string _path = Environment.ExpandEnvironmentVariables(path ?? string.Empty);

            if (string.IsNullOrWhiteSpace(_path))
            {
                Console.Error.WriteLine($"[EnsureDirectoryExists] Skipping directory creation: Path is null, empty, or only whitespace after expansion.");
                return;
            }
        }
    }
}