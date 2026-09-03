# com.jlyt.hwsdk 接入与升级 SOP

> 基线：Android 9.8.68 / iOS 9.8.75（上游），包版本 0.9.x。
> 前置阅读：《HWSDK模块化分析与落地方案.md》《HWSDK模块设计规范v1.md》（在 IsLand 工程 Docs/ 下）。

## 〇、Unity 版本兼容矩阵

| Unity | 支持 | 说明 |
|---|---|---|
| 2022.3 LTS | ✅ | 本模块开发验证环境（0 error / Diagnostics / Android 导出通过） |
| 6000.0+（Unity 6.x） | ✅ | 按兼容策略设计（跨版本稳定 API、Native 模板安装、版本清单驱动模板），首个 Unity 6 工程按下方清单核对 |
| < 2022.3 | ❌ | 团队决策不处理（package.json `unity: 2022.3`） |

**接入/升级到 Unity 6000 核对清单**（详见模块 README“Unity version compatibility”）：
1. 包解析后运行 `Window/JLYTSDK → Diagnostics`，头部应显示 `Editor: Unity 6xxx (supported)` 且 issues=0；
2. 历史手写模板先跑 `Sync Android Gradle Templates + Install Native Bridges` 重新生成托管段；
3. EDM4U 用支持 Unity 6 的版本（建议 ≥1.2.179）并跑一次 Android Resolver；
4. 勿把旧机器 NDK/JDK/keystore 绝对路径带进 Unity 6 工程模板（诊断会提示）；
5. iOS 重新 `Import iOS SDK Release (zip)` 并核验 Xcode 链接；
6. 首次出包前做 Android 导出工程构建 + 真机回归（见 §三）。

## 一、新工程接入

1. **加依赖**：`Packages/manifest.json`
   ```json
   "com.jlyt.hwsdk": "https://github.com/Herschy0829/com.jlyt.hwsdk.git#hw-9.8.68"
   ```
   本地联调可先写 `"com.jlyt.hwsdk": "file:../com.jlyt.hwsdk"`（相对路径指向包目录），上线前换成 git URL。

2. **放每工程配置件**（本包不含，属游戏/渠道）：
   - `Assets/hw-services.json`（后台下发；gb_id 与 adjust_token 与配置一致）
   - `Assets/google-services.json`
   - `Assets/Editor/raw/applovin_settings.json`（consent flow 隐私链接）
   - `Assets/Editor/xml/network_security_config.xml`
   - launcherTemplate.gradle 里 AppLovin API key（工程侧，勿入库到包）

3. **初始化**（启动早期、进入游戏前调一次；幂等）：
   ```csharp
   using Jlyt.HwAds;

   HwAdsSdk.Instance.Init(new HwAdsSdkConfig
   {
       gameBrainIdAndroid = "392",   // 与 hw-services.json 的 gb_id 一致（Android）
       gameBrainIdIos     = 393,     // iOS 端 serverURL
       appToken           = "tcdmx6lgk45c", // 通常 = adjust_token
       channel            = "Google Play",
   });
   ```
   推荐顺序（对齐 IsLand 现状）：先 `LinkThinkingAnalyticsId(TA.DistinctId)` → `Init(...)` → `SetAdsRemoved(...)`。

4. **广告调用**（示例）：
   ```csharp
   if (HwAdsSdk.Instance.IsRewardReady)
   {
       HwAdsSdk.Instance.TrackRewardButtonClick(slot);
       HwAdsSdk.Instance.ShowReward(slot, ok => { /* ok=true 看完给奖 */ });
   }
   // 插屏
   if (HwAdsSdk.Instance.IsInterstitialReady) HwAdsSdk.Instance.ShowInterstitial();
   ```

5. **菜单动作**：`Window/JLYTSDK/`
   - `Sync Android Gradle Templates + Install Native Bridges`：把版本清单写入 mainTemplate/settingsTemplate 的托管段，并把 Java/ObjC 桥源文件安装到 `Assets/Plugins/Android|iOS`（哈希校验，幂等）；
   - `Diagnostics / Validate Project`：检查 jar 重复、托管段、minSdk（≥24）、配置件、桥源文件是否就位；
   - `Import iOS SDK Release (zip)…`：把官方 iOS zip 导入 `Assets/Plugins/iOS/HwAdsNative/V9.8.75`（仅 framework 二进制，仓库不存）。
   iOS 框架二进制统一存放点待定（共享盘/对象存储 + sha256），届时把 URL 写进 importer。

## 二、升级上游 SDK

上游发布新版（README/Releases 会写"更新第三方库"等）时，**只改模块仓库**：

1. 下载官方 Android demo zip 与 iOS zip，对照 demo 的模块级 build.gradle 更新：
   - `Editor/HwSdkVersions.cs`：`UpstreamVersion`、`JarFileName`、`MinSdkVersion`、`MainDependencyLines`、`RepositoryLines`；
   - 替换 `Plugins/Android/hwads_<ver>.jar`；
   - 若桥接口变化（如 init 参数），同步 `Runtime/Platform/*` 与 `Plugins/Android/HWAdsBridge.java`、`Plugins/iOS/HwAdsInterface.m`；
2. `CHANGELOG.md` 记一行（依赖 diff + 迁移检查点）。
3. 打 tag：包版本 `0.10.x` + 上游 tag `hw-9.8.XX`。
4. 各工程：manifest 指向新 tag（或引用默认分支后 UPM Update）→ `Window/JLYTSDK → Sync Android Gradle Templates` → `Diagnostics` 通过 → 按下方回归清单冒烟。

## 三、回归清单

- 激励：加载/展示/看完给奖/中途关闭不给奖/失败分支
- 插屏：加载/展示/关闭
- 免广告：SetAdsRemoved 传值
- 内购二次验证打点（HwAdsPurchase 字段在双端一致）
- Adjust 事件（含 fireOnce 与带参 iOS）
- Firebase 事件、TA distinct_id 关联
- 首启隐私（CMP/ICM）、断网与广告失败分支
- Android 导出工程构建（含 hw-services/applovin_settings/network_security_config 是否已拷贝）
- iOS：HwAdsFramework + DependenceSDK 链接、启动无崩溃

## 四、已知注意点

- 不要在同一工程同时放旧 `hwads_*.jar` 与本包 jar（类重复）。
- 模板里 `// ==== com.jlyt.hwsdk managed begin/end ====` 之间是托管内容，勿手改。
- iOS 上工程若用 EDM4U 生成 Firebase/Adjust pods，与本包 framework 可能重复；建议以官方包内 framework 为准，工程不再重复引。
