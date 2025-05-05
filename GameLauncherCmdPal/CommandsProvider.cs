using GameLauncherCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GameLauncherCmdPal;

public partial class CommandsProvider : CommandProvider
{
    private readonly SettingsManager _settingsManager = new();
    // private readonly ListItem _listItem; // Should implement this probably
    private readonly ICommandItem[] _commands;

    public CommandsProvider()
    {
        //Id = "GameLauncherCmdPal";
        DisplayName = "Game Launcher";
        Icon = IconHelpers.FromRelativePaths(@"Assets\Extension.light.png", @"Assets\Extension.dark.png");
        Settings = _settingsManager.Settings;
        _commands = [
            new CommandItem(new GameList(_settingsManager)) {
                Title = DisplayName,
                MoreCommands = [
                    new CommandContextItem(Settings.SettingsPage) { Title = "Game Launcher Settings" }
                ]
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
