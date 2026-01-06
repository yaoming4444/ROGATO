using GameCore.Items;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentWindowController : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private MonoBehaviour inventoryServiceBehaviour; // DevInventoryService_AllItems
    [SerializeField] private VisualEquipmentService equipment;

    [Header("UI")]
    [SerializeField] private Transform inventoryGridRoot;
    [SerializeField] private InventoryItemView inventoryItemPrefab;

    [SerializeField] private List<EquipmentSlotView> equipSlots = new();

    private IInventoryService _inventory;

    private void Awake()
    {
        _inventory = inventoryServiceBehaviour as IInventoryService;
    }

    private void OnEnable()
    {
        if (equipment != null) equipment.OnChanged += RefreshEquipSlots;

        BuildInventory();
        RefreshEquipSlots();
    }

    private void OnDisable()
    {
        if (equipment != null) equipment.OnChanged -= RefreshEquipSlots;
    }

    private void BuildInventory()
    {
        if (_inventory == null || inventoryGridRoot == null || inventoryItemPrefab == null) return;

        // clear
        for (int i = inventoryGridRoot.childCount - 1; i >= 0; i--)
            Destroy(inventoryGridRoot.GetChild(i).gameObject);

        var items = _inventory.GetOwnedItems();
        foreach (var item in items)
        {
            if (item == null) continue;
            var view = Instantiate(inventoryItemPrefab, inventoryGridRoot);
            view.Bind(item, OnInventoryItemClicked);
        }
    }

    private void OnInventoryItemClicked(EquipItemDef item)
    {
        if (equipment == null || item == null) return;
        Debug.Log($"Clicked item: {item.itemId}");
        equipment.Equip(item);
    }

    private void RefreshEquipSlots()
    {
        if (equipment == null) return;

        foreach (var slotView in equipSlots)
        {
            if (slotView == null) continue;
            var equipped = equipment.GetEquipped(slotView.slot);
            slotView.Bind(equipped, s => equipment.Unequip(s));
        }
    }
}

