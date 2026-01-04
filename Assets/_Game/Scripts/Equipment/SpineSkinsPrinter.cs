using UnityEngine;
using Spine.Unity;

public class SpineSkinsPrinter : MonoBehaviour
{
    private void Start()
    {
        var skel = GetComponent<ISkeletonComponent>();
        var data = skel?.Skeleton?.Data;
        if (data == null) return;

        Debug.Log("=== SKINS ===");
        foreach (var skin in data.Skins)
            Debug.Log(skin.Name);
    }
}

