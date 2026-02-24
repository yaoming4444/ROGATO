using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace OctoberStudio
{
    [CreateAssetMenu(fileName = "Stage Data", menuName = "October/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Display Data")]
        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] string displayName;
        public string DisplayName => displayName;

        [Header("Timeline Data")]
        [SerializeField] TimelineAsset timeline;
        public TimelineAsset Timeline => timeline;

        [Header("Stage Settings")]
        [SerializeField] StageType stageType;
        public StageType StageType => stageType;

        [SerializeField] StageFieldData stageFieldData;
        public StageFieldData StageFieldData => stageFieldData;

        [SerializeField] bool spawnProp;
        public bool SpawnProp => spawnProp;

        [SerializeField] bool removePropFromBossfight;
        public bool RemovePropFromBossfight => removePropFromBossfight;

        [Space]
        [SerializeField] Color spotlightColor;
        public Color SpotlightColor => spotlightColor;

        [SerializeField] Color spotlightShadowColor;
        public Color SpotlightShadowColor => spotlightShadowColor;

        [Space]
        [SerializeField] float enemyDamage;
        public float EnemyDamage => enemyDamage;

        [SerializeField] float enemyHP;
        public float EnemyHP => enemyHP;

        [Space]
        [SerializeField] bool useCustomMusic;
        public bool UseCustomMusic => useCustomMusic;

        [SerializeField] string musicName;
        public string MusicName => musicName;

        // =========================
        // Rewards
        // =========================
        [Header("Rewards")]
        [SerializeField] private List<StageReward> rewards = new List<StageReward>();
        public IReadOnlyList<StageReward> Rewards => rewards;
    }

    [Serializable]
    public class StageReward
    {

        [SerializeField] private StageRewardType rewardType;
        public StageRewardType RewardType => rewardType;

        [SerializeField] private Sprite backgroundIcon;
        public Sprite BackgroundIcon => backgroundIcon;

        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] private string title;
        public string Title => title;

        [SerializeField] private int amount = 1;
        public int Amount => amount;
    }

    public enum StageRewardType
    {
        Coins,
        Exp
    }

    public enum StageType
    {
        Endless,
        VerticalEndless,
        HorizontalEndless,
        Rect
    }
}