using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Equipment/Equip Database", fileName = "EquipDatabase")]
public class EquipDatabase : ScriptableObject
{
    public List<EquipItemDef> allItems = new();

    public EquipItemDef GetById(string id)
    {
        return allItems.Find(x => x != null && x.itemId == id);
    }
}
