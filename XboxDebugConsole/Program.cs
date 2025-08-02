using GameFinder.Common;
using GameFinder.StoreHandlers.Xbox;
using Microsoft.WindowsAPICodePack.Shell;
using NexusMods.Paths;
using System.Diagnostics;

Console.WriteLine("=== Xbox Game Discovery Debug Test ===");

try
{
    // Test XboxHandler directly
    var handler = new XboxHandler(FileSystem.Shared);
    Console.WriteLine("XboxHandler created successfully.");
    
    var results = handler.FindAllGames().ToList();
    Console.WriteLine($"Total results from FindAllGames(): {results.Count}");
    
    var games = results.Where(x => x.IsGame()).Select(x => x.AsGame()).ToList();
    Console.WriteLine($"Games after filtering: {games.Count}");
    
    if (games.Count > 0)
    {
        Console.WriteLine("\n=== Found Xbox Games ===");
        foreach (var game in games)
        {
            Console.WriteLine($"ID: {game.Id.Value}");
            Console.WriteLine($"DisplayName: {game.DisplayName}");
            Console.WriteLine($"---");
        }
    }
    else
    {
        Console.WriteLine("No Xbox games found.");
    }
    
    // Test Apps folder access and search for Xbox games
    Console.WriteLine("\n=== Testing Apps Folder Access ===");
    var FOLDERID_AppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
    
    try
    {
        IKnownFolder appsFolder = KnownFolderHelper.FromKnownFolderId(FOLDERID_AppsFolder);
        Console.WriteLine($"Apps folder accessible: {appsFolder != null}");
        
        if (appsFolder != null && games.Count > 0)
        {
            var appCount = appsFolder.Count();
            Console.WriteLine($"Total apps in folder: {appCount}");
            
            // Test mapping for each Xbox game
            foreach (var game in games)
            {
                Console.WriteLine($"\n=== Searching for shell object for {game.DisplayName} (ID: {game.Id.Value}) ===");
                
                // Try ID match
                var shellGameById = appsFolder.FirstOrDefault(x =>
                    x.ParsingName.StartsWith(game.Id.Value, StringComparison.OrdinalIgnoreCase));
                
                if (shellGameById != null)
                {
                    Console.WriteLine($"✓ Found by ID: {shellGameById.Name} ({shellGameById.ParsingName})");
                }
                else
                {
                    Console.WriteLine($"✗ Not found by ID: {game.Id.Value}");
                }
                
                // Try exact name match
                var shellGameByName = appsFolder.FirstOrDefault(x =>
                    x.Name.Equals(game.DisplayName, StringComparison.OrdinalIgnoreCase));
                
                if (shellGameByName != null)
                {
                    Console.WriteLine($"✓ Found by exact name: {shellGameByName.Name} ({shellGameByName.ParsingName})");
                }
                else
                {
                    Console.WriteLine($"✗ Not found by exact name: {game.DisplayName}");
                }
                
                // Try partial name match
                var shellGameByPartialName = appsFolder.FirstOrDefault(x =>
                    x.Name.Contains(game.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                    game.DisplayName.Contains(x.Name, StringComparison.OrdinalIgnoreCase));
                
                if (shellGameByPartialName != null)
                {
                    Console.WriteLine($"✓ Found by partial name: {shellGameByPartialName.Name} ({shellGameByPartialName.ParsingName})");
                }
                else
                {
                    Console.WriteLine($"✗ Not found by partial name: {game.DisplayName}");
                }
                
                // Search for any app containing "Among" or "Us"
                if (game.DisplayName.Contains("Among", StringComparison.OrdinalIgnoreCase))
                {
                    var relatedApps = appsFolder.Where(x => 
                        x.Name.Contains("Among", StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Contains("Us", StringComparison.OrdinalIgnoreCase) ||
                        x.ParsingName.Contains("Among", StringComparison.OrdinalIgnoreCase) ||
                        x.ParsingName.Contains("Innersloth", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                    
                    Console.WriteLine($"\nRelated apps found ({relatedApps.Count}):");
                    foreach (var app in relatedApps)
                    {
                        Console.WriteLine($"  - {app.Name} ({app.ParsingName})");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error accessing apps folder: {ex.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

// Now test XboxSource class
Console.WriteLine("\n=== Testing XboxSource Class ===");
try
{
    // We need to add reference to GameLauncherCmdPal project to test XboxSource
    Console.WriteLine("XboxSource test would require project reference to GameLauncherCmdPal");
    Console.WriteLine("The raw handler test above shows Xbox games are discoverable.");
    Console.WriteLine("The issue might be in the XboxSource.MapShellObjectToGame method.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error testing XboxSource: {ex.Message}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
