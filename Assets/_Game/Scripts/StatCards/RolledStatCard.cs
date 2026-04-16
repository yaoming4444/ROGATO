using System;
using UnityEngine;

namespace GameCore.Stats
{
    /// <summary>
    /// Тип стата, который дает карточка.
    /// Потом по этому enum мы будем понимать,
    /// что именно усиливать у игрока.
    /// </summary>
    public enum StatType
    {
        None = 0,

        Attack = 1,       // Урон / атака
        Defense = 2,      // Защита
        Health = 3,       // Хп

        CritChance = 4,   // Шанс крита
        CritDamage = 5,   // Сила крита

        MoveSpeed = 6,    // Скорость передвижения
        AttackSpeed = 7,  // Скорость атаки
        Range = 8,        // Радиус / дальность
        PickupRange = 9,  // Радиус подбора
        Luck = 10         // Удача / шанс на полезные вещи
    }

    /// <summary>
    /// Редкость карточки.
    /// Нужна для:
    /// 1. UI (цвет, рамка, подпись)
    /// 2. логики ролла
    /// 3. разных капов / силы бонуса
    /// </summary>
    public enum StatRarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3
    }

    /// <summary>
    /// Runtime-модель карточки стата.
    /// Это НЕ ScriptableObject.
    /// Это именно объект, который хранит
    /// текущее состояние конкретной карточки:
    /// какой стат, какая редкость, какой уровень,
    /// текущее значение, шаг улучшения и максимальный кап.
    /// </summary>
    [Serializable]
    public class RolledStatCard
    {
        [Header("Identity")]
        [SerializeField] private StatType statType = StatType.None;
        [SerializeField] private StatRarity rarity = StatRarity.Common;

        [Header("Progress")]
        [SerializeField] private int level = 1;
        [SerializeField] private float currentValue = 0f;
        [SerializeField] private float upgradeStep = 1f;
        [SerializeField] private float maxCap = 10f;

        /// <summary>
        /// Какой стат усиливает карточка.
        /// </summary>
        public StatType StatType => statType;

        /// <summary>
        /// Редкость карточки.
        /// </summary>
        public StatRarity Rarity => rarity;

        /// <summary>
        /// Текущий уровень карточки.
        /// Обычно первая взятая карточка = Level 1.
        /// </summary>
        public int Level => level;

        /// <summary>
        /// Текущее значение бонуса.
        /// Например: Attack +5.
        /// Тогда CurrentValue = 5.
        /// </summary>
        public float CurrentValue => currentValue;

        /// <summary>
        /// Сколько добавляется за один апгрейд.
        /// Например:
        /// было +5, шаг = 2
        /// после апгрейда станет +7
        /// </summary>
        public float UpgradeStep => upgradeStep;

        /// <summary>
        /// Максимальное значение, выше которого карточка расти не может.
        /// Например:
        /// maxCap = 15
        /// значит выше +15 уже нельзя.
        /// </summary>
        public float MaxCap => maxCap;

        /// <summary>
        /// Проверка: карточка уже в максимуме или нет.
        /// Это пригодится:
        /// - для UI (показывать MAX)
        /// - для ролла (не предлагать карточку, если уже в капе)
        /// - для логики апгрейда
        /// </summary>
        public bool IsMaxed => currentValue >= maxCap;

        /// <summary>
        /// Конструктор для создания новой карточки.
        /// Пример:
        /// new RolledStatCard(StatType.Attack, StatRarity.Rare, 5f, 2f, 15f)
        /// </summary>
        public RolledStatCard(
            StatType statType,
            StatRarity rarity,
            float startValue,
            float upgradeStep,
            float maxCap)
        {
            this.statType = statType;
            this.rarity = rarity;
            this.level = 1;
            this.currentValue = Mathf.Max(0f, startValue);
            this.upgradeStep = Mathf.Max(0f, upgradeStep);
            this.maxCap = Mathf.Max(0f, maxCap);

            ClampToCap();
        }

        /// <summary>
        /// Улучшить карточку на 1 уровень.
        /// Если карточка уже в капе - ничего не делает.
        /// </summary>
        public void Upgrade()
        {
            if (IsMaxed)
                return;

            currentValue += upgradeStep;
            level++;

            ClampToCap();
        }

        /// <summary>
        /// Принудительно задать уровень и значение.
        /// Нужен для загрузки сейва, синка или дебага.
        /// </summary>
        public void SetLevelAndValue(int newLevel, float newValue)
        {
            level = Mathf.Max(1, newLevel);
            currentValue = Mathf.Max(0f, newValue);

            ClampToCap();
        }

        /// <summary>
        /// Посмотреть, каким будет значение после следующего апгрейда,
        /// но не применять его реально.
        /// Нужно для UI.
        /// Например:
        /// сейчас +5
        /// шаг +2
        /// покажет preview +7
        /// </summary>
        public float GetNextValuePreview()
        {
            if (IsMaxed)
                return currentValue;

            return Mathf.Min(currentValue + upgradeStep, maxCap);
        }

        /// <summary>
        /// Гарантируем, что текущее значение не выйдет за пределы капа.
        /// </summary>
        private void ClampToCap()
        {
            currentValue = Mathf.Clamp(currentValue, 0f, maxCap);
        }
    }
}