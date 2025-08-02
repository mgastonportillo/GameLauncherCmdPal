using GameLauncherCmdPal.Helpers;
using GameLauncherCmdPal.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GameLauncherCmdPal.GameSources
{
    internal sealed class FolderSource : IGameSource
    {
        public string SourceName => "Custom Shortcuts";
        public IEnumerable<Game> SyncedGames { get; private set; } = Array.Empty<Game>();

        private readonly SettingsManager _settingsManager;

        public FolderSource(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public async Task SyncGames()
        {
            string folderPath = _settingsManager.FolderPath;

            if (!Directory.Exists(folderPath))
            {
                SyncedGames = Array.Empty<Game>();
                await Task.CompletedTask;
                return;
            }

            var shortcuts = Directory.GetFiles(folderPath, "*.lnk");
            var syncedGames = new List<Game>();

            foreach (var shortcut in shortcuts)
            {
                syncedGames.Add(MapShortcutToGame(shortcut));
            }

            SyncedGames = syncedGames;
            await Task.CompletedTask;
        }

        private Game MapShortcutToGame(string shortcut)
        {
            string? iconFilePath = ShortcutHelper.ExtractIconToFile(shortcut);

            return new Game(
                name: Path.GetFileNameWithoutExtension(shortcut),
                uri: shortcut,
                platform: SourceName,
                iconPath: iconFilePath
            );
        }
    }
}
