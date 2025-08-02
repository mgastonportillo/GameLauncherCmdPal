using GameLauncherCmdPal.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GameLauncherCmdPal.Commands
{
    internal sealed partial class ToggleHiddenCommand : InvokableCommand
    {
        private readonly SettingsManager _settingsManager;

        public ToggleHiddenCommand(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public override string Name => "Toggle Hidden";

        public override CommandResult Invoke()
        {
            _settingsManager.ToggleHidden = !_settingsManager.ToggleHidden;
            _settingsManager.SaveSettings();

            var toastMessage = new ToastStatusMessage(_settingsManager.ToggleHidden
                ? "Hidden games are now visible."
                : "Hidden games are now hidden."
            );
            toastMessage.Show();

            return CommandResult.KeepOpen();
        }

    }
}

