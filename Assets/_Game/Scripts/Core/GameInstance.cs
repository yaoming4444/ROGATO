using System;
using UnityEngine;
using GameCore.Items;
using IDosGames;

namespace GameCore
{
    public class GameInstance : MonoBehaviour
    {
        public static GameInstance I { get; private set; }

        public PlayerState State { get; private set; }

        // Когда поменялось что-то в стейте (валюты/уровень/сундук/скин и тд)
        public event Action<PlayerState> StateChanged;

        // Когда поменялась экипировка (перерисовать слоты/статы)
        public event Action EquipmentChanged;

        [Header("Autosave Local")]
        [SerializeField] private bool localAutosave = true;
        [SerializeField] private float localInterval = 10f;

        [Header("Autosave Server")]
        [SerializeField] private bool serverAutosave = false; // включай после логина
        [SerializeField] private float serverInterval = 60f;

        private bool _dirty;
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

            // быстрый старт до авторизации: local/default
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
        /// Вызывать ПОСЛЕ логина, когда GetUserAllData уже отработал (кэш заполнен).
        /// Сервер -> источник истины, fallback: local/default.
        /// </summary>
        public void OnAuthorizedAndDataReady(bool enableServerAutosave = true)
        {
            var loaded = SaveSystem.LoadServerOrLocalOrDefault();
            ApplyState(loaded, notify: true);

            // сохраним локально как “кэш”
            SaveSystem.SaveLocal(State);

            serverAutosave = enableServerAutosave;

            Debug.Log($"[GameInstance] Authorized: state applied. Gold={State.Gold} Level={State.Level} ChestLv={State.ChestLevel} Last={State.LastSavedUnix}");
        }

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

        public string GetEquippedId(EquipSlot slot)
            => State?.GetEquippedId(slot) ?? "";

        public ItemDef GetEquippedDef(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            return string.IsNullOrWhiteSpace(id) ? null : ItemDatabase.I.GetById(id);
        }

        public bool IsSlotEmpty(EquipSlot slot)
            => string.IsNullOrWhiteSpace(GetEquippedId(slot));

        /// <summary>
        /// Экипировать itemId в слот. (перезапишет текущий предмет)
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
        /// Продать предмет (не обязан быть экипнут).
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
        /// Для твоего сундука: если слот пустой — автоэкип, иначе вернёт false (чтобы UI показал сравнение).
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

        public void NotifyStateChangedExternal()
        {
            StateChanged?.Invoke(State);
        }

    }
}







