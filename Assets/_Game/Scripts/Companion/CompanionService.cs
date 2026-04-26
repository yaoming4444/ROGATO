using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Companions
{
    public enum CompanionEquipSlot
    {
        SlotA = 0,
        SlotB = 1
    }

    public class CompanionService : MonoBehaviour
    {
        public static CompanionService I { get; private set; }

        [SerializeField] private CompanionDatabase database;

        public event Action OnChanged;

        public CompanionDatabase Database => database;

        private GameInstance Game => GameInstance.I;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;

            if (Game != null)
                Game.StateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (Game != null)
                Game.StateChanged -= HandleStateChanged;

            if (I == this)
                I = null;
        }

        private void HandleStateChanged(PlayerState _)
        {
            OnChanged?.Invoke();
        }

        public IReadOnlyList<CompanionDef> GetAllDefs()
        {
            if (database == null)
                return Array.Empty<CompanionDef>();

            return database.Companions;
        }

        public CompanionDef GetDef(string companionId)
        {
            if (database == null || string.IsNullOrWhiteSpace(companionId))
                return null;

            return database.GetById(companionId);
        }

        public OwnedCompanionState GetOwnedState(string companionId)
        {
            if (Game == null || Game.State == null || string.IsNullOrWhiteSpace(companionId))
                return null;

            var list = Game.State.ownedCompanions;
            if (list == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry == null) continue;
                if (string.Equals(entry.companionId, companionId, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        public bool IsUnlocked(string companionId)
        {
            var state = GetOwnedState(companionId);
            return state != null && state.unlocked;
        }

        public int GetLevel(string companionId)
        {
            var state = GetOwnedState(companionId);
            return state != null ? Mathf.Max(1, state.level) : 0;
        }

        public string GetEquippedId(CompanionEquipSlot slot)
        {
            if (Game == null || Game.State == null)
                return string.Empty;

            return slot == CompanionEquipSlot.SlotA
                ? Game.State.equippedCompanionSlotA
                : Game.State.equippedCompanionSlotB;
        }

        public CompanionDef GetEquippedDef(CompanionEquipSlot slot)
        {
            return GetDef(GetEquippedId(slot));
        }

        public bool IsEquipped(string companionId)
        {
            if (string.IsNullOrWhiteSpace(companionId) || Game == null || Game.State == null)
                return false;

            return string.Equals(Game.State.equippedCompanionSlotA, companionId, StringComparison.Ordinal) ||
                   string.Equals(Game.State.equippedCompanionSlotB, companionId, StringComparison.Ordinal);
        }

        public bool IsEquippedInSlot(string companionId, CompanionEquipSlot slot)
        {
            if (string.IsNullOrWhiteSpace(companionId))
                return false;

            return string.Equals(GetEquippedId(slot), companionId, StringComparison.Ordinal);
        }

        public bool Unlock(string companionId, bool immediateSave = false)
        {
            if (Game == null) return false;
            return Game.UnlockCompanion(companionId, immediateSave);
        }

        public bool Buy(string companionId, bool immediateSave = false)
        {
            var def = GetDef(companionId);
            if (def == null) return false;
            if (IsUnlocked(companionId)) return false;

            if (!Game.SpendGold(def.unlockCost, immediateSave: false))
                return false;

            return Game.UnlockCompanion(companionId, immediateSave);
        }

        public bool Equip(string companionId, CompanionEquipSlot slot, bool immediateSave = false)
        {
            if (Game == null) return false;
            return Game.EquipCompanion(companionId, slot, immediateSave);
        }

        public bool Unequip(CompanionEquipSlot slot, bool immediateSave = false)
        {
            if (Game == null) return false;
            return Game.UnequipCompanion(slot, immediateSave);
        }

        public bool ToggleEquip(string companionId, CompanionEquipSlot slot, bool immediateSave = false)
        {
            if (IsEquippedInSlot(companionId, slot))
                return Unequip(slot, immediateSave);

            return Equip(companionId, slot, immediateSave);
        }

        public bool Upgrade(string companionId, bool immediateSave = false)
        {
            var def = GetDef(companionId);
            var owned = GetOwnedState(companionId);

            if (def == null || owned == null || !owned.unlocked)
                return false;

            if (owned.level >= def.maxLevel)
                return false;

            int cost = GetUpgradeCost(companionId);
            if (!Game.SpendGold(cost, immediateSave: false))
                return false;

            return Game.UpgradeCompanion(companionId, 1, immediateSave);
        }

        public int GetUpgradeCost(string companionId)
        {
            var def = GetDef(companionId);
            var owned = GetOwnedState(companionId);

            if (def == null || owned == null)
                return int.MaxValue;

            int currentLevel = Mathf.Max(1, owned.level);
            return def.upgradeCostBase * currentLevel;
        }

        public bool CanUpgrade(string companionId)
        {
            var def = GetDef(companionId);
            var owned = GetOwnedState(companionId);

            if (def == null || owned == null || !owned.unlocked)
                return false;

            if (owned.level >= def.maxLevel)
                return false;

            return Game != null && Game.State != null && Game.State.Gold >= GetUpgradeCost(companionId);
        }
    }
}