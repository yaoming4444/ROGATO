using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class FooterTabItem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private RectTransform iconRect;
    [SerializeField] private TMP_Text label;
    [SerializeField] private LayoutElement layoutElement;

    [Header("Sprites")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    [Header("Background Colors")]
    [SerializeField] private bool useBackgroundColor = true;
    [SerializeField] private Color activeBackgroundColor = Color.white;
    [SerializeField] private Color inactiveBackgroundColor = Color.white;

    [Header("Sizes")]
    [SerializeField] private float inactiveWidth = 72f;
    [SerializeField] private float activeWidth = 132f;
    [SerializeField] private float height = 72f;

    [Header("Icon")]
    [SerializeField] private float inactiveIconSize = 28f;
    [SerializeField] private float activeIconSize = 32f;
    [SerializeField] private float inactiveIconX = 0f;
    [SerializeField] private float activeIconX = -28f;
    [SerializeField] private float inactiveIconY = 0f;
    [SerializeField] private float activeIconY = 10f;

    [Header("Label")]
    [SerializeField] private float inactiveLabelAlpha = 0f;
    [SerializeField] private float activeLabelAlpha = 1f;
    [SerializeField] private float labelX = 18f;
    [SerializeField] private float labelY = -14f;
    [SerializeField] private Color activeLabelColor = Color.white;
    [SerializeField] private Color inactiveLabelColor = Color.white;

    private FooterTabsController owner;
    private int index;

    public void Init(FooterTabsController controller, int tabIndex)
    {
        owner = controller;
        index = tabIndex;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (owner != null)
            owner.SelectTab(index);
    }

    public void SetState(bool isActive)
    {
        if (layoutElement != null)
        {
            layoutElement.preferredWidth = isActive ? activeWidth : inactiveWidth;
            layoutElement.preferredHeight = height;
        }

        if (background != null)
        {
            background.sprite = isActive ? activeSprite : inactiveSprite;

            if (useBackgroundColor)
                background.color = isActive ? activeBackgroundColor : inactiveBackgroundColor;
            else
                background.color = Color.white;
        }

        if (iconRect != null)
        {
            float iconSize = isActive ? activeIconSize : inactiveIconSize;
            float iconX = isActive ? activeIconX : inactiveIconX;
            float iconY = isActive ? activeIconY : inactiveIconY;

            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = new Vector2(iconX, iconY);
        }

        if (label != null)
        {
            Color labelColor = isActive ? activeLabelColor : inactiveLabelColor;
            labelColor.a = isActive ? activeLabelAlpha : inactiveLabelAlpha;
            label.color = labelColor;

            label.rectTransform.anchoredPosition = new Vector2(labelX, labelY);
        }
    }
}