using GameLauncherCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.IO;

namespace GameLauncherCmdPal.Helpers
{
    public class SettingsManager : JsonSettingsManager
    {
        private static readonly string _namespace = "gamelauncher";
        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

        private static readonly string _defaultCustomShortcutsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Game Launcher Shortcuts"
        );

        private readonly TextSetting _customShortcutsPath = new(
            Namespaced(nameof(CustomShortcutsPath)),
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

        private readonly ToggleSetting _toggleHidden = new(
            Namespaced(nameof(ToggleHidden)),
            Resources.toggle_hidden_label,
            Resources.toggle_hidden_desc,
            false
        );

        public string CustomShortcutsPath
        {
            get => _customShortcutsPath.Value ?? _defaultCustomShortcutsPath;
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

        public bool ToggleHidden
        {
            get => _toggleHidden.Value;
            set => _toggleHidden.Value = value;
        }

        internal static string SettingsJsonPath()
        {
            var baseDir = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(baseDir); // Idempotent
            return Path.Combine(baseDir, "gamelauncher.json");
        }

        public SettingsManager()
        {
            FilePath = SettingsJsonPath();

            Settings.Add(_customShortcutsPath);
            // TBA
            Settings.Add(_toggleEpic);
            Settings.Add(_toggleSteam);
            Settings.Add(_toggleXbox);
            Settings.Add(_toggleHidden);

            LoadSettings();

            Settings.SettingsChanged += (s, a) =>
            {
                SaveSettings();
            };
        }
    }
}