using System;
using UnityEngine;
using LayerLab.ArtMaker;
using Spine.Unity;

namespace GameCore.Visual
{
    public class PartsManagerStateBinder : MonoBehaviour
    {
        [SerializeField] private PartsManager partsManager;
        [SerializeField] private bool autoInit = true;

        [Header("If state value is a PREFIX, binder picks first matching skin")]
        [SerializeField] private bool allowPrefixMatch = true;

        private bool _inited;

        private void Awake()
        {
            if (!partsManager) partsManager = GetComponentInChildren<PartsManager>(true);
            if (autoInit) EnsureInit();
        }

        private void OnEnable()
        {
            EnsureInit();

            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.StateChanged += OnStateChanged;

            ApplyFromState(); // <-- важно: применить при открытии окна
        }

        private void OnDisable()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameCore.PlayerState s) => ApplyFromState();

        private void EnsureInit()
        {
            if (_inited) return;
            if (!partsManager)
            {
                Debug.LogError("[Binder] PartsManager not set");
                return;
            }

            partsManager.Init();
            _inited = true;
        }

        public void ApplyFromState()
        {
            EnsureInit();

            var st = GameCore.GameInstance.I?.State;
            if (st == null) return;

            st.EnsureValid();

            ApplyOne(PartsType.Back, st.GetVisualForRender(PartsType.Back, st.visual_back));
            ApplyOne(PartsType.Beard, st.GetVisualForRender(PartsType.Beard, st.visual_beard));
            ApplyOne(PartsType.Boots, st.GetVisualForRender(PartsType.Boots, st.visual_boots));
            ApplyOne(PartsType.Bottom, st.GetVisualForRender(PartsType.Bottom, st.visual_bottom));
            ApplyOne(PartsType.Brow, st.GetVisualForRender(PartsType.Brow, st.visual_brow));
            ApplyOne(PartsType.Eyes, st.GetVisualForRender(PartsType.Eyes, st.visual_eyes));
            ApplyOne(PartsType.Gloves, st.GetVisualForRender(PartsType.Gloves, st.visual_gloves));

            ApplyOne(PartsType.Hair_Short, st.GetVisualForRender(PartsType.Hair_Short, st.visual_hair_short));
            ApplyOne(PartsType.Hair_Hat, st.GetVisualForRender(PartsType.Hair_Hat, st.visual_hair_hat));
            ApplyOne(PartsType.Helmet, st.GetVisualForRender(PartsType.Helmet, st.visual_helmet));

            ApplyOne(PartsType.Mouth, st.GetVisualForRender(PartsType.Mouth, st.visual_mouth));
            ApplyOne(PartsType.Eyewear, st.GetVisualForRender(PartsType.Eyewear, st.visual_eyewear));

            ApplyOne(PartsType.Gear_Left, st.GetVisualForRender(PartsType.Gear_Left, st.visual_gear_left));
            ApplyOne(PartsType.Gear_Right, st.GetVisualForRender(PartsType.Gear_Right, st.visual_gear_right));

            ApplyOne(PartsType.Top, st.GetVisualForRender(PartsType.Top, st.visual_top));
            ApplyOne(PartsType.Skin, st.GetVisualForRender(PartsType.Skin, st.visual_skin));

            partsManager.ChangeSkinColor(st.GetSkinColor32());
            partsManager.ChangeHairColor(st.GetHairColor32());
            partsManager.ChangeBeardColor(st.GetBeardColor32());
            partsManager.ChangeBrowColor(st.GetBrowColor32());

            ForceGraphicRefresh();
        }


        private void ApplyOne(PartsType type, string skinNameOrPrefix)
        {
            var list = partsManager.GetCurrentSkinNames(type);
            if (list == null || list.Count == 0) return;

            if (string.IsNullOrWhiteSpace(skinNameOrPrefix))
            {
                partsManager.EquipParts(type, -1);   // снять / пусто
                return;
            }

            int idx = list.IndexOf(skinNameOrPrefix);

            if (idx < 0 && allowPrefixMatch)
            {
                idx = list.FindIndex(x =>
                    x.StartsWith(skinNameOrPrefix, StringComparison.OrdinalIgnoreCase));
            }

            if (idx < 0)
            {
                Debug.LogWarning($"[Binder] Skin '{skinNameOrPrefix}' not found for {type}");
                return;
            }

            partsManager.EquipParts(type, idx);
        }

        private void ForceGraphicRefresh()
        {
            // для UI иногда нужно
            var sg = GetComponentInChildren<SkeletonGraphic>(true);
            if (sg != null)
            {
                sg.SetVerticesDirty();
                sg.SetMaterialDirty();
            }
        }
    }
}






