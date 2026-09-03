using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Update detection & one-click upgrade.
    ///
    /// - "检测更新": compares
    ///     * the local module snapshot (resolved by Package Manager) with the remote git tag it should
    ///       point to (https://github.com/Herschy0829/com.jlyt.hwsdk, tag hw-9.8.68), and
    ///     * upstream SDK releases (artwl/hw_maxsdk_android & hw_maxsdk_ios) with the module baselines.
    /// - "一键更新": when the module tag moved to a newer commit, purge the stale local package cache,
    ///   re-resolve from git and let HwBridgeAutoSync re-write the bridge files / gradle managed regions
    ///   on the next domain load. (Upstream SDK version bumps are module-maintainer actions — the module
    ///   releases a new jar + dependency manifest; this tool surfaces them and links the changelog.)
    /// </summary>
    public static class HwUpdater
    {
        const string RepoApi = "https://api.github.com/repos/Herschy0829/com.jlyt.hwsdk";
        const string TagRef = "hw-9.8.68";
        const string AndroidUpstreamApi = "https://api.github.com/repos/artwl/hw_maxsdk_android/releases/latest";
        const string IosUpstreamApi = "https://api.github.com/repos/artwl/hw_maxsdk_ios/releases/latest";

        // ------------------------------------------------------------ state

        public static string LastCheckLog { get; private set; } = "";
        public static bool ModuleUpdateAvailable { get; private set; }
        public static bool UpstreamAndroidUpdateAvailable { get; private set; }
        public static bool UpstreamIosUpdateAvailable { get; private set; }

        // ------------------------------------------------------------ check

        public static string CheckAll()
        {
            var sb = new StringBuilder();
            ModuleUpdateAvailable = false;
            UpstreamAndroidUpdateAvailable = false;
            UpstreamIosUpdateAvailable = false;

            string local = DescribeLocal();
            bool gitMode = IsGitConsumed();
            string localCommit = GetLocalGitCommit();

            string remoteCommit = null;
            string remoteErr = null;
            GetRemoteTagCommit(out remoteCommit, out remoteErr);

            if (!string.IsNullOrEmpty(remoteErr))
            {
                sb.AppendLine("模块远端：检查失败 - " + remoteErr);
            }
            else if (!gitMode)
            {
                sb.AppendLine("本地模块：" + local);
                sb.AppendLine("远端标签 " + TagRef + "：" + Short(remoteCommit) +
                              "（当前为 file:/Local 开发引用：改用 git 引用后即可检测/一键更新）");
            }
            else if (IsSameSha(localCommit, remoteCommit))
            {
                sb.AppendLine("本地模块：" + local);
                sb.AppendLine("远端标签 " + TagRef + "：" + Short(remoteCommit) + "  → 已是最新");
            }
            else
            {
                ModuleUpdateAvailable = true;
                sb.AppendLine("本地模块：" + local);
                sb.AppendLine("远端标签 " + TagRef + "：" + Short(remoteCommit) + "  → 有更新，可一键更新");
            }

            string androidLatest = null;
            string iosLatest = null;
            string upstreamErr = null;
            GetUpstreamLatest(out androidLatest, out iosLatest, out upstreamErr);
            if (!string.IsNullOrEmpty(upstreamErr))
            {
                sb.AppendLine("上游 SDK：检查失败 - " + upstreamErr);
            }
            else
            {
                UpstreamAndroidUpdateAvailable = ParseVer(androidLatest) > ParseVer(HwSdkVersions.UpstreamVersion);
                UpstreamIosUpdateAvailable = ParseVer(iosLatest) > ParseVer(HwIosFrameworkImporter.ExpectedIosVersion);
                sb.AppendLine($"上游 Android：模块基线 {HwSdkVersions.UpstreamVersion}，官方最新 {androidLatest}" +
                              (UpstreamAndroidUpdateAvailable ? "（有新版，升级属模块发版动作）" : ""));
                sb.AppendLine($"上游 iOS：模块基线 {HwIosFrameworkImporter.ExpectedIosVersion}，官方最新 {iosLatest}" +
                              (UpstreamIosUpdateAvailable ? "（有新版，升级属模块发版动作）" : ""));
            }

            LastCheckLog = sb.ToString();
            return LastCheckLog;
        }

        // ------------------------------------------------------------ upgrade

        public static string UpgradeOneClick()
        {
            if (!IsGitConsumed())
            {
                LastCheckLog = "当前为 file:/Local 开发引用：一键更新仅对 git 引用生效（发布后改为 git URL）";
                return LastCheckLog;
            }

            string remoteCommit;
            string remoteErr;
            GetRemoteTagCommit(out remoteCommit, out remoteErr);
            if (!string.IsNullOrEmpty(remoteErr))
            {
                return "无法获取远端标签：" + remoteErr;
            }

            if (IsSameSha(GetLocalGitCommit(), remoteCommit))
            {
                LastCheckLog = "模块已是最新（" + Short(remoteCommit) + "）。";
                return LastCheckLog;
            }

            // 1) purge stale local snapshot + lock entry
            try
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HwUpdater).Assembly);
                if (info != null && !string.IsNullOrEmpty(info.resolvedPath) && Directory.Exists(info.resolvedPath))
                {
                    Directory.Delete(info.resolvedPath, true);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[Jlyt.HwAds] 清理旧包缓存失败(可忽略)：" + e.Message);
            }

            try
            {
                string lockPath = Path.Combine(Application.dataPath, "../Packages/packages-lock.json");
                if (File.Exists(lockPath))
                {
                    File.Delete(lockPath);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[Jlyt.HwAds] 清理 packages-lock.json 失败(可忽略)：" + e.Message);
            }

            // 2) request the upgrade; auto-sync runs on the next domain load after Resolve.
            HwBridgeAutoSync.RequestUpgrade(remoteCommit);
            UnityEditor.PackageManager.Client.Resolve();
            LastCheckLog = "已发起升级 " + Short(GetLocalGitCommit()) + " → " + Short(remoteCommit) +
                           "。编辑器会自动重新解析并重写桥源/托管段，完成后无需其它操作。";
            return LastCheckLog;
        }

        // ------------------------------------------------------------ http

        static void GetRemoteTagCommit(out string commit, out string error)
        {
            commit = null;
            error = null;
            string json = GetJson(RepoApi + "/git/ref/tags/" + TagRef, out error);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                var obj = UnityEngine.JsonUtility.FromJson<GitRef>(json);
                commit = obj != null && obj.@object != null ? obj.@object.sha : null;
                if (string.IsNullOrEmpty(commit))
                {
                    error = "标签响应缺少 commit sha";
                }
            }
            catch (Exception e)
            {
                error = e.Message;
            }
        }

        static void GetUpstreamLatest(out string android, out string ios, out string error)
        {
            android = ios = null;
            error = null;
            string aj = GetJson(AndroidUpstreamApi, out error);
            if (string.IsNullOrEmpty(aj))
            {
                return;
            }

            string ij = GetJson(IosUpstreamApi, out error);
            if (string.IsNullOrEmpty(ij))
            {
                return;
            }

            try
            {
                var a = UnityEngine.JsonUtility.FromJson<Release>(aj);
                var i = UnityEngine.JsonUtility.FromJson<Release>(ij);
                android = a != null ? a.tag_name : null;
                ios = i != null ? i.tag_name : null;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
        }

        static string GetJson(string url, out string error)
        {
            error = null;
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 20;
                req.SetRequestHeader("User-Agent", "jlyt-hwsdk-module");
                req.SetRequestHeader("Accept", "application/vnd.github+json");
                var sw = Stopwatch.StartNew();
                req.SendWebRequest();
                while (!req.isDone && sw.Elapsed.TotalSeconds < 25)
                {
                    System.Threading.Thread.Sleep(50);
                }

                if (req.isDone && req.result == UnityWebRequest.Result.Success)
                {
                    return req.downloadHandler.text;
                }

                error = string.IsNullOrEmpty(req.error)
                    ? "请求超时"
                    : $"HTTP {(int)req.responseCode} {req.error}";
                return null;
            }
        }

        // ------------------------------------------------------------ helpers

        static UnityEditor.PackageManager.PackageInfo CurrentPackage()
        {
            return UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HwUpdater).Assembly);
        }

        static bool IsGitConsumed()
        {
            var info = CurrentPackage();
            return info != null && info.source == UnityEditor.PackageManager.PackageSource.Git;
        }

        static string GetLocalGitCommit()
        {
            var info = CurrentPackage();
            if (info == null || info.source != UnityEditor.PackageManager.PackageSource.Git ||
                string.IsNullOrEmpty(info.resolvedPath))
            {
                return null;
            }

            int at = info.resolvedPath.LastIndexOf('@');
            return at >= 0 && at < info.resolvedPath.Length - 1
                ? info.resolvedPath.Substring(at + 1)
                : null;
        }

        static string DescribeLocal()
        {
            var info = CurrentPackage();
            if (info == null || string.IsNullOrEmpty(info.resolvedPath))
            {
                return "(未解析)";
            }

            if (info.source == UnityEditor.PackageManager.PackageSource.Git)
            {
                string commit = GetLocalGitCommit();
                return commit != null ? "git " + Short(commit) : "git v" + info.version;
            }

            return info.source + " v" + info.version;
        }

        /// <summary>
        /// Compares two commit identifiers regardless of length (UPM caches use short hashes,
        /// the GitHub API returns the full 40-char sha).
        /// </summary>
        static bool IsSameSha(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return false;
            }

            if (a.Equals(b, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int prefix = Math.Min(a.Length, b.Length);
            if (prefix < 8)
            {
                return false;
            }

            return string.Compare(a, 0, b, 0, 8, StringComparison.OrdinalIgnoreCase) == 0;
        }

        static string Short(string sha)
        {
            return string.IsNullOrEmpty(sha) || sha.Length < 8 ? sha : sha.Substring(0, 8);
        }

        static Version ParseVer(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return new Version(0, 0, 0);
            }

            var digits = System.Text.RegularExpressions.Regex.Match(tag, "(\\d+)\\.(\\d+)\\.(\\d+)");
            if (!digits.Success)
            {
                return new Version(0, 0, 0);
            }

            return new Version(
                int.Parse(digits.Groups[1].Value),
                int.Parse(digits.Groups[2].Value),
                int.Parse(digits.Groups[3].Value));
        }

        [Serializable]
        public class GitRef
        {
            [Serializable]
            public class RefObject
            {
                public string sha;
                public string type;
            }

            public RefObject @object;
        }

        [Serializable]
        public class Release
        {
            public string tag_name;
        }
    }
}
