using System;
using System.Threading;
using UnityEngine;

namespace Jlyt.HwAds.Internal
{
    /// <summary>
    /// Self-contained main-thread dispatcher (replaces project's UnityThreadUtil dependency).
    /// Captures the Unity synchronization context once and re-dispatches native callbacks onto it.
    /// </summary>
    internal static class HwAdsMainThread
    {
        static SynchronizationContext _ctx;
        static bool _captured;

        public static void Capture()
        {
            if (!_captured)
            {
                _ctx = SynchronizationContext.Current;
                _captured = true;
            }
        }

        public static void Post(Action action)
        {
            if (action == null)
            {
                return;
            }

            Capture();
            if (_ctx != null && SynchronizationContext.Current != _ctx)
            {
                _ctx.Post(_ => Safe(action), null);
            }
            else
            {
                Safe(action);
            }
        }

        static void Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
