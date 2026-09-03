using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Auto-sync of module-owned generated files (native bridges + gradle managed regions).
    ///
    /// Rule: the module (as resolved by the package manager) is the single source of truth.
    /// After a domain load we compare the currently resolved module revision with the last revision we
    /// synced (stored per-project in EditorPrefs). If the revision differs — i.e. the module was updated
    /// — we automatically:
    ///   1) re-install the native bridge sources into the host project,
    ///   2) re-sync the gradle managed regions (dependencies / repositories) when they changed,
    ///   3) store the new revision, log what happened, and (when an upgrade was requested) finish the
    ///      one-click upgrade.
    ///
    /// Host copies are generated artifacts: the module version always wins, no manual merge.
    /// </summary>
    [InitializeOnLoad]
    public static class HwBridgeAutoSync
    {
        const int MaxAttempts = 90;
        const string UpgradeRequestKeyPrefix = "Jlyt.HwAds.UpgradeRequest.";

        static readonly string SyncStateKey = "Jlyt.HwAds.AutoSync." + Application.dataPath;

        static HwBridgeAutoSync() // runs after every domain load / editor open
        {
            EditorApplication.delayCall += ScheduleIfReady;
        }

        static int _attempts;

        static void ScheduleIfReady()
        {
            if (Application.isBatchMode)
            {
                // CI: run the sync explicitly (or rely on a fresh clone + install step).
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || TrySyncNow())
            {
                return;
            }

            _attempts++;
            if (_attempts < MaxAttempts)
            {
                EditorApplication.delayCall += ScheduleIfReady;
            }
            else
            {
                Debug.LogWarning("[Jlyt.HwAds] Auto-sync deferred: module not yet resolvable.");
            }
        }

        static bool TrySyncNow()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HwBridgeAutoSync).Assembly);
            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
            {
                return false;
            }

            string signature = BuildSignature(info);
            string last = EditorPrefs.GetString(SyncStateKey, "");
            bool revisionChanged = signature != last;
            bool upgradeRequested = EditorPrefs.GetString(UpgradeRequestKeyPrefix + Application.dataPath, "").Length > 0;

            if (!revisionChanged && !upgradeRequested)
            {
                return true; // up to date and no pending request
            }

            var logs = new System.Text.StringBuilder();
            logs.AppendLine("[Jlyt.HwAds] module revision changed: " + last + " -> " + signature);

            HwNativeBridgeInstaller.InstallAll(out string bridgeLog);
            logs.AppendLine(bridgeLog);

            HwGradleSync.SyncMainTemplate(out string mainLog);
            logs.AppendLine(mainLog);

            HwGradleSync.SyncSettingsTemplate(out string settingsLog);
            logs.AppendLine(settingsLog);

            HwGradleSync.SyncBaseProjectTemplate(out string baseLog);
            logs.AppendLine(baseLog);

            EditorPrefs.SetString(SyncStateKey, signature);

            if (upgradeRequested)
            {
                EditorPrefs.DeleteKey(UpgradeRequestKeyPrefix + Application.dataPath);
                logs.AppendLine("One-click upgrade completed: package re-resolved, files re-written.");
            }

            Debug.Log(logs.ToString().TrimEnd());
            return true;
        }

        static string BuildSignature(UnityEditor.PackageManager.PackageInfo info)
        {
            if (info.source == UnityEditor.PackageManager.PackageSource.Git)
            {
                int at = info.resolvedPath.LastIndexOf('@');
                if (at >= 0 && at < info.resolvedPath.Length - 1)
                {
                    return "git@" + info.resolvedPath.Substring(at + 1);
                }
            }

            return info.source + "|" + info.version + "|" + info.resolvedPath;
        }

        /// <summary>Request an auto-upgrade on the next domain load (used by the one-click update).</summary>
        public static void RequestUpgrade(string remoteRevision)
        {
            EditorPrefs.SetString(UpgradeRequestKeyPrefix + Application.dataPath, remoteRevision);
        }

        /// <summary>True when a one-click upgrade is still pending (e.g. waiting for editor reload).</summary>
        public static bool HasPendingUpgrade() =>
            EditorPrefs.GetString(UpgradeRequestKeyPrefix + Application.dataPath, "").Length > 0;
    }
}
