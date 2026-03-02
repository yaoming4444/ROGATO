using UnityEngine;
using Spine.Unity;

public class HideSpineArms : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation sa;

    void Start()
    {
        HideSlot("arm_l");
        HideSlot("arm_r");

        HideSlot("top_arm_l");
        HideSlot("top_arm_r");

        // если нужно убрать и перчатки/кисти:
        HideSlot("gloves_l");
        HideSlot("gloves_r");

        // обновим меш, чтобы сразу применилось
        sa.LateUpdate();
    }

    void HideSlot(string slotName)
    {
        var slot = sa.Skeleton.FindSlot(slotName);
        if (slot != null) slot.A = 0f;
    }
}
