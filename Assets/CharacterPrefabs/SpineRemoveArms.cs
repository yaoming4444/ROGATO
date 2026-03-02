using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineRemoveArms : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation sa;

    void LateUpdate()
    {
        // каждый кадр, чтобы PartsManager/анимации не возвращали обратно
        RemoveAttachment("arm_l", "arm_l_skin");
        RemoveAttachment("arm_r", "arm_r_skin");

        // если ещё что-то рисуется в top_arm — можно добить так:
        // RemoveAttachment("top_arm_l", "top_arm_l");
        // RemoveAttachment("top_arm_r", "top_arm_r");
    }

    void RemoveAttachment(string slotName, string attachmentName)
    {
        var skeleton = sa.Skeleton;
        var slot = skeleton.FindSlot(slotName);
        if (slot == null) return;

        // если на слоте сейчас нужный attachment — убираем
        if (slot.Attachment != null && slot.Attachment.Name == attachmentName)
            slot.Attachment = null;
    }
}