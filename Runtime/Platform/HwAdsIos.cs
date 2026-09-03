#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Jlyt.HwAds.Platform
{
    /// <summary>
    /// iOS implementation. [DllImport("__Internal")] forwards to HwAdsInterface.m which drives
    /// HwAdsFramework (HwAds singleton). Native close callbacks arrive through UnitySendMessage
    /// on the "HwAdsBridge" GameObject handled by <see cref="HwAdsIosHost"/>.
    /// </summary>
    internal sealed class HwAdsIos : IHwAdsPlatform
    {
        // ---------- native entry points (must match HwAdsInterface.m) ----------

        [DllImport("__Internal")] static extern void initHwSDK(int serverURL);
        [DllImport("__Internal")] static extern void showHwInterAd();
        [DllImport("__Internal")] static extern bool isHwInterAdLoaded();
        [DllImport("__Internal")] static extern void showHwRewardAd(string rewardTag);
        [DllImport("__Internal")] static extern bool isHwRewardAdLoaded();
        [DllImport("__Internal")] static extern void hwAnalyticsPurchase(string dollars, string currency, string productId, string productName, int purchaseType, string orderId, string purchaseToken);
        [DllImport("__Internal")] static extern void adJustEvent(string token);
        [DllImport("__Internal")] static extern void adJustEventWithParam(string token, string stamp, string session, string version);
        [DllImport("__Internal")] static extern void firebaseEvent(string eventName, string eventKey, string eventValue);
        [DllImport("__Internal")] static extern void addTaId(string id);
        [DllImport("__Internal")] static extern void setRemoveAdsStatus(bool value);
        [DllImport("__Internal")] static extern void trackRewardButtonClick(string value);

        HwAdsSdkConfig _cfg;

        public void Init(HwAdsSdkConfig cfg)
        {
            _cfg = cfg;
            HwAdsIosHost.EnsureCreated();
            initHwSDK(cfg.gameBrainIdIos);
        }

        public void SetUserId(string userId)
        {
            // Not wired through the native bridge on iOS; kept as a semantic no-op.
        }

        // ---------- rewarded -------------------------------------------------

        public bool IsRewardReady => isHwRewardAdLoaded();

        public void ShowReward(string placement, Action<bool> completed)
        {
            HwAdsIosHost.EnsureCreated();
            HwAdsIosHost.SetRewardCallback(completed);
            showHwRewardAd(placement);
        }

        // ---------- interstitial ---------------------------------------------

        public bool IsInterstitialReady => isHwInterAdLoaded();

        public void ShowInterstitial(Action<bool> closed)
        {
            HwAdsIosHost.EnsureCreated();
            HwAdsIosHost.SetInterCallback(closed);
            showHwInterAd();
        }

        // ---------- tracking / reporting --------------------------------------

        public void TrackRewardButtonClick(string slot) => trackRewardButtonClick(slot);
        public void SetAdsRemoved(bool removed) => setRemoveAdsStatus(removed);

        public void ReportPurchase(HwAdsPurchase p)
        {
            hwAnalyticsPurchase(p.number ?? "", p.currency ?? "", p.productId ?? "",
                p.productName ?? "", p.purchaseType, p.orderId ?? "", p.purchaseToken ?? "");
        }

        public void TrackAdjust(string token, string category, string action, string label)
        {
            adJustEvent(token);
        }

        public void TrackAdjustWithParams(string token, string timestamp, string session, string version)
        {
            adJustEventWithParam(token, timestamp, session, version);
        }

        public void TrackFirebase(string eventName, string key, string value)
        {
            firebaseEvent(eventName, key, value);
        }

        public void LinkThinkingAnalyticsId(string distinctId)
        {
            addTaId(distinctId);
        }
    }
}
#endif
