using System;
using System.Collections.Generic;
using UnityEngine;
using LayerLab.ArtMaker; // for PartsType

/// <summary>
/// Data-only preset for companion appearance.
/// Uses the same PartsType categories as the player PartsManager.
/// </summary>
[CreateAssetMenu(fileName = "CompanionAppearance", menuName = "Rogato/Companion/Appearance Preset", order = 10)]
public class CompanionAppearance : ScriptableObject
{
    [Header("Parts (same categories as player)")]
    [SerializeField] private List<PartSelection> parts = new();

    [Header("Colors")]
    [SerializeField] private ColorPreset colors = ColorPreset.Default();

    [Header("Hide base body slots")]
    [SerializeField] private HideSlotsPreset hideSlots = HideSlotsPreset.OnlyArms();

    public IReadOnlyList<PartSelection> Parts => parts;
    public ColorPreset Colors => colors;
    public HideSlotsPreset HideSlots => hideSlots;

    [Serializable]
    public struct PartSelection
    {
        [Tooltip("PartsType category (same as your player binder).")]
        public PartsType Category;

        [Tooltip("Exact skin element name OR prefix (if your applier allows prefix match). Empty = unequip.")]
        public string SkinKey;
    }

    [Serializable]
    public struct ColorPreset
    {
        [Header("Body / Face")]
        public bool UseSkinColor;
        public Color32 Skin;

        [Tooltip("Optional. Only used if your PartsManager supports eye color.")]
        public bool UseEyeColor;
        public Color32 Eyes;

        [Header("Hair / Brows / Beard")]
        public bool UseHairColor;
        public Color32 Hair;

        public bool UseBrowColor;
        public Color32 Brows;

        public bool UseBeardColor;
        public Color32 Beard;

        [Header("Extras (optional for future use)")]
        public bool UseExtra1;
        public Color32 Extra1;

        public bool UseExtra2;
        public Color32 Extra2;

        public static ColorPreset Default()
        {
            return new ColorPreset
            {
                UseSkinColor = true,
                Skin = new Color32(255, 230, 200, 255),

                UseEyeColor = false,
                Eyes = new Color32(80, 120, 160, 255),

                UseHairColor = false,
                Hair = new Color32(40, 30, 25, 255),

                UseBrowColor = false,
                Brows = new Color32(40, 30, 25, 255),

                UseBeardColor = false,
                Beard = new Color32(40, 30, 25, 255),

                UseExtra1 = false,
                Extra1 = new Color32(255, 255, 255, 255),

                UseExtra2 = false,
                Extra2 = new Color32(255, 255, 255, 255)
            };
        }
    }

    [Serializable]
    public struct HideSlotsPreset
    {
        public bool Enabled;

        [Tooltip("Spine slot names to hide by setting Attachment = null after PartsManager applies skins.")]
        public List<string> SlotsToHide;

        public static HideSlotsPreset OnlyArms()
        {
            return new HideSlotsPreset
            {
                Enabled = true,
                SlotsToHide = new List<string>
                {
                    "arm_l",
                    "arm_r",
                    "top_arm_l",
                    "top_arm_r"
                    // If later needed:
                    // "gloves_l",
                    // "gloves_r"
                }
            };
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // clean up null lists
        if (parts == null) parts = new List<PartSelection>();
        if (hideSlots.SlotsToHide == null) hideSlots.SlotsToHide = new List<string>();

        // trim skin keys
        for (int i = 0; i < parts.Count; i++)
        {
            var p = parts[i];
            p.SkinKey = p.SkinKey?.Trim();
            parts[i] = p;
        }

        for (int i = hideSlots.SlotsToHide.Count - 1; i >= 0; i--)
        {
            var s = hideSlots.SlotsToHide[i]?.Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                hideSlots.SlotsToHide.RemoveAt(i);
                continue;
            }
            hideSlots.SlotsToHide[i] = s;
        }
    }
#endif
}