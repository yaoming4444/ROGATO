using GameCore.Items;
using GameCore.Progression;
using GameCore.Visual;
using IDosGames;
using LayerLab.ArtMaker; // <-- PartsType
using System;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Global runtime singleton that stores current PlayerState and exposes game actions:
    /// - currencies, level, chests
    /// - equipment operations
    /// - autosave (local + server)
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

        // Chest
        public bool AutoSellEnabled => State != null && State.AutoSellEnabled;

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

        /// <summary>
        /// Форснуть обновление всех подписчиков (оба PartsManagerStateBinder в сцене).
        /// Используй, если ты сделал несколько изменений без notify, а потом хочешь 1 раз обновить UI.
        /// </summary>
        public void RaiseStateChanged()
        {
            if (State == null) return;
            MarkDirty();
            StateChanged?.Invoke(State);
        }

        // ===================== VISUAL MUTATIONS (ВАЖНО ДЛЯ 2 СКЕЛЕТОНОВ) =====================

        /// <summary>
        /// Меняет конкретный визуальный слот в State (visual_*).
        /// skinNameOrEmpty:
        /// - "" (пусто) = по дефолту "снять" (в биндере ты сделаешь EquipParts(type, -1))
        /// - "top/top_c_10" и т.п. = надеть
        /// notify:
        /// - true = сразу поднимет StateChanged (оба скелета обновятся)
        /// - false = удобно для batch, потом вызови RaiseStateChanged()
        /// </summary>
        public void SetVisual(PartsType type, string skinNameOrEmpty, bool notify = true)
        {
            if (State == null) return;

            skinNameOrEmpty ??= "";

            switch (type)
            {
                case PartsType.Back: State.visual_back = skinNameOrEmpty; break;
                case PartsType.Beard: State.visual_beard = skinNameOrEmpty; break;
                case PartsType.Boots: State.visual_boots = skinNameOrEmpty; break;
                case PartsType.Bottom: State.visual_bottom = skinNameOrEmpty; break;
                case PartsType.Brow: State.visual_brow = skinNameOrEmpty; break;
                case PartsType.Eyes: State.visual_eyes = skinNameOrEmpty; break;
                case PartsType.Gloves: State.visual_gloves = skinNameOrEmpty; break;

                case PartsType.Hair_Short: State.visual_hair_short = skinNameOrEmpty; break;
                case PartsType.Hair_Hat: State.visual_hair_hat = skinNameOrEmpty; break;
                case PartsType.Helmet: State.visual_helmet = skinNameOrEmpty; break;

                case PartsType.Mouth: State.visual_mouth = skinNameOrEmpty; break;
                case PartsType.Eyewear: State.visual_eyewear = skinNameOrEmpty; break;

                case PartsType.Gear_Left: State.visual_gear_left = skinNameOrEmpty; break;
                case PartsType.Gear_Right: State.visual_gear_right = skinNameOrEmpty; break;

                case PartsType.Top: State.visual_top = skinNameOrEmpty; break;
                case PartsType.Skin: State.visual_skin = skinNameOrEmpty; break;

                default:
                    // None / unsupported
                    return;
            }

            MarkDirty();
            if (notify) StateChanged?.Invoke(State);
        }

        public void ClearVisual(PartsType type, bool notify = true)
        {
            SetVisual(type, "", notify);
        }

        /// <summary>
        /// Пример пачки: меняем несколько частей и 1 раз обновляем.
        /// </summary>
        public void SetVisualBatch(string top = null, string boots = null, string helmet = null, bool notify = true)
        {
            if (State == null) return;

            if (top != null) State.visual_top = top;
            if (boots != null) State.visual_boots = boots;
            if (helmet != null) State.visual_helmet = helmet;

            MarkDirty();
            if (notify) StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Смена цвета кожи (ты уже хранишь RGBA в PlayerState).
        /// Биндер потом должен вызвать partsManager.ChangeSkinColor(st.GetSkinColor32()).
        /// </summary>
        public void SetSkinColor(Color32 c, bool notify = true)
        {
            if (State == null) return;

            State.SetSkinColor32(c);
            MarkDirty();
            if (notify) StateChanged?.Invoke(State);
        }

        // ===================== ECONOMY / PROGRESSION =====================

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

        public void SetAutoSellEnabled(bool enabled, bool immediateSave = false)
        {
            if (State == null) return;

            State.AutoSellEnabled = enabled;
            MarkDirty();
            StateChanged?.Invoke(State);

            if (immediateSave) SaveAllNow();
        }

        /// <summary>
        /// Adds EXP to the player and auto-levels up using LevelProgression thresholds.
        /// </summary>
        public void AddExp(int amount, bool immediateSave = false)
        {
            if (State == null) return;
            if (amount <= 0) return;

            State.Exp += amount;

            if (levelProgression != null)
            {
                while (levelProgression.CanLevelUp(State.Level, State.Exp))
                    State.Level += 1;
            }

            Touch();
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

        /// <summary>
        /// Старое имя оставил, чтобы твои старые вызовы не поломались.
        /// </summary>
        public void NotifyStateChangedExternal()
        {
            RaiseStateChanged();
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










