using UnityEngine;

namespace GameCore.Items
{
    [CreateAssetMenu(menuName = "Game/Items/ItemDef", fileName = "ItemDef_")]
    public class ItemDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Equipment")]
        [SerializeField] private EquipSlot slot;
        [SerializeField] private Rarity rarity;

        [Header("Power (for income / score)")]
        [Min(0)]
        [SerializeField] private int power = 0;

        [Header("Base Stats")]
        [SerializeField] private ItemStats stats;

        [Header("Extra Stats (optional)")]
        [SerializeField] private ExtraStat[] extraStats;

        [Header("Economy")]
        [Min(0)]
        [SerializeField] private int sellGems = 1;

        [Header("UI")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite iconRarity;

        public string Id => id;
        public string DisplayName => displayName;
        public EquipSlot Slot => slot;
        public Rarity Rarity => rarity;

        public int Power => power;

        public ItemStats Stats => stats;
        public int SellGems => sellGems;

        public Sprite Icon => icon;
        public Sprite IconRarity => iconRarity;

        public ExtraStat[] ExtraStats => extraStats;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name.Trim();

            if (power < 0) power = 0;
            if (sellGems < 0) sellGems = 0;
        }
#endif
    }
}




