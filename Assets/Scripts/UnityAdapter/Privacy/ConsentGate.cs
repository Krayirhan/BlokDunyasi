using System;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Privacy
{
    public enum ConsentState
    {
        Unknown = 0,
        Accepted = 1,
        Declined = 2
    }

    /// <summary>
    /// Production source-of-truth for privacy-sensitive startup gates.
    /// Ads and analytics must treat Unknown as blocked until an explicit decision exists.
    /// </summary>
    public static class ConsentGate
    {
        private const string ConsentStateKey = "BlokDunyasi_ConsentState";
        private static bool _loaded;
        private static ConsentState _currentState;

        public static event Action<ConsentState> ConsentStateChanged;

        public static ConsentState CurrentState
        {
            get
            {
                EnsureLoaded();
                return _currentState;
            }
        }

        public static bool IsResolved => CurrentState != ConsentState.Unknown;
        public static bool CanCollectAnalytics => CurrentState == ConsentState.Accepted;
        public static bool CanInitializeAds => CurrentState == ConsentState.Accepted;

        /// <summary>
        /// True when ads were started without a confirmed UMP consent decision
        /// (e.g. the consent watchdog gave up waiting on a hung native
        /// callback). While true, ad requests must be marked non-personalized
        /// (npa=1) since we cannot prove the user actually consented to
        /// personalization. Not persisted -- re-evaluated fresh every launch.
        /// </summary>
        public static bool RequireNonPersonalizedAds { get; set; }

        public static void ResetForTesting()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _loaded = true;
            _currentState = ConsentState.Unknown;
            PlayerPrefs.DeleteKey(ConsentStateKey);
            PlayerPrefs.Save();
            ConsentStateChanged?.Invoke(_currentState);
#endif
        }

        public static void SetConsentState(ConsentState state)
        {
            EnsureLoaded();
            if (_currentState == state)
                return;

            _currentState = state;
            PlayerPrefs.SetInt(ConsentStateKey, (int)state);
            PlayerPrefs.Save();
            ConsentStateChanged?.Invoke(_currentState);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            int storedValue = PlayerPrefs.GetInt(ConsentStateKey, (int)ConsentState.Unknown);
            _currentState = Enum.IsDefined(typeof(ConsentState), storedValue)
                ? (ConsentState)storedValue
                : ConsentState.Unknown;
            _loaded = true;
        }
    }
}
