using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Installs module-owned native bridge source files into the host project.
    ///
    /// Rationale: plain .java / .m / .h sources are not reliably picked up from Packages/Plugins
    /// on every Unity version, so the module keeps the authoritative copies under Native/ and this
    /// tool copies them to the conventional host locations (idempotent, hash-checked, single source).
    ///
    /// Installed files:
    ///   Native/Android/HWAdsBridge.java        -> Assets/Plugins/Android/HWAdsBridge.java
    ///   Native/iOS/HwAdsInterface.{h,m}        -> Assets/Plugins/iOS/HwAdsInterface.{h,m}
    /// The versioned .jar stays inside the package's Plugins/Android (included by Unity natively).
    /// </summary>
    public static class HwNativeBridgeInstaller
    {
        static readonly (string Package, string Host)[] Entries =
        {
            ("Native/Android/HWAdsBridge.java", "Assets/Plugins/Android/HWAdsBridge.java"),
            ("Native/iOS/HwAdsInterface.h", "Assets/Plugins/iOS/HwAdsInterface.h"),
            ("Native/iOS/HwAdsInterface.m", "Assets/Plugins/iOS/HwAdsInterface.m"),
        };

        public static bool InstallAndroid(out string log) => InstallEntry(0, out log);

        public static bool InstallIos(out string log)
        {
            bool ok1 = InstallEntry(1, out string log1);
            bool ok2 = InstallEntry(2, out string log2);
            log = log1 + "\n" + log2;
            return ok1 && ok2;
        }

        public static bool InstallAll(out string log)
        {
            bool ok = true;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Entries.Length; i++)
            {
                ok &= InstallEntry(i, out string item);
                sb.AppendLine(item);
            }

            log = sb.ToString();
            return ok;
        }

        static bool InstallEntry(int index, out string log)
        {
            var (packageRel, hostRel) = Entries[index];
            string src = ResolvePackageFile(packageRel);
            if (string.IsNullOrEmpty(src) || !File.Exists(src))
            {
                log = $"Package source missing: {packageRel}";
                Debug.LogError("[Jlyt.HwAds] " + log);
                return false;
            }

            string hostDir = Path.GetDirectoryName(hostRel);
            if (!string.IsNullOrEmpty(hostDir) && !Directory.Exists(hostDir))
            {
                Directory.CreateDirectory(hostDir);
            }

            string dst = hostRel;
            if (File.Exists(dst) && FilesEqual(src, dst))
            {
                log = "Up to date: " + dst;
                return true;
            }

            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            log = "Installed -> " + dst;
            Debug.Log("[Jlyt.HwAds] " + log);
            return true;
        }

        public static bool HostFilesUpToDate()
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (!EntryUpToDate(i))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Android scope: only Assets/Plugins/Android/HWAdsBridge.java matters.</summary>
        public static bool AndroidBridgeUpToDate() => EntryUpToDate(0);

        /// <summary>iOS scope: only Assets/Plugins/iOS/HwAdsInterface.{h,m} matter.</summary>
        public static bool IosBridgeUpToDate() => EntryUpToDate(1) && EntryUpToDate(2);

        static bool EntryUpToDate(int index)
        {
            var (packageRel, hostRel) = Entries[index];
            string src = ResolvePackageFile(packageRel);
            return !string.IsNullOrEmpty(src) && File.Exists(src) &&
                   File.Exists(hostRel) && FilesEqual(src, hostRel);
        }

        /// <summary>
        /// Content comparison, insensitive to line endings (CRLF vs LF) and a UTF-8 BOM, so hosts are
        /// not falsely flagged when the git package is checked out with different EOL settings.
        /// </summary>
        static bool FilesEqual(string a, string b)
        {
            try
            {
                return Normalize(File.ReadAllText(a)) == Normalize(File.ReadAllText(b));
            }
            catch (Exception)
            {
                return false;
            }
        }

        static string Normalize(string text)
        {
            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }

            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        static string ResolvePackageFile(string relativePath)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HwNativeBridgeInstaller).Assembly);
            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
            {
                return null;
            }

            return Path.Combine(info.resolvedPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
