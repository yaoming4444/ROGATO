using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Companions
{
    public class CompanionWindow : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private CompanionCardView cardPrefab;
        [SerializeField] private Transform gridRoot;

        [Header("Preview Roots")]
        [SerializeField] private Transform previewRootA;
        [SerializeField] private Transform previewRootB;

        [Header("Behavior")]
        [SerializeField] private bool autoEquipAfterPurchase = false;

        private readonly List<CompanionCardView> spawnedCards = new();

        private string selectedCompanionId;
        private bool _purchaseInFlight;

        private void OnEnable()
        {
            if (CompanionService.I != null)
                CompanionService.I.OnChanged += HandleCompanionsChanged;

            Rebuild();
        }

        private void OnDisable()
        {
            if (CompanionService.I != null)
                CompanionService.I.OnChanged -= HandleCompanionsChanged;

            selectedCompanionId = string.Empty;
        }

        private void HandleCompanionsChanged()
        {
            RefreshAll();
        }

        public void ToggleSelection(string companionId)
        {
            if (string.IsNullOrWhiteSpace(companionId))
                return;

            if (selectedCompanionId == companionId)
                selectedCompanionId = string.Empty;
            else
                selectedCompanionId = companionId;

            RefreshCards();
        }

        public void HandleEquipPressed(string companionId)
        {
            var service = CompanionService.I;
            if (service == null)
                return;

            if (_purchaseInFlight || service.IsPurchaseInFlight())
                return;

            if (!service.IsUnlocked(companionId))
            {
                _purchaseInFlight = true;

                service.Buy(companionId, success =>
                {
                    _purchaseInFlight = false;

                    if (!success)
                    {
                        RefreshAll();
                        return;
                    }

                    if (autoEquipAfterPurchase)
                        EquipIntoFirstAvailableSlot(companionId);

                    RefreshAll();
                }, immediateSave: true);

                return;
            }

            if (service.IsEquipped(companionId))
            {
                if (service.IsEquippedInSlot(companionId, CompanionEquipSlot.SlotA))
                    service.Unequip(CompanionEquipSlot.SlotA, immediateSave: true);

                if (service.IsEquippedInSlot(companionId, CompanionEquipSlot.SlotB))
                    service.Unequip(CompanionEquipSlot.SlotB, immediateSave: true);

                RefreshAll();
                return;
            }

            EquipIntoFirstAvailableSlot(companionId);
            RefreshAll();
        }

        public void HandleUpgradePressed(string companionId)
        {
            var service = CompanionService.I;
            if (service == null)
                return;

            if (_purchaseInFlight || service.IsPurchaseInFlight())
                return;

            if (!service.IsUnlocked(companionId))
            {
                _purchaseInFlight = true;

                service.Buy(companionId, success =>
                {
                    _purchaseInFlight = false;
                    RefreshAll();
                }, immediateSave: true);

                return;
            }

            service.Upgrade(companionId, immediateSave: true);
            RefreshAll();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            ClearCards();

            var service = CompanionService.I;
            if (service == null || cardPrefab == null || gridRoot == null)
                return;

            var defs = service.GetAllDefs();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null)
                    continue;

                var card = Instantiate(cardPrefab, gridRoot);
                bool expanded = selectedCompanionId == def.id;

                card.Bind(def, this, expanded);
                spawnedCards.Add(card);
            }

            RefreshPreviews();
        }

        public void RefreshAll()
        {
            RefreshCards();
            RefreshPreviews();
        }

        private void RefreshCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                var card = spawnedCards[i];
                if (card == null)
                    continue;

                bool expanded = card.CompanionId == selectedCompanionId;
                card.Refresh(expanded);
            }
        }

        private void RefreshPreviews()
        {
            var service = CompanionService.I;
            if (service == null)
                return;

            RefreshPreviewRoot(previewRootA, service.GetEquippedDef(CompanionEquipSlot.SlotA));
            RefreshPreviewRoot(previewRootB, service.GetEquippedDef(CompanionEquipSlot.SlotB));
        }

        private void RefreshPreviewRoot(Transform root, CompanionDef def)
        {
            if (root == null)
                return;

            ClearChildren(root);

            if (def == null || def.uiPrefab == null)
                return;

            Instantiate(def.uiPrefab, root);
        }

        private void EquipIntoFirstAvailableSlot(string companionId)
        {
            var service = CompanionService.I;
            if (service == null)
                return;

            if (string.IsNullOrEmpty(service.GetEquippedId(CompanionEquipSlot.SlotA)))
            {
                service.Equip(companionId, CompanionEquipSlot.SlotA, immediateSave: true);
                return;
            }

            if (string.IsNullOrEmpty(service.GetEquippedId(CompanionEquipSlot.SlotB)))
            {
                service.Equip(companionId, CompanionEquipSlot.SlotB, immediateSave: true);
                return;
            }

            service.Equip(companionId, CompanionEquipSlot.SlotA, immediateSave: true);
        }

        private void ClearCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    Destroy(spawnedCards[i].gameObject);
            }

            spawnedCards.Clear();
        }

        private void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}