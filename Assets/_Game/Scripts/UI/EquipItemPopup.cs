using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipItemPopup : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonLabel;

    [Header("Color (optional)")]
    [SerializeField] private Button colorButton;                 // кнопка "Color"
    [SerializeField] private EquipmentColorPicker colorPicker;   // ссылка на ColorPicker
    [SerializeField] private Sprite skinPreviewSprite;           // <-- заготовленный спрайт дл€ кожи (head/skin preview)

    private Action _onConfirm;
    private EquipItemDef _currentItem;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        if (confirmButton) confirmButton.onClick.AddListener(Confirm);

        if (colorButton)
        {
            colorButton.onClick.RemoveAllListeners();
            colorButton.onClick.AddListener(OnColorButton);
        }

        Hide();
    }

    public void ShowForInventoryItem(EquipItemDef item, Action onConfirm)
    {
        _currentItem = item;
        _onConfirm = onConfirm;

        ApplyCommonUI(item);

        if (confirmButtonLabel) confirmButtonLabel.text = "Equip";
        if (confirmButton) confirmButton.interactable = item != null;

        RefreshColorButton(item);

        Show();
    }

    public void ShowForEquippedItem(EquipItemDef item, Action onConfirm)
    {
        _currentItem = item;
        _onConfirm = onConfirm;

        ApplyCommonUI(item);

        if (confirmButtonLabel) confirmButtonLabel.text = "Unequip";
        if (confirmButton) confirmButton.interactable = item != null;

        RefreshColorButton(item);

        Show();
    }

    private void ApplyCommonUI(EquipItemDef item)
    {
        if (icon) icon.sprite = item ? item.icon : null;
        if (title) title.text = item ? item.displayName : "Item";
        if (description) description.text = item ? item.description : "";
    }

    private void RefreshColorButton(EquipItemDef item)
    {
        if (!colorButton)
            return;

        var t = GetColorTargetForItem(item);

        // показываем кнопку только если:
        // - предмет краситс€ (Hair/Beard/Brow) »Ћ» это отдельна€ кнопка дл€ кожи (если ты еЄ тоже используешь через этот попап)
        bool canShow = (t != EquipmentColorPicker.Target.None) && colorPicker != null;

        colorButton.gameObject.SetActive(canShow);
        colorButton.interactable = (item != null && colorPicker != null);
    }

    private void OnColorButton()
    {
        if (_currentItem == null || colorPicker == null)
            return;

        var target = GetColorTargetForItem(_currentItem);
        if (target == EquipmentColorPicker.Target.None)
            return;

        var gi = GameCore.GameInstance.I;
        var st = gi?.State;
        if (st == null)
            return;

        // 1) берем текущий сохраненный цвет из PlayerState
        var initial = GetStateColor(st, target);

        // 2) превью-спрайт дл€ outputPreviewImage:
        //    - дл€ Skin: заготовленный спрайт skinPreviewSprite
        //    - дл€ Hair/Beard/Brow: иконка текущего предмета (или можешь заменить на отдельные спрайты)
        Sprite preview = GetPreviewSpriteForTarget(target, _currentItem);

        // 3) открываем пикер (и ¬ј∆Ќќ: auto-apply теперь выключен Ч примен€ем через Apply кнопкой на пикере)
        colorPicker.Open(
            target: target,
            previewSprite: preview,
            initialColor: (Color)initial
        );
    }

    private Sprite GetPreviewSpriteForTarget(EquipmentColorPicker.Target t, EquipItemDef item)
    {
        if (t == EquipmentColorPicker.Target.Skin)
        {
            // твой "заготовленный спрайт" дл€ кожи
            return skinPreviewSprite != null ? skinPreviewSprite : (item ? item.icon : null);
        }

        // Hair/Beard/Brow
        return item ? item.icon : null;
    }

    private EquipmentColorPicker.Target GetColorTargetForItem(EquipItemDef item)
    {
        if (item == null) return EquipmentColorPicker.Target.None;

        // под твои face слоты:
        switch (item.slotType)
        {
            case EquipmentType.Hair_Short:
            case EquipmentType.Hair_Hat:
                return EquipmentColorPicker.Target.Hair;

            case EquipmentType.Beard:
                return EquipmentColorPicker.Target.Beard;

            case EquipmentType.Brow:
                return EquipmentColorPicker.Target.Brow;

            // если захочешь вызывать skin picker через попап Ч раскомментируй:
            // case EquipmentType.Skin:
            //     return EquipmentColorPicker.Target.Skin;

            default:
                return EquipmentColorPicker.Target.None;
        }
    }

    private Color32 GetStateColor(GameCore.PlayerState st, EquipmentColorPicker.Target t)
    {
        // Ёти методы должны быть в PlayerState:
        // GetHairColor32 / GetBeardColor32 / GetBrowColor32 (как GetSkinColor32)
        switch (t)
        {
            case EquipmentColorPicker.Target.Skin: return st.GetSkinColor32();
            case EquipmentColorPicker.Target.Hair: return st.GetHairColor32();
            case EquipmentColorPicker.Target.Beard: return st.GetBeardColor32();
            case EquipmentColorPicker.Target.Brow: return st.GetBrowColor32();
            default: return new Color32(255, 255, 255, 255);
        }
    }

    private void Confirm()
    {
        var cb = _onConfirm;
        _onConfirm = null;
        Hide();
        cb?.Invoke();
    }

    public void Show()
    {
        if (root) root.SetActive(true);
        else gameObject.SetActive(true);
    }

    public void Hide()
    {
        _onConfirm = null;
        _currentItem = null;

        if (root) root.SetActive(false);
        else gameObject.SetActive(false);
    }
}




