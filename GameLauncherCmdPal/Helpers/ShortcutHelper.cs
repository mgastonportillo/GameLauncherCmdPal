using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace GameLauncherCmdPal.Helpers
{
    internal static class ShortcutHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon; // Handle to the icon
            public int iIcon; // Index of the icon within the file
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        // Flags for SHGetFileInfo function
        private const uint SHGFI_ICON = 0x000000100;     // Retrieve the handle to the icon that represents the file
        private const uint SHGFI_LARGEICON = 0x000000000; // Retrieve the large icon (typically 32x32)
        private const uint SHGFI_SMALLICON = 0x000000001; // Retrieve the small icon (typically 16x16)
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010; // Indicates that pszPath is a file specification (avoids accessing the file directly, faster but less reliable for some shortcuts)

        // P/Invoke declaration for SHGetFileInfo from shell32.dll
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        // P/Invoke declaration for DestroyIcon from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // Directory for caching extracted icons
        private static readonly string IconCacheDirectory = Path.Combine(Utilities.BaseSettingsPath("Cache"), "TempIcons");

        // Static constructor to ensure the cache directory exists when the class is first used.
        static ShortcutHelper()
        {
            Directory.CreateDirectory(IconCacheDirectory);
        }

        /// <summary>
        /// Extracts the icon associated with a file (especially .lnk shortcuts) and saves it as a temporary PNG file.
        /// Returns the absolute path to the saved PNG file. Returns null if extraction fails or the file doesn't exist.
        /// </summary>
        /// <param name="shortcutPath">The full path to the .lnk file.</param>
        /// <param name="useSmallIcon">True to extract the small icon (16x16), False for the large icon (32x32).</param>
        /// <returns>The absolute path to the cached icon PNG file, or null if an error occurred.</returns>
        public static string? ExtractIconToFile(string shortcutPath, bool useSmallIcon = false)
        {
            // Basic input validation
            if (string.IsNullOrWhiteSpace(shortcutPath) || !Path.GetExtension(shortcutPath).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!File.Exists(shortcutPath))
            {
                System.Diagnostics.Debug.WriteLine($"[ShortcutHelper] Shortcut file not found: {shortcutPath}");
                return null;
            }

            // Generate a unique cache file name based on the shortcut path and icon size
            string safeFileName = GenerateSafeCacheFileName(shortcutPath) + (useSmallIcon ? "_small.png" : "_large.png");
            string cachedIconFilePath = Path.Combine(IconCacheDirectory, safeFileName);

            // If the cached file exists, return its path immediately.
            if (File.Exists(cachedIconFilePath))
            {
                return cachedIconFilePath;
            }

            // --- Icon Extraction ---
            IntPtr hIcon = IntPtr.Zero; // Handle for the extracted icon
            SHFILEINFO shinfo = default; // Structure to receive icon info
            uint flags = SHGFI_ICON | (useSmallIcon ? SHGFI_SMALLICON : SHGFI_LARGEICON);

            try
            {
                // Attempt to get the icon handle using SHGetFileInfo
                // First, try with SHGFI_USEFILEATTRIBUTES for potentially faster access
                IntPtr result = SHGetFileInfo(shortcutPath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags | SHGFI_USEFILEATTRIBUTES);

                // If the first attempt failed (returned zero), try again without SHGFI_USEFILEATTRIBUTES
                // This forces it to access the actual file, which might be necessary for some shortcuts.
                if (result == IntPtr.Zero)
                {
                    result = SHGetFileInfo(shortcutPath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                }

                // Check if we successfully obtained an icon handle
                if (result != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
                {
                    hIcon = shinfo.hIcon; // Store the handle

                    // Convert the Win32 icon handle (HICON) to a managed System.Drawing.Icon object.
                    // Crucially, Clone() the icon. Icon.FromHandle does not take ownership,
                    // so we need a copy that we *do* own to safely convert and save it,
                    // while still being able to call DestroyIcon on the original handle later.
                    using (Icon icon = (Icon)Icon.FromHandle(hIcon).Clone())
                    {
                        // Convert the Icon object to a Bitmap
                        using (Bitmap bmp = icon.ToBitmap())
                        {
                            // Save the Bitmap to the cache file path as a PNG image
                            bmp.Save(cachedIconFilePath, ImageFormat.Png);
                            return cachedIconFilePath; // Success! Return the path to the cached file.
                        }
                    }
                }
                else
                {
                    // Failed to get icon handle from SHGetFileInfo
                    int errorCode = Marshal.GetLastWin32Error();
                    System.Diagnostics.Debug.WriteLine($"[ShortcutHelper] SHGetFileInfo failed for '{shortcutPath}'. Error code: {errorCode}");
                    return null; // Indicate failure
                }
            }
            catch (Exception ex)
            {
                // Catch potential errors during icon conversion or saving
                System.Diagnostics.Debug.WriteLine($"[ShortcutHelper] Error extracting or saving icon for '{shortcutPath}': {ex.Message}");

                // Attempt to clean up partially created cache file if save failed mid-way
                if (File.Exists(cachedIconFilePath))
                {
                    try
                    {
                        File.Delete(cachedIconFilePath);
                    }
                    catch { /* Ignore delete error */ }
                }
                return null; // Indicate failure
            }
            finally
            {
                // --- IMPORTANT Resource Cleanup ---
                // If we obtained an icon handle (hIcon), we *must* release it using DestroyIcon
                // to prevent resource leaks.
                if (hIcon != IntPtr.Zero)
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        /// <summary>
        /// Generates a safe file name suitable for caching based on the input path.
        /// Uses SHA256 hash to ensure uniqueness and avoid issues with invalid path characters.
        /// </summary>
        /// <param name="path">The original file path.</param>
        /// <returns>A safe string usable as a file name.</returns>
        private static string GenerateSafeCacheFileName(string path)
        {
            // Compute hash of the lowercase path to ensure case-insensitivity
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
            // Convert hash to Base64 string, make it URL/filesystem-safe, and remove padding
            return Convert.ToBase64String(hashBytes)
                          .Replace('/', '_') // Replace characters potentially problematic in file names
                          .Replace('+', '-')
                          .TrimEnd('=');   // Remove Base64 padding
        }
    }
}
