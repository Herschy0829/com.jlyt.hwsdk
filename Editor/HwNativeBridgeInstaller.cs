using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Installs the module-owned Java bridge (HWAdsBridge.java) into the host project's
    /// Assets/Plugins/Android folder. Plain .java sources are not reliably picked up from
    /// Packages/Plugins/Android on every Unity version, so the module keeps the authoritative
    /// copy under Native/Android and installs it into the project (idempotent, hash-checked).
    /// The .jar itself is versioned inside the package's Plugins/Android and is included by Unity.
    /// </summary>
    public static class HwNativeBridgeInstaller
    {
        public const string PackageSourceRelative = "Native/Android/HWAdsBridge.java";
        public const string HostRelative = "Assets/Plugins/Android/HWAdsBridge.java";

        public static bool Install(out string log)
        {
            string src = ResolvePackageFile(PackageSourceRelative);
            if (string.IsNullOrEmpty(src) || !File.Exists(src))
            {
                log = $"Package source missing: {PackageSourceRelative}";
                Debug.LogError("[Jlyt.HwAds] " + log);
                return false;
            }

            string hostDir = Path.GetDirectoryName(HostRelative);
            if (!Directory.Exists(hostDir))
            {
                Directory.CreateDirectory(hostDir);
            }

            string dst = HostRelative;
            if (File.Exists(dst) && FilesEqual(src, dst))
            {
                log = "Android Java bridge already up to date at " + dst;
                return true;
            }

            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            log = $"Installed Android Java bridge -> {dst}";
            Debug.Log("[Jlyt.HwAds] " + log);
            return true;
        }

        /// <summary>True when the host has the bridge and it matches the package copy.</summary>
        public static bool HostBridgeUpToDate()
        {
            string src = ResolvePackageFile(PackageSourceRelative);
            return !string.IsNullOrEmpty(src) && File.Exists(src) &&
                   File.Exists(HostRelative) && FilesEqual(src, HostRelative);
        }

        static bool FilesEqual(string a, string b)
        {
            byte[] ha;
            byte[] hb;
            try
            {
                using (var s1 = File.OpenRead(a))
                using (var s2 = File.OpenRead(b))
                {
                    ha = System.Security.Cryptography.SHA1.Create().ComputeHash(s1);
                    hb = System.Security.Cryptography.SHA1.Create().ComputeHash(s2);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return Convert.ToBase64String(ha) == Convert.ToBase64String(hb);
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
