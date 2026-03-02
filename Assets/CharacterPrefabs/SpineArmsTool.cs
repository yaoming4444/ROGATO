using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineArmsTool : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private SkeletonAnimation sa;

    [Header("Runtime settings")]
    [SerializeField] private bool removeArms = true;

    // Слоты, которые чаще всего отвечают за руки
    [SerializeField]
    private string[] armSlots = new[]
    {
        "arm_l", "arm_r",
        "top_arm_l", "top_arm_r",
        "gloves_l", "gloves_r"
    };

    void Reset()
    {
        if (!sa) sa = GetComponentInChildren<SkeletonAnimation>();
        if (!sa) sa = GetComponent<SkeletonAnimation>();
    }

    void Awake()
    {
        if (!sa) sa = GetComponentInChildren<SkeletonAnimation>();
        if (!sa)
            Debug.LogError("[SpineArmsTool] SkeletonAnimation not assigned/found!", this);
    }

    void LateUpdate()
    {
        if (!removeArms || sa == null || sa.Skeleton == null) return;

        foreach (var slotName in armSlots)
            NullAttachment(slotName);
    }

    [ContextMenu("Log Arm Slots (runtime)")]
    void LogArmSlots()
    {
        if (sa == null || sa.Skeleton == null)
        {
            Debug.LogWarning("[SpineArmsTool] Skeleton not ready. Enter Play mode or assign SkeletonAnimation.", this);
            return;
        }

        var sk = sa.Skeleton;
        Debug.Log("=== Spine Arm Slots (runtime) ===", this);

        foreach (var slotName in armSlots)
        {
            var slot = sk.FindSlot(slotName);
            if (slot == null)
            {
                Debug.Log($"Slot '{slotName}': NOT FOUND");
                continue;
            }

            var attName = slot.Attachment != null ? slot.Attachment.Name : "NULL";
            Debug.Log($"Slot '{slotName}' -> Attachment: {attName}, A={slot.A}");
        }

        // На всякий случай — покажем ещё body/top, вдруг руки “вшиты” туда
        LogOne(sk, "body");
        LogOne(sk, "Top");
        LogOne(sk, "top");
    }

    void LogOne(Skeleton sk, string slotName)
    {
        var slot = sk.FindSlot(slotName);
        if (slot == null) return;
        var attName = slot.Attachment != null ? slot.Attachment.Name : "NULL";
        Debug.Log($"Slot '{slotName}' -> Attachment: {attName}, A={slot.A}");
    }

    void NullAttachment(string slotName)
    {
        var slot = sa.Skeleton.FindSlot(slotName);
        if (slot != null) slot.Attachment = null;
    }
}