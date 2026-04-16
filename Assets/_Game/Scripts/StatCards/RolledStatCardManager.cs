using System.Collections.Generic;
/*using System.Diagnostics;*/
using System.Linq;
using UnityEngine;

namespace GameCore.Stats
{
    /// <summary>
    /// Менеджер применения выролленной карточки.
    ///
    /// Логика:
    /// - если такой карточки у игрока еще нет -> создает новую и добавляет
    /// - если такая карточка уже есть -> апгрейдит существующую
    ///
    /// Под "такой же карточкой" сейчас понимается совпадение:
    /// StatType + Rarity
    /// </summary>
    public class RolledStatCardManager : MonoBehaviour
    {
        [Header("Runtime Owned Cards")]
        [SerializeField] private List<RolledStatCard> ownedCards = new List<RolledStatCard>();

        public IReadOnlyList<RolledStatCard> OwnedCards => ownedCards;

        /// <summary>
        /// Применить выролленную карточку.
        ///
        /// Если карточка уже есть -> Upgrade()
        /// Если карточки нет -> создать новую через definition.CreateRuntimeCard()
        ///
        /// Возвращает итоговую карточку, которая была добавлена или улучшена.
        /// </summary>
        public RolledStatCard ApplyRolledCard(StatCardDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("[RolledStatCardManager] ApplyRolledCard failed: definition is null.");
                return null;
            }

            RolledStatCard existingCard = FindOwnedCard(definition);

            if (existingCard != null)
            {
                if (existingCard.IsMaxed)
                {
                    Debug.Log($"[RolledStatCardManager] Card is already maxed: {definition.DisplayName} ({definition.Rarity})");
                    return existingCard;
                }

                existingCard.Upgrade();

                Debug.Log($"[RolledStatCardManager] Upgraded existing card: {definition.DisplayName} | Rarity: {definition.Rarity} | New Value: {existingCard.CurrentValue} | Level: {existingCard.Level}");

                return existingCard;
            }

            RolledStatCard newCard = definition.CreateRuntimeCard();
            ownedCards.Add(newCard);

            Debug.Log($"[RolledStatCardManager] Added new card: {definition.DisplayName} | Rarity: {definition.Rarity} | Value: {newCard.CurrentValue}");

            return newCard;
        }

        /// <summary>
        /// Найти уже имеющуюся карточку игрока,
        /// которая соответствует переданному definition.
        ///
        /// Текущее правило совпадения:
        /// StatType + Rarity
        /// </summary>
        public RolledStatCard FindOwnedCard(StatCardDefinition definition)
        {
            if (definition == null)
                return null;

            return ownedCards.FirstOrDefault(card =>
                card != null &&
                card.StatType == definition.StatType &&
                card.Rarity == definition.Rarity);
        }

        /// <summary>
        /// Проверить, есть ли у игрока такая карточка.
        /// </summary>
        public bool HasCard(StatCardDefinition definition)
        {
            return FindOwnedCard(definition) != null;
        }

        /// <summary>
        /// Полностью очистить все карточки.
        /// Полезно для:
        /// - старта нового run
        /// - ресета прогресса
        /// - тестов
        /// </summary>
        public void ClearAll()
        {
            ownedCards.Clear();
        }

        /// <summary>
        /// Удалить конкретную карточку из коллекции игрока.
        /// </summary>
        public bool RemoveCard(StatCardDefinition definition)
        {
            RolledStatCard existingCard = FindOwnedCard(definition);
            if (existingCard == null)
                return false;

            return ownedCards.Remove(existingCard);
        }

        /// <summary>
        /// Получить суммарный бонус по конкретному типу стата
        /// со всех взятых карточек.
        /// Например, все Attack-карточки суммируются вместе.
        /// </summary>
        public float GetTotalValue(StatType statType)
        {
            float total = 0f;

            foreach (RolledStatCard card in ownedCards)
            {
                if (card == null)
                    continue;

                if (card.StatType != statType)
                    continue;

                total += card.CurrentValue;
            }

            return total;
        }

        /// <summary>
        /// Получить словарь всех итоговых бонусов по статам.
        /// Удобно потом передать в PlayerBehaviour или отдельный stats controller.
        /// </summary>
        public Dictionary<StatType, float> BuildTotals()
        {
            Dictionary<StatType, float> totals = new Dictionary<StatType, float>();

            foreach (RolledStatCard card in ownedCards)
            {
                if (card == null || card.StatType == StatType.None)
                    continue;

                if (!totals.ContainsKey(card.StatType))
                    totals[card.StatType] = 0f;

                totals[card.StatType] += card.CurrentValue;
            }

            return totals;
        }
    }
}