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

    // ? ВОТ ЭТО ДОБАВЬ
    public void LoadFromState(GameCore.PlayerState st, EquipDatabase db)
    {
        if (st == null || db == null) return;

        _equipped.Clear();

        TrySet(db, EquipmentType.Back, st.visual_back);
        TrySet(db, EquipmentType.Beard, st.visual_beard);
        TrySet(db, EquipmentType.Boots, st.visual_boots);
        TrySet(db, EquipmentType.Bottom, st.visual_bottom);
        TrySet(db, EquipmentType.Brow, st.visual_brow);
        TrySet(db, EquipmentType.Eyes, st.visual_eyes);
        TrySet(db, EquipmentType.Gloves, st.visual_gloves);

        TrySet(db, EquipmentType.Hair_Short, st.visual_hair_short);
        TrySet(db, EquipmentType.Hair_Hat, st.visual_hair_hat);
        TrySet(db, EquipmentType.Helmet, st.visual_helmet);

        TrySet(db, EquipmentType.Mouth, st.visual_mouth);
        TrySet(db, EquipmentType.Eyewear, st.visual_eyewear);

        TrySet(db, EquipmentType.Gear_Left, st.visual_gear_left);
        TrySet(db, EquipmentType.Gear_Right, st.visual_gear_right);

        TrySet(db, EquipmentType.Top, st.visual_top);
        TrySet(db, EquipmentType.Skin, st.visual_skin);

        OnChanged?.Invoke();
    }

    private void TrySet(EquipDatabase db, EquipmentType slot, string partId)
    {
        if (string.IsNullOrWhiteSpace(partId)) return;

        // метод ниже добавь в EquipDatabase (см. пункт 2)
        var item = db.FindByPartId(slot, partId);
        if (item != null) _equipped[slot] = item;
    }

    public IReadOnlyDictionary<EquipmentType, EquipItemDef> Snapshot() => _equipped;
}
