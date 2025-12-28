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

        public void OpenChest()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null || popup == null) return;

            // play chest animation (button always active => reliable)
            if (chestAnim) chestAnim.PlayOpen();

            if (!gi.SpendChest(1, immediateSave: false))
                return;

            gi.AddExp(5, immediateSave: false);

            var rolled = ChestService.Roll(dropTable);
            if (rolled.Item == null)
            {
                Debug.LogWarning("[Chest] Rolled null item (db pool empty?)");
                gi.SaveAllNow();
                return;
            }

            popup.Show(rolled.Item);

            gi.SaveAllNow();
        }
    }
}



