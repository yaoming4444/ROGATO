using UnityEngine;
using UnityEngine.UI;

public class AdaptiveGridBottomPadding : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Screen Height Thresholds")]
    [SerializeField] private float smallScreenHeight = 1400f;
    [SerializeField] private float mediumScreenHeight = 1800f;

    [Header("Bottom Padding Values")]
    [SerializeField] private int smallScreenBottomPadding = 300;
    [SerializeField] private int mediumScreenBottomPadding = 180;
    [SerializeField] private int largeScreenBottomPadding = 40;
     
    private void Awake()
    {
        ApplyPadding();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gridLayoutGroup != null)
            ApplyPadding();
    }
#endif

    private void ApplyPadding()
    {
        if (gridLayoutGroup == null)
            return;

        float screenHeight = Screen.height;

        RectOffset padding = gridLayoutGroup.padding;

        if (screenHeight <= smallScreenHeight)
        {
            padding.bottom = smallScreenBottomPadding;
        }
        else if (screenHeight <= mediumScreenHeight)
        {
            padding.bottom = mediumScreenBottomPadding;
        }
        else
        {
            padding.bottom = largeScreenBottomPadding;
        }

        gridLayoutGroup.padding = padding;
    }
}