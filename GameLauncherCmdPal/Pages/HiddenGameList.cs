using GameLauncherCmdPal.Commands;
using GameLauncherCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System.Linq;

namespace GameLauncherCmdPal.Pages
{
    internal sealed partial class HiddenGameList : ListPage
    {
        private readonly SettingsManager _settingsManager;

        public HiddenGameList(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            Title = "Hidden Games";
            Name = "Show Hidden Games";
        }

        public override IListItem[] GetItems()
        {
            var hiddenGames = SettingsManager.LoadGameData().Where(game => game.Hidden);

            return hiddenGames.Select(game => new ListItem(new HideGameCommand(game))
            {
                Title = game.Name,
                Subtitle = game.Platform,
                Icon = !string.IsNullOrEmpty(game.IconPath) ? new IconInfo(game.IconPath) : null
            }).ToArray();
        }
    }
}

