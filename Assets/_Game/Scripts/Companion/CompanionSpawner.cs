using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Companions
{
    public class CompanionSpawner : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CompanionDatabase database;

        [Header("Target")]
        [SerializeField] private Transform playerRoot;

        [Header("Spawn Points (optional)")]
        [SerializeField] private Transform spawnPointA;
        [SerializeField] private Transform spawnPointB;

        [Header("Fallback Offsets")]
        [SerializeField] private Vector3 fallbackOffsetA = new Vector3(-1.5f, 0f, -1f);
        [SerializeField] private Vector3 fallbackOffsetB = new Vector3(1.5f, 0f, -1f);

        [Header("Options")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool respawnOnStateChanged = false;
        [SerializeField] private bool parentToPlayer = false;

        private readonly List<GameObject> spawnedCompanions = new();

        private GameInstance Game => GameInstance.I;

        private void Start()
        {
            if (spawnOnStart)
                SpawnEquippedCompanions();
        }

        private void OnEnable()
        {
            if (respawnOnStateChanged && Game != null)
                Game.StateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (respawnOnStateChanged && Game != null)
                Game.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(PlayerState _)
        {
            RespawnEquippedCompanions();
        }

        [ContextMenu("Spawn Equipped Companions")]
        public void SpawnEquippedCompanions()
        {
            if (Game == null || Game.State == null)
            {
                Debug.LogWarning("[CompanionSpawner] GameInstance or PlayerState is missing.", this);
                return;
            }

            if (database == null)
            {
                Debug.LogWarning("[CompanionSpawner] CompanionDatabase is not assigned.", this);
                return;
            }

            ClearSpawned();

            SpawnOne(Game.State.equippedCompanionSlotA, 0);
            SpawnOne(Game.State.equippedCompanionSlotB, 1);
        }

        [ContextMenu("Respawn Equipped Companions")]
        public void RespawnEquippedCompanions()
        {
            ClearSpawned();
            SpawnEquippedCompanions();
        }

        [ContextMenu("Clear Spawned Companions")]
        public void ClearSpawned()
        {
            for (int i = spawnedCompanions.Count - 1; i >= 0; i--)
            {
                if (spawnedCompanions[i] != null)
                    Destroy(spawnedCompanions[i]);
            }

            spawnedCompanions.Clear();
        }

        private void SpawnOne(string companionId, int slotIndex)
        {
            if (string.IsNullOrWhiteSpace(companionId))
                return;

            var def = database.GetById(companionId);
            if (def == null)
            {
                Debug.LogWarning($"[CompanionSpawner] CompanionDef not found for id '{companionId}'.", this);
                return;
            }

            if (def.worldPrefab == null)
            {
                Debug.LogWarning($"[CompanionSpawner] worldPrefab is missing for companion '{companionId}'.", this);
                return;
            }

            Transform point = slotIndex == 0 ? spawnPointA : spawnPointB;

            Vector3 spawnPosition;
            Quaternion spawnRotation;

            if (point != null)
            {
                spawnPosition = point.position;
                spawnRotation = point.rotation;
            }
            else if (playerRoot != null)
            {
                Vector3 offset = slotIndex == 0 ? fallbackOffsetA : fallbackOffsetB;
                spawnPosition = playerRoot.position + offset;
                spawnRotation = playerRoot.rotation;
            }
            else
            {
                spawnPosition = transform.position;
                spawnRotation = transform.rotation;
            }

            Transform parent = parentToPlayer && playerRoot != null ? playerRoot : null;

            GameObject instance = Instantiate(def.worldPrefab, spawnPosition, spawnRotation, parent);
            instance.name = $"{def.id}_Companion";

            spawnedCompanions.Add(instance);
        }
    }
}