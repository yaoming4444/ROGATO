using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCore.Items
{
    [CreateAssetMenu(menuName = "Game/Items/ItemDef", fileName = "ItemDef_")]
    public class ItemDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Equipment")]
        [SerializeField] private EquipSlot slot;
        [SerializeField] private Rarity rarity;

        // NOTE:
        // Это поле теперь НЕ является "источником истины".
        // Оно существует только как PREVIEW в инспекторе (пересчитывается в OnValidate).
        [Header("Power (computed preview)")]
        [Min(0)]
        [SerializeField] private int power = 0;

        // В текущей системе реальный level предмета хранится в PlayerState (EquippedLevels / pendingChestItemLevel),
        // а НЕ в ScriptableObject. Тут только preview для инспектора.
        [Header("Item Level (legacy / preview only)")]
        [Min(1)]
        [FormerlySerializedAs("level")]
        [SerializeField] private int previewLevel = 1;

        [Header("Base Stats (level 1)")]
        [SerializeField] private ItemStats stats;

        [Header("Extra Stats (optional)")]
        [SerializeField] private ExtraStat[] extraStats;

        [Header("Economy")]
        [Min(0)]
        [SerializeField] private int sellGems = 1;

        [Header("UI")]
        [SerializeField] private Sprite icon;

        // NOTE: цвет редкости (иконка редкости в префабе может быть одна, цветом меняем)
        // alpha = 0 => можно скрывать
        [SerializeField] private Color rarityColor = new Color(1f, 1f, 1f, 0f);

        [SerializeField] private GameObject rarityVFX;

        // legacy field (не используется)
        [FormerlySerializedAs("iconRarity")]
        [SerializeField, HideInInspector] private Sprite iconRarity_Legacy;

        // ---------------------------
        // Level scaling
        // ---------------------------

        [Header("Level Scaling (core)")]
        [Tooltip("Сколько % к базовым статам добавляем за каждый уровень предмета выше 1.\nПример 0.05 = +5% за уровень.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float statsScalePerLevel = 0.05f;

        [Tooltip("Доп. множитель для Power по уровню (опционально). Если хочешь, чтобы Power = только от статов+рарности, поставь 0.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float powerScalePerLevel = 0.0f;

        // ---------------------------
        // Power formula (Variant A)
        // ---------------------------

        [Header("Power Scaling")]
        [Range(1f, 20f)]
        [SerializeField] private float powerScale = 5f;

        [Header("Power Formula (Variant A)")]
        [Tooltip("Вес атаки в Power формуле.")]
        [Range(0f, 5f)]
        [SerializeField] private float atkPowerWeight = 1.0f;

        [Tooltip("Вес HP в Power формуле (обычно меньше атаки).")]
        [Range(0f, 5f)]
        [SerializeField] private float hpPowerWeight = 0.35f;

        [Tooltip("Какая редкость считается базовой (множитель = 1.0). Рекомендация: E.")]
        [SerializeField] private Rarity rarityBaseline = Rarity.E;

        [Tooltip("Шаг множителя за 1 ступень редкости относительно baseline. Пример 0.02 = +2% за ступень.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float rarityStep = 0.02f;

        [Tooltip("Минимальный множитель редкости (чтобы common не уходил в ноль).")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float rarityMinMultiplier = 0.85f;

        [Tooltip("Максимальный множитель редкости (чтобы топ редкость не ломала баланс).")]
        [Range(1.0f, 3.0f)]
        [SerializeField] private float rarityMaxMultiplier = 1.35f;

        // ---------------------------
        // API
        // ---------------------------

        public string Id => id;
        public string DisplayName => displayName;
        public EquipSlot Slot => slot;
        public Rarity Rarity => rarity;

        /// <summary>
        /// Preview power (inspector). В геймплее используй GetPower(itemLevel).
        /// </summary>
        public int Power => power;

        /// <summary>
        /// legacy/preview only
        /// </summary>
        public int Level => previewLevel;

        /// <summary>
        /// Base stats (level 1)
        /// </summary>
        public ItemStats Stats => stats;

        public int SellGems => sellGems;
        public Sprite Icon => icon;

        public Color RarityColor => rarityColor;
        public GameObject RarityVFX => rarityVFX;

        public ExtraStat[] ExtraStats => extraStats;

        /// <summary>
        /// Возвращает статы с учётом itemLevel (+ опционально percent ExtraStats).
        /// </summary>
        public ItemStats GetStats(int itemLevel)
        {
            int lvl = Mathf.Max(1, itemLevel);

            int atk = ScaleInt(stats.Atk, lvl, statsScalePerLevel);
            int hp = ScaleInt(stats.Hp, lvl, statsScalePerLevel);
            int def = ScaleInt(stats.Def, lvl, statsScalePerLevel);

            ApplyPercentExtras(ref atk, ref hp, ref def);

            return new ItemStats
            {
                Atk = atk,
                Hp = hp,
                Def = def
            };
        }

        /// <summary>
        /// Вариант A: Power считается из статов + редкости (+ опционально powerScalePerLevel).
        /// Def в Power НЕ используется (как ты попросил).
        /// </summary>
        public int GetPower(int itemLevel)
        {
            int lvl = Mathf.Max(1, itemLevel);

            ItemStats s = GetStats(lvl);

            float statScore = (s.Atk * atkPowerWeight) + (s.Hp * hpPowerWeight);

            float rarityMult = GetRarityMultiplier(rarity);

            // optional extra level multiplier just for power (set 0 to disable)
            float lvlMult = (powerScalePerLevel <= 0f) ? 1f : (1f + (lvl - 1) * powerScalePerLevel);

            float p = statScore * rarityMult * lvlMult * powerScale;

            int result = Mathf.RoundToInt(p);
            if (result < 0) result = 0;
            return result;
        }

        /// <summary>
        /// Множитель редкости относительно baseline:
        /// mult = 1 + (rarityIndex - baselineIndex) * step
        /// </summary>
        private float GetRarityMultiplier(Rarity r)
        {
            int delta = (int)r - (int)rarityBaseline;
            float mult = 1f + delta * rarityStep;
            return Mathf.Clamp(mult, rarityMinMultiplier, rarityMaxMultiplier);
        }

        private void ApplyPercentExtras(ref int atk, ref int hp, ref int def)
        {
            if (extraStats == null || extraStats.Length == 0) return;

            float atkMul = 1f;
            float hpMul = 1f;
            float defMul = 1f;

            for (int i = 0; i < extraStats.Length; i++)
            {
                var ex = extraStats[i];
                float p = ex.Value / 100f; // Value хранится как проценты (1.5 = 1.5%)

                switch (ex.Type)
                {
                    case ExtraStatType.AtkPercent: atkMul += p; break;
                    case ExtraStatType.HpPercent: hpMul += p; break;
                    case ExtraStatType.DefPercent: defMul += p; break;
                }
            }

            atk = Mathf.RoundToInt(atk * atkMul);
            hp = Mathf.RoundToInt(hp * hpMul);
            def = Mathf.RoundToInt(def * defMul);
        }

        /// <summary>
        /// baseValue * (1 + (level - 1) * perLevel)
        /// </summary>
        public static int ScaleInt(int baseValue, int itemLevel, float perLevel)
        {
            if (itemLevel <= 1 || perLevel <= 0f) return baseValue;
            float mult = 1f + (itemLevel - 1) * perLevel;
            return Mathf.RoundToInt(baseValue * mult);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = name.Trim();

            if (sellGems < 0) sellGems = 0;
            if (previewLevel < 1) previewLevel = 1;

            if (statsScalePerLevel < 0f) statsScalePerLevel = 0f;
            if (powerScalePerLevel < 0f) powerScalePerLevel = 0f;

            if (atkPowerWeight < 0f) atkPowerWeight = 0f;
            if (hpPowerWeight < 0f) hpPowerWeight = 0f;

            if (rarityStep < 0f) rarityStep = 0f;
            if (rarityMinMultiplier < 0.1f) rarityMinMultiplier = 0.1f;
            if (rarityMaxMultiplier < rarityMinMultiplier) rarityMaxMultiplier = rarityMinMultiplier;

            // preview power in inspector
            power = GetPower(previewLevel);
            if (power < 0) power = 0;
        }
#endif
    }
}
