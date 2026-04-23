using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private bool applyLeft = true;
    [SerializeField] private bool applyRight = true;
    [SerializeField] private bool applyTop = true;
    [SerializeField] private bool applyBottom = false;

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea ||
            lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Rect safeArea = Screen.safeArea;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        Vector2 finalMin = rectTransform.anchorMin;
        Vector2 finalMax = rectTransform.anchorMax;

        if (applyLeft) finalMin.x = anchorMin.x;
        if (applyBottom) finalMin.y = anchorMin.y;
        if (applyRight) finalMax.x = anchorMax.x;
        if (applyTop) finalMax.y = anchorMax.y;

        rectTransform.anchorMin = finalMin;
        rectTransform.anchorMax = finalMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}