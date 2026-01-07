using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentColorPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public enum Target { None, Skin, Hair, Beard, Brow }

    [Header("UI")]
    [SerializeField] private RectTransform svRect;     // квадрат
    [SerializeField] private Image svImage;            // картинка квадрата
    [SerializeField] private RectTransform svHandle;

    [SerializeField] private RectTransform hueRect;    // полоска
    [SerializeField] private Image hueImage;
    [SerializeField] private RectTransform hueHandle;

    [SerializeField] private Image outputImage;        // цвет-квадратик
    [SerializeField] private Image outputPreviewImage; // превью-иконка (спрайт)

    [Header("Buttons (optional)")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("Target")]
    [SerializeField] private Target applyTarget = Target.Skin;

    public event Action<Color32> OnColorChanged;

    // HSV
    [Range(0f, 1f)][SerializeField] private float h = 0f;
    [Range(0f, 1f)][SerializeField] private float s = 1f;
    [Range(0f, 1f)][SerializeField] private float v = 1f;

    private Texture2D _hueTex;
    private Texture2D _svTex;

    private enum ActiveArea { None, SV, Hue }
    private ActiveArea _active = ActiveArea.None;

    private Color32 _initialColor = new Color32(255, 255, 255, 255);

    private void Awake()
    {
        if (applyButton) applyButton.onClick.AddListener(Apply);
        if (resetButton) resetButton.onClick.AddListener(ResetToInitial);
        if (closeButton) closeButton.onClick.AddListener(Hide);

        BuildHueTexture();
        BuildSVTexture();
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (_hueTex) Destroy(_hueTex);
        if (_svTex) Destroy(_svTex);
    }

    // =========================
    // Public API
    // =========================

    /// <summary>
    /// Открыть пикер под конкретную цель + превью спрайт + начальный цвет (без автоприменения)
    /// </summary>
    public void Open(Target target, Sprite previewSprite, Color initialColor)
    {
        applyTarget = target;

        SetPreviewSprite(previewSprite);

        _initialColor = (Color32)initialColor;
        SetColor(initialColor, emit: true);

        Show();
    }

    public void Close() => Hide();

    public void SetTarget(Target t) => applyTarget = t;
    public Target GetTarget() => applyTarget;

    public void SetPreviewSprite(Sprite sprite)
    {
        if (!outputPreviewImage) return;
        outputPreviewImage.sprite = sprite;
        outputPreviewImage.enabled = (sprite != null);
    }

    public void SetColor(Color color) => SetColor(color, emit: true);

    public Color GetColor() => Color.HSVToRGB(h, s, v);

    /// <summary>
    /// Применить текущий цвет в GameInstance/PlayerState (по applyTarget)
    /// </summary>
    public void Apply()
    {
        var c = (Color32)Color.HSVToRGB(h, s, v);
        ApplyToGameInstance(c);
    }

    /// <summary>
    /// Вернуть цвет к тому, с которым открыли пикер (НЕ применяет, только меняет выбор)
    /// </summary>
    public void ResetToInitial()
    {
        SetColor(_initialColor, emit: true);
    }

    // =========================
    // Input
    // =========================

    public void OnPointerDown(PointerEventData eventData)
    {
        _active = HitTest(eventData);
        HandleInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleInput(eventData);
    }

    private ActiveArea HitTest(PointerEventData e)
    {
        if (svRect && RectTransformUtility.RectangleContainsScreenPoint(svRect, e.position, e.pressEventCamera))
            return ActiveArea.SV;

        if (hueRect && RectTransformUtility.RectangleContainsScreenPoint(hueRect, e.position, e.pressEventCamera))
            return ActiveArea.Hue;

        return ActiveArea.None;
    }

    private void HandleInput(PointerEventData e)
    {
        if (_active == ActiveArea.None) return;

        if (_active == ActiveArea.SV) SetSVFromPointer(e);
        else if (_active == ActiveArea.Hue) SetHueFromPointer(e);

        UpdateUI();
        Emit(); // только событие/превью, без apply
    }

    private void SetSVFromPointer(PointerEventData e)
    {
        if (!svRect) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(svRect, e.position, e.pressEventCamera, out var local))
            return;

        var r = svRect.rect;
        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float ny = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        s = Mathf.Clamp01(nx);
        v = Mathf.Clamp01(ny);
    }

    private void SetHueFromPointer(PointerEventData e)
    {
        if (!hueRect) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hueRect, e.position, e.pressEventCamera, out var local))
            return;

        var r = hueRect.rect;
        float nx = Mathf.InverseLerp(r.xMin, r.xMax, local.x);

        h = Mathf.Clamp01(nx);
        BuildSVTexture();
    }

    // =========================
    // UI Update
    // =========================

    private void UpdateUI()
    {
        var c = Color.HSVToRGB(h, s, v);
        if (outputImage) outputImage.color = c;

        if (svHandle && svRect)
        {
            var r = svRect.rect;
            svHandle.anchoredPosition = new Vector2(
                Mathf.Lerp(r.xMin, r.xMax, s),
                Mathf.Lerp(r.yMin, r.yMax, v)
            );
        }

        if (hueHandle && hueRect)
        {
            var r = hueRect.rect;
            hueHandle.anchoredPosition = new Vector2(
                Mathf.Lerp(r.xMin, r.xMax, h),
                hueHandle.anchoredPosition.y
            );
        }
    }

    private void Emit()
    {
        var c = (Color32)Color.HSVToRGB(h, s, v);
        OnColorChanged?.Invoke(c);
    }

    private void SetColor(Color color, bool emit)
    {
        Color.RGBToHSV(color, out h, out s, out v);
        BuildSVTexture();
        UpdateUI();
        if (emit) Emit();
    }

    // =========================
    // Apply to Game
    // =========================

    private void ApplyToGameInstance(Color32 c)
    {
        var gi = GameCore.GameInstance.I;
        var st = gi?.State;
        if (st == null) return;

        switch (applyTarget)
        {
            case Target.Skin:
                st.SetSkinColor32(c);
                break;
            case Target.Hair:
                st.SetHairColor32(c);
                break;
            case Target.Beard:
                st.SetBeardColor32(c);
                break;
            case Target.Brow:
                st.SetBrowColor32(c);
                break;
            default:
                return;
        }

        gi.RaiseStateChanged();
    }

    // =========================
    // Texture generation
    // =========================

    private void BuildHueTexture()
    {
        if (!hueImage) return;

        const int w = 256;
        const int hTex = 1;

        if (_hueTex) Destroy(_hueTex);

        _hueTex = new Texture2D(w, hTex, TextureFormat.RGBA32, false);
        _hueTex.wrapMode = TextureWrapMode.Clamp;
        _hueTex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < w; x++)
        {
            float hh = x / (float)(w - 1);
            _hueTex.SetPixel(x, 0, Color.HSVToRGB(hh, 1f, 1f));
        }
        _hueTex.Apply();

        hueImage.sprite = Sprite.Create(_hueTex, new Rect(0, 0, w, hTex), new Vector2(0.5f, 0.5f));
        hueImage.type = Image.Type.Simple;
    }

    private void BuildSVTexture()
    {
        if (!svImage) return;

        const int size = 256;

        if (_svTex == null)
        {
            _svTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _svTex.wrapMode = TextureWrapMode.Clamp;
            _svTex.filterMode = FilterMode.Bilinear;
        }

        for (int y = 0; y < size; y++)
        {
            float vv = y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                float ss = x / (float)(size - 1);
                _svTex.SetPixel(x, y, Color.HSVToRGB(this.h, ss, vv));
            }
        }
        _svTex.Apply();

        svImage.sprite = Sprite.Create(_svTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        svImage.type = Image.Type.Simple;
    }

    // =========================
    // Show/Hide
    // =========================

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}




