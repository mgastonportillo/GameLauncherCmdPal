using GameLauncherCmdPal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameLauncherCmdPal.GameSources
{
    internal interface IGameSource
    {
        string SourceName { get; }

        IEnumerable<Game> SyncedGames { get; }

        Task SyncGames();
    }
}
