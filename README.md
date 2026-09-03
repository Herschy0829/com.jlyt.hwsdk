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

## Per-project configuration (never commit into this package)

- `Assets/hw-services.json`, `Assets/google-services.json`
- `Assets/Editor/raw/applovin_settings.json`, `Assets/Editor/xml/network_security_config.xml`
- AppLovin API key (in launcher template, project side)
- `HwAdsSdkConfig` values: `gameBrainIdAndroid` (e.g. "392"), `gameBrainIdIos` (e.g. 393), `appToken`, `channel`

## Quick start (consumer project)

1. Add dependency in `Packages/manifest.json`:
   `"com.jlyt.hwsdk": "https://github.com/Herschy0829/com.jlyt.hwsdk.git#hw-9.8.68"`
2. Call `HwAdsSdk.Instance.Init(config)` once at startup (see `Runtime/HwAdsSdkConfig.cs`).
3. See `Documentation~/HwSdk接入与升级SOP.md` for full setup/upgrade checklist.
