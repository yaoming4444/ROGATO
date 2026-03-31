using UnityEngine;

[DisallowMultipleComponent]
public class SimpleEnemySorter : MonoBehaviour
{
    [Header("Sorting")]
    [SerializeField] private Transform sortingAnchor;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private string sortingLayerName = "Actors";
    [SerializeField] private int orderOffset = 0;
    [SerializeField] private float precision = 100f;
    [SerializeField] private bool updateEveryFrame = true;

    private void Reset()
    {
        if (sortingAnchor == null)
            sortingAnchor = transform;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        ApplySorting();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame)
            ApplySorting();
    }

    [ContextMenu("Apply Sorting")]
    public void ApplySorting()
    {
        if (sortingAnchor == null)
            sortingAnchor = transform;

        if (targetRenderer == null)
            return;

        int order = Mathf.RoundToInt(-sortingAnchor.position.y * precision) + orderOffset;

        targetRenderer.sortingLayerName = sortingLayerName;
        targetRenderer.sortingOrder = order;
    }
}