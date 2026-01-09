using GameCore.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentWindowController : MonoBehaviour
{
    public enum Tab { Equipment, Face }

    [Header("Services")]
    [SerializeField] private MonoBehaviour inventoryServiceBehaviour; // DevInventoryService_AllItems
    [SerializeField] private VisualEquipmentService equipment;        // СЮДА ДАЙ ПРЕФАБНЫЙ (always on) сервис
    [SerializeField] private EquipDatabase equipDatabase;

    [Header("Tabs")]
    [SerializeField] private Tab startTab = Tab.Equipment;
    [SerializeField] private Button tabEquipmentButton;
    [SerializeField] private Button tabFaceButton;

    [Header("Tab Colors")]
    [SerializeField] private Color tabNormalColor = Color.white;
    [SerializeField] private Color tabSelectedColor = new Color(1f, 0.6f, 0.1f, 1f); // оранж

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
        EquipmentType.Eyewear,   // если у тебя eyewear в equipment вкладке
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

        // ВАЖНО: всегда берём singleton
        if (equipment == null)
            equipment = VisualEquipmentService.I;

        if (tabEquipmentButton) tabEquipmentButton.onClick.AddListener(() => SwitchTab(Tab.Equipment));
        if (tabFaceButton) tabFaceButton.onClick.AddListener(() => SwitchTab(Tab.Face));
    }

    private void OnEnable()
    {
        if (equipment == null)
            equipment = VisualEquipmentService.I;

        if (equipment != null)
            equipment.OnChanged += OnEquipmentChanged;

        // НЕ вызывай больше LoadFromState из окна.
        // Сервис сам синкнется на StateChanged и OnEnable.
        equipment?.SyncFromState();

        SwitchTab(startTab, rebuildInventory: true, refreshSlots: true);
    }

    private void OnDisable()
    {
        if (equipment != null)
            equipment.OnChanged -= OnEquipmentChanged;
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
        SetButtonColor(tabFaceButton,      active == Tab.Face      ? tabSelectedColor : tabNormalColor);
    }

    private void SetButtonColor(Button btn, Color c)
    {
        if (!btn) return;

        // красим TargetGraphic (обычно Image)
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
        if (equipment == null || item == null) return;

        if (popup != null)
        {
            popup.ShowForInventoryItem(item, onConfirm: () => equipment.Equip(item));
            return;
        }

        equipment.Equip(item);
    }

    private void RefreshSlots(List<EquipmentSlotView> slots)
    {
        if (equipment == null || slots == null) return;

        foreach (var slotView in slots)
        {
            if (slotView == null) continue;

            var equippedItem = equipment.GetEquipped(slotView.slot);
            slotView.Bind(equippedItem, OnSlotClickedUnequip);
        }
    }

    private void OnSlotClickedUnequip(EquipmentType slot)
    {
        if (equipment == null) return;

        var equippedItem = equipment.GetEquipped(slot);
        if (equippedItem == null) return;

        if (popup != null)
        {
            popup.ShowForEquippedItem(equippedItem, onConfirm: () => equipment.Unequip(slot));
            return;
        }

        equipment.Unequip(slot);
    }
}





