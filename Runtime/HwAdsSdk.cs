using System;
using Jlyt.HwAds.Internal;
using Jlyt.HwAds.Platform;
using UnityEngine;

namespace Jlyt.HwAds
{
    /// <summary>
    /// Unified facade for the HW / GameBrain monetization SDK.
    /// Replaces the former per-platform global classes (HwAdsBridge / HwAdsInterface /
    /// HwInterListener / HwRewardListener) with one cross-platform API.
    ///
    /// Threading: all methods are intended to be called from the Unity main thread;
    /// native callbacks are re-dispatched onto the main thread before invoking user callbacks.
    ///
    /// Usage:
    ///   HwAdsSdk.Instance.Init(new HwAdsSdkConfig { gameBrainIdAndroid = "392", gameBrainIdIos = 393, appToken = "..." });
    ///   if (HwAdsSdk.Instance.IsRewardReady) HwAdsSdk.Instance.ShowReward(slot, ok => { ... });
    /// </summary>
    public sealed class HwAdsSdk
    {
        static readonly Lazy<HwAdsSdk> s_Instance = new Lazy<HwAdsSdk>(() => new HwAdsSdk());

        public static HwAdsSdk Instance => s_Instance.Value;

        readonly IHwAdsPlatform _platform;
        HwAdsSdkConfig _cfg;
        bool _initialized;

        public bool IsInitialized => _initialized;

        HwAdsSdk()
        {
            _platform = HwAdsPlatformFactory.Create();
        }

        // ---------------------------------------------------------------- init

        /// <summary>Idempotent. Must be called once at startup before any other API.</summary>
        public void Init(HwAdsSdkConfig config)
        {
            if (_initialized)
            {
                Debug.Log("[Jlyt.HwAds] Init already called, ignored.");
                return;
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Validate();
            _cfg = config;
            HwAdsMainThread.Capture();
            _platform.Init(_cfg);
            _initialized = true;
            Debug.Log($"[Jlyt.HwAds] initialized (android gb={_cfg.gameBrainIdAndroid}, ios gb={_cfg.gameBrainIdIos}, channel={_cfg.channel})");
        }

        public void SetUserId(string userId)
        {
            if (Ready()) _platform.SetUserId(userId);
        }

        // -------------------------------------------------------------- reward

        public bool IsRewardReady
        {
            get
            {
                if (!_initialized)
                {
                    return false;
                }

                return _platform.IsRewardReady;
            }
        }

        public void ShowReward(string placement, Action<bool> completed)
        {
            if (string.IsNullOrEmpty(placement) || !Ready())
            {
                completed?.Invoke(false);
                return;
            }

            _platform.ShowReward(placement, completed);
        }

        /// <summary>Variant exposing a structured result; distinct name keeps lambda calls on the bool overload unambiguous.</summary>
        public void ShowRewardWithResult(string placement, Action<HwAdsShowResult> completed)
        {
            if (completed == null)
            {
                ShowReward(placement, (Action<bool>)null);
                return;
            }

            if (string.IsNullOrEmpty(placement) || !_initialized)
            {
                completed(HwAdsShowResult.NotReady(placement ?? string.Empty));
                return;
            }

            ShowReward(placement, ok =>
            {
                completed(ok
                    ? HwAdsShowResult.Completed(placement)
                    : HwAdsShowResult.ClosedNoReward(placement));
            });
        }

        // ---------------------------------------------------------- interstitial

        public bool IsInterstitialReady
        {
            get
            {
                if (!_initialized)
                {
                    return false;
                }

                return _platform.IsInterstitialReady;
            }
        }

        public void ShowInterstitial(Action<bool> closed = null)
        {
            if (!Ready())
            {
                closed?.Invoke(false);
                return;
            }

            _platform.ShowInterstitial(closed);
        }

        // ------------------------------------------------- tracking / reporting

        /// <summary>Record that a rewarded button was clicked (called whenever the button is tapped).</summary>
        public void TrackRewardButtonClick(string slot)
        {
            if (Ready()) _platform.TrackRewardButtonClick(slot);
        }

        /// <summary>Sets whether the user purchased ads-removal (only meaningful for rewarded; value = every launch).</summary>
        public void SetAdsRemoved(bool removed)
        {
            if (Ready()) _platform.SetAdsRemoved(removed);
        }

        /// <summary>Second-verification purchase report (former PurchaseEvent, unified arguments).</summary>
        public void ReportPurchase(HwAdsPurchase purchase)
        {
            if (purchase == null || !Ready())
            {
                return;
            }

            _platform.ReportPurchase(purchase);
        }

        /// <summary>
        /// Adjust custom event (former AdJustEvent). fireOnce makes the module record the token
        /// locally and report it only the first time.
        /// </summary>
        public void TrackAdjust(string token, string category, string action, string label, bool fireOnce = false)
        {
            if (!Ready())
            {
                return;
            }

            if (fireOnce && !HwAdsLocalOnce.TryConsume(token))
            {
                Debug.Log($"[Jlyt.HwAds] adjust token is one-shot and was already reported: {token}");
                return;
            }

            _platform.TrackAdjust(token, category, action, label);
        }

        /// <summary>Adjust event with callback parameters (iOS capability; Android falls back to a plain event).</summary>
        public void TrackAdjustWithParams(string token, string timestamp, string session, string version)
        {
            if (Ready()) _platform.TrackAdjustWithParams(token, timestamp, session, version);
        }

        public void TrackFirebase(string eventName, string key, string value)
        {
            if (Ready()) _platform.TrackFirebase(eventName, key, value);
        }

        /// <summary>Link ThinkingAnalytics distinct id into the adjust/hw funnel (former AddTAId).</summary>
        public void LinkThinkingAnalyticsId(string distinctId)
        {
            if (Ready()) _platform.LinkThinkingAnalyticsId(distinctId);
        }

        // --------------------------------------------------------------- helper

        bool Ready()
        {
            if (_initialized)
            {
                return true;
            }

            Debug.LogError("[Jlyt.HwAds] API called before Init. Call HwAdsSdk.Instance.Init(config) first.");
            return false;
        }
    }
}
