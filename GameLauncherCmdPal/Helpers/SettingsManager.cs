using GameLauncherCmdPal.Models;
using GameLauncherCmdPal.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GameLauncherCmdPal.Helpers
{
    public class SettingsManager : JsonSettingsManager
    {
        public event EventHandler? SettingsChanged;

        private static readonly string _namespace = "gamelauncher";
        private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";
        private static readonly string GameDataFilePath = Path.Combine(Utilities.BaseSettingsPath("Microsoft.CmdPal"), "game_data.json");

        private static readonly string _defaultFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Game Launcher Shortcuts"
        );

        private readonly TextSetting _folderPath = new(
            Namespaced(nameof(FolderPath)),
            Resources.custom_shortcuts_label,
            Resources.custom_shortcuts_desc,
            _defaultFolderPath
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

        private readonly ToggleSetting _toggleHidden = new(
            Namespaced(nameof(ToggleHidden)),
            Resources.toggle_hidden_label,
            Resources.toggle_hidden_desc,
            false
        );

        public string FolderPath
        {
            get => _folderPath.Value ?? _defaultFolderPath;
            set
            {
                _folderPath.Value = value;
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
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

            Settings.Add(_folderPath);
            // Settings.Add(_toggleEpic)s;
            Settings.Add(_toggleSteam);
            Settings.Add(_toggleXbox);
            Settings.Add(_toggleHidden);

            LoadSettings();

            Settings.SettingsChanged += (s, a) =>
            {
                SaveSettings();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        internal static void SaveGameData(IEnumerable<Game> games)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(games, GameLauncherJsonContext.Default.ListGame);
            File.WriteAllText(GameDataFilePath, json);
        }

        internal static IEnumerable<Game> LoadGameData()
        {
            if (!File.Exists(GameDataFilePath))
            {
                return Enumerable.Empty<Game>();
            }

            var json = File.ReadAllText(GameDataFilePath);
            return JsonSerializer.Deserialize(json, GameLauncherJsonContext.Default.ListGame) ?? new List<Game>();
        }

    }
}