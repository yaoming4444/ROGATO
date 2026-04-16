using UnityEngine;

namespace GameCore.Stats
{
    [CreateAssetMenu(fileName = "StatCardDefinition", menuName = "GameCore/Stats/Stat Card Definition")]
    public class StatCardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField] private StatType statType = StatType.None;
        [SerializeField] private StatRarity rarity = StatRarity.Common;

        [Header("Values")]
        [SerializeField] private float startValue = 1f;
        [SerializeField] private float upgradeStep = 1f;
        [SerializeField] private float maxCap = 10f;

        [Header("Roll")]
        [SerializeField] private float weight = 1f;

        public string CardId => cardId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? statType.ToString() : displayName;
        public StatType StatType => statType;
        public StatRarity Rarity => rarity;

        public float StartValue => startValue;
        public float UpgradeStep => upgradeStep;
        public float MaxCap => maxCap;

        public float Weight => weight;

        /// <summary>
        /// Создать runtime-карточку из этого шаблона.
        /// </summary>
        public RolledStatCard CreateRuntimeCard()
        {
            return new RolledStatCard(
                statType,
                rarity,
                startValue,
                upgradeStep,
                maxCap
            );
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (startValue < 0f)
                startValue = 0f;

            if (upgradeStep < 0f)
                upgradeStep = 0f;

            if (maxCap < 0f)
                maxCap = 0f;

            if (weight < 0f)
                weight = 0f;
        }
#endif
    }
}