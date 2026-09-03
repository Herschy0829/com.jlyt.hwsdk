using UnityEditor;

namespace Jlyt.HwAds.Editor
{
    public static class HwSdkMenu
    {
        const string Root = "Tools/Jlyt/HwSDK/";

        [MenuItem(Root + "Sync Android Gradle Templates + Install Native Bridges")]
        public static void SyncGradleTemplates()
        {
            HwGradleSync.SyncMainTemplate(out string mainLog);
            HwGradleSync.SyncSettingsTemplate(out string settingsLog);
            HwNativeBridgeInstaller.InstallAll(out string bridgeLog);
            UnityEngine.Debug.Log($"[Jlyt.HwAds] {mainLog}\n[Jlyt.HwAds] {settingsLog}\n[Jlyt.HwAds] {bridgeLog}");
        }

        [MenuItem(Root + "Install Android Java Bridge (copy into project)")]
        public static void InstallJavaBridge()
        {
            HwNativeBridgeInstaller.InstallAndroid(out string log);
            UnityEngine.Debug.Log("[Jlyt.HwAds] " + log);
        }

        [MenuItem(Root + "Install iOS Bridge Sources (copy into project)")]
        public static void InstallIosBridge()
        {
            HwNativeBridgeInstaller.InstallIos(out string log);
            UnityEngine.Debug.Log("[Jlyt.HwAds] " + log);
        }

        [MenuItem(Root + "Diagnostics / Validate Project")]
        public static void Validate() => HwSdkDiagnostics.Run();

        [MenuItem(Root + "Import iOS SDK Release (zip)…")]
        public static void ImportIosZip() => HwIosFrameworkImporter.Import();

        [MenuItem(Root + "Show Documentation (SOP)")]
        public static void OpenDocs()
        {
            // Documentation~ folder is stripped from packages; fall back to README guidance in Console.
            UnityEngine.Debug.Log(
                "[Jlyt.HwAds] See https://github.com/Herschy0829/com.jlyt.hwsdk " +
                "and the repo's Documentation~/HwSdk接入与升级SOP.md for setup/upgrade steps.");
        }
    }
}
