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

        [Header("Base Stats")]
        [SerializeField] private ItemStats stats;

        [Header("Extra Stats (optional)")]
        [SerializeField] private ExtraStat[] extraStats; // может быть пустым/NULL

        [Header("UI")]
        [SerializeField] private Sprite icon;

        [SerializeField] private Sprite iconRarity;

        [Header("Economy")]
        [SerializeField] private int sellGems = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public EquipSlot Slot => slot;
        public Rarity Rarity => rarity;
        public ItemStats Stats => stats;
        public Sprite Icon => icon;

        public Sprite IconRarity => iconRarity;
        public int SellGems => sellGems;

        public ExtraStat[] ExtraStats => extraStats;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(id)) return;
            id = name.Trim();
        }
#endif
    }
}


