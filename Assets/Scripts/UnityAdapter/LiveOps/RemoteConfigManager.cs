using System;
using UnityEngine;
using Debug = BlockPuzzle.Core.Common.GameLogger;

namespace BlockPuzzle.Core.LiveOps
{
    public class RemoteConfigManager : MonoBehaviour
    {
        public static RemoteConfigManager Instance { get; private set; }

        public event Action OnConfigFetched;

        // Remote Tuning Parameters
        public float DdaDifficultyScale { get; private set; } = 1.0f;
        public int InterstitialAdIntervalGames { get; private set; } = 3;
        public int MissionRewardScalePercent { get; private set; } = 100; // 100% is base

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

        public void FetchRemoteConfigs()
        {
            Debug.Log("[RemoteConfig] Fetching latest server settings...");
            
            // Simulating Unity Remote Configs or Firebase Remote Configs response
            DdaDifficultyScale = PlayerPrefs.GetFloat("rc_dda_scale", 1.0f);
            InterstitialAdIntervalGames = PlayerPrefs.GetInt("rc_ad_interval", 3);
            MissionRewardScalePercent = PlayerPrefs.GetInt("rc_reward_scale", 100);

            Debug.Log("[RemoteConfig] Configs applied.");
            OnConfigFetched?.Invoke();
        }
    }
}
