// DO NOT MODIFY THIS FILE

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace GameLauncherCmdPal;

[Guid("27c5cc40-bc44-4ef4-8077-2f68eae64b61")]
public sealed partial class GameLauncherCmdPal : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly CommandsProvider _provider = new();

    public GameLauncherCmdPal(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => _provider,
            _ => null,
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}
