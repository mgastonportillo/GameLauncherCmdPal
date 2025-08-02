using GameLauncherCmdPal.Commands;
using GameLauncherCmdPal.GameSources;
using GameLauncherCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.System;

namespace GameLauncherCmdPal;

internal sealed partial class GameList : ListPage
{
    private readonly SettingsManager _settingsManager;
    private readonly IEnumerable<IGameSource> _gameSources;
    private readonly IconInfo _icon;

    public GameList(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _settingsManager.SettingsChanged += OnSettingsChanged;

        _icon = IconHelpers.FromRelativePaths(@"Assets\Extension.light.png", @"Assets\Extension.dark.png");

        // Initialize game sources
        _gameSources =
        [
            new FolderSource(_settingsManager),
            new SteamSource(),
            new XboxSource(),
        ];

        SyncGameSources();

        Icon = _icon;
        Title = "Game Launcher";
        Name = "Find games";

        EmptyContent = new ListItem(new NoOpCommand())
        {
            Icon = _icon,
            Title = "No games match your search.",
            Subtitle = "Try again?",
        };
    }

    public override IListItem[] GetItems()
    {
        try
        {
            // Create a list of enabled sources based on toggle settings
            var enabledSources = new List<IGameSource>();

            // Always include custom source
            enabledSources.Add(_gameSources.First(s => s is FolderSource));

            // Add sources based on toggle settings
            if (_settingsManager.ToggleSteam)
            {
                enabledSources.Add(_gameSources.First(s => s is SteamSource));
            }

            if (_settingsManager.ToggleXbox)
            {
                enabledSources.Add(_gameSources.First(s => s is XboxSource));
            }

            // Retrieve games only from enabled sources
            var allGames = enabledSources.SelectMany(source => source.SyncedGames).ToList();

            // Load saved game data to merge hidden state
            var savedGames = SettingsManager.LoadGameData().ToList();
            
            // Merge hidden state from saved data
            foreach (var game in allGames)
            {
                var savedGame = savedGames.FirstOrDefault(sg => sg.Uri == game.Uri);
                if (savedGame != null)
                {
                    game.Hidden = savedGame.Hidden;
                }
            }

            // Filter out hidden games if the toggle is off
            if (!_settingsManager.ToggleHidden)
            {
                allGames = allGames.Where(game => !game.Hidden).ToList();
            }

            // Convert GameItem objects to ListItem objects
            var allItems = allGames.Select(game => new ListItem(new LaunchGameCommand(game, _settingsManager))
            {
                Title = game.Name,
                Subtitle = game.Platform,
                Icon = !string.IsNullOrEmpty(game.IconPath) ? new IconInfo(game.IconPath) : null,
                MoreCommands = [
                    new CommandContextItem(new ToggleHiddenCommand(_settingsManager)),
                    new CommandContextItem(new HideGameCommand(game))
                    {
                        RequestedShortcut = KeyChordHelpers.FromModifiers(ctrl: true, vkey: VirtualKey.H)
                    }
                ]
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
            Debug.WriteLine($"[GameList] Error retrieving game list: {ex.Message}");
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

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        SyncGameSources();
        RaiseItemsChanged();
    }

    private void SyncGameSources()
    {
        foreach (var source in _gameSources)
            source.SyncGames().Wait();
    }
}

