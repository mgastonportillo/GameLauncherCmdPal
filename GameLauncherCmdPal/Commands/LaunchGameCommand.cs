using GameLauncherCmdPal.Helpers;
using GameLauncherCmdPal.Models;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;

namespace GameLauncherCmdPal.Commands
{
    internal sealed partial class LaunchGameCommand : InvokableCommand
    {
        private readonly Game _gameItem;
        private readonly SettingsManager _settingsManager;

        public LaunchGameCommand(Game gameItem, SettingsManager settingsManager)
        {
            _gameItem = gameItem;
            _settingsManager = settingsManager;
        }

        public override string Name => $"Launch {TruncateGameName(_gameItem.Name)}";

        public override CommandResult Invoke()
        {
            _gameItem.UpdateRecentScore();

            // Save updated game data
            //var allGames = SettingsManager.LoadGameData().ToList();
            //var gameToUpdate = allGames.FirstOrDefault(g => g.Uri == _gameItem.Uri);
            //if (gameToUpdate != null)
            //{
            //    gameToUpdate.RecentScore = _gameItem.RecentScore;
            //}
            //SettingsManager.SaveGameData(allGames);

            Process.Start(new ProcessStartInfo
            {
                FileName = _gameItem.Uri,
                UseShellExecute = true
            });

            return CommandResult.Hide();
        }

        private static string TruncateGameName(string gameName)
        {
            const int maxLength = 15;
            return gameName.Length > maxLength ? string.Concat(gameName.AsSpan(0, maxLength), "...") : gameName;
        }

    }

}