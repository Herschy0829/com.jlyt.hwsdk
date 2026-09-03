using System;

namespace Jlyt.HwAds.Platform
{
    /// <summary>
    /// Internal platform contract. The facade (HwAdsSdk) owns the unified semantics;
    /// implementations below only adapt to the native bridge of each platform.
    /// </summary>
    internal interface IHwAdsPlatform
    {
        void Init(HwAdsSdkConfig cfg);
        void SetUserId(string userId);

        bool IsRewardReady { get; }
        void ShowReward(string placement, Action<bool> completed);

        bool IsInterstitialReady { get; }
        void ShowInterstitial(Action<bool> closed);

        void TrackRewardButtonClick(string slot);
        void SetAdsRemoved(bool removed);
        void ReportPurchase(HwAdsPurchase purchase);

        void TrackAdjust(string token, string category, string action, string label);
        void TrackAdjustWithParams(string token, string timestamp, string session, string version);
        void TrackFirebase(string eventName, string key, string value);
        void LinkThinkingAnalyticsId(string distinctId);
    }

    internal static class HwAdsPlatformFactory
    {
        public static IHwAdsPlatform Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new HwAdsAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
            return new HwAdsIos();
#else
            return new HwAdsEditorStub();
#endif
        }
    }
}
