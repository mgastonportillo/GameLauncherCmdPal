using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using GameLauncherCmdPal.Models;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GameLauncherCmdPal.GameSources
{
    internal sealed class SteamSource : IGameSource
    {
        public string SourceName => "Steam";
        public IEnumerable<Game> SyncedGames { get; private set; } = Array.Empty<Game>();

        private readonly SteamHandler handler = new(FileSystem.Shared, OperatingSystem.IsWindows() ? WindowsRegistry.Shared : null);

        public async Task SyncGames()
        {
            var games = handler.FindAllGames()
                .Where(x => x.IsGame())
                .Select(x => x.AsGame())
                .Where(IsActualGame); // Filter out redistributables and non-games

            var syncedGames = new List<Game>();

            foreach (var game in games)
            {
                syncedGames.Add(MapSteamGameToGame(game));
            }

            SyncedGames = syncedGames;
            await Task.CompletedTask;
        }

        private static bool IsActualGame(SteamGame steamGame)
        {
            var name = steamGame.Name?.ToLowerInvariant() ?? string.Empty;
            
            // Filter out common redistributables and non-game entries
            var excludePatterns = new[]
            {
                "redistributable",
                "redist",
                "vcredist",
                "directx",
                "microsoft visual c++",
                "visual c++ redistributable",
                ".net framework",
                "steamworks",
                "steam api",
                "dedicated server",
                "server",
                "sdk",
                "development kit",
                "editor",
                "modding tools",
                "benchmark",
                "demo"
            };
            
            return !excludePatterns.Any(pattern => name.Contains(pattern));
        }

        private Game MapSteamGameToGame(SteamGame steamGame)
        {
            return new Game(
                name: steamGame.Name,
                uri: $"steam://launch/{steamGame.AppId}/Dialog",
                platform: SourceName,
                iconPath: GetIconPath(steamGame)
            );
        }

        private static string? GetIconPath(SteamGame steamGame)
        {
            var appIdString = steamGame.AppId.ToString().Trim();
            var iconCachePath = Path.Combine(steamGame.SteamPath.ToString(), "appcache", "librarycache", appIdString);

            if (!Directory.Exists(iconCachePath))
            {
                return Path.Combine("Icons", "steam.png");
            }

            var files = Directory
                .GetFiles(iconCachePath, "*.jpg", SearchOption.TopDirectoryOnly)
                .Where(file =>
                {
                    string fileName = Path.GetFileName(file);
                    return !fileName.StartsWith("header", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.StartsWith("library", StringComparison.OrdinalIgnoreCase) &&
                           !fileName.StartsWith("logo", StringComparison.OrdinalIgnoreCase);
                });

            return files.FirstOrDefault();
        }
    }
}

