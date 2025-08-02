using GameLauncherCmdPal.Helpers;
using GameLauncherCmdPal.Models;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Linq;

namespace GameLauncherCmdPal.Commands
{
    internal sealed partial class HideGameCommand : InvokableCommand
    {
        private readonly Game _gameItem;
        private readonly SettingsManager _settingsManager;

        public HideGameCommand(Game gameItem)
        {
            _gameItem = gameItem;
            _settingsManager = new SettingsManager();
        }

        public override string Name => _gameItem.Hidden ? "Unhide Game" : "Hide Game";

        public override CommandResult Invoke()
        {
            // Toggle the hidden state
            _gameItem.Hidden = !_gameItem.Hidden;

            // Save the updated game data
            var allGames = SettingsManager.LoadGameData().ToList();
            var gameToUpdate = allGames.FirstOrDefault(g => g.Uri == _gameItem.Uri);
            if (gameToUpdate != null)
            {
                gameToUpdate.Hidden = _gameItem.Hidden;
            }
            else
            {
                // If game doesn't exist in saved data, add it
                allGames.Add(_gameItem);
            }
            SettingsManager.SaveGameData(allGames);

            // Provide feedback to the user
            var toastMessage = new ToastStatusMessage($"{_gameItem.Name} is now {(_gameItem.Hidden ? "hidden" : "visible")}.");
            toastMessage.Show();

            return CommandResult.KeepOpen();
        }

    }
}
