using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class SortingGroupYSorter : MonoBehaviour
{
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private Transform sortingAnchor;
    [SerializeField] private int baseOffset = 0;
    [SerializeField] private float precision = 100f;
    [SerializeField] private bool updateEveryFrame = true;

    private void Reset()
    {
        if (sortingGroup == null)
            sortingGroup = GetComponent<SortingGroup>();

        if (sortingAnchor == null)
            sortingAnchor = transform;
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
        if (sortingGroup == null)
            sortingGroup = GetComponent<SortingGroup>();

        if (sortingGroup == null)
            return;

        if (sortingAnchor == null)
            sortingAnchor = transform;

        sortingGroup.sortingOrder =
            Mathf.RoundToInt(-sortingAnchor.position.y * precision) + baseOffset;
    }
}