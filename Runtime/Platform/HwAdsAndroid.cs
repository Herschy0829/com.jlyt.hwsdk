#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Jlyt.HwAds.Internal;
using UnityEngine;

namespace Jlyt.HwAds.Platform
{
    /// <summary>
    /// Android implementation. Talks to com.unity3d.player.HWAdsBridge (Java) which forwards
    /// to com.hw.hwadssdk.HwAdsInterface inside hwads_<version>.jar.
    /// Listener proxies are kept alive for the whole session (GC safety).
    /// </summary>
    internal sealed class HwAdsAndroid : IHwAdsPlatform
    {
        const string BridgeClass = "com.unity3d.player.HWAdsBridge";

        AndroidJavaObject _bridge;
        readonly List<AndroidJavaProxy> _aliveProxies = new List<AndroidJavaProxy>();
        AndroidJavaObject _context;

        AndroidJavaObject Bridge => _bridge ?? (_bridge =
            new AndroidJavaClass(BridgeClass).CallStatic<AndroidJavaObject>("getInstance"));

        AndroidJavaObject Context => _context ?? (_context =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"));

        public void Init(HwAdsSdkConfig cfg)
        {
            Bridge.Call("initSdk", Context, cfg.gameBrainIdAndroid, cfg.appToken);
        }

        public void SetUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            Bridge.Call("setUserId", userId);
        }

        // ---------- rewarded -------------------------------------------------

        public bool IsRewardReady => Bridge.Call<bool>("isRewardLoaded");

        public void ShowReward(string placement, Action<bool> completed)
        {
            var listener = new HwAndroidRewardProxy(completed);
            _aliveProxies.Add(listener);
            Bridge.Call("setRewardListener", listener);
            Bridge.Call("showReward", placement);
        }

        // ---------- interstitial ---------------------------------------------

        public bool IsInterstitialReady => Bridge.Call<bool>("isInterLoaded");

        public void ShowInterstitial(Action<bool> closed)
        {
            var listener = new HwAndroidInterstitialProxy(closed);
            _aliveProxies.Add(listener);
            Bridge.Call("setInterListener", listener);
            Bridge.Call("showInter");
        }

        // ---------- tracking / reporting --------------------------------------

        public void TrackRewardButtonClick(string slot) => Bridge.Call("TrackRewardButtonClick", slot);
        public void SetAdsRemoved(bool removed) => Bridge.Call("SetRemoveAdsStatus", removed);

        public void ReportPurchase(HwAdsPurchase p)
        {
            Bridge.Call("purchaseEvent", p.number ?? "", p.currency ?? "", p.purchaseToken ?? "",
                p.productId ?? "", p.purchaseType, p.orderId ?? "", p.adjustToken ?? "");
        }

        public void TrackAdjust(string token, string category, string action, string label)
        {
            Bridge.Call("adJustEvent", token ?? "", category ?? "", action ?? "", label ?? "");
        }

        public void TrackAdjustWithParams(string token, string timestamp, string session, string version)
        {
            // Android bridge has no 4-extra-param variant; fall back to the plain event.
            Debug.LogWarning($"[Jlyt.HwAds] TrackAdjustWithParams is iOS-only; fallback TrackAdjust({token})");
            TrackAdjust(token, string.Empty, string.Empty, string.Empty);
        }

        public void TrackFirebase(string eventName, string key, string value)
        {
            Bridge.Call("firebaseEvent", eventName ?? "", key ?? "", value ?? "");
        }

        public void LinkThinkingAnalyticsId(string distinctId)
        {
            Bridge.Call("hwAdjustAddTA", distinctId ?? "", "");
        }

        // ---------- native listeners -------------------------------------------

        const string RewardJavaInterface = "com.hw.hwadssdk.HwAdsRewardVideoListener";
        const string InterJavaInterface = "com.hw.hwadssdk.HwAdsInterstitialListener";

        sealed class HwAndroidRewardProxy : AndroidJavaProxy
        {
            readonly Action<bool> _completed;
            bool _hasReward;

            public HwAndroidRewardProxy(Action<bool> completed) : base(RewardJavaInterface)
            {
                _completed = completed;
            }

            void onRewardedVideoLoadSuccess() { }
            void onRewardedVideoLoadFailure() { }
            void onRewardedVideoStarted() { }

            void onRewardedVideoPlaybackError()
            {
                _hasReward = false;
            }

            void onRewardedVideoClicked() { }

            void onRewardedVideoClosed()
            {
                bool ok = _hasReward;
                HwAdsMainThread.Post(() => _completed?.Invoke(ok));
            }

            void onRewardedVideoCompleted()
            {
                _hasReward = true;
            }
        }

        sealed class HwAndroidInterstitialProxy : AndroidJavaProxy
        {
            readonly Action<bool> _closed;

            public HwAndroidInterstitialProxy(Action<bool> closed) : base(InterJavaInterface)
            {
                _closed = closed;
            }

            void onInterstitialLoaded() { }
            void onInterstitialFailed() { }
            void onInterstitialShown() { }
            void onInterstitialClicked() { }

            void onInterstitialDismissed(bool var1)
            {
                HwAdsMainThread.Post(() => _closed?.Invoke(true));
            }
        }
    }
}
#endif
