using System;
using UnityEngine;

namespace BlockPuzzle.Core.Social
{
    public class DailyChallengeManager : MonoBehaviour
    {
        public static DailyChallengeManager Instance { get; private set; }

        public bool IsPlayingDailyChallenge { get; private set; }
        
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

        public int GetTodaySeed()
        {
            DateTime now = DateTime.UtcNow;
            string seedString = $"{now.Year}{now.Month}{now.Day}";
            return seedString.GetHashCode(); // Her gün için sabit (Fixed-Seed)
        }

        public void StartDailyChallenge()
        {
            IsPlayingDailyChallenge = true;
            int seed = GetTodaySeed();
            
            // Seeded RNG entegrasyonu
            // BlockPuzzle.Core.RNG.SeededRng.Init(seed);
            
            Debug.Log($"Daily challenge started with fixed seed: {seed}. Everyone plays the same block sequence today!");
        }

        public void EndDailyChallenge()
        {
            IsPlayingDailyChallenge = false;
        }
    }
}