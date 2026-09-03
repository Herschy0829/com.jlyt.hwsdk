using System.Collections.Generic;

namespace Jlyt.HwAds.Editor
{
    /// <summary>
    /// Single source of truth for the supported upstream version:
    /// jar name, minSdk, gradle dependency block, maven repositories.
    /// Bump these when upgrading the SDK (keep data aligned with the official release's build.gradle).
    /// </summary>
    public static class HwSdkVersions
    {
        public const string PackageName = "com.jlyt.hwsdk";

        // ---- upstream -----------------------------------------------------
        public const string UpstreamVersion = "9.8.68";
        public const string JarFileName = "hwads_9.8.68.jar";
        public const int MinSdkVersion = 24;

        // ---- gradle markers (content between markers is module-managed) ------
        public const string BeginMarker = "// ==== com.jlyt.hwsdk managed begin (upstream " + UpstreamVersion + ") ====";
        public const string EndMarker = "// ==== com.jlyt.hwsdk managed end ====";
        public const string LegacyBegin = "//hwads need add";
        public const string LegacyEnd = "//hwads need add end";

        /// <summary>Lines injected inside the dependencies {} block of mainTemplate.gradle.</summary>
        public static readonly string[] MainDependencyLines =
        {
            "coreLibraryDesugaring \"com.android.tools:desugar_jdk_libs:2.0.4\"",
            "implementation 'androidx.appcompat:appcompat:1.4.1'",
            "implementation 'androidx.constraintlayout:constraintlayout:2.1.3'",
            "implementation 'androidx.annotation:annotation:1.6.0'",
            "implementation 'com.applovin:applovin-sdk:13.6.3'",
            "implementation 'com.applovin.mediation:bidmachine-adapter:3.6.1.0'",
            "implementation 'com.applovin.mediation:chartboost-adapter:9.12.0.0'",
            "implementation 'com.google.android.gms:play-services-base:16.1.0'",
            "implementation 'com.applovin.mediation:fyber-adapter:8.4.5.0'",
            "implementation 'com.applovin.mediation:google-adapter:25.3.0.0'",
            "implementation 'com.applovin.mediation:google-ad-manager-adapter:25.3.0.0'",
            "implementation 'com.applovin.mediation:facebook-adapter:6.21.0.0'",
            "implementation 'com.squareup.picasso:picasso:2.8'",
            "implementation 'androidx.recyclerview:recyclerview:1.1.0'",
            "implementation 'com.applovin.mediation:ironsource-adapter:9.4.3.0.0'",
            "implementation 'com.applovin.mediation:moloco-adapter:4.9.0.0'",
            "implementation 'com.applovin.mediation:mintegral-adapter:17.1.61.0'",
            "implementation 'com.applovin.mediation:bytedance-adapter:8.1.0.3.0'",
            "implementation 'com.applovin.mediation:unityads-adapter:4.18.1.0'",
            "implementation 'com.applovin.mediation:vungle-adapter:7.7.1.0'",
            "implementation 'com.applovin.mediation:inmobi-adapter:11.2.0.0'",
            "implementation 'com.applovin.mediation:bigoads-adapter:5.8.2.0'",
            "implementation 'com.hyprmx.android:HyprMX-Max:6.4.6.0'",
            "implementation 'com.hyprmx.android:HyprMX-SDK:6.4.6'",
            "implementation 'com.adsurge.sdk:adapter-for-max:1.6.0.0'",
            "implementation 'com.adjust.sdk:adjust-android:5.7.0'",
            "implementation 'com.adjust.sdk:adjust-android-google-lvl:5.7.0'",
            "implementation 'com.google.android.gms:play-services-analytics:18.0.2'",
            "implementation 'com.android.installreferrer:installreferrer:2.2'",
            "implementation 'com.android.support:multidex:1.0.3'",
            "implementation platform('com.google.firebase:firebase-bom:34.9.0')",
            "implementation 'com.google.firebase:firebase-crashlytics'",
        };

        /// <summary>Lines injected inside every maven repositories {} block that carries the legacy hwads markers.</summary>
        public static readonly string[] RepositoryLines =
        {
            "maven { url 'https://artifactory.bidmachine.io/bidmachine' }",
            "maven { url 'https://cboost.jfrog.io/artifactory/chartboost-ads/' }",
            "maven { url 'https://android-sdk.is.com' }",
            "maven { url 'https://artifact.bytedance.com/repository/pangle' }",
            "maven { url 'https://s3.amazonaws.com/smaato-sdk-releases/' }",
            "maven { url 'https://dl-maven-android.mintegral.com/repository/mbridge_android_sdk_oversea' }",
        };

        public static IEnumerable<string> MainDependenciesBlock()
        {
            yield return BeginMarker;
            foreach (var line in MainDependencyLines)
            {
                yield return line;
            }
            yield return EndMarker;
        }
    }
}
