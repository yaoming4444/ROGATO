using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image rarityBG;
    [SerializeField] private Button button;


    private EquipItemDef _item;
    private System.Action<EquipItemDef> _onClick;

    public void Bind(EquipItemDef item, System.Action<EquipItemDef> onClick)
    {
        _item = item;
        _onClick = onClick;

        if (icon) icon.sprite = item ? item.icon : null;
        if (rarityBG) rarityBG.sprite = item ? item.rarityBG : null;
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_item));
        }

        Debug.Log($"[InventoryItemView] Bind called. item={(item ? item.itemId : "null")} button={(button ? "OK" : "NULL")}");
    }
}
