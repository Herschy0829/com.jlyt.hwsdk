using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Locates and rewrites the SDK-managed regions inside the host project's gradle templates
    /// (Assets/Plugins/Android/mainTemplate.gradle, settingsTemplate.gradle).
    /// Strategy: replace the content between our begin/end markers when present; otherwise replace the
    /// legacy "//hwads need add ... //hwads need add end" region (first occurrence per file) with our block.
    /// </summary>
    public static class HwGradleSync
    {
        static string AndroidPluginsDir => "Assets/Plugins/Android";

        static string MainTemplatePath => Path.Combine(AndroidPluginsDir, "mainTemplate.gradle");
        static string SettingsTemplatePath => Path.Combine(AndroidPluginsDir, "settingsTemplate.gradle");

        public static bool HasMarkers(string text) =>
            text.Contains(HwSdkVersions.BeginMarker) && text.Contains(HwSdkVersions.EndMarker);

        static int IndexOfLegacy(string text)
        {
            int begin = text.IndexOf(HwSdkVersions.LegacyBegin, StringComparison.Ordinal);
            int end = text.IndexOf(HwSdkVersions.LegacyEnd, StringComparison.Ordinal);
            if (begin < 0 || end < 0 || end < begin)
            {
                return -1;
            }

            return begin;
        }

        /// <summary>
        /// Writes `blockLines` (marker wrapped) into the region delimited by our markers, or replaces
        /// the first legacy region. Returns the new content, or null when no writable region exists.
        /// </summary>
        public static string WriteManagedRegion(string original, IReadOnlyList<string> blockLines)
        {
            string block = string.Join(Environment.NewLine, blockLines);

            if (HasMarkers(original))
            {
                return ReplaceBetween(original, HwSdkVersions.BeginMarker, HwSdkVersions.EndMarker, block);
            }

            int legacyIdx = IndexOfLegacy(original);
            if (legacyIdx >= 0)
            {
                int end = original.IndexOf(HwSdkVersions.LegacyEnd, legacyIdx, StringComparison.Ordinal);
                int endAfter = end + HwSdkVersions.LegacyEnd.Length;
                return original.Substring(0, legacyIdx) + block + original.Substring(endAfter);
            }

            return null;
        }

        static string ReplaceBetween(string text, string beginToken, string endToken, string replacement)
        {
            int begin = text.IndexOf(beginToken, StringComparison.Ordinal);
            int end = text.IndexOf(endToken, begin >= 0 ? begin : 0, StringComparison.Ordinal);
            if (begin < 0 || end < 0 || end < begin)
            {
                return null;
            }

            int endAfter = end + endToken.Length;
            return text.Substring(0, begin) + replacement + text.Substring(endAfter);
        }

        public static bool SyncMainTemplate(out string log)
        {
            return SyncTemplateFile(MainTemplatePath, HwSdkVersions.MainDependenciesBlock().ToList(), out log);
        }

        public static bool SyncSettingsTemplate(out string log)
        {
            var lines = new List<string>();
            lines.Add(HwSdkVersions.BeginMarker);
            lines.AddRange(HwSdkVersions.RepositoryLines);
            lines.Add(HwSdkVersions.EndMarker);
            return SyncTemplateFile(SettingsTemplatePath, lines, out log);
        }

        static bool SyncTemplateFile(string assetPath, List<string> lines, out string log)
        {
            var sb = new StringBuilder();
            if (!File.Exists(assetPath))
            {
                log = $"Missing gradle template: {assetPath}";
                Debug.LogError("[Jlyt.HwAds] " + log);
                return false;
            }

            string original = File.ReadAllText(assetPath);
            string updated = WriteManagedRegion(original, lines);
            if (updated == null)
            {
                log = $"No managed or legacy region found in {assetPath}. Skipped.";
                Debug.LogWarning("[Jlyt.HwAds] " + log);
                return false;
            }

            if (updated == original)
            {
                log = $"Already up to date: {assetPath}";
                return true;
            }

            File.WriteAllText(assetPath, updated, new UTF8Encoding(true));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            log = $"Updated {assetPath}";
            Debug.Log("[Jlyt.HwAds] " + log);
            return true;
        }
    }
}
