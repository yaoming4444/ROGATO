using System;
using System.Collections.Generic;
using UnityEngine;
using LayerLab.ArtMaker; // PartsType

public class VisualEquipmentService : MonoBehaviour
{
    public static VisualEquipmentService I { get; private set; }

    [Header("Database")]
    [SerializeField] private EquipDatabase equipDatabase;

    [Header("Auto Load")]
    [SerializeField] private bool syncFromStateOnStart = true;

    private readonly Dictionary<EquipmentType, EquipItemDef> _equipped = new();
    public event Action OnChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (I != null) return;

        var go = new GameObject("[VisualEquipmentService]");
        go.AddComponent<VisualEquipmentService>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        // если не назначил в инспекторе Ч пробуем загрузить из Resources
        if (equipDatabase == null)
            equipDatabase = Resources.Load<EquipDatabase>("EquipDatabase"); // положи asset в Resources/EquipDatabase.asset
    }

    private void OnEnable()
    {
        var gi = GameCore.GameInstance.I;
        if (gi != null)
            gi.StateChanged += OnGameStateChanged;

        if (syncFromStateOnStart)
            SyncFromState(); // важно: один раз сразу после enable
    }

    private void OnDisable()
    {
        var gi = GameCore.GameInstance.I;
        if (gi != null)
            gi.StateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameCore.PlayerState _)
    {
        // при логине / ApplyState об€зательно пересобираем кэш
        SyncFromState();
    }

    public EquipItemDef GetEquipped(EquipmentType slot)
        => _equipped.TryGetValue(slot, out var item) ? item : null;

    public IReadOnlyDictionary<EquipmentType, EquipItemDef> Snapshot() => _equipped;

    public bool Equip(EquipItemDef item)
    {
        if (item == null) return false;
        if (item.slotType == EquipmentType.None) return false;

        // 1) пишем в PlayerState через GameInstance (источник истины)
        var gi = GameCore.GameInstance.I;
        if (gi != null)
        {
            var partsType = ToPartsType(item.slotType);
            if (partsType.HasValue)
                gi.SetVisual(partsType.Value, item.partId, notify: true);
        }

        // 2) обновл€ем локальный кэш, чтобы UI мог сразу отрисовать
        _equipped[item.slotType] = item;
        OnChanged?.Invoke();
        return true;
    }

    public void Unequip(EquipmentType slot)
    {
        var gi = GameCore.GameInstance.I;
        if (gi != null)
        {
            var partsType = ToPartsType(slot);
            if (partsType.HasValue)
                gi.SetVisual(partsType.Value, "", notify: true);
        }

        if (_equipped.Remove(slot))
            OnChanged?.Invoke();
    }

    public void SyncFromState()
    {
        var st = GameCore.GameInstance.I?.State;
        if (st == null) return;

        // если базы нет Ч мы не можем сопоставить partId -> EquipItemDef (дл€ UI/статов)
        if (equipDatabase == null)
        {
            Debug.LogWarning("[VisualEquipmentService] equipDatabase is NULL. UI slots/stats cannot resolve items.");
            _equipped.Clear();
            OnChanged?.Invoke();
            return;
        }

        _equipped.Clear();

        TrySet(EquipmentType.Back, st.visual_back);
        TrySet(EquipmentType.Beard, st.visual_beard);
        TrySet(EquipmentType.Boots, st.visual_boots);
        TrySet(EquipmentType.Bottom, st.visual_bottom);
        TrySet(EquipmentType.Brow, st.visual_brow);
        TrySet(EquipmentType.Eyes, st.visual_eyes);
        TrySet(EquipmentType.Gloves, st.visual_gloves);

        TrySet(EquipmentType.Hair_Short, st.visual_hair_short);
        TrySet(EquipmentType.Hair_Hat, st.visual_hair_hat);
        TrySet(EquipmentType.Helmet, st.visual_helmet);

        TrySet(EquipmentType.Mouth, st.visual_mouth);
        TrySet(EquipmentType.Eyewear, st.visual_eyewear);

        TrySet(EquipmentType.Gear_Left, st.visual_gear_left);
        TrySet(EquipmentType.Gear_Right, st.visual_gear_right);

        TrySet(EquipmentType.Top, st.visual_top);
        TrySet(EquipmentType.Skin, st.visual_skin);

        OnChanged?.Invoke();
    }

    private void TrySet(EquipmentType slot, string partId)
    {
        if (string.IsNullOrWhiteSpace(partId)) return;
        var item = equipDatabase.FindByPartId(slot, partId);
        if (item != null) _equipped[slot] = item;
    }

    private static PartsType? ToPartsType(EquipmentType slot)
    {
        switch (slot)
        {
            case EquipmentType.Back: return PartsType.Back;
            case EquipmentType.Beard: return PartsType.Beard;
            case EquipmentType.Boots: return PartsType.Boots;
            case EquipmentType.Bottom: return PartsType.Bottom;
            case EquipmentType.Brow: return PartsType.Brow;
            case EquipmentType.Eyes: return PartsType.Eyes;
            case EquipmentType.Gloves: return PartsType.Gloves;

            case EquipmentType.Hair_Short: return PartsType.Hair_Short;
            case EquipmentType.Hair_Hat: return PartsType.Hair_Hat;
            case EquipmentType.Helmet: return PartsType.Helmet;

            case EquipmentType.Mouth: return PartsType.Mouth;
            case EquipmentType.Eyewear: return PartsType.Eyewear;

            case EquipmentType.Gear_Left: return PartsType.Gear_Left;
            case EquipmentType.Gear_Right: return PartsType.Gear_Right;

            case EquipmentType.Top: return PartsType.Top;
            case EquipmentType.Skin: return PartsType.Skin;

            default: return null;
        }
    }
}

