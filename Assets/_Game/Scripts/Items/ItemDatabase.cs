using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Items
{
    /// <summary>
    /// Central item database stored as a ScriptableObject.
    ///
    /// - You create an ItemDatabase asset in Assets/Resources/ItemDatabase.asset
    /// - At runtime ItemDatabase.I loads it via Resources.Load
    /// - The database builds caches:
    ///   * byId: fast lookup by string id
    ///   * bySlotRarity: pools for chest rolls (slot + rarity)
    ///
    /// This is designed to be simple and fast during gameplay.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Items/ItemDatabase", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDef> items = new List<ItemDef>();

        // Cache: id -> definition
        private Dictionary<string, ItemDef> _byId;

        // Cache: (slot, rarity) -> list of items for chest rolling
        private Dictionary<(EquipSlot, Rarity), List<ItemDef>> _bySlotRarity;

        public IReadOnlyList<ItemDef> Items => items;

        private void OnEnable()
        {
            RebuildCaches();
        }

        /// <summary>
        /// Rebuilds internal caches. Called on enable.
        /// Call this again if you modify the items list at runtime (usually not needed).
        /// </summary>
        public void RebuildCaches()
        {
            _byId = new Dictionary<string, ItemDef>(StringComparer.OrdinalIgnoreCase);
            _bySlotRarity = new Dictionary<(EquipSlot, Rarity), List<ItemDef>>();

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;

                // Cache by id
                if (!string.IsNullOrWhiteSpace(it.Id))
                {
                    // Last one wins if duplicates exist (better: validate duplicates in editor)
                    _byId[it.Id] = it;
                }

                // Cache by (slot, rarity) pool
                var key = (it.Slot, it.Rarity);
                if (!_bySlotRarity.TryGetValue(key, out var list))
                {
                    list = new List<ItemDef>();
                    _bySlotRarity[key] = list;
                }
                list.Add(it);
            }
        }

        /// <summary>
        /// Lookup item by id. Returns null if not found.
        /// </summary>
        public ItemDef GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (_byId == null) RebuildCaches();

            _byId.TryGetValue(id, out var def);
            return def;
        }

        /// <summary>
        /// Returns the underlying pool list for (slot, rarity).
        /// If missing, returns null.
        /// </summary>
        public List<ItemDef> GetPool(EquipSlot slot, Rarity rarity)
        {
            if (_bySlotRarity == null) RebuildCaches();
            _bySlotRarity.TryGetValue((slot, rarity), out var list);
            return list;
        }

        /// <summary>
        /// Returns random item from the (slot, rarity) pool.
        /// Returns null if pool is missing or empty.
        /// </summary>
        public ItemDef GetRandomFromPool(EquipSlot slot, Rarity rarity)
        {
            var pool = GetPool(slot, rarity);
            if (pool == null || pool.Count == 0) return null;

            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        // --------------- Singleton loader ---------------

        private static ItemDatabase _instance;

        /// <summary>
        /// Loads the ItemDatabase asset from Resources.
        /// Make sure the asset is located at:
        /// Assets/Resources/ItemDatabase.asset
        /// </summary>
        public static ItemDatabase I
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("[ItemDatabase] Resource ItemDatabase.asset not found. Create it and put into Assets/Resources/");
                }

                return _instance;
            }
        }
    }
}


