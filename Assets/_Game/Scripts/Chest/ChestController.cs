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

        private bool _busy;              // защита от спама кликов
        private ItemDef _pendingItem;    // предмет, который ждЄт показа попапа

        private void Awake()
        {
            // попап лучше держать выключенным по умолчанию в сцене,
            // но на вс€кий случай выключим здесь.
            if (popup != null && popup.gameObject.activeSelf)
                popup.gameObject.SetActive(false);

            // „тобы после Equip/Sell возвращать сундук в Idle
            if (popup != null)
            {
                popup.OnDecisionMade -= OnPopupDecision;
                popup.OnDecisionMade += OnPopupDecision;
            }
        }

        private void OnDestroy()
        {
            if (popup != null)
                popup.OnDecisionMade -= OnPopupDecision;
        }

        private void OnPopupDecision()
        {
            // игрок прин€л решение => возвращаем сундук в idle и снимаем busy
            if (chestAnim != null)
                chestAnim.ResetToIdle();

            _pendingItem = null;
            _busy = false;
        }

        public void OpenChest()
        {
            if (_busy) return;

            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return;

            // 1) сначала тратим сундук
            if (!gi.SpendChest(1, immediateSave: false))
                return;

            _busy = true;

            // 2) даЄм опыт
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

            // 4) на вс€кий случай спр€чем попап (если вдруг был открыт)
            if (popup.gameObject.activeSelf)
                popup.Hide();

            // 5) запускаем анимацию сундука; попап покажем “ќЋ№ ќ ѕќ—Ћ≈ окончани€
            if (chestAnim != null)
            {
                chestAnim.PlayOpen(item.Icon, onOpened: () =>
                {
                    // защита: вдруг уже сбросили/сменили
                    if (_pendingItem == null) { _busy = false; return; }

                    popup.Show(_pendingItem);
                    // busy снимем только когда игрок нажмЄт Equip/Sell (OnDecisionMade)
                });
            }
            else
            {
                // если анимации нет Ч показываем сразу
                popup.Show(item);
            }

            // 6) сохран€ем
            gi.SaveAllNow();
        }
    }
}




