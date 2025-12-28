using System;
using UnityEngine;
using GameCore.Items;
using IDosGames;

namespace GameCore
{
    /// <summary>
    /// Global runtime singleton that stores current PlayerState and exposes game actions:
    /// - currencies, level, chests
    /// - equipment operations
    /// - autosave (local + server)
    ///
    /// Lifecycle:
    /// - AutoCreate() ensures a persistent GameInstance exists before any scene loads.
    /// - Awake() performs "fast bootstrap" from local/default for instant UI.
    /// - After login + UserDataService.RequestUserAllData() completes,
    ///   call OnAuthorizedAndDataReady() to load the server cached save.
    ///
    /// Events:
    /// - StateChanged: fired when any part of state changes (currencies, level, etc.)
    /// - EquipmentChanged: fired when equipment changes specifically (slot UI refresh)
    /// </summary>
    public class GameInstance : MonoBehaviour
    {
        public static GameInstance I { get; private set; }

        /// <summary>
        /// Current runtime state. Always call EnsureValid() after loading.
        /// </summary>
        public PlayerState State { get; private set; }

        // Fired when any state values changed (currencies/level/chests/skin/etc.)
        public event Action<PlayerState> StateChanged;

        // Fired when equipment changed (to refresh slot UI/stats)
        public event Action EquipmentChanged;

        [Header("Autosave Local")]
        [SerializeField] private bool localAutosave = true;
        [SerializeField] private float localInterval = 10f;

        [Header("Autosave Server")]
        [SerializeField] private bool serverAutosave = false; // enable after login
        [SerializeField] private float serverInterval = 60f;

        // Dirty means state was changed and should be saved later.
        private bool _dirty;

        // Timers for autosave intervals
        private float _localTimer;
        private float _serverTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (I != null) return;

            var go = new GameObject("[GameInstance]");
            go.AddComponent<GameInstance>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            // Singleton enforcement
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);

            // Fast bootstrap for UI: load local save (or default)
            // This allows the UI to show something immediately even before login.
            ApplyState(SaveSystem.LoadLocalOrDefault(), notify: true);

            _dirty = false;
            _localTimer = 0f;
            _serverTimer = 0f;

            Debug.Log("[GameInstance] Ready (local bootstrap)");
        }

        private void Update()
        {
            if (!_dirty) return;

            // Local autosave loop
            if (localAutosave)
            {
                _localTimer += Time.unscaledDeltaTime;
                if (_localTimer >= localInterval)
                    SaveLocalNow();
            }

            // Server autosave loop
            if (serverAutosave)
            {
                _serverTimer += Time.unscaledDeltaTime;
                if (_serverTimer >= serverInterval)
                    SaveServerNow();
            }
        }

        // ===================== APPLY / LOGIN =====================

        /// <summary>
        /// Call AFTER login, when GetUserAllData already completed (server cache is filled).
        /// Server -> source of truth, fallback: local/default.
        ///
        /// If you want "newer save wins", modify SaveSystem to compare LastSavedUnix.
        /// </summary>
        public void OnAuthorizedAndDataReady(bool enableServerAutosave = true)
        {
            var loaded = SaveSystem.LoadServerOrLocalOrDefault();
            ApplyState(loaded, notify: true);

            // Write local copy after server load (for quick startup next time)
            SaveSystem.SaveLocal(State);

            // Enable server autosaves only when the user is authorized
            serverAutosave = enableServerAutosave;

            Debug.Log($"[GameInstance] Authorized: state applied. Gold={State.Gold} Level={State.Level} ChestLv={State.ChestLevel} Last={State.LastSavedUnix}");
        }

        /// <summary>
        /// Applies a new state instance and optionally triggers UI refresh events.
        /// Also resets autosave timers and dirty flag.
        /// </summary>
        private void ApplyState(PlayerState newState, bool notify)
        {
            State = newState ?? PlayerState.CreateDefault();
            State.EnsureValid();

            _dirty = false;
            _localTimer = 0f;
            _serverTimer = 0f;

            if (notify)
            {
                StateChanged?.Invoke(State);
                EquipmentChanged?.Invoke();
            }
        }

        // ===================== SAVE =====================

        /// <summary>
        /// Marks state as dirty so autosave can persist changes.
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        public void SaveLocalNow()
        {
            SaveSystem.SaveLocal(State);
            _dirty = false;
            _localTimer = 0f;
        }

        public void SaveServerNow()
        {
            SaveSystem.SaveServer(State);
            _serverTimer = 0f;
        }

        public void SaveAllNow()
        {
            SaveLocalNow();
            SaveServerNow();
        }

        // ===================== CORE MUTATIONS =====================

        /// <summary>
        /// Common helper:
        /// - mark dirty
        /// - notify StateChanged (so UI can update)
        /// </summary>
        private void Touch(bool notify = true)
        {
            MarkDirty();
            if (notify) StateChanged?.Invoke(State);
        }

        public void AddGold(long amount, bool immediateSave = false)
        {
            if (State == null) return;
            State.Gold += amount;
            Touch();
            if (immediateSave) SaveAllNow();
        }

        public void AddGems(int amount, bool immediateSave = false)
        {
            if (State == null) return;
            State.Gems += amount;
            Touch();
            if (immediateSave) SaveAllNow();
        }

        public bool SpendGems(int amount, bool immediateSave = false)
        {
            if (State == null) return false;
            if (amount <= 0) return true;
            if (State.Gems < amount) return false;

            State.Gems -= amount;
            Touch();
            if (immediateSave) SaveAllNow();
            return true;
        }

        public void AddChests(int amount, bool immediateSave = false)
        {
            if (State == null) return;
            State.Chests = Mathf.Max(0, State.Chests + amount);
            Touch();
            if (immediateSave) SaveAllNow();
        }

        public bool SpendChest(int amount = 1, bool immediateSave = false)
        {
            if (State == null) return false;
            if (amount <= 0) return true;
            if (State.Chests < amount) return false;

            State.Chests -= amount;
            Touch();
            if (immediateSave) SaveAllNow();
            return true;
        }

        public void LevelUp(bool immediateSave = false)
        {
            if (State == null) return;
            State.Level += 1;
            Touch();
            if (immediateSave) SaveAllNow();
        }

        public void SetSkin(string skinId, bool immediateSave = false)
        {
            if (State == null) return;
            State.SelectedSkinId = string.IsNullOrWhiteSpace(skinId) ? "default" : skinId;
            Touch();
            if (immediateSave) SaveAllNow();
        }

        public void SetChestLevel(int level, bool immediateSave = false)
        {
            if (State == null) return;
            State.ChestLevel = Mathf.Max(1, level);
            Touch();
            if (immediateSave) SaveAllNow();
        }

        // ===================== EQUIPMENT =====================

        /// <summary>
        /// Returns itemId for slot or "" if empty.
        /// </summary>
        public string GetEquippedId(EquipSlot slot)
            => State?.GetEquippedId(slot) ?? "";

        /// <summary>
        /// Returns ItemDef for equipped itemId (via ItemDatabase) or null if empty/not found.
        /// </summary>
        public ItemDef GetEquippedDef(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            return string.IsNullOrWhiteSpace(id) ? null : ItemDatabase.I.GetById(id);
        }

        public bool IsSlotEmpty(EquipSlot slot)
            => string.IsNullOrWhiteSpace(GetEquippedId(slot));

        /// <summary>
        /// Equip itemId into slot (overwrites current).
        /// Validates:
        /// - item exists in DB
        /// - item.Slot matches target slot
        ///
        /// Also triggers EquipmentChanged and StateChanged.
        /// </summary>
        public bool EquipItem(EquipSlot slot, string itemId, bool immediateSave = false)
        {
            if (State == null) return false;

            var def = ItemDatabase.I ? ItemDatabase.I.GetById(itemId) : null;
            if (def == null)
            {
                Debug.LogError($"[GameInstance] EquipItem failed: itemId not found: {itemId}");
                return false;
            }

            if (def.Slot != slot)
            {
                Debug.LogError($"[GameInstance] EquipItem failed: slot mismatch. item={def.Id} itemSlot={def.Slot} targetSlot={slot}");
                return false;
            }

            State.SetEquippedId(slot, itemId);

            MarkDirty();
            EquipmentChanged?.Invoke();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
            return true;
        }

        /// <summary>
        /// Clears a slot.
        /// </summary>
        public void Unequip(EquipSlot slot, bool immediateSave = false)
        {
            if (State == null) return;
            State.SetEquippedId(slot, "");

            MarkDirty();
            EquipmentChanged?.Invoke();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
        }

        /// <summary>
        /// Sell an item (does not require it to be equipped).
        /// Adds gems and saves optionally.
        ///
        /// NOTE:
        /// Currently you don't have "inventory ownership", so selling does not remove anything.
        /// Later you can add OwnedItems[] and remove from there.
        /// </summary>
        public void SellItem(ItemDef item, bool immediateSave = false)
        {
            if (State == null || item == null) return;

            State.Gems += item.SellGems;

            MarkDirty();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
        }

        /// <summary>
        /// Helper for chest flow:
        /// If target slot is empty -> equip automatically.
        /// If not empty -> returns false so UI can show comparison (Replace/Sell).
        /// </summary>
        public bool TryAutoEquipIfEmpty(ItemDef newItem, bool immediateSave = false)
        {
            if (State == null || newItem == null) return false;

            if (!IsSlotEmpty(newItem.Slot))
                return false;

            State.SetEquippedId(newItem.Slot, newItem.Id);

            MarkDirty();
            EquipmentChanged?.Invoke();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
            return true;
        }

        /// <summary>
        /// Manual event trigger if some external system changed the state.
        /// Use carefully; prefer calling methods that already call Touch()/MarkDirty().
        /// </summary>
        public void NotifyStateChangedExternal()
        {
            StateChanged?.Invoke(State);
        }

        public void DevResetProgress(bool enableServerAutosave = true)
        {
            var fresh = PlayerState.CreateDefault();
            ApplyState(fresh, notify: true);

            SaveSystem.DeleteLocal();
            SaveAllNow();

            serverAutosave = enableServerAutosave;

            Debug.Log("[GameInstance] DEV RESET DONE (local+server overwritten)");
        }
    }
}








