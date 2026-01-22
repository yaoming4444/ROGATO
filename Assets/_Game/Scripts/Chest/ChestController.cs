using System;
using UnityEngine;
using GameCore.Items;

namespace GameCore.UI
{
    public class ChestController : MonoBehaviour
    {
        [SerializeField] private ChestDropTable dropTable;
        [SerializeField] private ChestRewardPopup popup;

        [Header("Chest Button Animation")]
        [SerializeField] private ChestAnimDriver chestAnim;

        private bool _busy;           // protects from spam clicks while opening / popup open
        private ItemDef _pendingItem; // item awaiting decision

        public bool IsBusy => _busy;
        public ItemDef PendingItem => _pendingItem;

        private void Awake()
        {
            if (popup != null && popup.gameObject.activeSelf)
                popup.gameObject.SetActive(false);

            if (popup != null)
            {
                popup.OnDecisionMade -= OnPopupDecision;
                popup.OnDecisionMade += OnPopupDecision;
            }
        }

        private void Start()
        {
            RestorePendingIfAny();
        }

        private void OnDestroy()
        {
            if (popup != null)
                popup.OnDecisionMade -= OnPopupDecision;
        }

        /// <summary>
        /// Кнопка "X / Close" на попапе должна вызывать этот метод.
        /// Попап скрываем, но сундук (открытый + иконка/VFX) остаётся.
        /// </summary>
        public void HideRewardPopup()
        {
            if (popup != null && popup.gameObject.activeSelf)
                popup.Hide();

            // Разрешаем снова кликнуть по сундуку, чтобы открыть попап обратно.
            _busy = false;
        }

        private void OnPopupDecision()
        {
            // Игрок принял решение => очищаем pending, возвращаем сундук в idle :contentReference[oaicite:1]{index=1}
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.ClearPendingChestReward(immediateSave: false);

            if (chestAnim != null)
                chestAnim.ResetToIdle();

            _pendingItem = null;
            _busy = false;

            if (gi != null)
                gi.SaveAllNow();
        }

        private void RestorePendingIfAny()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || popup == null)
                return;

            if (!gi.HasPendingChestReward)
                return;

            var item = ResolveItem(gi.PendingChestItemId);
            if (item == null)
            {
                gi.ClearPendingChestReward(immediateSave: true);
                if (chestAnim != null) chestAnim.ResetToIdle();
                return;
            }

            _pendingItem = item;
            _busy = false;

            if (popup.gameObject.activeSelf)
                popup.Hide();

            if (chestAnim != null)
                chestAnim.SetOpenedStatic(item.Icon, item.RarityVFX);
        }

        private static ItemDef ResolveItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            var db = ItemDatabase.I;
            if (db == null)
                return null;

            return db.GetById(itemId);
        }

        /// <summary>
        /// Показать попап по текущему pending.
        /// </summary>
        public void ShowPendingPopup()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || popup == null) return;
            if (!gi.HasPendingChestReward) return;

            if (_pendingItem == null || _pendingItem.Id != gi.PendingChestItemId)
                _pendingItem = ResolveItem(gi.PendingChestItemId);

            if (_pendingItem == null)
            {
                gi.ClearPendingChestReward(immediateSave: true);
                if (chestAnim != null) chestAnim.ResetToIdle();
                _busy = false;
                return;
            }

            if (!popup.gameObject.activeSelf)
            {
                popup.Show(_pendingItem);
                _busy = true;
            }
        }

        /// <summary>
        /// Закрыть pending без попапа (для авто-продажи/авто-экипа).
        /// </summary>
        public void FinalizePending()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.ClearPendingChestReward(immediateSave: false);

            if (chestAnim != null)
                chestAnim.ResetToIdle();

            _pendingItem = null;
            _busy = false;

            if (gi != null)
                gi.SaveAllNow();
        }

        public void FinalizePendingKeepVisual()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.ClearPendingChestReward(immediateSave: false);

            _pendingItem = null;
            _busy = false;

            if (gi != null)
                gi.SaveAllNow();
        }

        /// <summary>
        /// Авто-режим: открывает сундук и вызывает onOpened ПОСЛЕ окончания анимации.
        /// Попап НЕ показываем автоматически.
        /// </summary>
        public bool TryOpenChestAuto(Action<ItemDef> onOpened)
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return false;

            // Есть pending — не тратим сундук, просто отдаём предмет
            if (gi.HasPendingChestReward)
            {
                if (_pendingItem == null || _pendingItem.Id != gi.PendingChestItemId)
                    _pendingItem = ResolveItem(gi.PendingChestItemId);

                if (_pendingItem == null)
                {
                    gi.ClearPendingChestReward(immediateSave: true);
                    if (chestAnim != null) chestAnim.ResetToIdle();
                    _busy = false;
                    return false;
                }

                onOpened?.Invoke(_pendingItem);
                return true;
            }

            if (_busy) return false;

            // 1) тратим сундук :contentReference[oaicite:2]{index=2}
            if (!gi.SpendChest(1, immediateSave: false))
                return false;

            _busy = true;

            // 2) опыт
            gi.AddExp(10, immediateSave: false);

            // 3) ролл предмета :contentReference[oaicite:3]{index=3}
            var rolled = ChestService.Roll(dropTable);
            var item = rolled.Item;
            if (item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                gi.SaveAllNow();
                _busy = false;
                return false;
            }

            _pendingItem = item;

            // 4) pending сохраняем сразу (чтобы пережило выход во время анимации) :contentReference[oaicite:4]{index=4}
            gi.SetPendingChestReward(item.Id, immediateSave: false);

            // 5) спрячем попап
            if (popup.gameObject.activeSelf)
                popup.Hide();

            // 6) запускаем анимацию; callback придёт ПОСЛЕ анимации (см. ChestAnimDriver)
            if (chestAnim != null)
            {
                chestAnim.PlayOpen(item.Icon, item.RarityVFX, onOpened: () =>
                {
                    if (_pendingItem == null) { _busy = false; return; }
                    onOpened?.Invoke(_pendingItem);
                });
            }
            else
            {
                // если анимации нет — считаем что "всё сразу"
                onOpened?.Invoke(item);
            }

            gi.SaveAllNow();
            return true;
        }

        // Ручной режим (как у тебя сейчас) :contentReference[oaicite:5]{index=5}
        public void OpenChest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return;

            if (gi.HasPendingChestReward)
            {
                if (_pendingItem == null || _pendingItem.Id != gi.PendingChestItemId)
                    _pendingItem = ResolveItem(gi.PendingChestItemId);

                if (_pendingItem == null)
                {
                    gi.ClearPendingChestReward(immediateSave: true);
                    if (chestAnim != null) chestAnim.ResetToIdle();
                    _busy = false;
                    return;
                }

                if (!popup.gameObject.activeSelf)
                {
                    popup.Show(_pendingItem);
                    _busy = true;
                }

                return;
            }

            if (_busy) return;

            if (!gi.SpendChest(1, immediateSave: false))
                return;

            _busy = true;

            gi.AddExp(10, immediateSave: false);

            var rolled = ChestService.Roll(dropTable);
            var item = rolled.Item;
            if (item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                gi.SaveAllNow();
                _busy = false;
                return;
            }

            _pendingItem = item;
            gi.SetPendingChestReward(item.Id, immediateSave: false);

            if (popup.gameObject.activeSelf)
                popup.Hide();

            if (chestAnim != null)
            {
                chestAnim.PlayOpen(item.Icon, item.RarityVFX, onOpened: () =>
                {
                    if (_pendingItem == null) { _busy = false; return; }
                    popup.Show(_pendingItem);
                });
            }
            else
            {
                popup.Show(item);
            }

            gi.SaveAllNow();
        }
    }
}
