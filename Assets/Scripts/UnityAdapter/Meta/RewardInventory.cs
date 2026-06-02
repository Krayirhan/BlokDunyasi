using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Core.Meta
{
    public class RewardInventory : MonoBehaviour
    {
        public static RewardInventory Instance { get; private set; }

        private Dictionary<string, int> inventory = new Dictionary<string, int>();

        public event Action<string, int> OnInventoryUpdated;

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

        public void AddReward(string rewardId, int amount)
        {
            if (inventory.ContainsKey(rewardId))
            {
                inventory[rewardId] += amount;
            }
            else
            {
                inventory[rewardId] = amount;
            }
            
            OnInventoryUpdated?.Invoke(rewardId, inventory[rewardId]);
        }

        public int GetAmount(string rewardId)
        {
            return inventory.TryGetValue(rewardId, out int amount) ? amount : 0;
        }

        public bool ConsumeReward(string rewardId, int amount)
        {
            if (GetAmount(rewardId) >= amount)
            {
                inventory[rewardId] -= amount;
                OnInventoryUpdated?.Invoke(rewardId, inventory[rewardId]);
                return true;
            }
            return false;
        }
    }
}