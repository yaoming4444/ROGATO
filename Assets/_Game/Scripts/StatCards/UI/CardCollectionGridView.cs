using System.Collections.Generic;
using UnityEngine;
using GameCore.Stats;

namespace GameCore.UI
{
    public class CardCollectionGridView : MonoBehaviour
    {
        // For card sorting
        private class SortedCardEntry
        {
            public StatCardDefinition definition;
            public bool isUnlocked;
            public int originalIndex;
        }

        [Header("Refs")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private CardCollectionPopupView collectionPopupView;

        [Header("Data")]
        [SerializeField] private StatRollConfig statRollConfig;

        [Header("Prefabs")]
        [SerializeField] private GameObject closedCardPrefab;
        [SerializeField] private GameObject openedCardPrefab;

        private readonly List<GameObject> spawnedCards = new();
        private readonly HashSet<string> unlockedCardIds = new();
        private readonly Dictionary<string, int> cardLevels = new();
        private readonly Dictionary<string, float> cardValues = new();
        private readonly List<CardCollectionPopupView.CardPopupData> openedCardsOrdered = new();

        public void SetUnlockedIds(IReadOnlyList<string> ids)
        {
            unlockedCardIds.Clear();

            if (ids == null)
                return;

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (!string.IsNullOrWhiteSpace(id))
                    unlockedCardIds.Add(id);
            }
        }

        public void SetCardLevels(Dictionary<string, int> levels)
        {
            cardLevels.Clear();

            if (levels == null)
                return;

            foreach (var pair in levels)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                    cardLevels[pair.Key] = Mathf.Max(1, pair.Value);
            }
        }

        public void SetCardValues(Dictionary<string, float> values)
        {
            cardValues.Clear();

            if (values == null)
                return;

            foreach (var pair in values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                    cardValues[pair.Key] = pair.Value;
            }
        }

        public void Rebuild()
        {
            Clear();

            if (contentRoot == null || statRollConfig == null)
                return;

            openedCardsOrdered.Clear();

            List<SortedCardEntry> sortedEntries = new List<SortedCardEntry>();

            IReadOnlyList<StatCardDefinition> cards = statRollConfig.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                StatCardDefinition definition = cards[i];
                if (definition == null)
                    continue;

                sortedEntries.Add(new SortedCardEntry
                {
                    definition = definition,
                    isUnlocked = unlockedCardIds.Contains(definition.CardId),
                    originalIndex = i
                });
            }

            sortedEntries.Sort((a, b) =>
            {
                // 1. Открытые всегда выше закрытых
                if (a.isUnlocked != b.isUnlocked)
                    return a.isUnlocked ? -1 : 1;

                // 2. Более высокий тир выше
                int rarityCompare = GetRaritySortScore(b.definition.Rarity).CompareTo(GetRaritySortScore(a.definition.Rarity));
                if (rarityCompare != 0)
                    return rarityCompare;

                // 3. Если одинаково — сохраняем порядок базы
                return a.originalIndex.CompareTo(b.originalIndex);
            });

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                StatCardDefinition definition = sortedEntries[i].definition;
                bool isUnlocked = sortedEntries[i].isUnlocked;

                if (isUnlocked)
                    SpawnOpened(definition);
                else
                    SpawnClosed();
            }
        }

        private void SpawnClosed()
        {
            if (closedCardPrefab == null)
                return;

            GameObject go = Instantiate(closedCardPrefab, contentRoot, false);
            spawnedCards.Add(go);
        }

        private void SpawnOpened(StatCardDefinition definition)
        {
            if (openedCardPrefab == null)
                return;

            int level = 1;
            if (cardLevels.TryGetValue(definition.CardId, out int savedLevel))
                level = savedLevel;

            float value = 0f;
            if (cardValues.TryGetValue(definition.CardId, out float savedValue))
                value = savedValue;

            openedCardsOrdered.Add(new CardCollectionPopupView.CardPopupData
            {
                cardId = definition.CardId,
                definition = definition,
                level = level,
                value = value
            });

            GameObject go = Instantiate(openedCardPrefab, contentRoot, false);
            spawnedCards.Add(go);

            OpenedCardView view = go.GetComponent<OpenedCardView>();
            if (view != null)
                view.Setup(definition, level, OnOpenedCardClicked);
        }

        private void OnOpenedCardClicked(string cardId)
        {
            if (collectionPopupView == null || string.IsNullOrWhiteSpace(cardId))
                return;

            int index = -1;

            for (int i = 0; i < openedCardsOrdered.Count; i++)
            {
                if (openedCardsOrdered[i].cardId == cardId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return;

            collectionPopupView.Show(openedCardsOrdered, index);
        }

        private void Clear()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                    Destroy(spawnedCards[i]);
            }

            spawnedCards.Clear();
        }

        // Sorting
        private int GetRaritySortScore(StatRarity rarity)
        {
            switch (rarity)
            {
                case StatRarity.Legendary: return 4;
                case StatRarity.Epic: return 3;
                case StatRarity.Rare: return 2;
                case StatRarity.Common: return 1;
                default: return 0;
            }
        }
    }
}