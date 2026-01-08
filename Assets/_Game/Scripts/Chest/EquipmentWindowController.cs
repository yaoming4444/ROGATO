using GameCore.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentWindowController : MonoBehaviour
{
    public enum Tab { Equipment, Face }

    [Header("Services")]
    [SerializeField] private MonoBehaviour inventoryServiceBehaviour; // DevInventoryService_AllItems
    [SerializeField] private VisualEquipmentService equipment;
    [SerializeField] private EquipDatabase equipDatabase;

    [Header("Tabs")]
    [SerializeField] private Tab startTab = Tab.Equipment;
    [SerializeField] private Button tabEquipmentButton;
    [SerializeField] private Button tabFaceButton;

    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject facePanel;

    [Header("Popup (optional)")]
    [SerializeField] private EquipItemPopup popup; // <-- ВАЖНО: конкретный тип, НЕ MonoBehaviour/dynamic

    [Header("UI - Inventory")]
    [SerializeField] private Transform inventoryGridRoot;
    [SerializeField] private InventoryItemView inventoryItemPrefab;

    [Header("UI - Slots (Top)")]
    [SerializeField] private List<EquipmentSlotView> equipmentSlots = new(); // 8 слотов экипы
    [SerializeField] private List<EquipmentSlotView> faceSlots = new();      // 6 face: hair_short, eyes, beard, brow, mouth, gear_left

    private IInventoryService _inventory;
    private Tab _activeTab;

    // ====== НАБОРЫ СЛОТОВ ДЛЯ ФИЛЬТРА ИНВЕНТАРЯ ======
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
            Debug.LogWarning("[EquipmentWindowController] inventoryServiceBehaviour does not implement IInventoryService");

        if (tabEquipmentButton) tabEquipmentButton.onClick.AddListener(() => SwitchTab(Tab.Equipment));
        if (tabFaceButton) tabFaceButton.onClick.AddListener(() => SwitchTab(Tab.Face));
    }

    private void OnEnable()
    {
        if (equipment != null)
            equipment.OnChanged += OnEquipmentChanged;

        // Восстановление экипы для UI из PlayerState (чтобы после запуска было видно)
        var gi = GameCore.GameInstance.I;
        if (equipment != null && equipDatabase != null && gi != null && gi.State != null)
            equipment.LoadFromState(gi.State, equipDatabase);

        SwitchTab(startTab, rebuildInventory: true, refreshSlots: true);
    }

    private void OnDisable()
    {
        if (equipment != null)
            equipment.OnChanged -= OnEquipmentChanged;
    }

    private void OnEquipmentChanged()
    {
        // лучше обновлять оба набора, чтобы при переключении вкладок всё уже было актуально
        RefreshSlots(equipmentSlots);
        RefreshSlots(faceSlots);
    }

    private void SwitchTab(Tab tab, bool rebuildInventory = true, bool refreshSlots = true)
    {
        _activeTab = tab;

        if (equipmentPanel) equipmentPanel.SetActive(tab == Tab.Equipment);
        if (facePanel) facePanel.SetActive(tab == Tab.Face);

        // оставь интерактивными обе, либо делай выбранную неинтерактивной — на вкус
        if (tabEquipmentButton) tabEquipmentButton.interactable = true;
        if (tabFaceButton) tabFaceButton.interactable = true;

        // выделяем выбранную (включит Selected Color)
        if (tab == Tab.Equipment) SetTabSelected(tabEquipmentButton, true);
        else SetTabSelected(tabFaceButton, true);

        if (rebuildInventory) BuildInventory();

        if (refreshSlots)
        {
            RefreshSlots(equipmentSlots);
            RefreshSlots(faceSlots);
        }
    }


    private void SetTabSelected(Button btn, bool selected)
    {
        if (!btn) return;

        // Важно: чтобы работало Selected, кнопка должна быть "Selectable"
        // (Button = Selectable)
        if (selected)
            btn.Select(); // даст Selected state
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

        // Попап: надеваем только после кнопки Equip
        if (popup != null)
        {
            popup.ShowForInventoryItem(
                item,
                onConfirm: () => equipment.Equip(item)
            );
            return;
        }

        // fallback без попапа
        equipment.Equip(item);
    }

    private void RefreshSlots(List<EquipmentSlotView> slots)
    {
        if (equipment == null || slots == null) return;

        foreach (var slotView in slots)
        {
            if (slotView == null) continue;

            var equippedItem = equipment.GetEquipped(slotView.slot);

            // SlotView сам решит: показывать дефолтную иконку / делать кнопку интерактивной
            slotView.Bind(equippedItem, OnSlotClickedUnequip);
        }
    }

    private void OnSlotClickedUnequip(EquipmentType slot)
    {
        if (equipment == null) return;

        var equippedItem = equipment.GetEquipped(slot);
        if (equippedItem == null)
            return; // если пусто — ничего не делаем (и кнопка у тебя должна быть неинтерактивной)

        if (popup != null)
        {
            popup.ShowForEquippedItem(
                equippedItem,
                onConfirm: () => equipment.Unequip(slot)
            );
            return;
        }

        equipment.Unequip(slot);
    }
}




