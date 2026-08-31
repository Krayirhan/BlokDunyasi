using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Core.Meta
{
    public class RewardInventory : MonoBehaviour
    {
        private static RewardInventory _instance;

        public static RewardInventory Instance
        {
            get
            {
                if (_instance == null)
                {
                    var inventoryObject = new GameObject("RewardInventory");
                    _instance = inventoryObject.AddComponent<RewardInventory>();
                }

                return _instance;
            }
        }

        private Dictionary<string, int> inventory = new Dictionary<string, int>();

        public event Action<string, int> OnInventoryUpdated;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddReward(string rewardId, int amount)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || amount <= 0)
                return;

            if (inventory.ContainsKey(rewardId))
            {
                inventory[rewardId] += amount;
            }
            else
            {
                inventory[rewardId] = amount;
            }

            PlayerPrefs.SetInt(GetRewardKey(rewardId), inventory[rewardId]);
            PlayerPrefs.Save();
            
            OnInventoryUpdated?.Invoke(rewardId, inventory[rewardId]);
        }

        public int GetAmount(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return 0;

            if (!inventory.ContainsKey(rewardId))
                inventory[rewardId] = PlayerPrefs.GetInt(GetRewardKey(rewardId), 0);

            return inventory.TryGetValue(rewardId, out int amount) ? amount : 0;
        }

        public bool ConsumeReward(string rewardId, int amount)
        {
            if (GetAmount(rewardId) >= amount)
            {
                inventory[rewardId] -= amount;
                PlayerPrefs.SetInt(GetRewardKey(rewardId), inventory[rewardId]);
                PlayerPrefs.Save();
                OnInventoryUpdated?.Invoke(rewardId, inventory[rewardId]);
                return true;
            }
            return false;
        }

        private static string GetRewardKey(string rewardId)
        {
            return $"reward_inventory_{rewardId}";
        }
    }
}
