using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Companions
{
    public class CompanionUpgradePopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject popupRoot;

        [Header("Preview")]
        [SerializeField] private Transform previewRoot;

        [Header("Texts")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text upgradePriceText;

        [Header("Buttons")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;

        [Header("Bonus Placeholder")]
        [SerializeField] private GameObject bonusPlaceholderRoot;

        private string currentCompanionId;
        private bool upgradeInFlight;

        private void Awake()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (popupRoot != null)
                popupRoot.SetActive(false);
        }

        public void Open(string companionId)
        {
            if (string.IsNullOrWhiteSpace(companionId))
                return;

            currentCompanionId = companionId;
            upgradeInFlight = false;

            if (popupRoot != null)
                popupRoot.SetActive(true);

            Refresh();
        }

        public void Close()
        {
            if (upgradeInFlight)
                return;

            currentCompanionId = string.Empty;

            ClearPreview();

            if (popupRoot != null)
                popupRoot.SetActive(false);
        }

        public void Refresh()
        {
            var service = CompanionService.I;
            if (service == null || string.IsNullOrWhiteSpace(currentCompanionId))
                return;

            var def = service.GetDef(currentCompanionId);
            var owned = service.GetOwnedState(currentCompanionId);

            if (def == null || owned == null)
                return;

            int level = Mathf.Max(1, owned.level);
            bool atMaxLevel = level >= def.maxLevel;

            if (nameText) nameText.text = def.displayName;
            if (levelText) levelText.text = $"{level}/{def.maxLevel}";

            if (upgradePriceText)
            {
                if (atMaxLevel)
                    upgradePriceText.text = "MAX";
                else
                    upgradePriceText.text = service.GetUpgradeCost(currentCompanionId).ToString();
            }

            if (upgradeButton)
            {
                bool canUpgrade = !atMaxLevel && !upgradeInFlight;
                upgradeButton.interactable = canUpgrade;
            }

            if (bonusPlaceholderRoot)
                bonusPlaceholderRoot.SetActive(true);

            RefreshPreview(def);
        }

        private void RefreshPreview(CompanionDef def)
        {
            ClearPreview();

            if (previewRoot == null || def == null || def.uiPrefab == null)
                return;

            Instantiate(def.uiPrefab, previewRoot);
        }

        private void ClearPreview()
        {
            if (previewRoot == null)
                return;

            for (int i = previewRoot.childCount - 1; i >= 0; i--)
                Destroy(previewRoot.GetChild(i).gameObject);
        }

        private void OnUpgradeClicked()
        {
            var service = CompanionService.I;
            if (service == null || string.IsNullOrWhiteSpace(currentCompanionId))
                return;

            if (upgradeInFlight || service.IsPurchaseInFlight())
                return;

            var def = service.GetDef(currentCompanionId);
            var owned = service.GetOwnedState(currentCompanionId);

            if (def == null || owned == null)
                return;

            int level = Mathf.Max(1, owned.level);
            if (level >= def.maxLevel)
                return;

            upgradeInFlight = true;

            if (upgradeButton != null)
                upgradeButton.interactable = false;

            service.UpgradeWithServerCurrency(currentCompanionId, success =>
            {
                upgradeInFlight = false;

                if (popupRoot == null || !popupRoot.activeInHierarchy)
                    return;

                Refresh();
            }, immediateSave: true);
        }
    }
}