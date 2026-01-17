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

        private void Awake()
        {
            // Popup лучше держать выключенным по умолчанию в сцене,
            // но на всякий случай выключим здесь.
            if (popup != null && popup.gameObject.activeSelf)
                popup.gameObject.SetActive(false);

            // Чтобы после Equip/Sell возвращать сундук в Idle
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
            // Игрок принял решение => очищаем pending, возвращаем сундук в idle
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
                // Если предмета больше нет в базе - не даём игроку застрять.
                gi.ClearPendingChestReward(immediateSave: true);
                if (chestAnim != null) chestAnim.ResetToIdle();
                return;
            }

            _pendingItem = item;
            _busy = false;

            // Попап по умолчанию скрываем. Игрок сам решит, когда вернуться.
            if (popup.gameObject.activeSelf)
                popup.Hide();

            // Восстанавливаем визуал сундука (открыт + иконка/VFX)
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

        public void OpenChest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return;

            // Если уже есть pending reward - не тратим сундук повторно.
            // Просто возвращаем попап, чтобы игрок принял решение.
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

            // 1) сначала тратим сундук
            if (!gi.SpendChest(1, immediateSave: false))
                return;

            _busy = true;

            // 2) даём опыт
            gi.AddExp(10, immediateSave: false);

            // 3) роллим предмет
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

            // 4) сохраняем pending в State сразу (чтобы пережило выход из игры даже во время анимации)
            gi.SetPendingChestReward(item.Id, immediateSave: false);

            // 5) на всякий случай спрячем попап (если вдруг был открыт)
            if (popup.gameObject.activeSelf)
                popup.Hide();

            // 6) запускаем анимацию сундука; попап покажем ТОЛЬКО ПОСЛЕ окончания
            if (chestAnim != null)
            {
                chestAnim.PlayOpen(item.Icon, item.RarityVFX, onOpened: () =>
                {
                    if (_pendingItem == null) { _busy = false; return; }

                    popup.Show(_pendingItem);
                    // busy снимем только когда игрок нажмёт Equip/Sell (OnDecisionMade)
                });
            }
            else
            {
                // если анимации нет — показываем сразу
                popup.Show(item);
            }

            // 7) сохраняем
            gi.SaveAllNow();
        }
    }
}
