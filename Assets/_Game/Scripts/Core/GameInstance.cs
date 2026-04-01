using GameCore.Progression;
using GameCore.Visual;
using IDosGames;
using LayerLab.ArtMaker;
using System;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Global runtime singleton that stores current PlayerState and exposes:
    /// - progression
    /// - currencies
    /// - visual equipment state
    /// - autosave (local + server)
    /// </summary>
    public class GameInstance : MonoBehaviour
    {
        public static GameInstance I { get; private set; }

        /// <summary>
        /// Current runtime state. Always call EnsureValid() after loading.
        /// </summary>
        public PlayerState State { get; private set; }

        // Fired when any state values changed.
        public event Action<PlayerState> StateChanged;

        [Header("Autosave Local")]
        [SerializeField] private bool localAutosave = true;
        [SerializeField] private float localInterval = 10f;

        [Header("Autosave Server")]
        [SerializeField] private bool serverAutosave = true; // enable after login
        [SerializeField] private float serverInterval = 30f;

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
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);

            if (levelProgression == null)
            {
                levelProgression = Resources.Load<LevelProgression>(LevelProgResourcePath);
                if (levelProgression == null)
                    Debug.LogError($"[GameInstance] LevelProgression not found: Resources/{LevelProgResourcePath}.asset");
            }

            ApplyState(SaveSystem.LoadLocalOrDefault(), notify: true);

            _dirty = false;
            _localTimer = 0f;
            _serverTimer = 0f;

            Debug.Log("[GameInstance] Ready (local bootstrap)");
        }

        private void Update()
        {
            if (!_dirty) return;

            if (localAutosave)
            {
                _localTimer += Time.unscaledDeltaTime;
                if (_localTimer >= localInterval)
                    SaveLocalNow();
            }

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

            serverAutosave = enableServerAutosave;

            Debug.Log($"[GameInstance] Authorized: state applied. Gold={State.Gold} Level={State.Level} Exp={State.Exp} Last={State.LastSavedUnix}");
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
                StateChanged?.Invoke(State);
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
        /// - notify StateChanged
        /// </summary>
        private void Touch(bool notify = true)
        {
            MarkDirty();
            if (notify)
                StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Force-refresh all subscribers.
        /// </summary>
        public void RaiseStateChanged()
        {
            if (State == null) return;

            MarkDirty();
            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// External helper for systems that only need to refresh current state listeners.
        /// </summary>
        public void NotifyStateChangedExternal()
        {
            RaiseStateChanged();
        }

        // ===================== VISUAL MUTATIONS =====================

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
                    return;
            }

            MarkDirty();
            if (notify)
                StateChanged?.Invoke(State);
        }

        public void ClearVisual(PartsType type, bool notify = true)
        {
            SetVisual(type, "", notify);
        }

        public void SetVisualBatch(string top = null, string boots = null, string helmet = null, bool notify = true)
        {
            if (State == null) return;

            if (top != null) State.visual_top = top;
            if (boots != null) State.visual_boots = boots;
            if (helmet != null) State.visual_helmet = helmet;

            MarkDirty();
            if (notify)
                StateChanged?.Invoke(State);
        }

        public void SetSkinColor(Color32 c, bool notify = true)
        {
            if (State == null) return;

            State.SetSkinColor32(c);
            MarkDirty();

            if (notify)
                StateChanged?.Invoke(State);
        }

        // ===================== ECONOMY / PROGRESSION =====================

        public void AddGold(long amount, bool immediateSave = false)
        {
            if (State == null) return;

            State.Gold += amount;
            Touch();

            if (immediateSave)
                SaveAllNow();
        }

        public bool SpendGold(long amount, bool immediateSave = false)
        {
            if (State == null) return false;
            if (amount <= 0) return true;
            if (State.Gold < amount) return false;

            State.Gold -= amount;
            Touch();

            if (immediateSave)
                SaveAllNow();

            return true;
        }

        public void AddGems(int amount, bool immediateSave = false)
        {
            if (State == null) return;

            State.Gems += amount;
            Touch();

            if (immediateSave)
                SaveAllNow();
        }

        public bool SpendGems(int amount, bool immediateSave = false)
        {
            if (State == null) return false;
            if (amount <= 0) return true;
            if (State.Gems < amount) return false;

            State.Gems -= amount;
            Touch();

            if (immediateSave)
                SaveAllNow();

            return true;
        }

        public void LevelUp(bool immediateSave = false)
        {
            if (State == null) return;

            State.Level += 1;
            Touch();

            if (immediateSave)
                SaveAllNow();
        }

        public void AddExp(int amount, bool immediateSave = false)
        {
            if (State == null) return;

            State.Exp += amount;

            if (levelProgression != null)
            {
                while (levelProgression.CanLevelUp(State.Level, State.Exp))
                    State.Level += 1;
            }

            Touch();

            if (immediateSave)
                SaveAllNow();
        }

        public void SetSkin(string skinId, bool immediateSave = false)
        {
            if (State == null) return;

            State.SelectedSkinId = string.IsNullOrWhiteSpace(skinId) ? "default" : skinId;
            Touch();

            if (immediateSave)
                SaveAllNow();
        }

        // ===================== VISUAL EQUIPMENT SLOT LEVEL =====================

        public bool UpgradeVisualSlot(EquipmentType slot, int delta = 1, bool saveLocal = true)
        {
            var st = State;
            if (st == null) return false;

            bool changed = st.UpgradeVisualSlotLevel(slot, delta);
            if (!changed) return false;

            RaiseStateChanged();

            if (saveLocal)
                SaveLocalNow();

            return true;
        }

        public int GetVisualSlotLevel(EquipmentType slot)
        {
            var st = State;
            return st != null ? st.GetVisualSlotLevel(slot) : 1;
        }

        public void SetVisualSlotLevel(EquipmentType slot, int level, bool immediateSave = false)
        {
            if (State == null) return;

            State.SetVisualSlotLevel(slot, level);
            Touch();

            if (immediateSave)
                SaveAllNow();
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