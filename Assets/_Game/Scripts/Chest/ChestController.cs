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

        private void Awake()
        {
            // Чтобы после Equip/Sell возвращать сундук в Idle
            if (popup != null)
            {
                popup.OnDecisionMade -= OnPopupDecision; // на всякий случай от дублей
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
            if (chestAnim != null)
                chestAnim.ResetToIdle();
        }

        public void OpenChest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null)
                return;

            // 1) сначала тратим сундук
            if (!gi.SpendChest(1, immediateSave: false))
                return;

            // 2) даём опыт
            gi.AddExp(5, immediateSave: false);

            // 3) роллим предмет
            var rolled = ChestService.Roll(dropTable);
            var item = rolled.Item;
            if (item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                gi.SaveAllNow();
                return;
            }

            // 4) запускаем анимацию сундука + иконка поверх (в конце анимации)
            if (chestAnim != null)
                chestAnim.PlayOpen(item.Icon);

            // 5) показываем попап
            popup.Show(item);

            // 6) сохраняем
            gi.SaveAllNow();
        }
    }
}



