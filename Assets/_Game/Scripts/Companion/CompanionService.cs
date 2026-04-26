using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IDosGames;

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

        [Header("Data")]
        [SerializeField] private CompanionDatabase database;

        [Header("Economy")]
        [SerializeField] private VirtualCurrencyID virtualCurrencyId = VirtualCurrencyID.CO;
        [SerializeField] private bool saveImmediatelyAfterPurchase = true;

        [Header("SDK Refresh Before Charge")]
        [Tooltip("Перед списанием сделать RequestUserInventory, чтобы баланс был актуальный")]
        [SerializeField] private bool refreshInventoryBeforeCharge = true;

        [Tooltip("После успешного списания сделать RequestUserInventory, чтобы currency bars/UI обновились")]
        [SerializeField] private bool refreshInventoryAfterCharge = true;

        [Tooltip("Таймаут ожидания InventoryUpdated")]
        [SerializeField] private float inventoryRefreshTimeoutSeconds = 3f;

        [Header("Debug / Temporary")]
        [SerializeField] private bool useLocalGoldFallbackForTesting = false;

        public event Action OnChanged;

        public CompanionDatabase Database => database;

        private GameInstance Game => GameInstance.I;

        // SDK charge flow
        private bool _chargeInFlight;
        private bool _sdkSpendFinished;
        private bool _sdkSpendSuccess;

        // inventory refresh flow
        private bool _waitingInventoryRefresh;
        private bool _inventoryRefreshFinished;
        private Coroutine _inventoryTimeoutCoroutine;

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

        private void OnEnable()
        {
            UserInventory.InventoryUpdated += OnInventoryAmountChanged;
        }

        private void OnDisable()
        {
            UserInventory.InventoryUpdated -= OnInventoryAmountChanged;

            CleanupServerCurrencyHandlers();
            CleanupInventoryRefreshHandlers();

            _chargeInFlight = false;
        }

        private void OnDestroy()
        {
            if (Game != null)
                Game.StateChanged -= HandleStateChanged;

            CleanupServerCurrencyHandlers();
            CleanupInventoryRefreshHandlers();

            if (I == this)
                I = null;
        }

        private void HandleStateChanged(PlayerState _)
        {
            OnChanged?.Invoke();
        }

        private void OnInventoryAmountChanged()
        {
            OnChanged?.Invoke();
        }

        // =========================
        // Data access
        // =========================

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

        // =========================
        // Purchase / unlock
        // =========================

        public bool IsPurchaseInFlight()
        {
            return _chargeInFlight;
        }

        public bool CanBuy(string companionId)
        {
            var def = GetDef(companionId);
            if (def == null || IsUnlocked(companionId))
                return false;

            if (useLocalGoldFallbackForTesting)
                return Game != null && Game.State != null && Game.State.Gold >= def.hardCurrencyUnlockCost;

            int currentAmount = UserInventory.GetVirtualCurrencyAmount(virtualCurrencyId);
            return currentAmount >= def.hardCurrencyUnlockCost;
        }

        public void Buy(string companionId, Action<bool> onComplete = null, bool immediateSave = true)
        {
            if (!gameObject.activeInHierarchy)
            {
                onComplete?.Invoke(false);
                return;
            }

            StartCoroutine(BuyCoroutine(companionId, onComplete, immediateSave));
        }

        private IEnumerator BuyCoroutine(string companionId, Action<bool> onComplete, bool immediateSave)
        {
            var def = GetDef(companionId);
            if (def == null)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (IsUnlocked(companionId))
            {
                onComplete?.Invoke(false);
                yield break;
            }

            bool chargeSuccess = false;
            yield return ChargeCurrencyCoroutine(def.hardCurrencyUnlockCost, success => chargeSuccess = success);

            if (!chargeSuccess)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            bool unlockResult = false;

            if (Game != null)
                unlockResult = Game.UnlockCompanion(companionId, immediateSave || saveImmediatelyAfterPurchase);

            if (unlockResult)
            {
                if (Game != null)
                    Game.RaiseStateChanged();

                OnChanged?.Invoke();
            }

            onComplete?.Invoke(unlockResult);
        }

        // =========================
        // Equip / unequip
        // =========================

        public bool Unlock(string companionId, bool immediateSave = false)
        {
            if (Game == null) return false;
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

        // =========================
        // Upgrade
        // =========================

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

        // =========================
        // Currency charge flow
        // =========================

        private IEnumerator ChargeCurrencyCoroutine(int amount, Action<bool> onComplete)
        {
            bool success = false;

            if (useLocalGoldFallbackForTesting)
            {
                success = Game != null && Game.SpendGold(amount, immediateSave: false);
                onComplete?.Invoke(success);
                yield break;
            }

            if (_chargeInFlight)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            if (refreshInventoryBeforeCharge)
            {
                BeginInventoryRefresh();

                while (!_inventoryRefreshFinished)
                    yield return null;
            }

            int current = UserInventory.GetVirtualCurrencyAmount(virtualCurrencyId);
            if (current < amount)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            CleanupServerCurrencyHandlers();

            _chargeInFlight = true;
            _sdkSpendFinished = false;
            _sdkSpendSuccess = false;

            UserInventory.SuccessSubtractVirtualCurrency += OnServerChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency += OnServerChargeError;

            UserInventory.SubtractVirtualCurrency(virtualCurrencyId, amount);

            while (!_sdkSpendFinished)
                yield return null;

            success = _sdkSpendSuccess;
            onComplete?.Invoke(success);
        }

        // =========================
        // SDK charge callbacks
        // =========================

        private void OnServerChargeSuccess()
        {
            CleanupServerCurrencyHandlers();

            _chargeInFlight = false;
            _sdkSpendSuccess = true;
            _sdkSpendFinished = true;

            if (refreshInventoryAfterCharge)
                UserDataService.RequestUserInventory();

            OnChanged?.Invoke();
        }

        private void OnServerChargeError()
        {
            CleanupServerCurrencyHandlers();

            _chargeInFlight = false;
            _sdkSpendSuccess = false;
            _sdkSpendFinished = true;

            OnChanged?.Invoke();
        }

        private void CleanupServerCurrencyHandlers()
        {
            UserInventory.SuccessSubtractVirtualCurrency -= OnServerChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency -= OnServerChargeError;
        }

        // =========================
        // Inventory refresh
        // =========================

        private void BeginInventoryRefresh()
        {
            CleanupInventoryRefreshHandlers();

            _waitingInventoryRefresh = true;
            _inventoryRefreshFinished = false;

            UserInventory.InventoryUpdated += OnInventoryUpdated;
            UserDataService.RequestUserInventory();

            _inventoryTimeoutCoroutine = StartCoroutine(InventoryRefreshTimeoutCoroutine());
        }

        private void OnInventoryUpdated()
        {
            if (!_waitingInventoryRefresh)
                return;

            CleanupInventoryRefreshHandlers();
            _inventoryRefreshFinished = true;
        }

        private IEnumerator InventoryRefreshTimeoutCoroutine()
        {
            float timer = 0f;

            while (_waitingInventoryRefresh && timer < inventoryRefreshTimeoutSeconds)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_waitingInventoryRefresh)
                yield break;

            Debug.LogWarning("[CompanionService] Inventory refresh timeout. Continue with current cache.");

            CleanupInventoryRefreshHandlers();
            _inventoryRefreshFinished = true;
        }

        private void CleanupInventoryRefreshHandlers()
        {
            _waitingInventoryRefresh = false;

            UserInventory.InventoryUpdated -= OnInventoryUpdated;

            if (_inventoryTimeoutCoroutine != null)
            {
                StopCoroutine(_inventoryTimeoutCoroutine);
                _inventoryTimeoutCoroutine = null;
            }
        }
    }
}