using UnityEngine;
using GameCore.Items;

namespace GameCore.UI
{
    public class ChestController : MonoBehaviour
    {
        [SerializeField] private ChestDropTable dropTable;
        [SerializeField] private ChestRewardPopup popup;

        public void OpenChest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || popup == null || dropTable == null) return;

            // 1) списали сундук
            if (!gi.SpendChest(1, immediateSave: true))
            {
                Debug.Log("[Chest] No chests");
                return;
            }

            // 2) ролл
            var rolled = ChestService.Roll(dropTable);
            if (rolled.Item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                return;
            }

            // 3) показать UI (внутри будет сравнение)
            popup.Show(rolled.Item);
        }
    }
}

