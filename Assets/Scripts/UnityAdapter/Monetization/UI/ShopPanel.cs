using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Core.Monetization;

namespace BlockPuzzle.UnityAdapter.Monetization.UI
{
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private ShopItemUI shopItemPrefab;
        [SerializeField] private Transform premiumProductsContainer;
        [SerializeField] private Transform consumableProductsContainer;

        private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

        private void OnEnable()
        {
            RefreshShop();
            if (StoreManager.Instance != null)
            {
                StoreManager.Instance.OnPurchaseSuccess += HandlePurchaseSuccess;
            }
        }

        private void OnDisable()
        {
            if (StoreManager.Instance != null)
            {
                StoreManager.Instance.OnPurchaseSuccess -= HandlePurchaseSuccess;
            }
        }

        private void RefreshShop()
        {
            foreach (var item in spawnedItems)
            {
                Destroy(item.gameObject);
            }
            spawnedItems.Clear();

            if (StoreManager.Instance == null) return;

            foreach (var product in StoreManager.Instance.availableProducts)
            {
                Transform parentContainer = product.productType == ProductType.Consumable 
                    ? consumableProductsContainer 
                    : premiumProductsContainer;

                ShopItemUI newItem = Instantiate(shopItemPrefab, parentContainer);
                newItem.Setup(product);
                spawnedItems.Add(newItem);
            }
        }

        private void HandlePurchaseSuccess(ProductDefinition product)
        {
            foreach (var item in spawnedItems)
            {
                item.RefreshState(); // Update 'Owned' overlays
            }
        }
    }
}