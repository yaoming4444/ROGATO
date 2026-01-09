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

    [Header("Rarity UI")]
    [SerializeField] private Image rarityBgImage;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private Image itemRarityBgImage;

    [Header("Rarity Colors (BG / Text)")]
    [SerializeField] private Color commonBg = new Color32(70, 70, 70, 255);
    [SerializeField] private Color commonText = new Color32(220, 220, 220, 255);

    [SerializeField] private Color uncommonBg = new Color32(30, 110, 60, 255);
    [SerializeField] private Color uncommonText = new Color32(190, 255, 210, 255);

    [SerializeField] private Color rareBg = new Color32(35, 75, 150, 255);
    [SerializeField] private Color rareText = new Color32(200, 220, 255, 255);

    [SerializeField] private Color epicBg = new Color32(110, 50, 150, 255);
    [SerializeField] private Color epicText = new Color32(240, 210, 255, 255);

    [SerializeField] private Color legendaryBg = new Color32(170, 120, 25, 255);
    [SerializeField] private Color legendaryText = new Color32(255, 240, 190, 255);

    [Header("Stats UI")]
    [SerializeField] private GameObject statsGroup;        // <-- NEW: весь блок статов (иконка+цифра)
    [SerializeField] private TMP_Text statsValueText;      // <-- цифра (например: 15)
    [SerializeField] private Image statTypeImage;          // <-- иконка ATK/DEF/HP
    [SerializeField] private Sprite atkSprite;
    [SerializeField] private Sprite defSprite;
    [SerializeField] private Sprite hpSprite;

    [Header("Extra UI (NEW)")]
    [SerializeField] private TMP_Text slotLevelText;       // <-- "LVL X/120"
    [SerializeField] private TMP_Text itemTypeText;        // <-- "Item Type" / slot type

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonLabel;

    [Header("Color (optional)")]
    [SerializeField] private Button colorButton;
    [SerializeField] private EquipmentColorPicker colorPicker;
    [SerializeField] private Sprite skinPreviewSprite;

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

        RefreshRarityUI(item);
        RefreshExtraUI(item);
        RefreshStatsUI(item);
    }

    private void RefreshExtraUI(EquipItemDef item)
    {
        if (item == null)
        {
            if (slotLevelText) slotLevelText.text = "";
            if (itemTypeText) itemTypeText.text = "";
            return;
        }

        var st = GameCore.GameInstance.I?.State;
        int slotLevel = st != null ? st.GetVisualSlotLevel(item.slotType) : 1;

        if (slotLevelText)
            slotLevelText.text = $"LVL {slotLevel}/120";

        if (itemTypeText)
            itemTypeText.text = item.slotType.ToString(); // хочешь красивее Ч сделаем маппинг
    }

    private void RefreshRarityUI(EquipItemDef item)
    {
        if (!rarityBgImage && !rarityText) return;

        if (item == null)
        {
            if (rarityText) rarityText.text = "";
            if (rarityBgImage) rarityBgImage.color = Color.clear;
            return;
        }

        itemRarityBgImage.sprite = item.rarityBG;

        if (rarityText)
            rarityText.text = item.rarity.ToString().ToUpperInvariant();

        var (bg, txt) = GetRarityColors(item.rarity);

        if (rarityBgImage) rarityBgImage.color = bg;
        if (rarityText) rarityText.color = txt;
    }

    private (Color bg, Color text) GetRarityColors(EquipRarity r)
    {
        switch (r)
        {
            case EquipRarity.Common: return (commonBg, commonText);
            case EquipRarity.Uncommon: return (uncommonBg, uncommonText);
            case EquipRarity.Rare: return (rareBg, rareText);
            case EquipRarity.Epic: return (epicBg, epicText);
            case EquipRarity.Legendary: return (legendaryBg, legendaryText);
            default: return (commonBg, commonText);
        }
    }

    private void RefreshStatsUI(EquipItemDef item)
    {
        // если стата нет Ч скрываем весь блок статов
        bool hasStat = (item != null && item.statType != EquipStatType.None);

        if (statsGroup)
            statsGroup.SetActive(hasStat);

        if (!hasStat)
            return;

        var st = GameCore.GameInstance.I?.State;
        int slotLevel = st != null ? st.GetVisualSlotLevel(item.slotType) : 1;
        int value = item.GetStatValueForSlotLevel(slotLevel);

        // только цифра, без текста и без "+"
        if (statsValueText)
            statsValueText.text = value.ToString();

        if (statTypeImage)
        {
            statTypeImage.enabled = true;
            statTypeImage.sprite = GetStatSprite(item.statType);
        }
    }

    private Sprite GetStatSprite(EquipStatType t)
    {
        switch (t)
        {
            case EquipStatType.ATK: return atkSprite;
            case EquipStatType.DEF: return defSprite;
            case EquipStatType.HP: return hpSprite;
            default: return null;
        }
    }

    private void RefreshColorButton(EquipItemDef item)
    {
        if (!colorButton) return;

        var t = GetColorTargetForItem(item);
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

        var st = GameCore.GameInstance.I?.State;
        if (st == null)
            return;

        var initial = GetStateColor(st, target);
        Sprite preview = GetPreviewSpriteForTarget(target, _currentItem);

        colorPicker.Open(
            target: target,
            previewSprite: preview,
            initialColor: (Color)initial
        );
    }

    private Sprite GetPreviewSpriteForTarget(EquipmentColorPicker.Target t, EquipItemDef item)
    {
        if (t == EquipmentColorPicker.Target.Skin)
            return skinPreviewSprite != null ? skinPreviewSprite : (item ? item.icon : null);

        return item ? item.icon : null;
    }

    private EquipmentColorPicker.Target GetColorTargetForItem(EquipItemDef item)
    {
        if (item == null) return EquipmentColorPicker.Target.None;

        switch (item.slotType)
        {
            case EquipmentType.Hair_Short:
            case EquipmentType.Hair_Hat:
                return EquipmentColorPicker.Target.Hair;

            case EquipmentType.Beard:
                return EquipmentColorPicker.Target.Beard;

            case EquipmentType.Brow:
                return EquipmentColorPicker.Target.Brow;

            default:
                return EquipmentColorPicker.Target.None;
        }
    }

    private Color32 GetStateColor(GameCore.PlayerState st, EquipmentColorPicker.Target t)
    {
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







