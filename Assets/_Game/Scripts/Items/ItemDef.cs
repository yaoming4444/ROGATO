using UnityEngine;

namespace GameCore.Items
{
    /// <summary>
    /// One item definition (ScriptableObject).
    /// You create many of these and put them into ItemDatabase.
    ///
    /// Fields:
    /// - Id: unique string used for saving/loading (stored in PlayerState.Equipped)
    /// - DisplayName: UI name
    /// - Slot: which equipment slot it fits into
    /// - Rarity: used for chest rolling and UI
    /// - Stats: base stats added to player
    /// - ExtraStats: optional extra bonuses (like crit chance, etc.)
    /// - SellGems: how many gems you get when selling
    /// - Icon/IconRarity: UI visuals
    /// </summary>
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
        [SerializeField] private ExtraStat[] extraStats;

        [Header("Economy")]
        [SerializeField] private int sellGems = 1;

        [Header("UI")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite iconRarity;

        public string Id => id;
        public string DisplayName => displayName;
        public EquipSlot Slot => slot;
        public Rarity Rarity => rarity;
        public ItemStats Stats => stats;
        public int SellGems => sellGems;
        public Sprite Icon => icon;
        public Sprite IconRarity => iconRarity;
        public ExtraStat[] ExtraStats => extraStats;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: if id is empty, automatically set it to asset name.
        /// This helps avoid forgetting to assign ids manually.
        /// </summary>
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(id)) return;
            id = name.Trim();
        }
#endif
    }
}



