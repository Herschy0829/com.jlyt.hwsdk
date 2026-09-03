using System;

namespace Jlyt.HwAds
{
    /// <summary>
    /// Per-project initialization config.
    /// Values used to be hard-coded consts inside per-project bridge code; now injected at Init.
    /// Reference values: gameBrainIdAndroid "392" / gameBrainIdIos 393 / appToken "tcdmx6lgk45c" (adjust token),
    /// channel "Google Play" (survivor-island). Fill per game from hw-services.json where possible.
    /// </summary>
    [Serializable]
    public sealed class HwAdsSdkConfig
    {
        public string gameBrainIdAndroid = "";
        public int gameBrainIdIos = 0;
        public string appToken = "";
        public string channel = "Google Play";

        public void Validate()
        {
            if (string.IsNullOrEmpty(gameBrainIdAndroid) || string.IsNullOrEmpty(appToken) || gameBrainIdIos <= 0)
            {
                throw new InvalidOperationException(
                    "[Jlyt.HwAds] HwAdsSdkConfig incomplete: gameBrainIdAndroid/appToken/gameBrainIdIos must be set per project.");
            }
        }
    }
}
