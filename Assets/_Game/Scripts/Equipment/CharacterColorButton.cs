using UnityEngine;
using UnityEngine.UI;

public class CharacterColorButton : MonoBehaviour
{
    [SerializeField] private EquipmentColorPicker picker;
    [SerializeField] private Sprite skinPreviewSprite; // заготовленная иконка "Skin"

    public void OpenSkinPicker()
    {
        var st = GameCore.GameInstance.I?.State;
        if (st == null || picker == null) return;

        picker.Open(
            EquipmentColorPicker.Target.Skin,
            skinPreviewSprite,
            (Color)st.GetSkinColor32()
        );
    }
}

