# com.jlyt.hwsdk

- Initial release skeleton. Unified C# API `Jlyt.HwAds`.
- Baseline alignment targets:
  - Android: upstream hwads **9.8.68** (jar + maven dependency manifest, minSdk 24).
  - iOS: upstream HwAds **9.8.75** (framework bundle imported out-of-band).

## Upstream version log (module changelog template)

### 0.9.3 — 2026-09-03
- 菜单迁移：`Window → JLYTSDK` 打开统一管理窗口（替代原 `Tools/Jlyt/HwSDK` 子菜单）。
- 窗口按当前构建平台只显示对应工具（Android/iOS），并展示操作说明；
- 打开窗口自动执行“SDK 所需内容检测”，缺失/建议项以红/黄标记并给出修复提示（含 AndroidManifest 必填项、配置文件、minSdk、托管段、桥源、iOS framework、JDK/NDK 建议）；
- 文档入口点击直达浏览器（GitHub README 与接入升级 SOP）。

### 0.9.2 — 2026-09-03
- Unity 兼容策略明确并写入 README/SOP/Diagnostics：支持 **2022.3 LTS 与 Unity 6000+**；2022.3 以下不支持（package.json `unity: 2022.3`）。
- Diagnostics 输出编辑器版本 + 支持矩阵（`Editor: Unity xxxx (supported / NOT SUPPORTED below Unity 2022.3)`）。
- README 增加兼容矩阵与「升级到 Unity 6000 核对清单」。

### hw-9.8.68 / 0.9.1
- First module baseline. Migrated from in-project integration (IsLand).
- Unified API replaces per-platform `HwAdsBridge` (Android) / `HwAdsBridge`+`HwAdsInterface` (iOS).
- Known upstream deltas vs 9.8.59 folded into the manifest:
  - jar `hwads_9.8.68.jar`, minSdk 24
  - `com.applovin:applovin-sdk:13.6.3`, various adapter bumps, `adjust-android:5.7.0` (+ google-lvl)
  - init SDK call carries the 6-argument form (see Java bridge).

## Migration notes for consumers
- Old global classes (`HwAdsBridge`, `HwInterListener`, `HwRewardListener`) are gone; use `Jlyt.HwAds.HwAdsSdk.Instance`.
- Event tables (TA / Adjust tokens, business events) stay in the game project.
