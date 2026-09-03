using System;
using System.IO;
using System.Security.Cryptography;
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
            foreach (var (packageRel, hostRel) in Entries)
            {
                string src = ResolvePackageFile(packageRel);
                if (string.IsNullOrEmpty(src) || !File.Exists(src) ||
                    !File.Exists(hostRel) || !FilesEqual(src, hostRel))
                {
                    return false;
                }
            }

            return true;
        }

        static bool FilesEqual(string a, string b)
        {
            try
            {
                using (var s1 = File.OpenRead(a))
                using (var s2 = File.OpenRead(b))
                using (var sha = SHA1.Create())
                {
                    return Convert.ToBase64String(sha.ComputeHash(s1)) ==
                           Convert.ToBase64String(sha.ComputeHash(s2));
                }
            }
            catch (Exception)
            {
                return false;
            }
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
