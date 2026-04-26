using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Companions
{
    public class CompanionCardView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private Button selectButton;

        [Header("Compact State")]
        [SerializeField] private GameObject compactRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text levelText;

        [Header("Expanded State")]
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject expandedRoot;
        [SerializeField] private GameObject upgradeButtonRoot;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonText;
        [SerializeField] private TMP_Text upgradeButtonText;

        [Header("Button Visuals")]
        [SerializeField] private Image upgradeButtonImage;
        [SerializeField] private Sprite upgradeNormalSprite;
        [SerializeField] private Sprite upgradeRealMoneySprite;

        [Header("Hard Currency Visuals")]
        [SerializeField] private GameObject equipCurrencyRoot;
        [SerializeField] private Image equipCurrencyImage;

        [Header("Visual States")]
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private Image indicatorImage;

        [Header("Indicator Colors")]
        [SerializeField] private Color unequippedColor = Color.black;
        [SerializeField] private Color equippedColor = Color.green;

        private CompanionDef def;
        private CompanionWindow owner;
        private ICompanionPurchaseProvider purchaseProvider;

        public string CompanionId => def != null ? def.id : string.Empty;

        public void Bind(CompanionDef companionDef, CompanionWindow window, bool expanded)
        {
            def = companionDef;
            owner = window;

            CachePurchaseProvider();
            BindButtons();
            Refresh(expanded);
        }

        public void Refresh(bool expanded)
        {
            if (def == null)
                return;

            var service = CompanionService.I;
            if (service == null)
                return;

            bool unlocked = service.IsUnlocked(def.id);
            bool equipped = service.IsEquipped(def.id);
            int level = service.GetLevel(def.id);
            bool atMaxLevel = unlocked && level >= def.maxLevel;

            bool supportsPlatformPurchase = purchaseProvider != null && purchaseProvider.SupportsPlatformPurchase();

            if (compactRoot) compactRoot.SetActive(true);
            if (background) background.SetActive(expanded);
            if (expandedRoot) expandedRoot.SetActive(expanded);
            if (lockedRoot) lockedRoot.SetActive(!unlocked);

            if (indicatorImage)
                indicatorImage.color = equipped ? equippedColor : unequippedColor;

            if (iconImage) iconImage.sprite = def.cardIcon;
            if (levelText) levelText.text = unlocked ? $"{level}/{def.maxLevel}" : $"1/{def.maxLevel}";

            if (!unlocked)
            {
                RefreshLockedState(supportsPlatformPurchase);
                return;
            }

            RefreshUnlockedState(service, atMaxLevel, equipped);
        }

        private void RefreshLockedState(bool supportsPlatformPurchase)
        {
            if (upgradeButtonRoot)
                upgradeButtonRoot.SetActive(supportsPlatformPurchase);

            if (upgradeButtonImage)
                upgradeButtonImage.sprite = upgradeRealMoneySprite != null ? upgradeRealMoneySprite : upgradeButtonImage.sprite;

            if (upgradeButtonText)
            {
                if (supportsPlatformPurchase && purchaseProvider != null)
                    upgradeButtonText.text = purchaseProvider.GetDisplayPrice(def);
                else
                    upgradeButtonText.text = string.Empty;
            }

            if (upgradeButton)
                upgradeButton.interactable = supportsPlatformPurchase;

            if (equipCurrencyRoot)
                equipCurrencyRoot.SetActive(true);

            if (equipCurrencyImage)
                equipCurrencyImage.enabled = true;

            if (equipButtonText)
                equipButtonText.text = def.hardCurrencyUnlockCost.ToString();
        }

        private void RefreshUnlockedState(CompanionService service, bool atMaxLevel, bool equipped)
        {
            if (upgradeButtonRoot)
                upgradeButtonRoot.SetActive(true);

            if (upgradeButtonImage)
                upgradeButtonImage.sprite = upgradeNormalSprite != null ? upgradeNormalSprite : upgradeButtonImage.sprite;

            if (upgradeButtonText)
            {
                if (atMaxLevel)
                {
                    upgradeButtonText.text = "Max";
                }
                else
                {
                    int cost = service.GetUpgradeCost(def.id);
                    upgradeButtonText.text = $"Upgrade {cost}";
                }
            }

            if (upgradeButton)
            {
                bool canUpgrade = service.CanUpgrade(def.id);
                upgradeButton.interactable = canUpgrade && !atMaxLevel;
            }

            if (equipCurrencyRoot)
                equipCurrencyRoot.SetActive(false);

            if (equipButtonText)
                equipButtonText.text = equipped ? "Unequip" : "Equip";
        }

        private void BindButtons()
        {
            if (selectButton)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectClicked);
            }

            if (upgradeButton)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (equipButton)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(OnEquipClicked);
            }
        }

        private void CachePurchaseProvider()
        {
#if UNITY_2023_1_OR_NEWER
            purchaseProvider = FindFirstObjectByType<MockCompanionPurchaseProvider>();
#else
            purchaseProvider = FindObjectOfType<MockCompanionPurchaseProvider>();
#endif
        }

        private void OnSelectClicked()
        {
            if (def == null || owner == null)
                return;

            owner.ToggleSelection(def.id);
        }

        private void OnUpgradeClicked()
        {
            if (def == null || owner == null)
                return;

            owner.HandleUpgradePressed(def.id);
        }

        private void OnEquipClicked()
        {
            if (def == null || owner == null)
                return;

            owner.HandleEquipPressed(def.id);
        }
    }
}