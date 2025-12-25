using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Items
{
    [CreateAssetMenu(menuName = "Game/Items/ItemDatabase", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDef> items = new List<ItemDef>();

        private Dictionary<string, ItemDef> _byId;
        private Dictionary<(EquipSlot, Rarity), List<ItemDef>> _bySlotRarity;

        public IReadOnlyList<ItemDef> Items => items;

        private void BuildCache()
        {
            _byId = new Dictionary<string, ItemDef>(StringComparer.Ordinal);
            _bySlotRarity = new Dictionary<(EquipSlot, Rarity), List<ItemDef>>();

            foreach (var it in items)
            {
                if (it == null) continue;
                if (string.IsNullOrWhiteSpace(it.Id))
                {
                    Debug.LogError($"[ItemDatabase] Item has empty Id: {it.name}", it);
                    continue;
                }

                if (_byId.ContainsKey(it.Id))
                {
                    Debug.LogError($"[ItemDatabase] Duplicate Id='{it.Id}' (asset: {it.name})", it);
                    continue;
                }

                _byId.Add(it.Id, it);

                var key = (it.Slot, it.Rarity);
                if (!_bySlotRarity.TryGetValue(key, out var list))
                {
                    list = new List<ItemDef>();
                    _bySlotRarity[key] = list;
                }
                list.Add(it);
            }
        }

        public ItemDef GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (_byId == null) BuildCache();
            _byId.TryGetValue(id, out var it);
            return it;
        }

        public List<ItemDef> GetPool(EquipSlot slot, Rarity rarity)
        {
            if (_bySlotRarity == null) BuildCache();
            _bySlotRarity.TryGetValue((slot, rarity), out var list);
            return list;
        }

        public ItemDef GetRandomFromPool(EquipSlot slot, Rarity rarity)
        {
            var pool = GetPool(slot, rarity);
            if (pool == null || pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        // -------- Singleton loader via Resources --------
        private static ItemDatabase _instance;

        public static ItemDatabase I
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("[ItemDatabase] Resources/ItemDatabase.asset not found. Create it and put into Assets/Resources/");
                }
                return _instance;
            }
        }
    }
}

