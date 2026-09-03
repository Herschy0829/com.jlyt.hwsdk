# com.jlyt.hwsdk

- Initial release skeleton. Unified C# API `Jlyt.HwAds`.
- Baseline alignment targets:
  - Android: upstream hwads **9.8.68** (jar + maven dependency manifest, minSdk 24).
  - iOS: upstream HwAds **9.8.75** (framework bundle imported out-of-band).

## Upstream version log (module changelog template)

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
