using System;
using UnityEngine;

namespace GameCore.Companions
{
    public class MockCompanionPurchaseProvider : MonoBehaviour, ICompanionPurchaseProvider
    {
        [Header("Platform")]
        [SerializeField] private CompanionStorePlatform currentPlatform = CompanionStorePlatform.WebGL;

        [Header("Mock Prices")]
        [SerializeField] private string tgMiniAppPrice = "5 890,00 ?";
        [SerializeField] private string appStorePrice = "$4.99";
        [SerializeField] private string googlePlayPrice = "2 390,00 ?";

        [Header("Mock Result")]
        [SerializeField] private bool purchaseSucceeds = true;

        public CompanionStorePlatform GetCurrentPlatform()
        {
            return currentPlatform;
        }

        public bool SupportsPlatformPurchase()
        {
            return currentPlatform != CompanionStorePlatform.WebGL;
        }

        public string GetDisplayPrice(CompanionDef def)
        {
            if (!SupportsPlatformPurchase())
                return string.Empty;

            switch (currentPlatform)
            {
                case CompanionStorePlatform.TgMiniApp:
                    return tgMiniAppPrice;

                case CompanionStorePlatform.AppStore:
                    return appStorePrice;

                case CompanionStorePlatform.GooglePlay:
                    return googlePlayPrice;

                default:
                    return string.Empty;
            }
        }

        public string GetProductId(CompanionDef def)
        {
            if (def == null)
                return string.Empty;

            switch (currentPlatform)
            {
                case CompanionStorePlatform.TgMiniApp:
                    return def.tgMiniAppProductId;

                case CompanionStorePlatform.AppStore:
                    return def.appStoreProductId;

                case CompanionStorePlatform.GooglePlay:
                    return def.googlePlayProductId;

                default:
                    return string.Empty;
            }
        }

        public void Purchase(CompanionDef def, Action<bool> onComplete)
        {
            if (def == null)
            {
                onComplete?.Invoke(false);
                return;
            }

            if (!SupportsPlatformPurchase())
            {
                Debug.Log("[MockCompanionPurchaseProvider] Platform purchase is disabled on WebGL.");
                onComplete?.Invoke(false);
                return;
            }

            string productId = GetProductId(def);
            string price = GetDisplayPrice(def);

            Debug.Log($"[MockCompanionPurchaseProvider] Purchase start -> Platform: {currentPlatform}, ProductId: {productId}, Price: {price}");

            onComplete?.Invoke(purchaseSucceeds);
        }
    }
}