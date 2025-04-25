using System;
using System.IO;
using System.Linq;
using GameLauncherCmdPal.Commands;
using GameLauncherCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GameLauncherCmdPal;

internal sealed partial class GameList : ListPage
{
    private readonly SettingsManager _settingsManager;

    public GameList(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        Icon = IconHelpers.FromRelativePaths(@"Assets\GL_LightIcon.png", @"Assets\GL_DarkIcon.png");
        Title = "Game Launcher";
        Name = "Find games";
    }

    public override IListItem[] GetItems()
    {
        string customShortcutsPath = _settingsManager.DefaultPath;

        if (!Directory.Exists(customShortcutsPath))
        {
            return [
                new ListItem(new NoOpCommand())
                {
                    Title = $"Directory not found: '{customShortcutsPath}'",
                    Subtitle = "Check Game Launcher settings in PowerToys."
                }
            ];
        }

        try
        {
            var shortcutFiles = Directory.GetFiles(customShortcutsPath, "*.lnk");

            // Case insensitive
            var filteredFiles = string.IsNullOrWhiteSpace(SearchText)
                ? shortcutFiles
                : shortcutFiles.Where(file => Path.GetFileNameWithoutExtension(file) // Search on filename without extension
                    .Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            // Convert filtered files to ListItems
            var items = filteredFiles.Select(shortcutFilePath =>
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
            }).ToArray(); // LINQ to array

            // Return the generated items or a message if no matching games were found
            return items.Length > 0
                ? items
                : [
                    new ListItem(new NoOpCommand()) // A non-actionable item
                    {
                        Title = "No matching games found.",
                        Subtitle = "Try a different search value."
                    }
                  ];
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GameList] Access denied reading directory '{customShortcutsPath}': {ex.Message}");
            return [new ListItem(new NoOpCommand()) { Title = "Access Denied", Subtitle = $"Cannot read directory: {customShortcutsPath}" }];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GameList] Error retrieving game list from '{customShortcutsPath}': {ex.Message}");
            return [new ListItem(new NoOpCommand()) { Title = "Error Reading Games", Subtitle = "An unexpected error occurred." }];
        }
    }
}
