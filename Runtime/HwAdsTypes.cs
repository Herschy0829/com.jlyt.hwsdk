namespace Jlyt.HwAds
{
    public enum HwAdsShowResultCode
    {
        ShownCompleted = 0,
        NotReady = 1,
        Failed = 2,
        ClosedWithoutReward = 3,
    }

    /// <summary>
    /// Uniform show result for rewarded/interstitial. `rewarded` means the ad was watched to the end
    /// (reward should be granted on the "completed" semantics of the underlying SDK).
    /// </summary>
    public readonly struct HwAdsShowResult
    {
        public readonly HwAdsShowResultCode code;
        public readonly bool rewarded;
        public readonly string placement;

        public HwAdsShowResult(HwAdsShowResultCode code, bool rewarded, string placement)
        {
            this.code = code;
            this.rewarded = rewarded;
            this.placement = placement;
        }

        public static HwAdsShowResult Completed(string placement) => new HwAdsShowResult(HwAdsShowResultCode.ShownCompleted, true, placement);
        public static HwAdsShowResult ClosedNoReward(string placement) => new HwAdsShowResult(HwAdsShowResultCode.ClosedWithoutReward, false, placement);
        public static HwAdsShowResult NotReady(string placement) => new HwAdsShowResult(HwAdsShowResultCode.NotReady, false, placement);
        public static HwAdsShowResult Failed(string placement) => new HwAdsShowResult(HwAdsShowResultCode.Failed, false, placement);
    }
}
