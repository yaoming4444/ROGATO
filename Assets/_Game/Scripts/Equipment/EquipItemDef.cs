using UnityEngine;

[CreateAssetMenu(menuName = "Game/Equipment/Equip Item", fileName = "EquipItem_")]
public class EquipItemDef : ScriptableObject
{
    [Header("Identity")]
    public string itemId;            // уникальный id (например "helm_001")
    public string displayName;

    [Header("Equip rules")]
    public EquipmentType slotType;        // куда надевается
    public string partId;            // ключ для PartsManager (что именно ставить в визуал)

    [Header("UI")]
    public Sprite icon;

    [TextArea] public string description;
}

