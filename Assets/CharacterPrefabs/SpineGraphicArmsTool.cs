using UnityEngine;
using Spine;
using Spine.Unity;

public class SpineGraphicArmsTool : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private SkeletonGraphic skeletonGraphic;

    [Header("Runtime settings")]
    [SerializeField] private bool removeArms = true;

    [SerializeField]
    private string[] armSlots = new[]
    {
        "arm_l", "arm_r",
        "top_arm_l", "top_arm_r",
        "gloves_l", "gloves_r"
    };

    void Reset()
    {
        if (!skeletonGraphic) skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();
        if (!skeletonGraphic) skeletonGraphic = GetComponent<SkeletonGraphic>();
    }

    void Awake()
    {
        if (!skeletonGraphic) skeletonGraphic = GetComponentInChildren<SkeletonGraphic>();

        if (!skeletonGraphic)
        {
            Debug.LogError("[SpineGraphicArmsTool] SkeletonGraphic not assigned/found!", this);
            return;
        }

        // На всякий случай убеждаемся, что skeleton создан
        if (skeletonGraphic.Skeleton == null)
            skeletonGraphic.Initialize(true);
    }

    void OnEnable()
    {
        ApplyHideArms();
    }

    void LateUpdate()
    {
        if (!removeArms) return;
        ApplyHideArms();
    }

    [ContextMenu("Apply Hide Arms")]
    public void ApplyHideArms()
    {
        if (!removeArms || skeletonGraphic == null)
            return;

        if (skeletonGraphic.Skeleton == null)
            skeletonGraphic.Initialize(true);

        var skeleton = skeletonGraphic.Skeleton;
        if (skeleton == null)
            return;

        foreach (var slotName in armSlots)
            NullAttachment(skeleton, slotName);

        // Обновляем UI-рендер
        skeletonGraphic.SetVerticesDirty();
        skeletonGraphic.SetMaterialDirty();
    }

    [ContextMenu("Log Arm Slots (runtime)")]
    public void LogArmSlots()
    {
        if (skeletonGraphic == null)
        {
            Debug.LogWarning("[SpineGraphicArmsTool] SkeletonGraphic is missing.", this);
            return;
        }

        if (skeletonGraphic.Skeleton == null)
            skeletonGraphic.Initialize(true);

        var sk = skeletonGraphic.Skeleton;
        if (sk == null)
        {
            Debug.LogWarning("[SpineGraphicArmsTool] Skeleton not ready.", this);
            return;
        }

        Debug.Log("=== SpineGraphic Arm Slots (runtime) ===", this);

        foreach (var slotName in armSlots)
        {
            var slot = sk.FindSlot(slotName);
            if (slot == null)
            {
                Debug.Log($"Slot '{slotName}': NOT FOUND", this);
                continue;
            }

            var attName = slot.Attachment != null ? slot.Attachment.Name : "NULL";
            Debug.Log($"Slot '{slotName}' -> Attachment: {attName}, A={slot.A}", this);
        }

        LogOne(sk, "body");
        LogOne(sk, "Top");
        LogOne(sk, "top");
    }

    private void LogOne(Skeleton sk, string slotName)
    {
        var slot = sk.FindSlot(slotName);
        if (slot == null) return;

        var attName = slot.Attachment != null ? slot.Attachment.Name : "NULL";
        Debug.Log($"Slot '{slotName}' -> Attachment: {attName}, A={slot.A}", this);
    }

    private void NullAttachment(Skeleton skeleton, string slotName)
    {
        var slot = skeleton.FindSlot(slotName);
        if (slot != null)
            slot.Attachment = null;
    }
}