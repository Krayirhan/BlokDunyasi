using System;
using UnityEngine;

namespace BlockPuzzle.Core.Monetization
{
    public class EntitlementManager : MonoBehaviour
    {
        public static EntitlementManager Instance { get; private set; }

        private const string REMOVE_ADS_KEY = "Entitlement_RemoveAds";

        public event Action OnAdsRemoved;

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

        public bool HasRemovedAds()
        {
            return PlayerPrefs.GetInt(REMOVE_ADS_KEY, 0) == 1;
        }

        public void GrantRemoveAds()
        {
            if (!HasRemovedAds())
            {
                PlayerPrefs.SetInt(REMOVE_ADS_KEY, 1);
                PlayerPrefs.Save();
                OnAdsRemoved?.Invoke();
                Debug.Log("Ads removed entitlement granted.");
            }
        }
    }
}