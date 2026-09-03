using System;
using UnityEngine;

namespace Jlyt.HwAds.Platform
{
    /// <summary>Editor / unsupported platform: no native SDK, keep behavior quiet and safe (same as old #if UNITY_EDITOR branches).</summary>
    internal sealed class HwAdsEditorStub : IHwAdsPlatform
    {
        void Log(string s) => Debug.Log("[Jlyt.HwAds][Editor] " + s);

        public void Init(HwAdsSdkConfig cfg) => Log($"Init(cfg) editor stub; gbA={cfg.gameBrainIdAndroid} gbI={cfg.gameBrainIdIos}");
        public void SetUserId(string userId) => Log($"SetUserId({userId})");
        public bool IsRewardReady => false;
        public void ShowReward(string placement, Action<bool> completed)
        {
            Log($"ShowReward({placement}) editor stub");
            completed?.Invoke(true);
        }
        public bool IsInterstitialReady => false;
        public void ShowInterstitial(Action<bool> closed)
        {
            Log("ShowInterstitial editor stub");
            closed?.Invoke(true);
        }
        public void TrackRewardButtonClick(string slot) => Log($"TrackRewardButtonClick({slot})");
        public void SetAdsRemoved(bool removed) => Log($"SetAdsRemoved({removed})");
        public void ReportPurchase(HwAdsPurchase p) => Log($"ReportPurchase({p.productId})");
        public void TrackAdjust(string token, string category, string action, string label) => Log($"TrackAdjust({token},{category})");
        public void TrackAdjustWithParams(string token, string timestamp, string session, string version) => Log($"TrackAdjustWithParams({token})");
        public void TrackFirebase(string eventName, string key, string value) => Log($"TrackFirebase({eventName})");
        public void LinkThinkingAnalyticsId(string distinctId) => Log($"LinkThinkingAnalyticsId({distinctId})");
    }
}
