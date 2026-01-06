using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotView : MonoBehaviour
{
    public EquipmentType slot;

    [SerializeField] private Image equippedIcon;
    [SerializeField] private Button unequipButton;

    private System.Action<EquipmentType> _onUnequip;

    public void Bind(EquipItemDef equipped, System.Action<EquipmentType> onUnequip)
    {
        _onUnequip = onUnequip;

        if (equippedIcon)
        {
            equippedIcon.sprite = equipped ? equipped.icon : null;
            equippedIcon.enabled = equipped != null;
        }

        if (unequipButton)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(() => _onUnequip?.Invoke(slot));
            unequipButton.gameObject.SetActive(equipped != null);
        }
    }
}

