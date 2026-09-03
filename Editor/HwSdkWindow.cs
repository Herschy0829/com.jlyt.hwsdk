using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// JLYTSDK management window (Window > JLYTSDK).
    /// - Auto-detects what the current platform needs from the hwsdk module and marks missing items.
    /// - Shows only the tools for the currently selected build platform (Android / iOS).
    /// - Every tool has a short description; documentation links open in the browser.
    /// </summary>
    public class HwSdkWindow : EditorWindow
    {
        public enum PlatformMode
        {
            Android,
            Ios,
            None,
        }

        public enum CheckState
        {
            Ok,
            Warn,
            Missing,
            Info,
        }

        sealed class CheckItem
        {
            public string name;
            public CheckState state = CheckState.Info;
            public string detail;
            public string fix;
            public string quickFixLabel;
            public Action quickFix;
        }

        sealed class CheckGroup
        {
            public string title;
            public readonly List<CheckItem> items = new List<CheckItem>();
        }

        sealed class ToolEntry
        {
            public string name;
            public string description;
            public Action execute;
        }

        Vector2 _scroll;
        readonly List<CheckGroup> _groups = new List<CheckGroup>();
        readonly List<ToolEntry> _tools = new List<ToolEntry>();
        string _lastLog;
        string _updateStatus;
        bool _checkedOnce;
        readonly Dictionary<string, bool> _groupFold = new Dictionary<string, bool>();

        const string RepoUrl = "https://github.com/Herschy0829/com.jlyt.hwsdk";
        const string RepoTag = "#hw-9.8.68";

        // ---------------------------------------------------------------- open

        public static void Open() => GetWindow<HwSdkWindow>(true, "JLYTSDK · HWSDK", true);

        [MenuItem("Window/JLYTSDK")]
        public static void MenuOpen()
        {
            var win = GetWindow<HwSdkWindow>(true, "JLYTSDK · HWSDK", true);
            win.minSize = new Vector2(560, 560);
        }

        void OnEnable()
        {
            minSize = new Vector2(560, 560);
            // Auto-run detection shortly after the window opens.
            EditorApplication.delayCall += () =>
            {
                if (this != null && !_checkedOnce)
                {
                    RefreshAll();
                }
            };
        }

        // ---------------------------------------------------------------- refresh

        void RefreshAll()
        {
            _groups.Clear();
            _tools.Clear();
            _lastLog = null;
            _checkedOnce = true;

            PlatformMode platform = ResolvePlatform();

            _groups.Add(BuildCommonGroup(platform));
            if (platform == PlatformMode.Android)
            {
                _groups.Add(BuildAndroidGroup());
                _groups.Add(BuildToolchainGroup(PlatformMode.Android));
                FillAndroidTools();
            }
            else if (platform == PlatformMode.Ios)
            {
                _groups.Add(BuildIosGroup());
                _groups.Add(BuildToolchainGroup(PlatformMode.Ios));
                FillIosTools();
            }
            else
            {
                _groups.Add(BuildNoPlatformGroup());
            }

            FillDocsTools();

            foreach (var g in _groups)
            {
                if (!_groupFold.ContainsKey(g.title))
                {
                    _groupFold[g.title] = true; // expand by default
                }
            }

            Repaint();
        }

        static PlatformMode ResolvePlatform()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android: return PlatformMode.Android;
                case BuildTarget.iOS: return PlatformMode.Ios;
                default: return PlatformMode.None;
            }
        }

        // ---------------------------------------------------------------- checks

        CheckGroup BuildCommonGroup(PlatformMode platform)
        {
            var g = new CheckGroup { title = "通用 · 模块与编辑器" };

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HwSdkWindow).Assembly);
            bool pkgOk = info != null && !string.IsNullOrEmpty(info.resolvedPath);
            g.items.Add(new CheckItem
            {
                name = "模块 com.jlyt.hwsdk 已解析",
                state = pkgOk ? CheckState.Ok : CheckState.Missing,
                detail = pkgOk ? $"{info.source} · v{info.version}" : "未在 Package Manager 中解析",
                fix = pkgOk ? null : "检查 Packages/manifest.json 是否引用 com.jlyt.hwsdk，并在 Package Manager 中更新",
            });

            string header = HwSdkDiagnostics.BuildHeader();
            bool supported = header.Contains("supported");
            g.items.Add(new CheckItem
            {
                name = "Unity 版本支持",
                state = supported ? CheckState.Ok : CheckState.Warn,
                detail = header,
                fix = supported ? null : "支持的编辑器为 2022.3 LTS 与 Unity 6000+；低于 2022.3 不在支持范围",
            });

            g.items.Add(new CheckItem
            {
                name = "平台",
                state = CheckState.Info,
                detail = platform == PlatformMode.Android ? "当前构建平台：Android"
                       : platform == PlatformMode.Ios ? "当前构建平台：iOS"
                       : "当前构建平台非 Android/iOS（请先切换构建目标）",
            });

            // Map the shared diagnostics onto per-item rows (bridge rows are handled per-platform below).
            foreach (var issue in HwSdkDiagnostics.CollectIssues())
            {
                if (issue.Contains("Native bridge sources"))
                {
                    continue;
                }

                g.items.Add(new CheckItem
                {
                    name = "诊断项",
                    state = CheckState.Missing,
                    detail = issue,
                    fix = "按工具区提示修复后点击“重新检测”",
                });
            }

            return g;
        }

        CheckGroup BuildAndroidGroup()
        {
            var g = new CheckGroup { title = "Android · SDK 所需内容检测" };
            bool markersMain = SafeRead("Assets/Plugins/Android/mainTemplate.gradle").Contains(HwSdkVersions.BeginMarker);
            bool markersSettings = SafeRead("Assets/Plugins/Android/settingsTemplate.gradle").Contains(HwSdkVersions.BeginMarker);
            bool javaExists = File.Exists("Assets/Plugins/Android/HWAdsBridge.java");
            bool javaOk = javaExists && HwNativeBridgeInstaller.AndroidBridgeUpToDate();
            bool minSdkOk = PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto ||
                            (int)PlayerSettings.Android.minSdkVersion >= HwSdkVersions.MinSdkVersion;

            g.items.Add(new CheckItem
            {
                name = "依赖托管段（mainTemplate.gradle）",
                state = markersMain ? CheckState.Ok : CheckState.Missing,
                detail = markersMain ? $"含 {HwSdkVersions.UpstreamVersion} 依赖清单" : "缺少 com.jlyt.hwsdk managed 段",
                fix = "运行工具“同步 Android Gradle 模板 + 安装桥源”",
            });
            g.items.Add(new CheckItem
            {
                name = "仓库托管段（settingsTemplate.gradle）",
                state = markersSettings ? CheckState.Ok : CheckState.Missing,
                detail = markersSettings ? "dependencyResolutionManagement 含托管仓库" : "缺少托管仓库段",
                fix = "运行工具“同步 Android Gradle 模板 + 安装桥源”",
            });
            g.items.Add(new CheckItem
            {
                name = "Java 桥源 HWAdsBridge.java",
                state = !javaExists ? CheckState.Missing : (javaOk ? CheckState.Ok : CheckState.Warn),
                detail = !javaExists ? "未安装到 Assets/Plugins/Android"
                       : javaOk ? "已安装且与模块版本一致"
                       : "文件存在但与模块内容不一致（模块更新或本地改动），以模块版本覆盖",
                fix = "点击右侧“修复”按钮重装，或运行工具“同步 Android Gradle 模板 + 安装桥源”",
                quickFixLabel = "修复",
                quickFix = QuickFixAndroid,
            });

            foreach (var cfg in new[]
                     {
                         "Assets/hw-services.json",
                         "Assets/google-services.json",
                         "Assets/Editor/raw/applovin_settings.json",
                         "Assets/Editor/xml/network_security_config.xml",
                     })
            {
                g.items.Add(new CheckItem
                {
                    name = "配置文件 " + cfg,
                    state = File.Exists(cfg) ? CheckState.Ok : CheckState.Missing,
                    detail = File.Exists(cfg) ? "已存在" : "缺失",
                    fix = "向负责人获取该每工程配置并放置到上述路径",
                });
            }

            g.items.Add(new CheckItem
            {
                name = "minSdk ≥ " + HwSdkVersions.MinSdkVersion,
                state = minSdkOk ? CheckState.Ok : CheckState.Missing,
                detail = "当前 " + PlayerSettings.Android.minSdkVersion,
                fix = "Player Settings → Android → Min API Level 设为 ≥ " + HwSdkVersions.MinSdkVersion,
            });

            // Manifest requirements (from the official demo + Unity needs).
            string manifest = SafeRead("Assets/Plugins/Android/AndroidManifest.xml");
            foreach (var req in ManifestRequirements)
            {
                bool ok = manifest.Contains(req.Token);
                g.items.Add(new CheckItem
                {
                    name = "AndroidManifest · " + req.Name,
                    state = ok ? CheckState.Ok : CheckState.Missing,
                    detail = ok ? "已包含" : "缺失",
                    fix = "在 Assets/Plugins/Android/AndroidManifest.xml 中添加：" + req.Example,
                });
            }

            return g;
        }

        sealed class ManifestReq
        {
            public string Name;
            public string Token;
            public string Example;
        }

        static readonly ManifestReq[] ManifestRequirements =
        {
            new ManifestReq { Name = "权限 INTERNET", Token = "android.permission.INTERNET", Example = "<uses-permission android:name=\"android.permission.INTERNET\"/>" },
            new ManifestReq { Name = "权限 ACCESS_NETWORK_STATE", Token = "android.permission.ACCESS_NETWORK_STATE", Example = "<uses-permission android:name=\"android.permission.ACCESS_NETWORK_STATE\"/>" },
            new ManifestReq { Name = "权限 READ_PHONE_STATE", Token = "android.permission.READ_PHONE_STATE", Example = "<uses-permission android:name=\"android.permission.READ_PHONE_STATE\"/>" },
            new ManifestReq { Name = "权限 AD_ID (Android 12+)", Token = "com.google.android.gms.permission.AD_ID", Example = "<uses-permission android:name=\"com.google.android.gms.permission.AD_ID\"/>" },
            new ManifestReq { Name = "networkSecurityConfig", Token = "networkSecurityConfig=\"@xml/network_security_config\"", Example = "application 上 android:networkSecurityConfig=\"@xml/network_security_config\"" },
            new ManifestReq { Name = "gms version meta", Token = "com.google.android.gms.version", Example = "<meta-data android:name=\"com.google.android.gms.version\" android:value=\"@integer/google_play_services_version\"/>" },
            new ManifestReq { Name = "ads APPLICATION_ID", Token = "com.google.android.gms.ads.APPLICATION_ID", Example = "<meta-data android:name=\"com.google.android.gms.ads.APPLICATION_ID\" android:value=\"ca-app-pub-...\"/>" },
            new ManifestReq { Name = "AD_MANAGER_APP meta", Token = "com.google.android.gms.ads.AD_MANAGER_APP", Example = "<meta-data android:name=\"com.google.android.gms.ads.AD_MANAGER_APP\" android:value=\"true\"/>" },
            new ManifestReq { Name = "AdActivity 组件", Token = "com.google.android.gms.ads.AdActivity", Example = "<activity android:name=\"com.google.android.gms.ads.AdActivity\" .../>" },
        };

        CheckGroup BuildIosGroup()
        {
            var g = new CheckGroup { title = "iOS · SDK 所需内容检测" };
            bool bridgeH = File.Exists("Assets/Plugins/iOS/HwAdsInterface.h");
            bool bridgeM = File.Exists("Assets/Plugins/iOS/HwAdsInterface.m");
            bool bridgesOk = bridgeH && bridgeM && HwNativeBridgeInstaller.IosBridgeUpToDate();
            bool frameworkImported = Directory.Exists("Assets/Plugins/iOS/HwAdsNative") &&
                                     Directory.GetDirectories("Assets/Plugins/iOS/HwAdsNative", "HwAdsFramework.framework", SearchOption.AllDirectories).Length > 0;

            g.items.Add(new CheckItem
            {
                name = "iOS 桥源 HwAdsInterface.h/.m",
                state = !(bridgeH && bridgeM) ? CheckState.Missing : (bridgesOk ? CheckState.Ok : CheckState.Warn),
                detail = !(bridgeH && bridgeM) ? "缺失于 Assets/Plugins/iOS"
                       : bridgesOk ? "已安装且与模块版本一致"
                       : "文件存在但与模块内容不一致（模块更新或本地改动），以模块版本覆盖",
                fix = "点击右侧“修复”按钮重装，或运行工具“安装 iOS 桥源”",
                quickFixLabel = "修复",
                quickFix = QuickFixIos,
            });
            g.items.Add(new CheckItem
            {
                name = "HwAdsFramework（官方 zip 导入）",
                state = frameworkImported ? CheckState.Ok : CheckState.Missing,
                detail = frameworkImported ? "Assets/Plugins/iOS/HwAdsNative 已含 HwAdsFramework.framework" : "未导入（约 113MB，不入 git）",
                fix = "运行工具“导入 iOS SDK Release (zip)”选择 HwAds_iOS_V9.8.75.zip",
            });
            g.items.Add(new CheckItem
            {
                name = "构建环境",
                state = CheckState.Info,
                detail = "iOS 构建需在 macOS + Xcode 上进行；导入工具在任意平台都可将 framework 就位",
            });
            return g;
        }

        CheckGroup BuildToolchainGroup(PlatformMode platform)
        {
            var g = new CheckGroup { title = platform == PlatformMode.Android ? "Android · 工具链建议" : "iOS · 工具链建议" };
            string jdk = EditorPrefs.GetString("JdkPath", "");
            int jdkMajor = ReadJdkMajor(jdk);
            bool jdkOk = jdkMajor >= 17;
            g.items.Add(new CheckItem
            {
                name = "JDK 17+（AGP 8.x 必需）",
                state = string.IsNullOrEmpty(jdk) ? CheckState.Warn : (jdkOk ? CheckState.Ok : CheckState.Warn),
                detail = string.IsNullOrEmpty(jdk) ? "未配置" : $"{jdk} (major {jdkMajor})",
                fix = "安装 JDK 17+（如 Temurin 17）并到 Preferences → External Tools 指定；Unity 2022.3 自带为 JDK 11",
            });

            if (platform == PlatformMode.Android)
            {
                string ndk = EditorPrefs.GetString("AndroidNdkRoot", "");
                int ndkMajor = ReadNdkMajor(ndk);
                g.items.Add(new CheckItem
                {
                    name = "NDK（建议 ≥27 或与 AGP 匹配）",
                    state = string.IsNullOrEmpty(ndk) ? CheckState.Warn : CheckState.Info,
                    detail = string.IsNullOrEmpty(ndk) ? "未配置（如仅导出工程可暂不要求）" : $"{ndk} (r{ndkMajor})",
                    fix = "AGP 8.10 默认 NDK 27.0.12077973；做原生/IL2CPP 编译时用 sdkmanager 安装或模板显式指向已装版本",
                });
            }

            return g;
        }

        CheckGroup BuildNoPlatformGroup()
        {
            var g = new CheckGroup { title = "未选择 Android/iOS 构建目标" };
            g.items.Add(new CheckItem
            {
                name = "请先切换构建平台",
                state = CheckState.Missing,
                detail = "File → Build Settings → Android 或 iOS，然后再打开本窗口",
                fix = "切换到 Android 或 iOS 后，本窗口将只显示对应平台的检测与工具",
            });
            return g;
        }

        static int ReadJdkMajor(string jdkRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(jdkRoot) || !Directory.Exists(jdkRoot))
                {
                    return 0;
                }

                string release = Path.Combine(jdkRoot, "release");
                if (File.Exists(release))
                {
                    foreach (var line in File.ReadAllLines(release))
                    {
                        if (line.StartsWith("JAVA_VERSION="))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(line, "\"(\\d+)");
                            if (m.Success && int.TryParse(m.Groups[1].Value, out int v))
                            {
                                return v;
                            }
                        }
                    }
                }

                return File.Exists(Path.Combine(jdkRoot, "bin", "java.exe")) ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        static int ReadNdkMajor(string ndkRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(ndkRoot) || !Directory.Exists(ndkRoot))
                {
                    return 0;
                }

                string sp = Path.Combine(ndkRoot, "source.properties");
                if (File.Exists(sp))
                {
                    foreach (var line in File.ReadAllLines(sp))
                    {
                        if (line.StartsWith("Pkg.Revision"))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(line, "(\\d+)");
                            if (m.Success && int.TryParse(m.Groups[1].Value, out int v))
                            {
                                return v;
                            }
                        }
                    }
                }

                return 1;
            }
            catch
            {
                return 0;
            }
        }

        static string SafeRead(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // ---------------------------------------------------------------- tools

        void FillAndroidTools()
        {
            _tools.Add(new ToolEntry
            {
                name = "同步 Android Gradle 模板 + 安装桥源",
                description = "把模块版本清单(依赖/仓库托管段)重新写入 mainTemplate/settingsTemplate，并把 HWAdsBridge.java 安装到 Assets/Plugins/Android。模块升级后必须执行。",
                execute = RunSync,
            });
            _tools.Add(new ToolEntry
            {
                name = "安装 Android Java 桥（HWAdsBridge.java）",
                description = "单独重装 Java 桥到 Assets/Plugins/Android（带哈希校验，幂等）。用于“Java 桥源缺失/不一致”修复。",
                execute = () => LogResult(HwNativeBridgeInstaller.InstallAndroid(out string log), log),
            });
        }

        void FillIosTools()
        {
            _tools.Add(new ToolEntry
            {
                name = "安装 iOS 桥源（HwAdsInterface.h/.m）",
                description = "把两个桥源文件安装到 Assets/Plugins/iOS（哈希校验，幂等）。",
                execute = () => LogResult(HwNativeBridgeInstaller.InstallIos(out string log), log),
            });
            _tools.Add(new ToolEntry
            {
                name = "导入 iOS SDK Release (zip)",
                description = "选择官方 HwAds_iOS_V9.8.75.zip（约113MB），解包 framework 到 Assets/Plugins/iOS/HwAdsNative（二进制不入 git）。iOS 构建前必须执行。",
                execute = HwIosFrameworkImporter.Import,
            });
        }

        void FillDocsTools()
        {
            _tools.Add(new ToolEntry
            {
                name = "打开 GitHub 仓库（浏览器）",
                description = "在默认浏览器打开模块仓库 README（含 Unity 版本兼容矩阵与说明）。",
                execute = () => Application.OpenURL(RepoUrl),
            });
            _tools.Add(new ToolEntry
            {
                name = "打开接入与升级 SOP（浏览器）",
                description = "在默认浏览器打开仓库内 Documentation~/HwSdk接入与升级SOP.md（含安装/升级/回归清单）。",
                execute = () =>
                {
                    string file = "Documentation~/" + Uri.EscapeDataString("HwSdk接入与升级SOP.md");
                    Application.OpenURL(RepoUrl + "/blob/main/" + file);
                },
            });
        }

        void RunSync()
        {
            HwGradleSync.SyncMainTemplate(out string mainLog);
            HwGradleSync.SyncSettingsTemplate(out string settingsLog);
            HwNativeBridgeInstaller.InstallAll(out string bridgeLog);
            _lastLog = mainLog + "\n" + settingsLog + "\n" + bridgeLog;
            Debug.Log("[Jlyt.HwAds] " + _lastLog);
            RefreshAll();
        }

        void LogResult(bool ok, string log)
        {
            _lastLog = (ok ? "OK： " : "失败： ") + log;
            Debug.Log("[Jlyt.HwAds] " + _lastLog);
            RefreshAll();
        }

        void QuickFixAndroid() => LogResult(HwNativeBridgeInstaller.InstallAndroid(out string log), log);
        void QuickFixIos() => LogResult(HwNativeBridgeInstaller.InstallIos(out string log), log);

        void CheckForUpdates()
        {
            _updateStatus = "正在检测（访问 GitHub）…";
            try
            {
                _updateStatus = HwUpdater.CheckAll();
            }
            catch (Exception e)
            {
                _updateStatus = "检测异常：" + e.Message;
            }

            Repaint();
        }

        void UpgradeOneClick()
        {
            try
            {
                _updateStatus = HwUpdater.UpgradeOneClick();
                if (HwUpdater.ModuleUpdateAvailable)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (this != null)
                        {
                            _updateStatus = "升级已发起：等待编辑器重载后自动完成桥源/托管段同步…";
                        }
                    };
                }
            }
            catch (Exception e)
            {
                _updateStatus = "一键更新异常：" + e.Message;
            }

            Repaint();
        }

        void DrawUpdateSection()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("更新", ToolNameStyle);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("检测更新", GUILayout.Height(24), GUILayout.Width(100)))
                    {
                        CheckForUpdates();
                    }

                    if (GUILayout.Button("一键更新", GUILayout.Height(24), GUILayout.Width(100)))
                    {
                        UpgradeOneClick();
                    }
                }

                if (!string.IsNullOrEmpty(_updateStatus))
                {
                    EditorGUILayout.LabelField(_updateStatus, WordWrapStyle);
                }
            }
        }

        // ---------------------------------------------------------------- GUI

        void OnGUI()
        {
            var platform = ResolvePlatform();
            if (!_checkedOnce)
            {
                RefreshAll();
            }

            using (new EditorGUILayout.VerticalScope())
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                _scroll = scroll.scrollPosition;

                DrawHeader(platform);
                GUILayout.Space(6);

                DrawUpdateCard();
                GUILayout.Space(8);

                DrawToolsSection(platform);
                GUILayout.Space(10);

                DrawDetectionSection();
            }
        }

        void DrawHeader(PlatformMode platform)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("JLYTSDK · HW/GameBrain 变现 SDK", TitleStyle, GUILayout.ExpandWidth(true));
                DrawPlatformChip(platform);
            }

            EditorGUILayout.LabelField(
                "模块 com.jlyt.hwsdk · 基线 Android " + HwSdkVersions.UpstreamVersion +
                " / iOS " + HwIosFrameworkImporter.ExpectedIosVersion +
                " · 文档点击即打开浏览器", SubTitleStyle);
        }

        void DrawPlatformChip(PlatformMode platform)
        {
            string text;
            Color c;
            switch (platform)
            {
                case PlatformMode.Android: text = "● Android"; c = new Color(0.45f, 0.8f, 0.4f); break;
                case PlatformMode.Ios: text = "● iOS"; c = new Color(0.6f, 0.75f, 0.95f); break;
                default: text = "○ 未选择"; c = Color.gray; break;
            }

            var prev = GUI.color;
            GUI.color = c;
            EditorGUILayout.LabelField(text, StatusStyle, GUILayout.Width(96));
            GUI.color = prev;
        }

        void DrawUpdateCard()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("更新", ToolNameStyle, GUILayout.Width(48));
                    if (GUILayout.Button("检测更新", GUILayout.Height(24), GUILayout.Width(120)))
                    {
                        CheckForUpdates();
                    }

                    if (GUILayout.Button("一键更新", GUILayout.Height(24), GUILayout.Width(120)))
                    {
                        UpgradeOneClick();
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(
                        HwUpdater.ModuleUpdateAvailable ? "发现新版本" : "跟随标签 hw-9.8.68",
                        HwUpdater.ModuleUpdateAvailable ? WarnStyle : DetailStyle,
                        GUILayout.Width(220));
                }

                if (!string.IsNullOrEmpty(_updateStatus))
                {
                    EditorGUILayout.LabelField(_updateStatus, WordWrapStyle);
                }
            }
        }

        void DrawToolsSection(PlatformMode platform)
        {
            DrawSectionTitle("工具（仅显示当前平台 " + PlatformLabel(platform) + " 的可用项）");
            if (_tools.Count == 0)
            {
                EditorGUILayout.HelpBox("当前平台没有可用工具，请先切换构建平台。", MessageType.Info);
                return;
            }

            for (int i = 0; i < _tools.Count; i += 2)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawToolCard(_tools[i]);
                    if (i + 1 < _tools.Count)
                    {
                        DrawToolCard(_tools[i + 1]);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        static string PlatformLabel(PlatformMode platform)
        {
            switch (platform)
            {
                case PlatformMode.Android: return "Android";
                case PlatformMode.Ios: return "iOS";
                default: return "未选择 (非 Android/iOS)";
            }
        }

        void DrawSectionTitle(string text)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(text, SectionStyle);
            GUILayout.Space(2);
        }

        void DrawToolCard(ToolEntry tool)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandWidth(true)))
            {
                GUILayout.Label(tool.name, ToolNameStyle);
                EditorGUILayout.LabelField(tool.description, WordWrapStyle);
                if (GUILayout.Button("执行", GUILayout.Height(24)))
                {
                    try
                    {
                        tool.execute?.Invoke();
                    }
                    catch (Exception e)
                    {
                        _lastLog = "执行异常：" + e.Message;
                        Debug.LogException(e);
                    }
                }
            }
        }

        void DrawDetectionSection()
        {
            DrawSectionTitle("SDK 所需内容检测（打开窗口自动检测）");

            int missing = 0;
            int warn = 0;
            int ok = 0;
            foreach (var g in _groups)
            {
                missing += g.items.Count(i => i.state == CheckState.Missing);
                warn += g.items.Count(i => i.state == CheckState.Warn);
                ok += g.items.Count(i => i.state == CheckState.Ok);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(BuildSummaryText(missing, warn, ok), StatusStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("重新检测", GUILayout.Width(110), GUILayout.Height(24)))
                {
                    RefreshAll();
                }
            }

            GUILayout.Space(4);
            foreach (var group in _groups.ToArray())
            {
                DrawGroup(group);
            }

            if (!string.IsNullOrEmpty(_lastLog))
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("最近操作：" + _lastLog, MessageType.Info);
            }
        }

        static string BuildSummaryText(int missing, int warn, int ok)
        {
            int total = missing + warn + ok;
            string parts = "";
            if (ok > 0) parts += $" 正常 {ok}";
            if (warn > 0) parts += $"  建议 {warn}";
            if (missing > 0) parts += $"  缺失 {missing}";
            return "检测结果：" + (missing == 0 && warn == 0 ? "全部通过 ✔" : parts.TrimStart());
        }

        void DrawGroup(CheckGroup group)
        {
            bool expanded;
            _groupFold.TryGetValue(group.title, out expanded);
            bool next = EditorGUILayout.Foldout(expanded, group.title, true, GroupFoldStyle);
            if (next != expanded)
            {
                _groupFold[group.title] = next;
                Repaint();
            }

            if (!expanded)
            {
                return;
            }

            foreach (var item in group.items.ToArray())
            {
                DrawCheckRow(item);
            }
        }

        void DrawCheckRow(CheckItem item)
        {
            var prev = GUI.color;
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
            {
                GUI.color = StateColor(item.state);
                GUILayout.Label(StateIcon(item.state), IconStyle, GUILayout.Width(20));
                GUI.color = prev;

                string tip = item.fix ?? item.detail;
                GUILayout.Label(item.name, RowLabelStyle, GUILayout.ExpandWidth(true));

                if (item.quickFix != null &&
                    (item.state == CheckState.Missing || item.state == CheckState.Warn))
                {
                    GUI.color = new Color(0.35f, 0.7f, 0.95f);
                    if (GUILayout.Button(string.IsNullOrEmpty(item.quickFixLabel) ? "修复" : item.quickFixLabel,
                            GUILayout.Width(58), GUILayout.Height(20)))
                    {
                        try
                        {
                            item.quickFix?.Invoke();
                        }
                        catch (Exception e)
                        {
                            _lastLog = "执行异常：" + e.Message;
                            Debug.LogException(e);
                        }
                    }

                    GUI.color = prev;
                }

                EditorGUILayout.LabelField(StateText(item.state), StateTextStyle,
                    GUILayout.Width(110), GUILayout.ExpandWidth(false));
            }

            GUI.color = prev;
            if (item.state == CheckState.Warn || item.state == CheckState.Missing)
            {
                EditorGUILayout.LabelField("    " + (item.state == CheckState.Missing ? "缺失：" : "注意：") +
                    (string.IsNullOrEmpty(item.detail) ? item.fix : item.detail), DetailStyle);
                if (!string.IsNullOrEmpty(item.fix))
                {
                    EditorGUILayout.LabelField("    处理：" + item.fix, FixStyle);
                }
            }
        }

        // ---------------------------------------------------------------- state helpers

        static string StateText(CheckState s)
        {
            switch (s)
            {
                case CheckState.Ok: return "正常";
                case CheckState.Warn: return "建议处理";
                case CheckState.Missing: return "缺失";
                default: return "";
            }
        }

        static string StateIcon(CheckState s)
        {
            switch (s)
            {
                case CheckState.Ok: return "✔";
                case CheckState.Warn: return "⚠";
                case CheckState.Missing: return "✘";
                default: return "ℹ";
            }
        }

        static Color StateColor(CheckState s)
        {
            switch (s)
            {
                case CheckState.Ok: return new Color(0.45f, 0.85f, 0.4f);
                case CheckState.Warn: return Color.yellow;
                case CheckState.Missing: return new Color(1f, 0.45f, 0.4f);
                default: return new Color(0.7f, 0.75f, 0.9f);
            }
        }

        // ---------------------------------------------------------------- misc

        static GUIStyle _stateText, _groupFoldStyle, _warn;

        static GUIStyle StateTextStyle => _stateText ?? (_stateText = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 10,
        });

        static GUIStyle GroupFoldStyle => _groupFoldStyle ?? (_groupFoldStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
        });

        static GUIStyle WarnStyle => _warn ?? (_warn = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.yellow },
        });

static GUIStyle _title,_subtitle,_section,_group,_row,_detail,_fix,_icon,_toolName,_word,_status;
        static GUIStyle TitleStyle => _title ?? (_title = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18, fontStyle = FontStyle.Bold, richText = true,
        });

        static GUIStyle SubTitleStyle => _subtitle ?? (_subtitle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 11 });

        static GUIStyle SectionStyle => _section ?? (_section = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14, margin = new RectOffset(0, 0, 10, 4),
        });

        static GUIStyle GroupStyle => _group ?? (_group = new GUIStyle(EditorStyles.boldLabel)
        {
            margin = new RectOffset(0, 0, 4, 2),
        });

        static GUIStyle RowLabelStyle => _row ?? (_row = new GUIStyle(EditorStyles.label) { richText = true });

        static GUIStyle DetailStyle => _detail ?? (_detail = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true });

        static GUIStyle FixStyle => _fix ?? (_fix = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, fontStyle = FontStyle.Italic });

        static GUIStyle IconStyle => _icon ?? (_icon = new GUIStyle(EditorStyles.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter });

        static GUIStyle ToolNameStyle => _toolName ?? (_toolName = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });

        static GUIStyle WordWrapStyle => _word ?? (_word = new GUIStyle(EditorStyles.label) { wordWrap = true });

        static GUIStyle StatusStyle => _status ?? (_status = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, richText = true });
    }
}