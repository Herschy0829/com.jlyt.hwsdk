#if UNITY_IOS && !UNITY_EDITOR
using System;
using UnityEngine;

namespace Jlyt.HwAds.Platform
{
    /// <summary>
    /// UnitySendMessage receiver host. The ObjC bridge (HwAdsInterface.m) sends
    /// UnitySendMessage("HwAdsBridge","RewardCallBack"/"InterCallBack",...) — so the host GameObject
    /// must keep the exact name "HwAdsBridge" and expose those two method names.
    /// </summary>
    internal sealed class HwAdsIosHost : MonoBehaviour
    {
        static Action<bool> s_RewardCallback;
        static Action<bool> s_InterCallback;

        public static HwAdsIosHost EnsureCreated()
        {
            var go = GameObject.Find("HwAdsBridge");
            HwAdsIosHost host = go != null ? go.GetComponent<HwAdsIosHost>() : null;
            if (host == null)
            {
                go = new GameObject("HwAdsBridge");
                host = go.AddComponent<HwAdsIosHost>();
                DontDestroyOnLoad(go);
            }

            return host;
        }

        public static void SetRewardCallback(Action<bool> callback) => s_RewardCallback = callback;
        public static void SetInterCallback(Action<bool> callback) => s_InterCallback = callback;

        public void RewardCallBack(string msg)
        {
            Debug.Log("[Jlyt.HwAds] receive reward callback msg:" + msg);
            bool ok = string.Equals(msg, "true", StringComparison.Ordinal);
            Action<bool> cb = s_RewardCallback;
            s_RewardCallback = null;
            cb?.Invoke(ok);
        }

        public void InterCallBack(string msg)
        {
            Debug.Log("[Jlyt.HwAds] receive inter callback msg:" + msg);
            Action<bool> cb = s_InterCallback;
            s_InterCallback = null;
            cb?.Invoke(true);
        }

        void OnDestroy()
        {
            s_RewardCallback = null;
            s_InterCallback = null;
        }
    }
}
#endif
