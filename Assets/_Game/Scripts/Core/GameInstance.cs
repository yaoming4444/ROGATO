using GameCore.Items;
using GameCore.Progression;
using IDosGames;
using System;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Global runtime singleton that stores current PlayerState and exposes game actions:
    /// - currencies, level, chests
    /// - equipment operations
    /// - autosave (local + server)
    ///
    /// NEW:
    /// - Loads LevelProgression from Resources (because GameInstance is runtime-created)
    /// - AddExp() now uses LevelProgression to auto level-up with thresholds
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

        [Header("Progression")]
        [Tooltip("If null (runtime-created instance), it will be loaded from Resources/LevelProgression.asset")]
        [SerializeField] private LevelProgression levelProgression;

        private const string LevelProgResourcePath = "LevelProgression";

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

            // Auto-load LevelProgression because GameInstance is runtime-created (no Inspector refs)
            if (levelProgression == null)
            {
                levelProgression = Resources.Load<LevelProgression>(LevelProgResourcePath);
                if (levelProgression == null)
                    Debug.LogError($"[GameInstance] LevelProgression not found: Resources/{LevelProgResourcePath}.asset");
            }

            // Fast bootstrap for UI: load local save (or default)
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
        /// </summary>
        public void OnAuthorizedAndDataReady(bool enableServerAutosave = true)
        {
            var loaded = SaveSystem.LoadServerOrLocalOrDefault();
            ApplyState(loaded, notify: true);

            // Write local copy after server load (for quick startup next time)
            SaveSystem.SaveLocal(State);

            // Enable server autosaves only when the user is authorized
            serverAutosave = enableServerAutosave;

            Debug.Log($"[GameInstance] Authorized: state applied. Gold={State.Gold} Level={State.Level} Exp={State.Exp} ChestLv={State.ChestLevel} Last={State.LastSavedUnix}");
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

        public bool TryUpgradeChest(ChestDropTable table, bool immediateSave = false)
        {
            if (State == null || table == null) return false;

            int cur = Mathf.Max(1, State.ChestLevel);
            if (table.IsMaxLevel(cur)) return false;

            int next = cur + 1;
            int cost = table.GetUpgradeCostGems(next);

            if (!SpendGems(cost, immediateSave: false))
                return false;

            State.ChestLevel = next;

            Touch();              // dirty + StateChanged
            if (immediateSave) SaveAllNow();
            return true;
        }

        /// <summary>
        /// Adds EXP to the player and auto-levels up using LevelProgression thresholds.
        /// Behavior:
        /// - Exp is TOTAL exp (not "exp inside level")
        /// - When total exp reaches threshold for next level -> Level increments
        /// - Exp is NOT reduced on level up (like many idle games)
        /// </summary>
        public void AddExp(int amount, bool immediateSave = false)
        {
            if (State == null) return;
            if (amount <= 0) return;

            State.Exp += amount;

            // Auto-level-up via LevelProgression (if exists)
            if (levelProgression != null)
            {
                // while we can level up
                while (levelProgression.CanLevelUp(State.Level, State.Exp))
                {
                    State.Level += 1;
                }
            }

            Touch(); // MarkDirty + StateChanged

            if (immediateSave) SaveAllNow();
        }

        // ===================== EQUIPMENT =====================

        public string GetEquippedId(EquipSlot slot)
            => State?.GetEquippedId(slot) ?? "";

        public ItemDef GetEquippedDef(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            if (string.IsNullOrWhiteSpace(id)) return null;

            var db = ItemDatabase.I;
            if (db == null) return null;

            return db.GetById(id);
        }

        public bool IsSlotEmpty(EquipSlot slot)
            => string.IsNullOrWhiteSpace(GetEquippedId(slot));

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

        public void Unequip(EquipSlot slot, bool immediateSave = false)
        {
            if (State == null) return;
            State.SetEquippedId(slot, "");

            MarkDirty();
            EquipmentChanged?.Invoke();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
        }

        public void SellItem(ItemDef item, bool immediateSave = false)
        {
            if (State == null || item == null) return;

            State.Gems += item.SellGems;

            MarkDirty();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
        }

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









