using UnityEditor;

namespace Jlyt.HwAds.Editor
{
    public static class HwSdkMenu
    {
        const string Root = "Tools/Jlyt/HwSDK/";

        [MenuItem(Root + "Sync Android Gradle Templates")]
        public static void SyncGradleTemplates()
        {
            HwGradleSync.SyncMainTemplate(out string mainLog);
            HwGradleSync.SyncSettingsTemplate(out string settingsLog);
            UnityEngine.Debug.Log($"[Jlyt.HwAds] {mainLog}\n[Jlyt.HwAds] {settingsLog}");
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
