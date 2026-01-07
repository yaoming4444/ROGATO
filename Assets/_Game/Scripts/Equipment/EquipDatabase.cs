using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Equipment/Equip Database", fileName = "EquipDatabase")]
public class EquipDatabase : ScriptableObject
{
    public List<EquipItemDef> allItems = new();

    public EquipItemDef FindByPartId(EquipmentType slot, string partId)
    {
        if (string.IsNullOrWhiteSpace(partId)) return null;

        for (int i = 0; i < allItems.Count; i++)
        {
            var it = allItems[i];
            if (it == null) continue;
            if (it.slotType != slot) continue;
            if (it.partId == partId) return it;
        }
        return null;
    }
}
