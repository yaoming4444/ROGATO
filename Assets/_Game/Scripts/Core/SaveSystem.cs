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

        public static void SaveServer(PlayerState state)
        {
            if (state == null)
            {
                Debug.LogError("[SaveSystem] SaveServer: state is null");
                return;
            }

            // Update timestamp before writing
            state.LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var json = ToJson(state, pretty: false);
            var bytes = Encoding.UTF8.GetByteCount(json);

            Debug.Log($"[SaveSystem] Saving server key={ServerKey}, bytes={bytes}");

            try
            {
                // iDosGames SDK call
                UserDataService.UpdateCustomUserData(ServerKey, json);
                Debug.Log("[SaveSystem] UpdateCustomUserData called");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] SaveServer exception: {e}");
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
                // IMPORTANT:
                // Make sure the key used here matches SaveServer() key.
                // If SDK supports string overload, prefer: GetCachedCustomUserData(ServerKey)
                //
                // Currently uses enum key:
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





