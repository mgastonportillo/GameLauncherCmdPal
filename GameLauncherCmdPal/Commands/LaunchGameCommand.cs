using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Diagnostics;

namespace GameLauncherCmdPal.Commands
{
    internal sealed partial class LaunchGameCommand : InvokableCommand
    {
        public override string Name => $"Launch {TruncateGameName(System.IO.Path.GetFileNameWithoutExtension(_shortcutPath))}";
        private readonly string _shortcutPath;

        public LaunchGameCommand(string shortcutPath)
        {
            _shortcutPath = shortcutPath;
        }

        public override CommandResult Invoke()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _shortcutPath,
                UseShellExecute = true
            });

            // Hide CmdPal after execution
            return CommandResult.Hide();
        }

        private static string TruncateGameName(string gameName)
        {
            const int maxLength = 40;
            return gameName.Length > maxLength ? string.Concat(gameName.AsSpan(0, maxLength), "...") : gameName;
        }
    }
}
