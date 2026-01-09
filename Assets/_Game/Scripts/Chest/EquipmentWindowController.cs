using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentWindowController : MonoBehaviour
{
    public enum Tab { Equipment, Face }

    [Header("Services")]
    [SerializeField] private MonoBehaviour inventoryServiceBehaviour; // DevInventoryService_AllItems (implements IInventoryService)
    [SerializeField] private EquipDatabase equipDatabase;

    [Header("Tabs")]
    [SerializeField] private Tab startTab = Tab.Equipment;
    [SerializeField] private Button tabEquipmentButton;
    [SerializeField] private Button tabFaceButton;

    [Header("Tab Colors")]
    [SerializeField] private Color tabNormalColor = Color.white;
    [SerializeField] private Color tabSelectedColor = new Color(1f, 0.6f, 0.1f, 1f);

    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject facePanel;

    [Header("Popup (optional)")]
    [SerializeField] private EquipItemPopup popup;

    [Header("UI - Inventory")]
    [SerializeField] private Transform inventoryGridRoot;
    [SerializeField] private InventoryItemView inventoryItemPrefab;

    [Header("UI - Slots (Top)")]
    [SerializeField] private List<EquipmentSlotView> equipmentSlots = new();
    [SerializeField] private List<EquipmentSlotView> faceSlots = new();

    private IInventoryService _inventory;
    private Tab _activeTab;

    // мы больше не сериализуем equipment, берём singleton
    private VisualEquipmentService _equipment;
    private Coroutine _ensureRoutine;

    private static readonly HashSet<EquipmentType> EquipmentSlotSet = new()
    {
        EquipmentType.Helmet,
        EquipmentType.Top,
        EquipmentType.Bottom,
        EquipmentType.Boots,
        EquipmentType.Gloves,
        EquipmentType.Back,
        EquipmentType.Gear_Left,
        EquipmentType.Gear_Right,
        EquipmentType.Eyewear,
    };

    private static readonly HashSet<EquipmentType> FaceSlotSet = new()
    {
        EquipmentType.Hair_Short,
        EquipmentType.Eyes,
        EquipmentType.Beard,
        EquipmentType.Brow,
        EquipmentType.Mouth,
        EquipmentType.Gear_Left,
    };

    private void Awake()
    {
        _inventory = inventoryServiceBehaviour as IInventoryService;
        if (_inventory == null)
            Debug.LogWarning("[EWC] inventoryServiceBehaviour does not implement IInventoryService");

        if (tabEquipmentButton) tabEquipmentButton.onClick.AddListener(() => SwitchTab(Tab.Equipment));
        if (tabFaceButton) tabFaceButton.onClick.AddListener(() => SwitchTab(Tab.Face));
    }

    private void OnEnable()
    {
        // стартуем вкладку/UI сразу
        SwitchTab(startTab, rebuildInventory: true, refreshSlots: true);

        // и отдельно гарантируем, что сервис визуалки будет найден (даже если DontDestroy ещё не поднялся)
        if (_ensureRoutine != null) StopCoroutine(_ensureRoutine);
        _ensureRoutine = StartCoroutine(EnsureVisualServiceAndBind());
    }

    private void OnDisable()
    {
        if (_ensureRoutine != null)
        {
            StopCoroutine(_ensureRoutine);
            _ensureRoutine = null;
        }

        if (_equipment != null)
            _equipment.OnChanged -= OnEquipmentChanged;
    }

    private IEnumerator EnsureVisualServiceAndBind()
    {
        // на Unity 6/2022+ порядок иногда такой, что DontDestroy создаётся чуть позже
        // подождём немного кадров и найдём singleton
        const int framesToWait = 30;

        for (int i = 0; i < framesToWait; i++)
        {
            TryResolveVisualService();
            if (_equipment != null) break;
            yield return null;
        }

        if (_equipment == null)
        {
            Debug.LogWarning("[EWC] VisualEquipmentService singleton not found. Popup will open, but Equip/Unequip won't work until service exists.");
            yield break;
        }

        // подписка
        _equipment.OnChanged -= OnEquipmentChanged;
        _equipment.OnChanged += OnEquipmentChanged;

        // сервис сам умеет SyncFromState на StateChanged, но раз мы в UI — можно форснуть один раз
        _equipment.SyncFromState();

        // обновим слоты
        OnEquipmentChanged();
    }

    private void TryResolveVisualService()
    {
        if (_equipment != null) return;

        // 1) предпочитаем singleton
        if (VisualEquipmentService.I != null)
        {
            _equipment = VisualEquipmentService.I;
            return;
        }

        // 2) fallback: найти в сцене (если вдруг забыли сделать singleton)
#if UNITY_2023_1_OR_NEWER
        _equipment = Object.FindFirstObjectByType<VisualEquipmentService>(FindObjectsInactive.Include);
#else
        _equipment = Object.FindObjectOfType<VisualEquipmentService>(true);
#endif
    }

    private void OnEquipmentChanged()
    {
        RefreshSlots(equipmentSlots);
        RefreshSlots(faceSlots);
    }

    private void SwitchTab(Tab tab, bool rebuildInventory = true, bool refreshSlots = true)
    {
        _activeTab = tab;

        if (equipmentPanel) equipmentPanel.SetActive(tab == Tab.Equipment);
        if (facePanel) facePanel.SetActive(tab == Tab.Face);

        ApplyTabColors(tab);

        if (rebuildInventory) BuildInventory();

        if (refreshSlots)
        {
            RefreshSlots(equipmentSlots);
            RefreshSlots(faceSlots);
        }
    }

    private void ApplyTabColors(Tab active)
    {
        SetButtonColor(tabEquipmentButton, active == Tab.Equipment ? tabSelectedColor : tabNormalColor);
        SetButtonColor(tabFaceButton, active == Tab.Face ? tabSelectedColor : tabNormalColor);
    }

    private void SetButtonColor(Button btn, Color c)
    {
        if (!btn) return;
        if (btn.targetGraphic != null)
            btn.targetGraphic.color = c;
    }

    private void BuildInventory()
    {
        if (_inventory == null || inventoryGridRoot == null || inventoryItemPrefab == null)
            return;

        for (int i = inventoryGridRoot.childCount - 1; i >= 0; i--)
            Destroy(inventoryGridRoot.GetChild(i).gameObject);

        var items = _inventory.GetOwnedItems();
        if (items == null) return;

        var allowed = (_activeTab == Tab.Equipment) ? EquipmentSlotSet : FaceSlotSet;

        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.slotType == EquipmentType.None) continue;
            if (!allowed.Contains(item.slotType)) continue;

            var view = Instantiate(inventoryItemPrefab, inventoryGridRoot);
            view.Bind(item, OnInventoryItemClicked);
        }
    }

    private void OnInventoryItemClicked(EquipItemDef item)
    {
        if (item == null) return;

        // Попап показываем ВСЕГДА (чтобы UI не “молчал”)
        if (popup != null)
        {
            popup.ShowForInventoryItem(item, onConfirm: () =>
            {
                TryResolveVisualService();
                if (_equipment == null)
                {
                    Debug.LogWarning("[EWC] Can't Equip: VisualEquipmentService is null (singleton not ready?).");
                    return;
                }

                _equipment.Equip(item);
            });
            return;
        }

        // Без попапа — тоже пытаемся надеть
        TryResolveVisualService();
        if (_equipment == null)
        {
            Debug.LogWarning("[EWC] Can't Equip: VisualEquipmentService is null (singleton not ready?).");
            return;
        }

        _equipment.Equip(item);
    }

    private void RefreshSlots(List<EquipmentSlotView> slots)
    {
        if (slots == null) return;

        TryResolveVisualService();

        foreach (var slotView in slots)
        {
            if (slotView == null) continue;

            var equippedItem = (_equipment != null) ? _equipment.GetEquipped(slotView.slot) : null;
            slotView.Bind(equippedItem, OnSlotClickedUnequip);
        }
    }

    private void OnSlotClickedUnequip(EquipmentType slot)
    {
        TryResolveVisualService();
        if (_equipment == null)
        {
            Debug.LogWarning("[EWC] Can't Unequip: VisualEquipmentService is null (singleton not ready?).");
            return;
        }

        var equippedItem = _equipment.GetEquipped(slot);
        if (equippedItem == null) return;

        if (popup != null)
        {
            popup.ShowForEquippedItem(equippedItem, onConfirm: () => _equipment.Unequip(slot));
            return;
        }

        _equipment.Unequip(slot);
    }
}






