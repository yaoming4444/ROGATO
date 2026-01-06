using GameCore.Visual;
using LayerLab.ArtMaker;
using UnityEngine;

public class VisualEquipmentStateSync : MonoBehaviour
{
    [SerializeField] private VisualEquipmentService visualEquip;

    [Header("Optional")]
    [SerializeField] private bool clearMissingSlots = false; // если хочешь реально "снимать" при отсутствии предмета

    private void OnEnable()
    {
        Debug.Log($"[VESSync {GetInstanceID()}] OnEnable. visualEquip={(visualEquip ? visualEquip.GetInstanceID().ToString() : "NULL")}");
        if (visualEquip != null)
            if (visualEquip != null)
            visualEquip.OnChanged += ApplyToState;

        // НЕ вызываем сразу ApplyToState() если не хочешь затирать стейт на старте
        // ApplyToState();
    }

    private void OnDisable()
    {
        if (visualEquip != null)
            visualEquip.OnChanged -= ApplyToState;
    }

    private void ApplyToState()
    {
        Debug.Log($"[VESSync {GetInstanceID()}] ApplyToState via visualEquip={visualEquip.GetInstanceID()}");
        var gi = GameCore.GameInstance.I;
        if (gi == null || gi.State == null) return;

        // ВАЖНО: обновляем через API как в DevEquipVisualTest
        SetIfEquipped(gi, PartsType.Back, EquipmentType.Back);
        SetIfEquipped(gi, PartsType.Beard, EquipmentType.Beard);
        SetIfEquipped(gi, PartsType.Boots, EquipmentType.Boots);
        SetIfEquipped(gi, PartsType.Bottom, EquipmentType.Bottom);
        SetIfEquipped(gi, PartsType.Brow, EquipmentType.Brow);
        SetIfEquipped(gi, PartsType.Eyes, EquipmentType.Eyes);
        SetIfEquipped(gi, PartsType.Gloves, EquipmentType.Gloves);

        SetIfEquipped(gi, PartsType.Hair_Short, EquipmentType.Hair_Short);
        SetIfEquipped(gi, PartsType.Hair_Hat, EquipmentType.Hair_Hat);
        SetIfEquipped(gi, PartsType.Helmet, EquipmentType.Helmet);

        SetIfEquipped(gi, PartsType.Mouth, EquipmentType.Mouth);
        SetIfEquipped(gi, PartsType.Eyewear, EquipmentType.Eyewear);

        SetIfEquipped(gi, PartsType.Gear_Left, EquipmentType.Gear_Left);
        SetIfEquipped(gi, PartsType.Gear_Right, EquipmentType.Gear_Right);

        SetIfEquipped(gi, PartsType.Top, EquipmentType.Top);

        // Skin лучше НЕ трогать без нужды:
        // SetIfEquipped(gi, PartsType.Skin, EquipmentType.Skin);

        gi.RaiseStateChanged(); // <- КЛЮЧЕВО: как в тесте
    }

    private void SetIfEquipped(GameCore.GameInstance gi, PartsType parts, EquipmentType slot)
    {
        var item = visualEquip != null ? visualEquip.GetEquipped(slot) : null;

        if (item != null && !string.IsNullOrWhiteSpace(item.partId))
        {
            gi.SetVisual(parts, item.partId, notify: false);
            // Debug.Log($"[EquipSync] {parts} = {item.partId}");
        }
        else if (clearMissingSlots)
        {
            gi.SetVisual(parts, "", notify: false); // снять
        }
    }
}


