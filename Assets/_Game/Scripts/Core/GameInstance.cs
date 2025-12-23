using UnityEngine;
using IDosGames;

namespace GameCore
{
    public class GameInstance : MonoBehaviour
    {
        public static GameInstance I { get; private set; }
        public PlayerState State { get; private set; }

        [Header("Autosave Local")]
        [SerializeField] private bool localAutosave = true;
        [SerializeField] private float localInterval = 10f;

        [Header("Autosave Server")]
        [SerializeField] private bool serverAutosave = false;   // включай после логина если хочешь
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

            // Быстрый старт (оффлайн / без логина)
            State = SaveSystem.LoadLocalOrDefault();

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

        // ---------- MAIN ENTRY AFTER LOGIN ----------
        /// <summary>
        /// Вызывать ПОСЛЕ успешного логина, когда GetUserAllData уже отработал (кэш заполнен).
        /// Сервер становится источником истины (fallback: локал/дефолт).
        /// </summary>
        public void OnAuthorizedAndDataReady(bool enableServerAutosave = false)
        {
            State = SaveSystem.LoadServerOrLocalOrDefault();

            // фиксируем как локальную базу, чтобы при следующем старте было ок
            SaveSystem.SaveLocal(State);

            _dirty = false;
            _localTimer = 0f;
            _serverTimer = 0f;

            serverAutosave = enableServerAutosave;

            Debug.Log($"[GameInstance] Authorized: state applied. Gold={State.Gold} Level={State.Level} Last={State.LastSavedUnix}");
        }

        // ---------- SAVE API ----------
        public void MarkDirty() => _dirty = true;

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

        // ---------- OPTIONAL: manual apply cached ----------
        public bool ApplyServerCached(bool preferNewest = true)
        {
            if (!SaveSystem.TryLoadServerCached(out var serverState))
                return false;

            if (!preferNewest || State == null || serverState.LastSavedUnix >= State.LastSavedUnix)
            {
                State = serverState;
                SaveSystem.SaveLocal(State);

                _dirty = false;
                _localTimer = 0f;
                _serverTimer = 0f;

                Debug.Log($"[GameInstance] Applied server cached. Gold={State.Gold} Level={State.Level} Last={State.LastSavedUnix}");
                return true;
            }

            Debug.Log("[GameInstance] Server cached older than local. Keep local.");
            return false;
        }

        // тестовые изменения
        public void AddGold(long amount) { State.Gold += amount; MarkDirty(); }
        public void LevelUp() { State.Level += 1; MarkDirty(); }
    }
}




