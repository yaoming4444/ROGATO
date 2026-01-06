using System;
using System.Collections.Generic;
using UnityEngine;

public class VisualEquipmentService : MonoBehaviour
{
    // slot -> item
    private readonly Dictionary<EquipmentType, EquipItemDef> _equipped = new();

    public event Action OnChanged;

    public EquipItemDef GetEquipped(EquipmentType slot)
        => _equipped.TryGetValue(slot, out var item) ? item : null;

    public bool Equip(EquipItemDef item)
    {
        Debug.Log($"[VES {GetInstanceID()}] Equip {item?.itemId} slot={item?.slotType} partId={item?.partId}");
        if (item == null) return false;
        if (item.slotType == EquipmentType.None) return false;

        _equipped[item.slotType] = item;
        OnChanged?.Invoke();
        return true;
    }

    public void Unequip(EquipmentType slot)
    {
        if (_equipped.Remove(slot))
            OnChanged?.Invoke();
    }

    public IReadOnlyDictionary<EquipmentType, EquipItemDef> Snapshot() => _equipped;
}
