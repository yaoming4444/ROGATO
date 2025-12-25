using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Items
{
    [CreateAssetMenu(menuName = "Game/Chest/ChestDropTable", fileName = "ChestDropTable")]
    public class ChestDropTable : ScriptableObject
    {
        [Serializable]
        public struct RarityChance
        {
            public Rarity Rarity;
            [Range(0, 100)] public float Chance; // %
        }

        [Serializable]
        public class LevelEntry
        {
            public int Level = 1;
            public List<RarityChance> Chances = new List<RarityChance>();
        }

        [SerializeField] private List<LevelEntry> levels = new List<LevelEntry>();

        public LevelEntry GetForLevel(int level)
        {
            if (levels == null || levels.Count == 0) return null;

            // ищем точный
            for (int i = 0; i < levels.Count; i++)
                if (levels[i] != null && levels[i].Level == level) return levels[i];

            // иначе берём ближайший меньший (если апнул выше чем заполнено)
            LevelEntry best = null;
            for (int i = 0; i < levels.Count; i++)
            {
                var e = levels[i];
                if (e == null) continue;
                if (e.Level <= level && (best == null || e.Level > best.Level))
                    best = e;
            }
            return best ?? levels[0];
        }

        public Rarity RollRarity(int chestLevel)
        {
            var entry = GetForLevel(chestLevel);
            if (entry == null || entry.Chances == null || entry.Chances.Count == 0)
                return Rarity.G;

            float sum = 0f;
            for (int i = 0; i < entry.Chances.Count; i++)
                sum += Mathf.Max(0f, entry.Chances[i].Chance);

            if (sum <= 0.0001f) return Rarity.G;

            float r = UnityEngine.Random.Range(0f, sum);
            float acc = 0f;

            for (int i = 0; i < entry.Chances.Count; i++)
            {
                acc += Mathf.Max(0f, entry.Chances[i].Chance);
                if (r <= acc) return entry.Chances[i].Rarity;
            }

            return entry.Chances[entry.Chances.Count - 1].Rarity;
        }
    }
}

