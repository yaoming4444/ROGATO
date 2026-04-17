using GameCore.Stats;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.UI
{
    public class CardCollectionGridView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform contentRoot;

        [Header("Database")]
        [SerializeField] private StatRollConfig statRollConfig;

        [Header("Prefabs")]
        [SerializeField] private GameObject closedCardPrefab;
        [SerializeField] private GameObject openedCardPrefab;

        [Header("Opened Cards")]
        [SerializeField] private List<string> unlockedCardIds = new List<string>();

        private readonly List<GameObject> spawnedCards = new List<GameObject>();

        private void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            Clear();

            if (contentRoot == null)
            {
                Debug.LogWarning("[CardCollectionGridView] Content root is missing.");
                return;
            }

            if (statRollConfig == null)
            {
                Debug.LogWarning("[CardCollectionGridView] StatRollConfig is missing.");
                return;
            }

            IReadOnlyList<StatCardDefinition> cards = statRollConfig.Cards;

            for (int i = 0; i < cards.Count; i++)
            {
                StatCardDefinition definition = cards[i];
                if (definition == null)
                    continue;

                bool isUnlocked = unlockedCardIds.Contains(definition.CardId);

                if (isUnlocked)
                    SpawnOpenedCard(definition);
                else
                    SpawnClosedCard();
            }
        }

        public void UnlockCard(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return;

            if (!unlockedCardIds.Contains(cardId))
                unlockedCardIds.Add(cardId);

            Rebuild();
        }

        public bool IsUnlocked(string cardId)
        {
            return unlockedCardIds.Contains(cardId);
        }

        private void SpawnClosedCard()
        {
            if (closedCardPrefab == null)
            {
                Debug.LogWarning("[CardCollectionGridView] Closed card prefab is missing.");
                return;
            }

            GameObject card = Instantiate(closedCardPrefab, contentRoot, false);
            spawnedCards.Add(card);
        }

        private void SpawnOpenedCard(StatCardDefinition definition)
        {
            if (openedCardPrefab == null)
            {
                Debug.LogWarning("[CardCollectionGridView] Opened card prefab is missing.");
                return;
            }

            GameObject card = Instantiate(openedCardPrefab, contentRoot, false);
            spawnedCards.Add(card);

            OpenedCardView openedView = card.GetComponent<OpenedCardView>();
            if (openedView != null)
                openedView.Setup(definition);
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