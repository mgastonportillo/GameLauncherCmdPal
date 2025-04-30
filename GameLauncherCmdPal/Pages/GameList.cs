using GameLauncherCmdPal.Commands;
using GameLauncherCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GameLauncherCmdPal;

internal sealed partial class GameList : ListPage
{
    private readonly SettingsManager _settingsManager;

    public GameList(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        Icon = IconHelpers.FromRelativePaths(@"Assets\Extension.light.png", @"Assets\Extension.dark.png");
        Title = "Game Launcher";
        Name = "Find games";
    }

    public override IListItem[] GetItems()
    {
        string customShortcutsPath = _settingsManager.DefaultPath;

        if (!Directory.Exists(customShortcutsPath))
        {
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = $"Directory not found: '{customShortcutsPath}'",
                    Subtitle = "Check Game Launcher settings in PowerToys.",
                    MoreCommands = [
                        new CommandContextItem(_settingsManager.Settings.SettingsPage) { Title = "Game Launcher Settings" }
                    ]
                }
            ];
        }

        EmptyContent = new CommandItem(new NoOpCommand())
        {
            Icon = Icon,
            Title = "No matching games found."
        };

        try
        {
            var shortcutFiles = Directory.GetFiles(customShortcutsPath, "*.lnk");

            // Convert shortcut files to ListItems
            var allItems = shortcutFiles.Select(shortcutFilePath =>
            {
                IIconInfo? gameIcon = null;

                // Attempt to extract the icon
                string? iconFilePath = ShortcutHelper.ExtractIconToFile(shortcutFilePath);

                if (!string.IsNullOrEmpty(iconFilePath))
                {
                    gameIcon = new IconInfo(iconFilePath);
                }

                return new ListItem(new LaunchGameCommand(shortcutFilePath))
                {
                    Title = Path.GetFileNameWithoutExtension(shortcutFilePath),
                    Subtitle = "Game Shortcut",
                    Icon = gameIcon
                };
            }).ToList();

            // Use ListHelpers.FilterList to filter items based on SearchText
            var filteredItems = ListHelpers.FilterList(allItems, SearchText);

            // Score and sort the filtered items
            var scoredItems = filteredItems
                .Select(item => new { Item = item, Score = ListHelpers.ScoreListItem(SearchText, item) })
                .OrderByDescending(scored => scored.Score)
                .Select(scored => scored.Item)
                .ToArray();

            return scoredItems;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GameList] Error retrieving game list from '{customShortcutsPath}': {ex.Message}");
            return
            [
                new ListItem(new NoOpCommand())
                {
                    Title = "Error Reading Games",
                    Subtitle = "An unexpected error occurred."
                }
            ];
        }
    }
}
