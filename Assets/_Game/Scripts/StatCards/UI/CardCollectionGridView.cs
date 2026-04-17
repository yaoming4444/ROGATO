using System.Collections.Generic;
using UnityEngine;
using GameCore.Stats;

namespace GameCore.UI
{
    public class CardCollectionGridView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;

        [Header("Data")]
        [SerializeField] private StatRollConfig statRollConfig;

        [Header("Prefabs")]
        [SerializeField] private GameObject closedCardPrefab;
        [SerializeField] private GameObject openedCardPrefab;

        private readonly List<GameObject> spawnedCards = new();
        private readonly HashSet<string> unlockedCardIds = new();
        private readonly Dictionary<string, int> cardLevels = new();

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

        public void Rebuild()
        {
            Clear();

            if (contentRoot == null || statRollConfig == null)
                return;

            IReadOnlyList<StatCardDefinition> cards = statRollConfig.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                StatCardDefinition definition = cards[i];
                if (definition == null)
                    continue;

                bool isUnlocked = unlockedCardIds.Contains(definition.CardId);

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

            GameObject go = Instantiate(openedCardPrefab, contentRoot, false);
            spawnedCards.Add(go);

            int level = 1;
            if (cardLevels.TryGetValue(definition.CardId, out int savedLevel))
                level = savedLevel;

            OpenedCardView view = go.GetComponent<OpenedCardView>();
            if (view != null)
                view.Setup(definition, level);
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
    }
}