# com.jlyt.hwsdk

- Initial release skeleton. Unified C# API `Jlyt.HwAds`.
- Baseline alignment targets:
  - Android: upstream hwads **9.8.68** (jar + maven dependency manifest, minSdk 24).
  - iOS: upstream HwAds **9.8.75** (framework bundle imported out-of-band).

## Upstream version log (module changelog template)

### 0.9.7 — 2026-09-03
- 新增 Android 工具“设置 AppLovin Key”：把每工程自己的 Key 写入 launcherTemplate.gradle 的 applovin{apiKey}（显示时脱敏）。
- 检测新增“launcherTemplate · AppLovin Key（每工程）”行：读取/校验/脱敏展示，行内可直接“设置”。

### 0.9.6 — 2026-09-03
- 新增 Android 工具“设置 AdMob / Ad Manager App ID”：把每工程自己的 `ca-app-pub-…~…` 写入 `AndroidManifest.xml` 的 `com.google.android.gms.ads.APPLICATION_ID`。
- 检测新增“AndroidManifest · AdMob APP_ID（每工程）”行：显示当前值并校验格式，行内可直接“设置”。

### 0.9.5 — 2026-09-03
- 修复“始终提示可更新”：本地 UPM 短哈希与 GitHub 完整 SHA 的比较改为前 8 位前缀匹配；file:/Local 开发引用不再误报，检测/一键更新仅对 git 引用生效并给出提示。
- 一键更新健壮性：git 模式且有新版时才执行，先清旧缓存与 lock 再 Resolve，由自动同步在重载后重写文件。
- 窗口布局优化：工具改为两列紧凑卡片；检测分组可折叠；行内“状态文字/修复按钮”；顶部平台徽章与更新卡片；缺失/建议逐项高亮与处理提示。

### 0.9.4 — 2026-09-03
- 自动同步：域加载后比较“上次已同步的模块版本”与当前解析版本，变化时自动重装三份桥源并同步 gradle 托管段（模块即唯一权威，生成文件直接覆盖）。
- 更新检测：窗口新增“检测更新 / 一键更新”。
  - 检测：本地模块提交 vs 远端标签 hw-9.8.68；上游 artwl Android/iOS 官方最新 vs 模块基线。
  - 一键更新：有新版时清除旧包缓存并重新解析，编辑器重载后由自动同步完成桥源/托管段重写。
- 桥源误报修复：比较改为忽略 CRLF/LF 与 BOM；检测按当前平台只看本平台文件（Android 只看 java，iOS 只看 h/m）。
- 窗口：检测行支持内联“修复”按钮；信息展示优化。

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
