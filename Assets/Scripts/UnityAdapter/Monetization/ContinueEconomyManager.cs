using System;
using UnityEngine;
using BlockPuzzle.Core.Meta;

namespace BlockPuzzle.Core.Monetization
{
    public class ContinueEconomyManager : MonoBehaviour
    {
        public static ContinueEconomyManager Instance { get; private set; }

        private const string CONTINUE_TOKEN_ID = "continue_token";

        public event Action OnContinueSuccess;
        public event Action OnContinueFailed;

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

        public bool HasContinueToken()
        {
            return RewardInventory.Instance != null && RewardInventory.Instance.GetAmount(CONTINUE_TOKEN_ID) > 0;
        }

        public void TryContinueWithToken()
        {
            if (RewardInventory.Instance != null && RewardInventory.Instance.ConsumeReward(CONTINUE_TOKEN_ID, 1))
            {
                Debug.Log("Consumed 1 continue token. Resuming game.");
                OnContinueSuccess?.Invoke();
            }
            else
            {
                OnContinueFailed?.Invoke();
            }
        }

        public void TryContinueWithAd()
        {
            // Here you would hook into RewardedAdManager.Instance.ShowRewardedAd(OnAdSuccess, OnAdFailed)
            Debug.Log("Waiting for Rewarded Ad finish...");
            // Simulated Ad Success
            OnContinueSuccess?.Invoke();
        }
    }
}