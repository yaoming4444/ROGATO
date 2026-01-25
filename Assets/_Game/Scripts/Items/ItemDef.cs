using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Item Level")]
        [Min(1)]
        [SerializeField] private int level = 1;

        [Header("Base Stats")]
        [SerializeField] private ItemStats stats;

        [Header("Extra Stats (optional)")]
        [SerializeField] private ExtraStat[] extraStats;

        [Header("Economy")]
        [Min(0)]
        [SerializeField] private int sellGems = 1;

        [Header("UI")]
        [SerializeField] private Sprite icon;

        // ? НОВОЕ: цвет редкости (вместо спрайта)
        // Если alpha = 0 => бейдж можно скрывать
        [SerializeField] private Color rarityColor = new Color(1f, 1f, 1f, 0f);

        [SerializeField] private GameObject rarityVFX;

        // (опционально) чтобы Unity не ругался на старое поле при обновлении ассетов:
        // оставляем "на память", но больше не используем
        [FormerlySerializedAs("iconRarity")]
        [SerializeField, HideInInspector] private Sprite iconRarity_Legacy;

        public string Id => id;
        public string DisplayName => displayName;
        public EquipSlot Slot => slot;
        public Rarity Rarity => rarity;

        public int Power => power;
        public int Level => level;

        public ItemStats Stats => stats;
        public int SellGems => sellGems;

        public Sprite Icon => icon;

        public Color RarityColor => rarityColor;
        public GameObject RarityVFX => rarityVFX;

        public ExtraStat[] ExtraStats => extraStats;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name.Trim();

            if (power < 0) power = 0;
            if (sellGems < 0) sellGems = 0;
            if (level < 1) level = 1;
        }
#endif
    }
}
