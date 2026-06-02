using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Core.Monetization;

namespace BlockPuzzle.UnityAdapter.Monetization.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject ownedOverlay;

        private ProductDefinition _product;

        public void Setup(ProductDefinition product)
        {
            _product = product;
            titleText.text = product.productName;
            descriptionText.text = product.description;
            priceText.text = product.priceString;

            RefreshState();

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void RefreshState()
        {
            bool isOwned = false;

            if (_product.productType == ProductType.NonConsumable && _product.removesAds)
            {
                if (EntitlementManager.Instance != null && EntitlementManager.Instance.HasRemovedAds())
                {
                    isOwned = true;
                }
            }

            if (isOwned)
            {
                buyButton.gameObject.SetActive(false);
                if (ownedOverlay != null) ownedOverlay.SetActive(true);
            }
            else
            {
                buyButton.gameObject.SetActive(true);
                if (ownedOverlay != null) ownedOverlay.SetActive(false);
            }
        }

        private void OnBuyClicked()
        {
            if (StoreManager.Instance != null)
            {
                StoreManager.Instance.PurchaseProduct(_product.productId);
            }
        }
    }
}