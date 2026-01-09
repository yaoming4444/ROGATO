using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotView : MonoBehaviour
{
    [Header("Config")]
    public EquipmentType slot;

    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Image backgroundImage; 
    [SerializeField] private Sprite defaultBackgroundImage;
    [SerializeField] private Button button;

    private System.Action<EquipmentType> _onUnequip;

    public void Bind(EquipItemDef equipped, System.Action<EquipmentType> onUnequip)
    {
        _onUnequip = onUnequip;

        // --- ICON ---
        if (icon)
        {
            icon.sprite = equipped != null && equipped.icon != null
                ? equipped.icon
                : defaultIcon;

            icon.enabled = true;
        }

        // --- BG IMAGE --- 
        if (backgroundImage) 
        { 
            backgroundImage.sprite = equipped != null && equipped.rarityBG != null 
                ? equipped.rarityBG 
                : defaultBackgroundImage;

            backgroundImage.enabled = true; 
        }


        // --- BUTTON ---
        if (button)
        {
            button.onClick.RemoveAllListeners();

            bool hasItem = equipped != null;

            // слот кликабелен ТОЛЬКО если надет предмет
            button.interactable = hasItem;

            if (hasItem)
            {
                button.onClick.AddListener(() =>
                    _onUnequip?.Invoke(slot)
                );
            }
        }
    }
}


