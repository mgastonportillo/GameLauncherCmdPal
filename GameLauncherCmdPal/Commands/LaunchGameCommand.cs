using System.Diagnostics;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GameLauncherCmdPal.Commands
{
    internal sealed partial class LaunchGameCommand : InvokableCommand
    {
        public override string Name => $"Launch {System.IO.Path.GetFileNameWithoutExtension(_shortcutPath)}";
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
    }
}
