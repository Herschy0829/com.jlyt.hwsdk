using UnityEngine;

namespace Jlyt.HwAds.Internal
{
    /// <summary>
    /// Fire-once flag storage (replaces per-project CLocalizeData usage for "report this adjust event only once").
    /// </summary>
    internal static class HwAdsLocalOnce
    {
        const string Prefix = "jlyt.hwsdk.once.";

        public static bool TryConsume(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string storeKey = Prefix + key;
            if (PlayerPrefs.GetInt(storeKey, 0) == 1)
            {
                return false;
            }

            PlayerPrefs.SetInt(storeKey, 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
