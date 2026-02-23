using OctoberStudio.Bossfight;
using OctoberStudio.Extensions;
using OctoberStudio.Pool;
using OctoberStudio.Timeline.Bossfight;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace OctoberStudio
{
    public class StageFieldManager : MonoBehaviour
    {
        private static StageFieldManager instance;

        [SerializeField] BossfightDatabase bossfightDatabase;

        public StageType StageType { get; private set; }
        public GameObject BackgroundPrefab { get; private set; }

        public BossFenceBehavior Fence { get; private set; }

        private IFieldBehavior field;
        private Dictionary<BossType, BossFenceBehavior> fences;

        private void Awake()
        {
            instance = this;
        }

        public void Init(StageData stageData, PlayableDirector director)
        {
            StageType = stageData.StageType;

            switch (stageData.StageType)
            {
                case StageType.Endless: field = new EndlessFieldBehavior(); break;
                case StageType.VerticalEndless: field = new VerticalFieldBehavior(); break;
                case StageType.HorizontalEndless: field = new HorizontalFieldBehavior(); break;
                case StageType.Rect: field = new RectFieldBehavior(); break;
            }

            field.Init(stageData.StageFieldData, stageData.SpawnProp);

            fences = new Dictionary<BossType, BossFenceBehavior>();

            var bossAssets = director.GetAssets<BossTrack, Boss>();

            for (int i = 0; i < bossAssets.Count; i++)
            {
                var bossAsset = bossAssets[i];
                var bossData = bossfightDatabase.GetBossfight(bossAsset.BossType);

                if (!fences.ContainsKey(bossData.BossType))
                {
                    var fence = Instantiate(bossData.FencePrefab).GetComponent<BossFenceBehavior>();
                    fence.gameObject.SetActive(false);
                    fence.Init();

                    fences.Add(bossData.BossType, fence);
                }
            }
        }

        public Vector2 SpawnFence(BossType bossType, Vector2 offset)
        {
            Fence = fences[bossType];

            var center = field.GetBossSpawnPosition(Fence, offset);

            Fence.SpawnFence(center);

            return center;
        }

        public void RemoveFence()
        {
            Fence.RemoveFence();
            Fence = null;
        }

        public void RemovePropFromFence()
        {
            field.RemovePropFromBossFence(Fence);
        }

        private void Update()
        {
            field.Update();
        }

        public bool ValidatePosition(Vector2 position, Vector2 offset, bool withFence = true)
        {
            var isFenceValid = true;
            if (Fence != null && withFence)
            {
                isFenceValid = Fence.ValidatePosition(position, offset);
            }

            return instance.field.ValidatePosition(position) && isFenceValid;
        }

        /// <summary>
        /// Проверяет, помещается ли точка внутри поля с запасом (padding) от краёв.
        /// padding = условный "радиус" объекта.
        /// </summary>
        public bool ValidatePositionWithPadding(Vector2 position, float padding, bool withFence = true)
        {
            if (padding < 0f) padding = 0f;

            // Проверка поля (4 стороны вокруг центра)
            if (!instance.field.ValidatePosition(position))
                return false;

            if (padding > 0f)
            {
                if (!instance.field.ValidatePosition(position + Vector2.right * padding)) return false;
                if (!instance.field.ValidatePosition(position + Vector2.left * padding)) return false;
                if (!instance.field.ValidatePosition(position + Vector2.up * padding)) return false;
                if (!instance.field.ValidatePosition(position + Vector2.down * padding)) return false;
            }

            // Проверка фэнса (если есть)
            if (Fence != null && withFence)
            {
                // Для фэнса используем offset как "габарит" объекта
                var fenceOffset = new Vector2(padding, padding);
                if (!Fence.ValidatePosition(position, fenceOffset))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Зажимает позицию внутрь поля с учётом padding от краёв.
        /// Не учитывает фэнс (его можно отдельно валидировать).
        /// </summary>
        public Vector2 ClampPositionInsideField(Vector2 position, float padding = 0f)
        {
            if (padding < 0f) padding = 0f;

            Vector2 clamped = position;
            float distance;

            if (field.IsPointOutsideRight(clamped + Vector2.right * padding, out distance))
                clamped.x -= distance;

            if (field.IsPointOutsideLeft(clamped + Vector2.left * padding, out distance))
                clamped.x += distance;

            if (field.IsPointOutsideTop(clamped + Vector2.up * padding, out distance))
                clamped.y -= distance;

            if (field.IsPointOutsideBottom(clamped + Vector2.down * padding, out distance))
                clamped.y += distance;

            return clamped;
        }

        public virtual Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset, bool withFence)
        {
            if (Fence != null && withFence)
            {
                return Fence.GetIntersectionPoint(start, end, offset);
            }

            return instance.field.GetIntersectionPoint(start, end, offset);
        }

        public Vector2 GetRandomPositionOnBorder()
        {
            return instance.field.GetRandomPositionOnBorder();
        }

        /// <summary>
        /// Возвращает точку у границы поля, но с уводом внутрь на inset.
        /// Для Rect-поля использует нативную реализацию RectFieldBehavior.
        /// Для остальных — fallback через старую логику + clamp.
        /// </summary>
        public Vector2 GetRandomPositionOnBorder(float inset)
        {
            if (inset <= 0f)
                return instance.field.GetRandomPositionOnBorder();

            // Лучший путь: прямой вызов у прямоугольного поля
            if (field is RectFieldBehavior rectField)
            {
                return rectField.GetRandomPositionOnBorder(inset);
            }

            // Fallback для других типов полей
            var point = instance.field.GetRandomPositionOnBorder();

            const float probe = 0.001f;
            float _;

            if (field.IsPointOutsideRight(point + Vector2.right * probe, out _))
            {
                point.x -= inset;
            }
            else if (field.IsPointOutsideLeft(point + Vector2.left * probe, out _))
            {
                point.x += inset;
            }
            else if (field.IsPointOutsideTop(point + Vector2.up * probe, out _))
            {
                point.y -= inset;
            }
            else if (field.IsPointOutsideBottom(point + Vector2.down * probe, out _))
            {
                point.y += inset;
            }

            return ClampPositionInsideField(point, inset);
        }

        public bool IsPointOutsideFieldRight(Vector2 point, out float distance)
        {
            return field.IsPointOutsideRight(point, out distance);
        }

        public bool IsPointOutsideFieldLeft(Vector2 point, out float distance)
        {
            return field.IsPointOutsideLeft(point, out distance);
        }

        public bool IsPointOutsideFieldTop(Vector2 point, out float distance)
        {
            return field.IsPointOutsideTop(point, out distance);
        }

        public bool IsPointOutsideFieldBottom(Vector2 point, out float distance)
        {
            return field.IsPointOutsideBottom(point, out distance);
        }
    }
}