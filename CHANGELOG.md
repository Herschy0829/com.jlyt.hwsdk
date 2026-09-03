# com.jlyt.hwsdk

- Initial release skeleton. Unified C# API `Jlyt.HwAds`.
- Baseline alignment targets:
  - Android: upstream hwads **9.8.68** (jar + maven dependency manifest, minSdk 24).
  - iOS: upstream HwAds **9.8.75** (framework bundle imported out-of-band).

## Upstream version log (module changelog template)

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
