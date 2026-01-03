using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Items
{
    [CreateAssetMenu(menuName = "Game/Chests/Chest Drop Table", fileName = "ChestDropTable")]
    public class ChestDropTable : ScriptableObject
    {
        [Serializable]
        public struct RarityWeight
        {
            public Rarity rarity;
            [Min(0f)] public float weight; // можно считать как "проценты", можно как "веса"
        }

        [Serializable]
        public class LevelEntry
        {
            [Min(1)] public int level = 1;

            [Header("Upgrade cost to reach THIS level (optional)")]
            [Min(0)] public int upgradeCostGems = 0;

            [Header("Rarity rules")]
            public Rarity minRarity = Rarity.G; // на высоких уровн€х ставишь например E/A/S и т.д.

            [Tooltip("Weights for each rarity. Rarities below minRarity are ignored.")]
            public List<RarityWeight> weights = new();
        }

        [SerializeField] private List<LevelEntry> levels = new();

        public int MaxLevel => levels == null ? 1 : Mathf.Max(1, levels.Count > 0 ? levels[^1].level : 1);

        public bool IsMaxLevel(int lvl) => lvl >= MaxLevel;

        public LevelEntry GetLevel(int lvl)
        {
            if (levels == null || levels.Count == 0) return null;

            // берЄм самый близкий <= lvl (если lvl больше max Ч вернЄм max)
            LevelEntry best = levels[0];
            for (int i = 0; i < levels.Count; i++)
            {
                var e = levels[i];
                if (e == null) continue;
                if (e.level <= lvl) best = e;
                else break;
            }
            return best;
        }

        public int GetUpgradeCostGems(int targetLevel)
        {
            var e = GetLevel(targetLevel);
            return e != null ? e.upgradeCostGems : 0;
        }

        public Rarity GetMinRarity(int lvl)
        {
            var e = GetLevel(lvl);
            return e != null ? e.minRarity : Rarity.G;
        }

        public Rarity RollRarity(int chestLevel)
        {
            var e = GetLevel(chestLevel);
            if (e == null || e.weights == null || e.weights.Count == 0)
                return Rarity.G;

            // собираем только допустимые (>= minRarity)
            float sum = 0f;
            for (int i = 0; i < e.weights.Count; i++)
            {
                var w = e.weights[i];
                if ((int)w.rarity < (int)e.minRarity) continue;
                if (w.weight <= 0f) continue;
                sum += w.weight;
            }

            if (sum <= 0f)
                return e.minRarity; // если всЄ нули Ч хот€ бы minRarity

            float roll = UnityEngine.Random.value * sum;
            float acc = 0f;

            for (int i = 0; i < e.weights.Count; i++)
            {
                var w = e.weights[i];
                if ((int)w.rarity < (int)e.minRarity) continue;
                if (w.weight <= 0f) continue;

                acc += w.weight;
                if (roll <= acc)
                    return w.rarity;
            }

            return e.minRarity;
        }

        // ƒл€ UI: шанс в % (нормализованный)
        public float GetChancePercent(int chestLevel, Rarity rarity)
        {
            var e = GetLevel(chestLevel);
            if (e == null || e.weights == null || e.weights.Count == 0) return 0f;
            if ((int)rarity < (int)e.minRarity) return 0f;

            float sum = 0f;
            float val = 0f;

            foreach (var w in e.weights)
            {
                if ((int)w.rarity < (int)e.minRarity) continue;
                if (w.weight <= 0f) continue;

                sum += w.weight;
                if (w.rarity == rarity) val += w.weight;
            }

            if (sum <= 0f) return 0f;
            return (val / sum) * 100f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (levels == null) return;

            // —ортируем по level
            levels.Sort((a, b) =>
            {
                int la = a != null ? a.level : int.MaxValue;
                int lb = b != null ? b.level : int.MaxValue;
                return la.CompareTo(lb);
            });
        }
#endif

    }
}



