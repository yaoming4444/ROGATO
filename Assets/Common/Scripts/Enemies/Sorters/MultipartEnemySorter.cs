using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MultipartEnemySorter : MonoBehaviour
{
    [Serializable]
    public class RenderPart
    {
        public string name;
        public SpriteRenderer renderer;
        public int orderOffset;
    }

    [Header("Sorting")]
    [SerializeField] private Transform sortingAnchor;
    [SerializeField] private string sortingLayerName = "Actors";
    [SerializeField] private int baseOffset = 0;
    [SerializeField] private float precision = 100f;
    [SerializeField] private bool updateEveryFrame = true;

    [Header("Parts")]
    [SerializeField] private RenderPart[] parts;

    private void Reset()
    {
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
        if (sortingAnchor == null)
            sortingAnchor = transform;

        int baseOrder = Mathf.RoundToInt(-sortingAnchor.position.y * precision) + baseOffset;

        if (parts == null) return;

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == null || parts[i].renderer == null)
                continue;

            parts[i].renderer.sortingLayerName = sortingLayerName;
            parts[i].renderer.sortingOrder = baseOrder + parts[i].orderOffset;
        }
    }
}