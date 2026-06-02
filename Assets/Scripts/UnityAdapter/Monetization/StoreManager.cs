using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Meta;

namespace BlockPuzzle.Core.Monetization
{
    public enum PurchaseRuntimeState
    {
        NotInitialized,
        Initializing,
        Ready,
        Purchasing,
        Succeeded,
        Failed,
        Cancelled,
        Disabled
    }

    public class StoreManager : MonoBehaviour
    {
        public static StoreManager Instance { get; private set; }

        public List<ProductDefinition> availableProducts = new List<ProductDefinition>();

        public event Action<ProductDefinition> OnPurchaseSuccess;
        public event Action<string> OnPurchaseFailed;

        private readonly HashSet<string> _processedTransactionIds = new HashSet<string>(StringComparer.Ordinal);
        private PurchaseRuntimeState _runtimeState = PurchaseRuntimeState.NotInitialized;

        public PurchaseRuntimeState RuntimeState => _runtimeState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _runtimeState = HasAnyConfiguredProducts() ? PurchaseRuntimeState.Ready : PurchaseRuntimeState.Disabled;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PurchaseProduct(string productId)
        {
            ProductDefinition product = availableProducts.Find(p => p != null && p.productId == productId);
            if (product == null)
            {
                _runtimeState = PurchaseRuntimeState.Failed;
                OnPurchaseFailed?.Invoke("Product not found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(product.productId))
            {
                _runtimeState = PurchaseRuntimeState.Failed;
                Debug.LogError("[StoreManager] Purchase blocked: product configuration is missing a product ID.");
                OnPurchaseFailed?.Invoke("Product configuration is incomplete.");
                return;
            }

            if (!CanUseSimulatedPurchases())
            {
                _runtimeState = PurchaseRuntimeState.Disabled;
                Debug.LogError("[StoreManager] Simulated purchases are disabled in release builds. A real store backend is required.");
                OnPurchaseFailed?.Invoke("Store is unavailable in this build.");
                return;
            }

            _runtimeState = PurchaseRuntimeState.Purchasing;

            // Editor/development-only fallback until a real store backend is wired.
            string simulatedTransactionId = Guid.NewGuid().ToString("N");
            CompletePurchase(product, simulatedTransactionId);
        }

        private void CompletePurchase(ProductDefinition product, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                _runtimeState = PurchaseRuntimeState.Failed;
                OnPurchaseFailed?.Invoke("Purchase transaction is missing.");
                return;
            }

            if (!_processedTransactionIds.Add(transactionId))
            {
                Debug.LogWarning("[StoreManager] Duplicate purchase completion ignored.");
                return;
            }

            if (product.productType == ProductType.Consumable)
            {
                if (RewardInventory.Instance != null && !string.IsNullOrEmpty(product.rewardId))
                {
                    RewardInventory.Instance.AddReward(product.rewardId, product.rewardAmount);
                }
            }
            else if (product.productType == ProductType.NonConsumable)
            {
                if (product.removesAds && EntitlementManager.Instance != null)
                {
                    EntitlementManager.Instance.GrantRemoveAds();
                }

                // Give additional one-time items from bundles (e.g. Starter Pack)
                if (RewardInventory.Instance != null && !string.IsNullOrEmpty(product.rewardId))
                {
                    RewardInventory.Instance.AddReward(product.rewardId, product.rewardAmount);
                }
            }

            _runtimeState = PurchaseRuntimeState.Succeeded;
            OnPurchaseSuccess?.Invoke(product);
        }

        private bool HasAnyConfiguredProducts()
        {
            for (int i = 0; i < availableProducts.Count; i++)
            {
                ProductDefinition product = availableProducts[i];
                if (product != null && !string.IsNullOrWhiteSpace(product.productId))
                    return true;
            }

            return false;
        }

        private static bool CanUseSimulatedPurchases()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }
}
