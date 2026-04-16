using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Stats
{
    [CreateAssetMenu(fileName = "StatRollConfig", menuName = "GameCore/Stats/Stat Roll Config")]
    public class StatRollConfig : ScriptableObject
    {
        [Header("All Available Cards")]
        [SerializeField] private List<StatCardDefinition> cards = new List<StatCardDefinition>();

        [Header("Rarity Roll Chances")]
        [SerializeField]
        private List<RarityChanceEntry> rarityChances = new List<RarityChanceEntry>()
        {
            new RarityChanceEntry(StatRarity.Common, 60f),
            new RarityChanceEntry(StatRarity.Rare, 25f),
            new RarityChanceEntry(StatRarity.Epic, 10f),
            new RarityChanceEntry(StatRarity.Legendary, 5f),
        };

        public IReadOnlyList<StatCardDefinition> Cards => cards;
        public IReadOnlyList<RarityChanceEntry> RarityChances => rarityChances;
    }

    [Serializable]
    public class RarityChanceEntry
    {
        [SerializeField] private StatRarity rarity;
        [SerializeField] private float chanceWeight = 1f;

        public StatRarity Rarity => rarity;
        public float ChanceWeight => chanceWeight;

        public RarityChanceEntry(StatRarity rarity, float chanceWeight)
        {
            this.rarity = rarity;
            this.chanceWeight = chanceWeight;
        }
    }
}