using System;
using UnityEngine;
using LayerLab.ArtMaker;
using Spine.Unity;

namespace GameCore.Visual
{
    /// <summary>
    /// Companion binder that mirrors PartsManagerStateBinder behavior,
    /// but uses CompanionAppearance (ScriptableObject) instead of PlayerState.
    ///
    /// - For each PartSelection: ApplyOne(category, skinKey)
    /// - Empty skinKey => EquipParts(type, -1) (unequip)  [same as player binder]
    /// - Optional prefix match
    /// - Applies colors from CompanionAppearance
    /// - Refreshes SkeletonGraphic (UI) if present
    /// </summary>
    public class CompanionAppearanceBinder : MonoBehaviour
    {
        [SerializeField] private PartsManager partsManager;
        [SerializeField] private CompanionAppearance appearance;
        [SerializeField] private bool autoInit = true;

        [Header("If SkinKey is a PREFIX, binder picks first matching skin")]
        [SerializeField] private bool allowPrefixMatch = true;

        [Header("Apply on enable")]
        [SerializeField] private bool applyOnEnable = true;

        private bool _inited;

        private void Awake()
        {
            if (!partsManager) partsManager = GetComponentInChildren<PartsManager>(true);
            if (autoInit) EnsureInit();
        }

        private void OnEnable()
        {
            if (applyOnEnable)
                ApplyFromAppearance();
        }

        public void SetAppearance(CompanionAppearance newAppearance, bool applyImmediately = true)
        {
            appearance = newAppearance;
            if (applyImmediately)
                ApplyFromAppearance();
        }

        public void ApplyFromAppearance()
        {
            EnsureInit();
            if (appearance == null) return;

            var parts = appearance.Parts;
            if (parts != null)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    var p = parts[i];
                    ApplyOne(p.Category, p.SkinKey);
                }
            }

            ApplyColors(appearance.Colors);

            ForceGraphicRefresh();
        }

        private void EnsureInit()
        {
            if (_inited) return;

            if (!partsManager)
            {
                Debug.LogError("[CompanionBinder] PartsManager not set", this);
                return;
            }

            partsManager.Init();
            _inited = true;
        }

        private void ApplyOne(PartsType type, string skinNameOrPrefix)
        {
            var list = partsManager.GetCurrentSkinNames(type);
            if (list == null || list.Count == 0) return;

            if (string.IsNullOrWhiteSpace(skinNameOrPrefix))
            {
                // EXACTLY like player binder: empty => unequip
                partsManager.EquipParts(type, -1);
                return;
            }

            skinNameOrPrefix = skinNameOrPrefix.Trim();

            int idx = list.IndexOf(skinNameOrPrefix);

            if (idx < 0 && allowPrefixMatch)
            {
                idx = list.FindIndex(x =>
                    x.StartsWith(skinNameOrPrefix, StringComparison.OrdinalIgnoreCase));
            }

            if (idx < 0)
            {
                Debug.LogWarning($"[CompanionBinder] Skin '{skinNameOrPrefix}' not found for {type}", this);
                return;
            }

            partsManager.EquipParts(type, idx);
        }

        private void ApplyColors(CompanionAppearance.ColorPreset c)
        {
            // Same pattern as player binder, but from SO
            if (c.UseSkinColor) partsManager.ChangeSkinColor(c.Skin);
            if (c.UseHairColor) partsManager.ChangeHairColor(c.Hair);
            if (c.UseBeardColor) partsManager.ChangeBeardColor(c.Beard);
            if (c.UseBrowColor) partsManager.ChangeBrowColor(c.Brows);

            // If you later add in PartsManager:
            // if (c.UseEyeColor) partsManager.ChangeEyeColor(c.Eyes);
        }

        private void ForceGraphicRefresh()
        {
            // For UI sometimes needed (same as player binder)
            var sg = GetComponentInChildren<SkeletonGraphic>(true);
            if (sg != null)
            {
                sg.SetVerticesDirty();
                sg.SetMaterialDirty();
            }
        }
    }
}