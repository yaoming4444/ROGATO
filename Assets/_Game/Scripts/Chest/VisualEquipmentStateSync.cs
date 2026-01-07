using UnityEngine;
using LayerLab.ArtMaker;

public class VisualEquipmentStateSync : MonoBehaviour
{
    [SerializeField] private VisualEquipmentService visualEquip;

    [Header("Behavior")]
    [SerializeField] private bool revertToDefaultsOnMissing = true; // ? вернуть дефолт из CreateDefault
    [SerializeField] private bool clearMissingSlots = false;        // если revert=false -> ставить ""

    private GameCore.PlayerState _defaults;

    private void Awake()
    {
        _defaults = GameCore.PlayerState.CreateDefault(); // ? кэш дефолтов
    }

    private void OnEnable()
    {
        Debug.Log($"[VESSync {GetInstanceID()}] OnEnable. visualEquip={(visualEquip ? visualEquip.GetInstanceID().ToString() : "NULL")}");
        if (visualEquip != null)
            visualEquip.OnChanged += ApplyToState;
    }

    private void OnDisable()
    {
        if (visualEquip != null)
            visualEquip.OnChanged -= ApplyToState;
    }

    private void ApplyToState()
    {
        var gi = GameCore.GameInstance.I;
        if (gi == null || gi.State == null) return;

        Debug.Log($"[VESSync {GetInstanceID()}] ApplyToState via visualEquip={(visualEquip ? visualEquip.GetInstanceID().ToString() : "NULL")}");

        SetFromEquipOrDefault(gi, PartsType.Back, EquipmentType.Back);
        SetFromEquipOrDefault(gi, PartsType.Beard, EquipmentType.Beard);
        SetFromEquipOrDefault(gi, PartsType.Boots, EquipmentType.Boots);
        SetFromEquipOrDefault(gi, PartsType.Bottom, EquipmentType.Bottom);
        SetFromEquipOrDefault(gi, PartsType.Brow, EquipmentType.Brow);
        SetFromEquipOrDefault(gi, PartsType.Eyes, EquipmentType.Eyes);
        SetFromEquipOrDefault(gi, PartsType.Gloves, EquipmentType.Gloves);

        SetFromEquipOrDefault(gi, PartsType.Hair_Short, EquipmentType.Hair_Short);
        SetFromEquipOrDefault(gi, PartsType.Hair_Hat, EquipmentType.Hair_Hat);
        SetFromEquipOrDefault(gi, PartsType.Helmet, EquipmentType.Helmet);

        SetFromEquipOrDefault(gi, PartsType.Mouth, EquipmentType.Mouth);
        SetFromEquipOrDefault(gi, PartsType.Eyewear, EquipmentType.Eyewear);

        SetFromEquipOrDefault(gi, PartsType.Gear_Left, EquipmentType.Gear_Left);
        SetFromEquipOrDefault(gi, PartsType.Gear_Right, EquipmentType.Gear_Right);

        SetFromEquipOrDefault(gi, PartsType.Top, EquipmentType.Top);

        // Skin лучше не трогать без нужды:
        // SetFromEquipOrDefault(gi, PartsType.Skin, EquipmentType.Skin);

        gi.RaiseStateChanged(); // ? моментально обновит визуал
    }

    private void SetFromEquipOrDefault(GameCore.GameInstance gi, PartsType parts, EquipmentType slot)
    {
        var item = visualEquip != null ? visualEquip.GetEquipped(slot) : null;

        if (item != null && !string.IsNullOrWhiteSpace(item.partId))
        {
            gi.SetVisual(parts, item.partId, notify: false);
            return;
        }

        // ? предмета нет -> вернуть дефолт или очистить
        if (revertToDefaultsOnMissing)
        {
            gi.SetVisual(parts, GetDefault(parts), notify: false); // "" тоже ок -> биндер снимет (-1)
        }
        else if (clearMissingSlots)
        {
            gi.SetVisual(parts, "", notify: false);
        }
        // иначе не трогаем (но тогда снова будет "залипание")
    }

    private string GetDefault(PartsType parts)
    {
        if (_defaults == null) return "";

        return parts switch
        {
            PartsType.Back => _defaults.visual_back,
            PartsType.Beard => _defaults.visual_beard,
            PartsType.Boots => _defaults.visual_boots,
            PartsType.Bottom => _defaults.visual_bottom,
            PartsType.Brow => _defaults.visual_brow,
            PartsType.Eyes => _defaults.visual_eyes,
            PartsType.Gloves => _defaults.visual_gloves,

            PartsType.Hair_Short => _defaults.visual_hair_short,
            PartsType.Hair_Hat => _defaults.visual_hair_hat,
            PartsType.Helmet => _defaults.visual_helmet,

            PartsType.Mouth => _defaults.visual_mouth,
            PartsType.Eyewear => _defaults.visual_eyewear,

            PartsType.Gear_Left => _defaults.visual_gear_left,
            PartsType.Gear_Right => _defaults.visual_gear_right,

            PartsType.Top => _defaults.visual_top,
            PartsType.Skin => _defaults.visual_skin,

            _ => ""
        };
    }
}



