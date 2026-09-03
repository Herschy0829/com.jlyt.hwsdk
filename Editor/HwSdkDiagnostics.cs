using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    public static class HwSdkDiagnostics
    {
        public static List<string> CollectIssues()
        {
            var issues = new List<string>();

            // 1. legacy jar files still living in the host project (would duplicate the SDK classes).
            var oldJars = System.IO.Directory.GetFiles("Assets/Plugins/Android", "hwads_*.jar")
                ?? new string[0];
            foreach (var jar in oldJars)
            {
                string name = System.IO.Path.GetFileName(jar);
                if (name != HwSdkVersions.JarFileName)
                {
                    issues.Add($"Legacy SDK jar found in host project: {jar}. Delete it (the package provides {HwSdkVersions.JarFileName}).");
                }
            }

            // 2. gradle templates carry our managed markers.
            string main = SafeRead("Assets/Plugins/Android/mainTemplate.gradle");
            if (!HwGradleSync.HasMarkers(main))
            {
                issues.Add("mainTemplate.gradle has no com.jlyt.hwsdk managed region. Run Tools/Jlyt/HwSDK/Sync Gradle Templates.");
            }

            // 2b. host native bridge sources installed & matching the package version.
            if (!HwNativeBridgeInstaller.HostFilesUpToDate())
            {
                issues.Add("Native bridge sources (Assets/Plugins/Android/HWAdsBridge.java, Assets/Plugins/iOS/HwAdsInterface.h/.m) are missing or out of date. Run Tools/Jlyt/HwSDK/Sync Android Gradle Templates + Install Native Bridges.");
            }

            // 3. minSdk.
            if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevelAuto &&
                (int)PlayerSettings.Android.minSdkVersion < HwSdkVersions.MinSdkVersion)
            {
                issues.Add($"Android minSdkVersion {PlayerSettings.Android.minSdkVersion} < required {HwSdkVersions.MinSdkVersion}.");
            }

            // 4. per-project config files.
            string[] required =
            {
                "Assets/hw-services.json",
                "Assets/google-services.json",
                "Assets/Editor/raw/applovin_settings.json",
                "Assets/Editor/xml/network_security_config.xml",
            };
            foreach (var path in required)
            {
                if (!System.IO.File.Exists(path))
                {
                    issues.Add($"Missing per-project config: {path}.");
                }
            }

            return issues;
        }

        public static void Run()
        {
            var issues = CollectIssues();
            if (issues.Count == 0)
            {
                Debug.Log("[Jlyt.HwAds] Diagnostics OK (upstream " + HwSdkVersions.UpstreamVersion + ").");
                EditorUtility.DisplayDialog("HWSDK Diagnostics",
                    "All checks passed.\n\nUpstream: " + HwSdkVersions.UpstreamVersion, "OK");
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < issues.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {issues[i]}");
            }

            Debug.LogWarning("[Jlyt.HwAds] Diagnostics issues:\n" + sb);
            EditorUtility.DisplayDialog("HWSDK Diagnostics", sb.ToString(), "OK");
        }

        static string SafeRead(string path)
        {
            try
            {
                return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
