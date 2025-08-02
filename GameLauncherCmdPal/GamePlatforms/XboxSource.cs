using GameFinder.Common;
using GameFinder.StoreHandlers.Xbox;
using GameLauncherCmdPal.Models;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.WindowsAPICodePack.Shell;
using NexusMods.Paths;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GameLauncherCmdPal.GameSources
{
    internal sealed class XboxSource : IGameSource
    {
        public string SourceName => "Xbox";
        public IEnumerable<Game> SyncedGames { get; private set; } = Array.Empty<Game>();

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        private readonly XboxHandler handler = new(FileSystem.Shared);
        private readonly Guid FOLDERID_AppsFolder = new("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");

        public XboxSource()
        {
        }

        public async Task SyncGames()
        {
            try
            {
                var allResults = handler.FindAllGames().ToList();
                var gameResults = allResults.Where(x => x.IsGame()).ToList();
                var games = gameResults.Select(x => x.AsGame()).ToList();

                if (games.Count == 0)
                {
                    SyncedGames = Array.Empty<Game>();
                    return;
                }

                var syncedGames = new List<Game>();
                IKnownFolder appsFolder = KnownFolderHelper.FromKnownFolderId(FOLDERID_AppsFolder);

                // Process each game synchronously to avoid shell object threading issues
                foreach (var game in games)
                {
                    var mappedGame = await MapShellObjectToGame(appsFolder, game);
                    if (mappedGame != null)
                    {
                        syncedGames.Add(mappedGame);
                    }
                }

                SyncedGames = syncedGames;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XboxSource] Error during SyncGames: {ex.Message}");
                SyncedGames = Array.Empty<Game>();
            }
        }

        private async Task<Game> MapShellObjectToGame(IKnownFolder appsFolder, XboxGame gameFromFinder)
        {
            try
            {
                // Try multiple matching strategies
                var shellGame = appsFolder.FirstOrDefault(x =>
                    x.ParsingName.StartsWith(gameFromFinder.Id.Value, StringComparison.OrdinalIgnoreCase));

                // If not found by ID, try by name
                if (shellGame == null && !string.IsNullOrEmpty(gameFromFinder.DisplayName))
                {
                    shellGame = appsFolder.FirstOrDefault(x =>
                        x.Name.Equals(gameFromFinder.DisplayName, StringComparison.OrdinalIgnoreCase));
                }

                // If still not found, try partial name match
                if (shellGame == null && !string.IsNullOrEmpty(gameFromFinder.DisplayName))
                {
                    shellGame = appsFolder.FirstOrDefault(x =>
                        x.Name.Contains(gameFromFinder.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                        gameFromFinder.DisplayName.Contains(x.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (shellGame != null)
                {
                    return new Game(
                        name: shellGame.Name,
                        uri: $"shell:appsFolder\\{shellGame.ParsingName}",
                        platform: SourceName,
                        iconPath: ExtractXboxAppIcon(shellGame)
                    );
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XboxSource] Error mapping shell object for {gameFromFinder.Id.Value}: {ex.Message}");
                return null;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
         private struct SHFILEINFO
         {
             public IntPtr hIcon;
             public int iIcon;
             public uint dwAttributes;
             [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
             public string szDisplayName;
             [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
             public string szTypeName;
         }

         private const uint SHGFI_ICON = 0x000000100;
         private const uint SHGFI_LARGEICON = 0x000000000;

         [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
         private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

         [DllImport("user32.dll", SetLastError = true)]
         private static extern bool DestroyIcon(IntPtr hIcon);

         private static readonly string IconCacheDirectory = Path.Combine(Utilities.BaseSettingsPath("Cache"), "XboxIcons");

         static XboxSource()
         {
             Directory.CreateDirectory(IconCacheDirectory);
         }

         private static string? ExtractXboxAppIcon(ShellObject shellGame)
         {
             if (shellGame == null)
                 return null;

             string parsingName = shellGame.ParsingName;
             
             // Generate cache file name
             string safeFileName = GenerateSafeCacheFileName(parsingName) + "_large.png";
             string cachedIconFilePath = Path.Combine(IconCacheDirectory, safeFileName);

             // Return cached file if it exists
             if (File.Exists(cachedIconFilePath))
             {
                 return cachedIconFilePath;
             }

             try
             {
                 // Try to get icon using ShellObject's Thumbnail property
                 var thumbnail = shellGame.Thumbnail;
                 if (thumbnail != null)
                 {
                     using (var bitmap = thumbnail.LargeBitmap ?? thumbnail.MediumBitmap ?? thumbnail.SmallBitmap)
                     {
                         if (bitmap != null)
                         {
                             bitmap.Save(cachedIconFilePath, ImageFormat.Png);
                             return cachedIconFilePath;
                         }
                     }
                 }

                 // Fallback: try using the Properties.System.ItemTypeText icon
                 var iconLocation = shellGame.Properties.System.ItemTypeText;
                 if (!string.IsNullOrEmpty(iconLocation?.Value))
                 {
                     // Try SHGetFileInfo with the shell object's path
                     IntPtr hIcon = IntPtr.Zero;
                     SHFILEINFO shinfo = default;
                     uint flags = SHGFI_ICON | SHGFI_LARGEICON;

                     try
                     {
                         string shellUri = $"shell:appsFolder\\{parsingName}";
                         IntPtr result = SHGetFileInfo(shellUri, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                         if (result != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                         {
                             hIcon = shinfo.hIcon;

                             using (Icon icon = (Icon)Icon.FromHandle(hIcon).Clone())
                             {
                                 using (Bitmap bmp = icon.ToBitmap())
                                 {
                                     bmp.Save(cachedIconFilePath, ImageFormat.Png);
                                     return cachedIconFilePath;
                                 }
                             }
                         }
                     }
                     finally
                     {
                         if (hIcon != IntPtr.Zero)
                         {
                             DestroyIcon(hIcon);
                         }
                     }
                 }

                 Debug.WriteLine($"[XboxSource] Failed to get icon for {parsingName}");
                 return null;
             }
             catch (Exception ex)
             {
                 Debug.WriteLine($"[XboxSource] Error extracting icon for {parsingName}: {ex.Message}");
                 if (File.Exists(cachedIconFilePath))
                 {
                     try { File.Delete(cachedIconFilePath); } catch { }
                 }
                 return null;
             }
         }

         private static string GenerateSafeCacheFileName(string path)
         {
             byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
             return Convert.ToBase64String(hashBytes)
                          .Replace('/', '_')
                          .Replace('+', '-')
                          .TrimEnd('=');
         }
    }
}
