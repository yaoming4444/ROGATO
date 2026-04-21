using GameCore.Stats;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IDosGames;

namespace GameCore.UI
{
    public class CardRollController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StatRollConfig statRollConfig;
        [SerializeField] private StatCardRollService rollService;
        [SerializeField] private RolledStatCardManager cardManager;
        [SerializeField] private CardCollectionGridView collectionGridView;

        [Header("UI")]
        [SerializeField] private Button rollButton;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text resultText;

        [Header("Economy")]
        [SerializeField] private VirtualCurrencyID virtualCurrencyId = VirtualCurrencyID.CO;
        [SerializeField] private int rollPriceStep = 50;
        [SerializeField] private bool saveImmediatelyAfterRoll = true;

        [Header("SDK Refresh Before Charge")]
        [Tooltip("Перед списанием сделать RequestUserInventory, чтобы баланс был актуальный")]
        [SerializeField] private bool refreshInventoryBeforeCharge = true;

        [Tooltip("Таймаут ожидания InventoryUpdated")]
        [SerializeField] private float inventoryRefreshTimeoutSeconds = 3f;

        [Header("Debug / Temporary")]
        [SerializeField] private bool useLocalGoldFallbackForTesting = true;

        private GameInstance game;
        private bool isRolling;

        // SDK charge flow
        private bool _chargeInFlight;
        private bool _sdkSpendFinished;
        private bool _sdkSpendSuccess;

        // inventory refresh flow
        private bool _waitingInventoryRefresh;
        private bool _inventoryRefreshFinished;
        private Coroutine _inventoryTimeoutCoroutine;

        private void Awake()
        {
            game = GameInstance.I;
        }

        private void OnEnable()
        {
            if (rollButton != null)
                rollButton.onClick.AddListener(OnRollClicked);

            LoadFromState();
            RefreshAllUI();
        }

        private void OnDisable()
        {
            if (rollButton != null)
                rollButton.onClick.RemoveListener(OnRollClicked);

            CleanupServerCurrencyHandlers();
            CleanupInventoryRefreshHandlers();

            isRolling = false;
            _chargeInFlight = false;
        }

        private void OnDestroy()
        {
            CleanupServerCurrencyHandlers();
            CleanupInventoryRefreshHandlers();
        }

        private void LoadFromState()
        {
            if (game == null)
                game = GameInstance.I;

            if (game == null || game.State == null)
                return;

            LoadOwnedCardsFromState();
            SyncRolledStatsToPlayer();
            SyncCollectionFromState();
        }

        private void LoadOwnedCardsFromState()
        {
            if (cardManager == null || statRollConfig == null || game == null)
                return;

            cardManager.ClearAll();

            List<SavedRolledCardData> saved = game.GetRolledCards();
            if (saved == null)
                return;

            for (int i = 0; i < saved.Count; i++)
            {
                SavedRolledCardData savedCard = saved[i];
                if (savedCard == null)
                    continue;

                StatType statType = (StatType)savedCard.statType;
                StatRarity rarity = (StatRarity)savedCard.rarity;

                StatCardDefinition definition = FindDefinition(statType, rarity);
                if (definition == null)
                    continue;

                RolledStatCard runtimeCard = cardManager.ApplyRolledCard(definition);
                if (runtimeCard != null)
                    runtimeCard.SetLevelAndValue(savedCard.level, savedCard.currentValue);
            }
        }

        private StatCardDefinition FindDefinition(StatType statType, StatRarity rarity)
        {
            if (statRollConfig == null)
                return null;

            IReadOnlyList<StatCardDefinition> cards = statRollConfig.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                StatCardDefinition def = cards[i];
                if (def == null)
                    continue;

                if (def.StatType == statType && def.Rarity == rarity)
                    return def;
            }

            return null;
        }

        private void SyncCollectionFromState()
        {
            if (collectionGridView == null || game == null)
                return;

            collectionGridView.SetUnlockedIds(game.GetUnlockedCardIds());
            collectionGridView.SetCardLevels(BuildCardLevelsMap());
            collectionGridView.Rebuild();
        }

        private void RefreshAllUI()
        {
            RefreshPriceUI();
            RefreshRollButtonState();
        }

        private void RefreshPriceUI()
        {
            if (priceText == null || game == null)
                return;

            priceText.text = game.GetCardRollPrice().ToString();
        }

        private void RefreshRollButtonState()
        {
            if (rollButton != null)
                rollButton.interactable = !isRolling;
        }

        private void SetResultText(string text)
        {
            if (resultText != null)
                resultText.text = text;

            Debug.Log($"[CardRollController] {text}");
        }

        public void OnRollClicked()
        {
            if (!isRolling)
                StartCoroutine(RollRoutine());
        }

        private IEnumerator RollRoutine()
        {
            if (game == null)
                game = GameInstance.I;

            if (game == null || rollService == null || cardManager == null)
                yield break;

            isRolling = true;
            RefreshRollButtonState();

            StatCardDefinition rolledDefinition = rollService.RollOneCard(cardManager.OwnedCards);

            if (rolledDefinition == null)
            {
                SetResultText("No cards available");
                isRolling = false;
                RefreshRollButtonState();
                yield break;
            }

            int currentPrice = game.GetCardRollPrice();

            bool spendSuccess = false;
            yield return StartCoroutine(SpendRollCurrencyRoutine(currentPrice, success =>
            {
                spendSuccess = success;
            }));

            if (!spendSuccess)
            {
                SetResultText("Not enough currency");
                isRolling = false;
                RefreshRollButtonState();
                yield break;
            }

            bool alreadyOwned = cardManager.HasCard(rolledDefinition);
            bool alreadyUnlocked = game.IsCardUnlocked(rolledDefinition.CardId);

            RolledStatCard resultCard = cardManager.ApplyRolledCard(rolledDefinition);

            if (resultCard == null)
            {
                SetResultText("Roll failed");
                isRolling = false;
                RefreshRollButtonState();
                yield break;
            }

            if (!alreadyUnlocked)
                game.UnlockCardId(rolledDefinition.CardId, immediateSave: false);

            SaveOwnedCardsToState();
            SyncRolledStatsToPlayer();

            game.IncreaseCardRollPrice(rollPriceStep, immediateSave: false);

            if (saveImmediatelyAfterRoll)
                game.SaveAllNow();

            SyncCollectionFromState();
            RefreshAllUI();

            if (alreadyOwned)
                SetResultText($"Upgraded: {rolledDefinition.DisplayName} Lv.{resultCard.Level}");
            else
                SetResultText($"Unlocked: {rolledDefinition.DisplayName}");

            isRolling = false;
            RefreshRollButtonState();
        }

        private void SaveOwnedCardsToState()
        {
            if (game == null || cardManager == null)
                return;

            List<SavedRolledCardData> saved = new();

            IReadOnlyList<RolledStatCard> owned = cardManager.OwnedCards;
            for (int i = 0; i < owned.Count; i++)
            {
                RolledStatCard card = owned[i];
                if (card == null)
                    continue;

                saved.Add(new SavedRolledCardData
                {
                    statType = (int)card.StatType,
                    rarity = (int)card.Rarity,
                    level = card.Level,
                    currentValue = card.CurrentValue
                });
            }

            game.SetRolledCards(saved, immediateSave: false);
        }

        private IEnumerator SpendRollCurrencyRoutine(int amount, Action<bool> onComplete)
        {
            bool success = false;

            if (useLocalGoldFallbackForTesting)
            {
                success = game != null && game.SpendGold(amount, immediateSave: false);
                onComplete?.Invoke(success);
                yield break;
            }

            if (_chargeInFlight)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            // 1. Перед списанием можно обновить inventory
            if (refreshInventoryBeforeCharge)
            {
                BeginInventoryRefresh();

                while (!_inventoryRefreshFinished)
                    yield return null;
            }

            // 2. Проверяем локальный кэш после refresh
            int current = UserInventory.GetVirtualCurrencyAmount(virtualCurrencyId);
            if (current < amount)
            {
                onComplete?.Invoke(false);
                yield break;
            }

            // 3. Подписка на успех/ошибку списания
            CleanupServerCurrencyHandlers();

            _chargeInFlight = true;
            _sdkSpendFinished = false;
            _sdkSpendSuccess = false;

            UserInventory.SuccessSubtractVirtualCurrency += OnServerChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency += OnServerChargeError;

            UserInventory.SubtractVirtualCurrency(virtualCurrencyId, amount);

            while (!_sdkSpendFinished)
                yield return null;

            success = _sdkSpendSuccess;
            onComplete?.Invoke(success);
        }

        // =========================
        // SDK Charge Callbacks
        // =========================
        private void OnServerChargeSuccess()
        {
            CleanupServerCurrencyHandlers();

            _chargeInFlight = false;
            _sdkSpendSuccess = true;
            _sdkSpendFinished = true;
        }

        private void OnServerChargeError()
        {
            CleanupServerCurrencyHandlers();

            _chargeInFlight = false;
            _sdkSpendSuccess = false;
            _sdkSpendFinished = true;
        }

        private void CleanupServerCurrencyHandlers()
        {
            UserInventory.SuccessSubtractVirtualCurrency -= OnServerChargeSuccess;
            UserInventory.ErrorSubtractVirtualCurrency -= OnServerChargeError;
        }

        // =========================
        // Inventory Refresh Before Charge
        // =========================
        private void BeginInventoryRefresh()
        {
            CleanupInventoryRefreshHandlers();

            _waitingInventoryRefresh = true;
            _inventoryRefreshFinished = false;

            UserInventory.InventoryUpdated += OnInventoryUpdated;
            UserDataService.RequestUserInventory();

            _inventoryTimeoutCoroutine = StartCoroutine(InventoryRefreshTimeoutCoroutine());
        }

        private void OnInventoryUpdated()
        {
            if (!_waitingInventoryRefresh)
                return;

            CleanupInventoryRefreshHandlers();
            _inventoryRefreshFinished = true;
        }

        private IEnumerator InventoryRefreshTimeoutCoroutine()
        {
            float timer = 0f;

            while (_waitingInventoryRefresh && timer < inventoryRefreshTimeoutSeconds)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_waitingInventoryRefresh)
                yield break;

            Debug.LogWarning("[CardRollController] Inventory refresh timeout. Continue with current cache.");

            CleanupInventoryRefreshHandlers();
            _inventoryRefreshFinished = true;
        }

        private void CleanupInventoryRefreshHandlers()
        {
            _waitingInventoryRefresh = false;

            UserInventory.InventoryUpdated -= OnInventoryUpdated;

            if (_inventoryTimeoutCoroutine != null)
            {
                StopCoroutine(_inventoryTimeoutCoroutine);
                _inventoryTimeoutCoroutine = null;
            }
        }

        private Dictionary<string, int> BuildCardLevelsMap()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            List<SavedRolledCardData> saved = game.GetRolledCards();
            if (saved == null || statRollConfig == null)
                return result;

            for (int i = 0; i < saved.Count; i++)
            {
                SavedRolledCardData savedCard = saved[i];
                if (savedCard == null)
                    continue;

                StatType statType = (StatType)savedCard.statType;
                StatRarity rarity = (StatRarity)savedCard.rarity;

                StatCardDefinition definition = FindDefinition(statType, rarity);
                if (definition == null || string.IsNullOrWhiteSpace(definition.CardId))
                    continue;

                result[definition.CardId] = Mathf.Max(1, savedCard.level);
            }

            return result;
        }

        /// SYNC ROLLED STATS TO PLAYER
        private void SyncRolledStatsToPlayer()
        {
            if (cardManager == null)
                return;

            var player = OctoberStudio.PlayerBehavior.Player;
            if (player == null)
            {
                Debug.Log("[CardRollController] PlayerBehavior not found in current scene. Rolled stats saved to state only.");
                return;
            }

            Dictionary<StatType, float> totals = cardManager.BuildTotals();
            player.SetRolledCardBonuses(totals);

            Debug.Log("[CardRollController] Rolled stats synced to PlayerBehavior.");
        }
    }
}