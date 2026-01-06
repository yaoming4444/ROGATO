using System.Collections.Generic;

public interface IInventoryService
{
    IReadOnlyList<EquipItemDef> GetOwnedItems();
}

