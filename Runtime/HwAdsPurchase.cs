using System;

namespace Jlyt.HwAds
{
    /// <summary>
    /// In-app purchase second-verification report sent to the HW backend.
    /// Maps the former per-platform PurchaseEvent arguments (Android 7 args / iOS 8 args).
    /// </summary>
    [Serializable]
    public sealed class HwAdsPurchase
    {
        public string number;          // localized amount
        public string currency;        // localized currency code
        public string purchaseToken;   // store purchase token (iOS: payload; Android: purchaseToken)
        public string productId;       // store product id
        public string productName;     // iOS localized title; Android may leave empty
        public int purchaseType;       // 0 = consumable/normal, 1 = subscription
        public string orderId;         // GPA.xxx / iOS transaction id
        public string adjustToken;     // adjust purchase event token (tier-based)

        public HwAdsPurchase() { }
        public HwAdsPurchase(string number, string currency, string purchaseToken, string productId, string productName, int purchaseType, string orderId, string adjustToken)
        {
            this.number = number; this.currency = currency; this.purchaseToken = purchaseToken;
            this.productId = productId; this.productName = productName;
            this.purchaseType = purchaseType; this.orderId = orderId; this.adjustToken = adjustToken;
        }
    }
}
