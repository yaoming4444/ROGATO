/*using System;*/
using System.Collections.Generic;
/*using System.Diagnostics;*/
using System.Linq;
using UnityEngine;

namespace GameCore.Stats
{
    /// <summary>
    /// Сервис ролла карточек статов.
    /// Делает строго ОДИН ролл:
    /// 1. выбирает доступные карточки
    /// 2. исключает карточки, которые уже в капе
    /// 3. при необходимости убирает дубли
    /// 4. роллит редкость
    /// 5. роллит конкретную карточку внутри этой редкости
    /// </summary>
    public class StatCardRollService : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private StatRollConfig rollConfig;

        [Header("Rules")]
        [SerializeField] private bool allowDuplicatesByExactCard = false;
        [SerializeField] private bool blockSameStatTypeIfAlreadyOwned = false;

        /// <summary>
        /// Роллит одну карточку.
        /// ownedCards = уже имеющиеся у игрока карточки.
        /// Возвращает StatCardDefinition, из которого потом можно создать runtime-карту
        /// или использовать как апгрейд существующей.
        /// </summary>
        public StatCardDefinition RollOneCard(IReadOnlyList<RolledStatCard> ownedCards)
        {
            if (rollConfig == null)
            {
                Debug.LogWarning("[StatCardRollService] Roll config is missing.");
                return null;
            }

            var availableCards = GetAvailableCards(ownedCards);

            if (availableCards.Count == 0)
            {
                Debug.LogWarning("[StatCardRollService] No available cards to roll.");
                return null;
            }

            var rolledRarity = RollRarity(availableCards);

            if (rolledRarity == null)
            {
                Debug.LogWarning("[StatCardRollService] Failed to roll rarity.");
                return null;
            }

            var rarityCards = availableCards
                .Where(x => x.Rarity == rolledRarity.Value)
                .ToList();

            if (rarityCards.Count == 0)
            {
                Debug.LogWarning("[StatCardRollService] No cards found for rolled rarity.");
                return null;
            }

            var rolledCard = RollCardWithinRarity(rarityCards);

            if (rolledCard == null)
            {
                Debug.LogWarning("[StatCardRollService] Failed to roll card inside rarity.");
                return null;
            }

            return rolledCard;
        }

        /// <summary>
        /// Возвращает список доступных карточек после фильтрации.
        /// </summary>
        private List<StatCardDefinition> GetAvailableCards(IReadOnlyList<RolledStatCard> ownedCards)
        {
            var result = new List<StatCardDefinition>();

            foreach (var definition in rollConfig.Cards)
            {
                if (definition == null)
                    continue;

                if (IsBlockedByDuplicateRules(definition, ownedCards))
                    continue;

                if (IsCardMaxed(definition, ownedCards))
                    continue;

                result.Add(definition);
            }

            return result;
        }

        /// <summary>
        /// Проверка на дубли.
        /// </summary>
        private bool IsBlockedByDuplicateRules(StatCardDefinition definition, IReadOnlyList<RolledStatCard> ownedCards)
        {
            if (ownedCards == null || ownedCards.Count == 0)
                return false;

            // Блок полного дубля той же самой карточки
            if (!allowDuplicatesByExactCard)
            {
                bool sameExactCardExists = ownedCards.Any(card =>
                    card != null &&
                    card.StatType == definition.StatType &&
                    card.Rarity == definition.Rarity);

                if (sameExactCardExists)
                    return true;
            }

            // Блок по самому типу стата, даже если rarity другая
            if (blockSameStatTypeIfAlreadyOwned)
            {
                bool sameStatTypeExists = ownedCards.Any(card =>
                    card != null &&
                    card.StatType == definition.StatType);

                if (sameStatTypeExists)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Проверка: достиг ли игрок уже капа по этой карточке.
        /// Смотрим по совпадению statType + rarity.
        /// </summary>
        private bool IsCardMaxed(StatCardDefinition definition, IReadOnlyList<RolledStatCard> ownedCards)
        {
            if (ownedCards == null || ownedCards.Count == 0)
                return false;

            var existing = ownedCards.FirstOrDefault(card =>
                card != null &&
                card.StatType == definition.StatType &&
                card.Rarity == definition.Rarity);

            if (existing == null)
                return false;

            return existing.IsMaxed;
        }

        /// <summary>
        /// Ролл редкости с учетом:
        /// 1. глобальных шансов из конфига
        /// 2. только тех редкостей, по которым реально есть доступные карточки
        /// </summary>
        private StatRarity? RollRarity(List<StatCardDefinition> availableCards)
        {
            var rarityPool = new List<RarityRollEntry>();

            foreach (var rarityEntry in rollConfig.RarityChances)
            {
                if (rarityEntry == null)
                    continue;

                if (rarityEntry.ChanceWeight <= 0f)
                    continue;

                bool hasCardsInThatRarity = availableCards.Any(card => card.Rarity == rarityEntry.Rarity);
                if (!hasCardsInThatRarity)
                    continue;

                rarityPool.Add(new RarityRollEntry(rarityEntry.Rarity, rarityEntry.ChanceWeight));
            }

            if (rarityPool.Count == 0)
                return null;

            float totalWeight = rarityPool.Sum(x => x.Weight);
            if (totalWeight <= 0f)
                return null;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in rarityPool)
            {
                cumulative += entry.Weight;
                if (roll <= cumulative)
                    return entry.Rarity;
            }

            return rarityPool[rarityPool.Count - 1].Rarity;
        }

        /// <summary>
        /// Ролл конкретной карточки внутри уже выбранной редкости.
        /// Тут используется Weight самой карточки.
        /// </summary>
        private StatCardDefinition RollCardWithinRarity(List<StatCardDefinition> cards)
        {
            if (cards == null || cards.Count == 0)
                return null;

            float totalWeight = 0f;

            foreach (var card in cards)
            {
                totalWeight += Mathf.Max(0f, card.Weight);
            }

            // Если веса у всех 0, просто берем случайную
            if (totalWeight <= 0f)
            {
                int randomIndex = Random.Range(0, cards.Count);
                return cards[randomIndex];
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var card in cards)
            {
                cumulative += Mathf.Max(0f, card.Weight);

                if (roll <= cumulative)
                    return card;
            }

            return cards[cards.Count - 1];
        }

        private class RarityRollEntry
        {
            public StatRarity Rarity { get; }
            public float Weight { get; }

            public RarityRollEntry(StatRarity rarity, float weight)
            {
                Rarity = rarity;
                Weight = weight;
            }
        }
    }
}