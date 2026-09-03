using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Post-processes an exported Android Gradle project with the per-app config files the HWSDK
    /// requires (replaces the SDK-related copies that used to live in each project's export script).
    /// Runs only for Gradle "export project" builds, mirroring the previous per-project behavior.
    /// </summary>
    public class HwAndroidExportPatcher : IPostprocessBuildWithReport
    {
        public int callbackOrder => 900;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
            {
                return;
            }

            if (EditorUserBuildSettings.androidBuildSystem != AndroidBuildSystem.Gradle ||
                !EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                return;
            }

            string target = report.summary.outputPath;
            Copy("google-services.json", "launcher/google-services.json", target);
            Copy("hw-services.json", "unityLibrary/src/main/assets/hw-services.json", target);
            CopyRaw("Editor/raw/applovin_settings.json", "unityLibrary/src/main/res/raw/applovin_settings.json", target);
            CopyXml("Editor/xml/network_security_config.xml", "unityLibrary/src/main/res/xml/network_security_config.xml", target);
        }

        static void Copy(string assetUnderDataPath, string targetRelative, string targetRoot)
        {
            string source = Path.Combine(Application.dataPath, assetUnderDataPath);
            if (!File.Exists(source))
            {
                Debug.LogError($"[Jlyt.HwAds] Missing {source} (per-project config). Android export may fail.");
                return;
            }

            WriteTarget(source, Path.Combine(targetRoot, targetRelative));
        }

        static void CopyRaw(string relativeUnderDataPath, string targetRelative, string targetRoot)
        {
            string source = Path.Combine(Application.dataPath, relativeUnderDataPath);
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[Jlyt.HwAds] Missing {source} (applovin_settings.json). Skipped.");
                return;
            }

            WriteTarget(source, Path.Combine(targetRoot, targetRelative));
        }

        static void CopyXml(string relativeUnderDataPath, string targetRelative, string targetRoot)
        {
            string source = Path.Combine(Application.dataPath, relativeUnderDataPath);
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[Jlyt.HwAds] Missing {source} (network_security_config.xml). Skipped.");
                return;
            }

            WriteTarget(source, Path.Combine(targetRoot, targetRelative));
        }

        static void WriteTarget(string source, string target)
        {
            try
            {
                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(source, target, true);
                Debug.Log($"[Jlyt.HwAds] Copied {source} -> {target}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Jlyt.HwAds] Failed copying to {target}: {e.Message}");
            }
        }
    }
}
