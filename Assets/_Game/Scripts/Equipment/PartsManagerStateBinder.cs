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
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            ApplyFromState();
        }

        private void OnDisable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;
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

            // APPLY ALL (ты хотел именно это)
            ApplyOne(PartsType.Back, st.visual_back);
            ApplyOne(PartsType.Beard, st.visual_beard);
            ApplyOne(PartsType.Boots, st.visual_boots);
            ApplyOne(PartsType.Bottom, st.visual_bottom);
            ApplyOne(PartsType.Brow, st.visual_brow);
            ApplyOne(PartsType.Eyes, st.visual_eyes);
            ApplyOne(PartsType.Gloves, st.visual_gloves);

            ApplyOne(PartsType.Hair_Short, st.visual_hair_short);
            ApplyOne(PartsType.Hair_Hat, st.visual_hair_hat);
            ApplyOne(PartsType.Helmet, st.visual_helmet);

            ApplyOne(PartsType.Mouth, st.visual_mouth);
            ApplyOne(PartsType.Eyewear, st.visual_eyewear);

            ApplyOne(PartsType.Gear_Left, st.visual_gear_left);
            ApplyOne(PartsType.Gear_Right, st.visual_gear_right);

            ApplyOne(PartsType.Top, st.visual_top);
            ApplyOne(PartsType.Skin, st.visual_skin);

            partsManager.ChangeSkinColor(st.GetSkinColor32());

            ForceGraphicRefresh();
        }

        private void ApplyOne(PartsType type, string skinNameOrPrefix)
        {
            var list = partsManager.GetCurrentSkinNames(type);
            if (list == null || list.Count == 0) return;

            // ? ключевое изменение:
            if (string.IsNullOrWhiteSpace(skinNameOrPrefix))
            {
                partsManager.EquipParts(type, -1);   // <- снять / пусто
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





