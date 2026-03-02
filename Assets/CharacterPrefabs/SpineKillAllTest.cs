using UnityEngine;
using Spine.Unity;

public class SpineKillAllTest : MonoBehaviour
{
    [SerializeField] SkeletonAnimation sa;

    void LateUpdate()
    {
        foreach (var slot in sa.Skeleton.Slots)
            slot.A = 0f; // сделать всё прозрачным
    }
}