using System.Collections.Generic;
using UnityEngine;

public class DevInventoryService_AllItems : MonoBehaviour, IInventoryService
{
    [SerializeField] private EquipDatabase database;

    public IReadOnlyList<EquipItemDef> GetOwnedItems()
    {
        if (database == null) return new List<EquipItemDef>();
        return database.allItems;
    }
}

