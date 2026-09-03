using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Imports the official iOS release zip (HwAds_iOS_V9.8.75.zip, ~113MB) into
    /// Assets/Plugins/iOS/HwAdsNative/<version>/ so Unity links the bundled frameworks on the next
    /// iOS build. The binary is intentionally NOT versioned inside the git package.
    ///
    /// Expected zip layout (verified against V9.8.75):
    ///   HwAdsFramework.framework/
    ///   DependenceSDK/... (adjust/, firebase/, commen_sdk/, max_adapter/)
    /// </summary>
    public static class HwIosFrameworkImporter
    {
        public const string ExpectedIosVersion = "9.8.75";
        const string DestRoot = "Assets/Plugins/iOS/HwAdsNative";

        public static void Import()
        {
            string zipPath = EditorUtility.OpenFilePanel(
                "Select HwAds iOS release zip",
                string.Empty,
                "zip");
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            {
                return;
            }

            string fileName = Path.GetFileName(zipPath);
            string version = ParseVersion(fileName) ?? "unknown";
            if (version != ExpectedIosVersion)
            {
                Debug.LogWarning(
                    $"[Jlyt.HwAds] zip version '{version}' != expected '{ExpectedIosVersion}'. " +
                    "The C#/ObjC bridge is written against the expected version's API.");
            }

            string dest = Path.Combine(DestRoot, "V" + version);
            if (Directory.Exists(dest))
            {
                if (!EditorUtility.DisplayDialog("HWSDK iOS import",
                        $"Target folder already exists:\n{dest}\n\nOverwrite?", "Overwrite", "Cancel"))
                {
                    return;
                }

                Directory.Delete(dest, true);
            }

            try
            {
                Directory.CreateDirectory(dest);
                ExtractSanitized(zipPath, dest);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError("[Jlyt.HwAds] iOS import failed: " + e);
                return;
            }

            bool ok = Directory.Exists(Path.Combine(dest, "HwAdsFramework.framework"));
            Debug.Log(ok
                ? $"[Jlyt.HwAds] iOS SDK {version} imported to {dest}. Verify Xcode build links frameworks (Unity links .framework files under Plugins/iOS automatically)."
                : $"[Jlyt.HwAds] Imported to {dest} but HwAdsFramework.framework was not found at the expected location.");
        }

        static string ParseVersion(string fileName)
        {
            var m = System.Text.RegularExpressions.Regex.Match(fileName, @"V?(\d+\.\d+\.\d+)");
            return m.Success ? m.Groups[1].Value : null;
        }

        static void ExtractSanitized(string zipPath, string destDir)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                string name = entry.FullName;
                if (name.IndexOf("__MACOSX", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string rel = Sanitize(name);
                string target = Path.Combine(destDir, rel);
                if (entry.FullName.EndsWith("/"))
                {
                    Directory.CreateDirectory(target);
                    continue;
                }

                var parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                entry.ExtractToFile(target, true);
            }
        }

        static string Sanitize(string entryName)
        {
            string[] parts = entryName.Replace('\\', '/').Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                char[] invalid = Path.GetInvalidFileNameChars();
                foreach (char c in invalid)
                {
                    parts[i] = parts[i].Replace(c, '_');
                }

                if (parts[i].Trim().Length == 0)
                {
                    parts[i] = "_";
                }
            }

            return Path.Combine(parts);
        }
    }
}
