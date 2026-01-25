using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using IDosGames;

namespace GameCore
{
    /// <summary>
    /// Save/Load utilities for PlayerState:
    /// - Local JSON file (persistentDataPath)
    /// - Server custom data via iDosGames SDK (UpdateCustomUserData / cached read)
    ///
    /// IMPORTANT:
    /// - Server load uses "cached" custom user data.
    ///   The cache is updated only after UserDataService.RequestUserAllData().
    /// - Make sure you use the SAME KEY for saving and reading cached data.
    /// </summary>
    public static class SaveSystem
    {
        // Server custom data key (string).
        public const string ServerKey = "player_state_v1";

        // Local file name in Application.persistentDataPath.
        private const string LocalFileName = "player_state.json";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static string LocalPath => Path.Combine(Application.persistentDataPath, LocalFileName);

        // =========================
        // SERVER SAVE THROTTLING
        // =========================

        // Tune these:
        private const float DefaultDebounceDelay = 1.0f; // wait after last change
        private const float DefaultMinInterval = 3.0f;   // do not send more often than this

        private static float _debounceDelay = DefaultDebounceDelay;
        private static float _minInterval = DefaultMinInterval;

        private static bool _pendingServerSave;
        private static string _pendingServerJson;
        private static float _pendingDueRealtime;

        private static float _lastServerSendRealtime = -999f;
        private static string _lastSentJson; // prevent duplicate sends

        private static SaveSystemRuntime _runtime;

        // -------- JSON --------

        public static string ToJson(PlayerState state, bool pretty = false)
        {
            return pretty
                ? JsonConvert.SerializeObject(state, Formatting.Indented, JsonSettings)
                : JsonConvert.SerializeObject(state, Formatting.None, JsonSettings);
        }

        public static bool TryFromJson(string json, out PlayerState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                state = JsonConvert.DeserializeObject<PlayerState>(json, JsonSettings);
                return state != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] JSON parse error: {e}\njson={json}");
                return false;
            }
        }

        // -------- LOCAL --------

        public static PlayerState LoadLocalOrDefault()
        {
            if (!File.Exists(LocalPath))
            {
                Debug.Log($"[SaveSystem] No local save -> default. Path: {LocalPath}");
                return PlayerState.CreateDefault();
            }

            try
            {
                var json = File.ReadAllText(LocalPath, Encoding.UTF8);
                if (TryFromJson(json, out var state))
                {
                    Debug.Log($"[SaveSystem] Loaded local <- {LocalPath}");
                    return state;
                }

                Debug.LogWarning("[SaveSystem] Local save invalid -> default");
                return PlayerState.CreateDefault();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load local exception: {e}");
                return PlayerState.CreateDefault();
            }
        }

        /// <summary>
        /// Current behavior:
        /// - Prefer server cached data (if exists)
        /// - Otherwise fallback to local/default
        ///
        /// If you want "newer save wins", add comparison by LastSavedUnix.
        /// </summary>
        public static PlayerState LoadServerOrLocalOrDefault()
        {
            // 1) server (from cache after GetUserAllData)
            if (TryLoadServerCached(out var server))
            {
                Debug.Log("[SaveSystem] Loaded state from SERVER (cached)");
                return server;
            }

            // 2) local fallback
            var local = LoadLocalOrDefault();
            Debug.Log("[SaveSystem] Server empty -> fallback to LOCAL/DEFAULT");
            return local;
        }

        public static void SaveLocal(PlayerState state)
        {
            if (state == null)
            {
                Debug.LogError("[SaveSystem] SaveLocal: state is null");
                return;
            }

            // Update save timestamp every time we write
            state.LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                var json = ToJson(state, pretty: true);
                File.WriteAllText(LocalPath, json, Encoding.UTF8);
                Debug.Log($"[SaveSystem] Saved local -> {LocalPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] SaveLocal exception: {e}");
            }
        }

        public static void DeleteLocal()
        {
            try
            {
                if (File.Exists(LocalPath))
                    File.Delete(LocalPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] DeleteLocal exception: {e}");
            }
        }

        // -------- SERVER SAVE (iDos) --------

        /// <summary>
        /// NEW DEFAULT: debounced/throttled server save.
        /// This prevents spamming UpdateCustomUserData when many small actions happen (auto-open etc).
        /// </summary>
        public static void SaveServer(PlayerState state)
        {
            SaveServerDebounced(state, DefaultDebounceDelay, DefaultMinInterval);
        }

        /// <summary>
        /// Debounced server save:
        /// - waits debounceDelay after the last call
        /// - sends at most once per minInterval
        /// - always sends the latest state (coalesced)
        /// </summary>
        public static void SaveServerDebounced(PlayerState state, float debounceDelay, float minInterval)
        {
            if (state == null)
            {
                Debug.LogError("[SaveSystem] SaveServerDebounced: state is null");
                return;
            }

            EnsureRuntime();

            _debounceDelay = Mathf.Max(0.05f, debounceDelay);
            _minInterval = Mathf.Max(0.2f, minInterval);

            // Update timestamp before writing
            state.LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var json = ToJson(state, pretty: false);

            // if identical to last sent and nothing pending -> no need
            if (!_pendingServerSave && _lastSentJson == json)
                return;

            _pendingServerSave = true;
            _pendingServerJson = json;

            // push due time forward (classic debounce)
            _pendingDueRealtime = Time.realtimeSinceStartup + _debounceDelay;
        }

        /// <summary>
        /// Immediate server save (use ONLY when you really need it right now).
        /// </summary>
        public static void SaveServerImmediate(PlayerState state)
        {
            if (state == null)
            {
                Debug.LogError("[SaveSystem] SaveServerImmediate: state is null");
                return;
            }

            // Update timestamp before writing
            state.LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var json = ToJson(state, pretty: false);
            DoSendServer(json);
        }

        /// <summary>
        /// Forces sending pending save right now (if any).
        /// Call this on app pause/quit.
        /// </summary>
        public static void FlushServerNow()
        {
            if (!_pendingServerSave) return;
            EnsureRuntime();
            TrySendIfDue(force: true);
        }

        private static void EnsureRuntime()
        {
            if (_runtime != null) return;

            var go = new GameObject("[SaveSystemRuntime]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runtime = go.AddComponent<SaveSystemRuntime>();
        }

        private static void Tick()
        {
            if (!_pendingServerSave) return;
            TrySendIfDue(force: false);
        }

        private static void TrySendIfDue(bool force)
        {
            if (!_pendingServerSave) return;

            float now = Time.realtimeSinceStartup;

            // throttle: don't send more often than minInterval unless forced
            if (!force && (now - _lastServerSendRealtime) < _minInterval)
                return;

            // debounce due time
            if (!force && now < _pendingDueRealtime)
                return;

            var json = _pendingServerJson;

            // clear pending first to avoid loops if send triggers more saves
            _pendingServerSave = false;
            _pendingServerJson = null;

            // skip duplicates
            if (_lastSentJson == json)
                return;

            DoSendServer(json);
        }

        private static void DoSendServer(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var bytes = Encoding.UTF8.GetByteCount(json);
            Debug.Log($"[SaveSystem] Saving server key={ServerKey}, bytes={bytes}");

            try
            {
                // iDosGames SDK call (was called every time before) :contentReference[oaicite:1]{index=1}
                UserDataService.UpdateCustomUserData(ServerKey, json);
                Debug.Log("[SaveSystem] UpdateCustomUserData called (throttled)");

                _lastServerSendRealtime = Time.realtimeSinceStartup;
                _lastSentJson = json;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] DoSendServer exception: {e}");

                // if failed -> restore pending so we can retry later
                _pendingServerSave = true;
                _pendingServerJson = json;
                _pendingDueRealtime = Time.realtimeSinceStartup + Mathf.Max(1f, _debounceDelay);
            }
        }

        private class SaveSystemRuntime : MonoBehaviour
        {
            private void Update()
            {
                SaveSystem.Tick();
            }

            private void OnApplicationPause(bool pause)
            {
                if (pause)
                    SaveSystem.FlushServerNow();
            }

            private void OnApplicationQuit()
            {
                SaveSystem.FlushServerNow();
            }
        }

        // -------- SERVER LOAD (cached) --------
        // IMPORTANT:
        // Cache is updated only after UserDataService.RequestUserAllData()
        public static bool TryLoadServerCached(out PlayerState serverState)
        {
            serverState = null;

            try
            {
                var json = UserDataService.GetCachedCustomUserData(CustomUserDataKey.player_state_v1);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning("[SaveSystem] Cached server state is empty. Did you call RequestUserAllData()?");
                    return false;
                }

                if (!TryFromJson(json, out serverState))
                {
                    Debug.LogError("[SaveSystem] Cached server json parse failed.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] TryLoadServerCached exception: {e}");
                return false;
            }
        }
    }
}
