using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace GameCore.Visual
{
    public class SpineVisualEquipmentApplier : MonoBehaviour
    {
        [Header("Target (set one of them)")]
        [SerializeField] private SkeletonAnimation skeletonAnimation; // prefab in world
        [SerializeField] private SkeletonGraphic skeletonGraphic;     // UI

        [Header("Base skin")]
        [SerializeField] private string fallbackBaseSkin = "default";
        [SerializeField] private bool usePlayerSelectedSkinAsBase = true;

        private void Reset()
        {
            // авто-подхват на объекте
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            skeletonGraphic = GetComponent<SkeletonGraphic>();
        }

        private void OnEnable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            ApplyNow();
        }

        private void OnDisable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameCore.PlayerState s) => ApplyNow();

        public void ApplyNow()
        {
            var gi = GameCore.GameInstance.I;
            var state = gi?.State;
            if (state == null) return;

            // 1) берём Skeleton и SkeletonData
            var skeleton = GetSkeleton();
            if (skeleton == null) return;

            // 2) выбираем base skin
            string baseSkin = fallbackBaseSkin;
            
            // ВАЖНО: берём текущий скин, который уже стоит на SkeletonGraphic/SkeletonAnimation
            if (skeleton.Skin != null && !string.IsNullOrWhiteSpace(skeleton.Skin.Name))
                    baseSkin = skeleton.Skin.Name;
            


            var combined = new Skin("combined-runtime");

            // base
            Debug.Log($"[SpineEquip] baseSkin = {baseSkin}, currentSkin = {skeleton.Skin?.Name}, selected = {state.SelectedSkinId}");
            TryAddSkinByName(combined, skeleton, baseSkin);

            // parts (у тебя это уже есть в коде — оставь как было)
            // пример из твоего скрина:
            // foreach (VisualSlot slot in Enum.GetValues(typeof(VisualSlot))) {
            //     var skinName = state.GetVisualSkin(slot);
            //     if (!string.IsNullOrWhiteSpace(skinName))
            //         TryAddSkinByName(combined, skeleton, skinName);
            // }

            // !!! ВАЖНО: вставь сюда твой блок parts как он был
            // (я не трогаю логику получения skinName из state)

            // 3) применяем
            skeleton.SetSkin(combined);
            skeleton.SetSlotsToSetupPose();

            // если есть AnimationState — применяем позу текущей анимации
            var animState = GetAnimationState();
            animState?.Apply(skeleton);

            // ? ВОТ ТУТ ТВОЙ ФИКС: нужен аргумент
            skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

            // 4) принудительно обновляем меш (особенно важно для UI)
            if (skeletonGraphic != null)
            {
                skeletonGraphic.SetMaterialDirty();
                skeletonGraphic.SetVerticesDirty();
            }
        }

        private Skeleton GetSkeleton()
        {
            if (skeletonGraphic != null)
                return skeletonGraphic.Skeleton;

            if (skeletonAnimation != null)
                return skeletonAnimation.Skeleton;

            // авто-поиск если не назначил
            skeletonGraphic = GetComponent<SkeletonGraphic>();
            if (skeletonGraphic != null) return skeletonGraphic.Skeleton;

            skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation != null) return skeletonAnimation.Skeleton;

            return null;
        }

        private Spine.AnimationState GetAnimationState()
        {
            if (skeletonGraphic != null)
                return skeletonGraphic.AnimationState;

            if (skeletonAnimation != null)
                return skeletonAnimation.AnimationState;

            return null;
        }

        private static void TryAddSkinByName(Skin combined, Skeleton skeleton, string skinName)
        {
            if (combined == null || skeleton == null || string.IsNullOrWhiteSpace(skinName)) return;

            var skin = skeleton.Data.FindSkin(skinName);
            if (skin == null) return;

            combined.AddSkin(skin);
        }
    }
}


