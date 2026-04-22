using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class AdaptiveGridBottomPadding : MonoBehaviour
{
    public enum SizeSource
    {
        ScreenHeight,
        ViewportHeight
    }

    public enum PaddingMode
    {
        Breakpoints,
        Curve
    }

    [Header("References")]
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private RectTransform viewportRect;

    [Header("How To Measure")]
    [SerializeField] private SizeSource sizeSource = SizeSource.ViewportHeight;
    [SerializeField] private PaddingMode paddingMode = PaddingMode.Breakpoints;

    [Header("Apply Options")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool keepWatchingSizeChanges = true;
    [SerializeField] private bool includeExistingBottomPadding = false;
    [SerializeField] private int extraBottomPadding = 0;

    [Header("Breakpoint Thresholds")]
    [SerializeField] private float smallScreenHeight = 700f;
    [SerializeField] private float mediumScreenHeight = 900f;

    [Header("Breakpoint Padding Values")]
    [SerializeField] private int smallScreenBottomPadding = 300;
    [SerializeField] private int mediumScreenBottomPadding = 180;
    [SerializeField] private int largeScreenBottomPadding = 40;

    [Header("Curve Mode")]
    [Tooltip("X = measured height, Y = bottom padding")]
    [SerializeField]
    private AnimationCurve paddingByHeight = new AnimationCurve(
        new Keyframe(600f, 320f),
        new Keyframe(800f, 180f),
        new Keyframe(1200f, 40f)
    );

    [Header("Debug")]
    [SerializeField] private bool logChanges = false;

    private RectTransform _selfRect;
    private int _baseBottomPadding;
    private float _lastMeasuredHeight = -1f;
    private Vector2Int _lastScreenSize = new Vector2Int(-1, -1);
    private Rect _lastSafeArea;

    private void Reset()
    {
        gridLayoutGroup = GetComponent<GridLayoutGroup>();

        if (viewportRect == null)
        {
            ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect != null && scrollRect.viewport != null)
                viewportRect = scrollRect.viewport;
        }
    }

    private void Awake()
    {
        CacheRefs();

        if (applyOnAwake)
            ApplyPaddingNow();
    }

    private void OnEnable()
    {
        CacheRefs();

        if (applyOnEnable)
            ApplyPaddingNow();
    }

    private void Start()
    {
        CacheRefs();

        if (applyOnStart)
            ApplyPaddingNow();

        // Важно: после полного layout pass
        StartCoroutine(ApplyNextFrame());
    }

    private System.Collections.IEnumerator ApplyNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ApplyPaddingNow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRefs();

        if (!Application.isPlaying)
            ApplyPaddingNow();
    }
#endif

    private void Update()
    {
        if (!keepWatchingSizeChanges)
            return;

        if (DidSizeChange())
            ApplyPaddingNow();
    }

    private void CacheRefs()
    {
        if (gridLayoutGroup == null)
            gridLayoutGroup = GetComponent<GridLayoutGroup>();

        if (_selfRect == null)
            _selfRect = transform as RectTransform;

        if (gridLayoutGroup != null)
            _baseBottomPadding = gridLayoutGroup.padding.bottom;
    }

    private bool DidSizeChange()
    {
        Vector2Int currentScreen = new Vector2Int(Screen.width, Screen.height);
        Rect currentSafeArea = Screen.safeArea;
        float measured = GetMeasuredHeight();

        bool changed =
            currentScreen != _lastScreenSize ||
            currentSafeArea != _lastSafeArea ||
            !Mathf.Approximately(measured, _lastMeasuredHeight);

        if (changed)
        {
            _lastScreenSize = currentScreen;
            _lastSafeArea = currentSafeArea;
            _lastMeasuredHeight = measured;
        }

        return changed;
    }

    public void ApplyPaddingNow()
    {
        if (gridLayoutGroup == null)
            return;

        float measuredHeight = GetMeasuredHeight();
        int adaptivePadding = CalculateAdaptivePadding(measuredHeight);

        int basePadding = includeExistingBottomPadding ? _baseBottomPadding : 0;
        int finalBottom = Mathf.Max(0, basePadding + adaptivePadding + extraBottomPadding);

        RectOffset padding = gridLayoutGroup.padding;
        if (padding.bottom != finalBottom)
        {
            padding.bottom = finalBottom;
            gridLayoutGroup.padding = padding;

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);

            if (logChanges)
            {
                Debug.Log($"[AdaptiveGridBottomPadding] MeasuredHeight={measuredHeight:0.##}, Bottom={finalBottom}");
            }
        }
    }

    private float GetMeasuredHeight()
    {
        if (sizeSource == SizeSource.ViewportHeight && viewportRect != null)
            return viewportRect.rect.height;

        if (_selfRect != null && sizeSource == SizeSource.ViewportHeight)
            return _selfRect.rect.height;

        return Screen.height;
    }

    private int CalculateAdaptivePadding(float measuredHeight)
    {
        switch (paddingMode)
        {
            case PaddingMode.Curve:
                return Mathf.RoundToInt(Mathf.Max(0f, paddingByHeight.Evaluate(measuredHeight)));

            case PaddingMode.Breakpoints:
            default:
                if (measuredHeight <= smallScreenHeight)
                    return smallScreenBottomPadding;

                if (measuredHeight <= mediumScreenHeight)
                    return mediumScreenBottomPadding;

                return largeScreenBottomPadding;
        }
    }

    [ContextMenu("Apply Padding Now")]
    private void ContextApplyPaddingNow()
    {
        ApplyPaddingNow();
    }
}