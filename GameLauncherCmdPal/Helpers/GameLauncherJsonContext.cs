using GameLauncherCmdPal.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameLauncherCmdPal.Helpers
{
    [JsonSerializable(typeof(List<Game>))]
    internal sealed partial class GameLauncherJsonContext : JsonSerializerContext
    {
    }
}