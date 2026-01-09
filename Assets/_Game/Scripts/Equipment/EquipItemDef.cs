using UnityEngine;

public enum EquipStatType
{
    None = 0,
    ATK = 1,
    DEF = 2,
    HP = 3
}

public enum EquipRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

[CreateAssetMenu(menuName = "Game/Equipment/Equip Item", fileName = "EquipItem_")]
public class EquipItemDef : ScriptableObject
{
    [Header("Identity")]
    public string itemId;            // уникальный id (например "helm_001")
    public string displayName;

    [Header("Equip rules")]
    public EquipmentType slotType;   // куда надеваетс€
    public string partId;            // ключ дл€ PartsManager (что именно ставить в визуал)

    [Header("UI")]
    public Sprite icon;

    [TextArea] public string description;

    [Header("Rarity (NEW)")]
    public EquipRarity rarity = EquipRarity.Common;
    public Sprite rarityBG;

    [Header("Stats (Visual Equipment)")]
    public EquipStatType statType = EquipStatType.None;

    [Tooltip("X = уровень слота (1..120), Y = значение стата. Ќапример: (1,2) (120,250)")]
    public AnimationCurve statBySlotLevel = AnimationCurve.Linear(1, 1, 120, 120);

    [Tooltip("ќкругление значени€ стата (обычно true).")]
    public bool roundToInt = true;

    public int GetStatValueForSlotLevel(int slotLevel)
    {
        if (statType == EquipStatType.None) return 0;

        slotLevel = Mathf.Clamp(slotLevel, 1, 120);
        float val = statBySlotLevel != null ? statBySlotLevel.Evaluate(slotLevel) : 0f;

        return roundToInt ? Mathf.RoundToInt(val) : Mathf.FloorToInt(val);
    }
}



