using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Locates and rewrites the SDK-managed regions inside the host project's gradle templates
    /// (Assets/Plugins/Android/mainTemplate.gradle, settingsTemplate.gradle).
    ///
    /// mainTemplate: the managed block replaces the dependencies region between our begin/end markers,
    /// or the first legacy "//hwads need add ... //hwads need add end" region.
    ///
    /// settingsTemplate: repositories are kept exactly once inside the dependencyResolutionManagement
    /// repositories block (the one Gradle actually resolves module deps from). Legacy/duplicate copies
    /// of the canonical repositories are removed so the template stays a single source driven by the
    /// module version manifest.
    /// </summary>
    public static class HwGradleSync
    {
        static string AndroidPluginsDir => "Assets/Plugins/Android";

        static string MainTemplatePath => Path.Combine(AndroidPluginsDir, "mainTemplate.gradle");
        static string SettingsTemplatePath => Path.Combine(AndroidPluginsDir, "settingsTemplate.gradle");

        static readonly string[] KnownRepoUrls =
        {
            "https://artifactory.bidmachine.io/bidmachine",
            "https://cboost.jfrog.io/artifactory/chartboost-ads/",
            "https://android-sdk.is.com",
            "https://artifact.bytedance.com/repository/pangle",
            "https://s3.amazonaws.com/smaato-sdk-releases/",
            "https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea",
        };

        static readonly Regex RepoUrlRegex =
            new Regex(@"maven\s*\{\s*url\s*['""](?<url>[^'""]+)['""]", RegexOptions.Compiled);

        public static bool HasMarkers(string text) =>
            text.Contains(HwSdkVersions.BeginMarker) && text.Contains(HwSdkVersions.EndMarker);

        static bool IsEndToken(string trimmed) =>
            trimmed == HwSdkVersions.LegacyEnd || trimmed == HwSdkVersions.LegacyEndAlt;

        static string CanonicalBlock() =>
            string.Join(Environment.NewLine, HwSdkVersions.SettingsBlockLines());

        // ---------------------------------------------------------------- main template

        /// <summary>Replace content between our markers, or replace the first legacy region.</summary>
        public static string WriteManagedRegion(string original, IReadOnlyList<string> blockLines)
        {
            string block = string.Join(Environment.NewLine, blockLines);

            if (HasMarkers(original))
            {
                return ReplaceBetweenTokens(original, HwSdkVersions.BeginMarker, HwSdkVersions.EndMarker, block);
            }

            string[] lines = SplitLines(original);
            int beginIdx = -1;
            int endIdx = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(HwSdkVersions.LegacyBegin))
                {
                    beginIdx = i;
                    continue;
                }

                if (beginIdx >= 0 && endIdx < 0 && IsEndToken(lines[i].Trim()))
                {
                    endIdx = i;
                    break;
                }
            }

            if (beginIdx < 0 || endIdx < 0)
            {
                return null;
            }

            var head = lines.Take(beginIdx);
            var tail = lines.Skip(endIdx + 1);
            return JoinLines(head.Concat(new[] { block }).Concat(tail));
        }

        // ---------------------------------------------------------------- settings template

        public static string SyncSettingsTemplateContent(string original)
        {
            string block = CanonicalBlock();
            string updated = original;

            if (HasMarkers(updated))
            {
                // Replace between the (last) marker pair, then let the dedupe pass drop strays.
                updated = ReplaceBetweenTokens(updated, HwSdkVersions.BeginMarker, HwSdkVersions.EndMarker, block, last: true);
            }
            else
            {
                // One-time legacy migration: locate the legacy region.
                string[] lines = SplitLines(updated);
                int beginIdx = -1;
                int endIdx = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(HwSdkVersions.LegacyBegin))
                    {
                        beginIdx = i;
                        continue;
                    }

                    if (beginIdx >= 0 && endIdx < 0 && IsEndToken(lines[i].Trim()))
                    {
                        endIdx = i;
                        break;
                    }
                }

                if (beginIdx >= 0 && endIdx >= 0)
                {
                    int depIdx = -1;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("dependencyResolutionManagement"))
                        {
                            depIdx = i;
                            break;
                        }
                    }

                    bool legacyInsideDepResolution = depIdx >= 0 && depIdx < beginIdx;
                    var head = lines.Take(beginIdx);
                    var tail = lines.Skip(endIdx + 1);
                    // If the legacy block lives inside dependencyResolutionManagement, adopt it as the
                    // managed block; otherwise drop it (the managed block is re-added below in the right place).
                    var middle = legacyInsideDepResolution
                        ? new[] { block }
                        : Enumerable.Empty<string>();
                    updated = JoinLines(head.Concat(middle).Concat(tail));
                }
            }

            // Remove duplicate copies of the canonical repositories outside the managed block first,
            // then make sure the canonical set exists inside dependencyResolutionManagement.
            updated = RemoveDuplicateRepos(updated);
            updated = EnsureDependencyResolutionRepos(updated, block);

            return updated;
        }

        static string EnsureDependencyResolutionRepos(string text, string block)
        {
            string[] lines = SplitLines(text);
            int depIdx = -1;
            int repositoriesIdx = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (depIdx < 0 && lines[i].Contains("dependencyResolutionManagement"))
                {
                    depIdx = i;
                    continue;
                }

                if (depIdx >= 0 && lines[i].Contains("repositories {"))
                {
                    repositoriesIdx = i;
                    break;
                }
            }

            if (depIdx < 0 || repositoriesIdx < 0)
            {
                return text;
            }

            // Already present inside the dependency resolution area?
            bool hasBlock = false;
            bool hasRepo = false;
            for (int i = repositoriesIdx; i < lines.Length; i++)
            {
                if (lines[i].Contains(HwSdkVersions.BeginMarker))
                {
                    hasBlock = true;
                }

                if (RepoUrlRegex.Match(lines[i]).Success)
                {
                    hasRepo = true;
                }

                // The repositories block is closed by the first line at column 0 that is '}'.
                if (!string.IsNullOrWhiteSpace(lines[i]) && lines[i].Trim() == "}" && i > repositoriesIdx && i > depIdx)
                {
                    break;
                }
            }

            if (hasBlock || hasRepo)
            {
                return text;
            }

            var head = lines.Take(repositoriesIdx + 1);
            var tail = lines.Skip(repositoriesIdx + 1);
            return JoinLines(head.Concat(new[] { block }).Concat(tail));
        }

        static string RemoveDuplicateRepos(string text)
        {
            string[] lines = SplitLines(text);
            var kept = new List<string>(lines.Length);
            bool insideMarkers = false;
            foreach (var raw in lines)
            {
                string line = raw;
                if (line.Contains(HwSdkVersions.BeginMarker))
                {
                    insideMarkers = true;
                }

                if (!insideMarkers)
                {
                    var m = RepoUrlRegex.Match(line);
                    if (m.Success && KnownRepoUrls.Contains(m.Groups["url"].Value, StringComparer.Ordinal))
                    {
                        // Duplicate copy outside the managed block -> drop.
                        continue;
                    }
                }

                kept.Add(line);

                if (insideMarkers && line.Contains(HwSdkVersions.EndMarker))
                {
                    insideMarkers = false;
                }
            }

            return JoinLines(kept);
        }

        // ---------------------------------------------------------------- shared helpers

        static string[] SplitLines(string text) =>
            text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        static string JoinLines(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

        static string ReplaceBetweenTokens(string text, string beginToken, string endToken, string replacement, bool last = false)
        {
            int begin = last ? text.LastIndexOf(beginToken, StringComparison.Ordinal)
                             : text.IndexOf(beginToken, StringComparison.Ordinal);
            int startOfEnd = begin >= 0 ? text.IndexOf(endToken, begin + beginToken.Length, StringComparison.Ordinal) : -1;
            if (begin < 0 || startOfEnd < 0)
            {
                return null;
            }

            int endAfter = startOfEnd + endToken.Length;
            return text.Substring(0, begin) + replacement + text.Substring(endAfter);
        }

        public static bool SyncMainTemplate(out string log)
        {
            var lines = HwSdkVersions.MainDependenciesBlock().ToList();
            return SyncTemplateFile(MainTemplatePath, () =>
            {
                string original = File.ReadAllText(MainTemplatePath);
                return WriteManagedRegion(original, lines);
            }, out log);
        }

        public static bool SyncSettingsTemplate(out string log)
        {
            return SyncTemplateFile(SettingsTemplatePath, () =>
            {
                string original = File.ReadAllText(SettingsTemplatePath);
                return SyncSettingsTemplateContent(original);
            }, out log);
        }

        static bool SyncTemplateFile(string assetPath, Func<string> update, out string log)
        {
            if (!File.Exists(assetPath))
            {
                log = $"Missing gradle template: {assetPath}";
                Debug.LogError("[Jlyt.HwAds] " + log);
                return false;
            }

            string original = File.ReadAllText(assetPath);
            string updated = update();
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
