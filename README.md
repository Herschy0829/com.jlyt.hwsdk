# com.jlyt.hwsdk

Shared Unity package for the HW / GameBrain monetization SDK (AppLovin MAX based).

- Android upstream: https://github.com/artwl/hw_maxsdk_android — baseline **9.8.68**
- iOS upstream: https://github.com/artwl/hw_maxsdk_ios — baseline **9.8.75**
- Official integration docs (Feishu, intranet):
  - Android: https://hellowd.feishu.cn/docs/doccnJOWCBfsHiAGPmkFeNg3D2f
  - iOS: https://hellowd.feishu.cn/docs/doccnM0dCN19JMcNyOLTruK80Ud

## Layout

- `Runtime/` — unified cross-platform C# API (`Jlyt.HwAds`), self-contained (no dependency on project singletons/helpers).
- `Plugins/Android/` — `hwads_<version>.jar` (versioned here, included by Unity natively).
- `Native/` — authoritative native bridge sources installed into the host project by the Editor tool:
  `Android/HWAdsBridge.java` -> `Assets/Plugins/Android/`, `iOS/HwAdsInterface.{h,m}` -> `Assets/Plugins/iOS/`.
- `Editor/` — per-version dependency manifest, gradle template writer/diagnostics, native bridge installer,
  Android export patcher, iOS framework importer, one-click project setup menus.

## Unity version compatibility

| Unity | Support | 状态 |
|---|---|---|
| **2022.3 LTS** | ✅ 支持 | 已在本模块开发/验证环境跑通（编译 0 error、Diagnostics 通过、Android 导出工程构建成功） |
| **Unity 6000.0+ (Unity 6.x)** | ✅ 目标支持 | 按兼容策略设计（见下），首个 Unity 6 工程接入时按“升级注意”逐项核对即可 |
| < 2022.3 | ❌ 不支持 | 团队决策：不处理 2022 以下版本（package.json `"unity": "2022.3"` 亦会提示最低版本） |

### 兼容策略（为什么 6000 可直接用）
- 代码只使用跨版本稳定 API：`AndroidJavaProxy/AndroidJavaObject`、`[DllImport("__Internal")]` + `UnitySendMessage`、`IPostprocessBuildWithReport`、`PackageInfo.FindForAssembly`、`EditorUserBuildSettings.androidBuildSystem`。
- 原生件布局避开版本差异：.jar 走包内 `Plugins/Android`（Unity 官方支持路径）；.java/.m/.h 由 `Native/` 模板经 Editor 工具安装进宿主（不受包内插件规则差异影响）。
- 构建配置由版本清单+Editor 生成器产出，模板内容（compileSdk/java 17/minSdk 24/multidex/desugaring）与 Unity 6000 默认模板一致；Unity 主模板路径（`Assets/Plugins/Android/*Template.gradle`）在 2022.3 与 6000 相同。
- iOS 二进制由“导入工具+校验”分发（不随包入库），framework 放 `Plugins/iOS` 由 Unity 链接，2022.3/6000 行为一致。

### 升级到 Unity 6000 时的核对清单
1. 包解析后跑 `Tools/Jlyt/HwSDK → Diagnostics`：确认头部显示 `Editor: Unity 6xxx (supported)` 且 issues=0；
2. 如工程模板是历史手写内容，跑一次 `Sync Android Gradle Templates + Install Native Bridges`（重新生成托管段/仓库段并装桥源）；
3. 保持 EDM4U(External Dependency Manager) 为支持 Unity 6 的版本（如 1.2.179+），Android Resolver 跑一次；
4. 若从 2022.3 升级项目：Unity 6 会按自身 SDK 版本重写 `**TOKENS**` 默认值，勿把旧机器路径（NDK/JDK/keystore）带进模板——应依赖本机 EditorPrefs（诊断会检查）；
5. iOS：重新执行 `Import iOS SDK Release (zip)` 导入对应版本 framework，确认 Xcode 链接正常；
6. 首次在 Unity 6 出包前先做一次“Android 导出工程构建 + 真机回归”（清单见 SOP）。

## Per-project configuration (never commit into this package)

- `Assets/hw-services.json`, `Assets/google-services.json`
- `Assets/Editor/raw/applovin_settings.json`, `Assets/Editor/xml/network_security_config.xml`
- AppLovin API key (in launcher template, project side)
- `HwAdsSdkConfig` values: `gameBrainIdAndroid` (e.g. "392"), `gameBrainIdIos` (e.g. 393), `appToken`, `channel`

## Quick start (consumer project)

1. Add dependency in `Packages/manifest.json`:
   `"com.jlyt.hwsdk": "https://github.com/Herschy0829/com.jlyt.hwsdk.git#hw-9.8.68"`
   > Note: the repo is **private** — every machine/CI that resolves this dependency needs GitHub
   > credentials (e.g. `git config --global credential.helper store` + a PAT, or GitHub credential
   > manager). Package Manager caches the resolved copy under `Library/PackageCache`, so after the
   > first successful resolve a machine can usually reopen offline.
2. Call `HwAdsSdk.Instance.Init(config)` once at startup (see `Runtime/HwAdsSdkConfig.cs`).
3. See `Documentation~/HwSdk接入与升级SOP.md` for full setup/upgrade checklist.
