using System;
using UnityEngine;
using BlockPuzzle.Core.Meta;
using BlockPuzzle.UnityAdapter.Boot;
using BlockPuzzle.UnityAdapter.UI;

namespace BlockPuzzle.Core.Monetization
{
    public class ContinueEconomyManager : MonoBehaviour
    {
        public static ContinueEconomyManager Instance { get; private set; }

        private const string CONTINUE_TOKEN_ID = "continue_token";

        public event Action OnContinueSuccess;
        public event Action OnContinueFailed;

        private bool _pendingAdReward;
        private bool _adFlowActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            UnhookAdCallbacks();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnhookAdCallbacks();
                Instance = null;
            }
        }

        public bool HasContinueToken()
        {
            return RewardInventory.Instance != null &&
                   RewardInventory.Instance.GetAmount(CONTINUE_TOKEN_ID) > 0;
        }

        public void TryContinueWithToken()
        {
            if (!HasContinueToken())
            {
                OnContinueFailed?.Invoke();
                return;
            }

            if (!TryRestoreRunAfterContinue())
            {
                OnContinueFailed?.Invoke();
                return;
            }

            if (RewardInventory.Instance != null &&
                RewardInventory.Instance.ConsumeReward(CONTINUE_TOKEN_ID, 1))
            {
                OnContinueSuccess?.Invoke();
                return;
            }

            OnContinueFailed?.Invoke();
        }

        public void TryContinueWithAd()
        {
            if (_adFlowActive || !RewardedAdBridge.HasProvider || !RewardedAdBridge.IsReady())
            {
                OnContinueFailed?.Invoke();
                return;
            }

            _pendingAdReward = false;
            _adFlowActive = true;
            HookAdCallbacks();
            RewardedAdBridge.Show();
        }

        private void HandleRewardEarned()
        {
            _pendingAdReward = true;
        }

        private void HandleAdClosed()
        {
            UnhookAdCallbacks();
            _adFlowActive = false;

            if (_pendingAdReward && TryRestoreRunAfterContinue())
                OnContinueSuccess?.Invoke();
            else
                OnContinueFailed?.Invoke();

            _pendingAdReward = false;
        }

        private void HookAdCallbacks()
        {
            RewardedAdBridge.RewardedUserEarned -= HandleRewardEarned;
            RewardedAdBridge.RewardedAdClosed -= HandleAdClosed;
            RewardedAdBridge.RewardedUserEarned += HandleRewardEarned;
            RewardedAdBridge.RewardedAdClosed += HandleAdClosed;
        }

        private void UnhookAdCallbacks()
        {
            RewardedAdBridge.RewardedUserEarned -= HandleRewardEarned;
            RewardedAdBridge.RewardedAdClosed -= HandleAdClosed;
        }

        private static bool TryRestoreRunAfterContinue()
        {
            var gameBootstrap = FindFirstObjectByType<GameBootstrap>();
            return gameBootstrap != null && gameBootstrap.TryContinueAfterRewardedAd();
        }
    }
}
