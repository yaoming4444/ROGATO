using TMPro;
using UnityEngine;

namespace GameCore.UI
{
    public class PlayerStateView : MonoBehaviour
    {
        [Header("Bind TMP Texts")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text gemsText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private TMP_Text chestsText;

        private void OnEnable()
        {
            Subscribe();
            Refresh(GameCore.GameInstance.I?.State);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= Refresh;
        }

        private void Refresh(GameCore.PlayerState s)
        {
            if (s == null)
            {
                Set(gold: "—", level: "—", gems: "—", exp: "—", chests: "—");
                return;
            }

            Set(
                gold: s.Gold.ToString(),
                level: s.Level.ToString(),
                gems: s.Gems.ToString(),
                exp: s.Exp.ToString(),
                chests: s.Chests.ToString()
            );
        }

        private void Set(string gold, string level, string gems, string exp, string chests)
        {
            if (goldText) goldText.text = gold;
            if (levelText) levelText.text = level;
            if (gemsText) gemsText.text = gems;
            if (expText) expText.text = exp;
            if (chestsText) chestsText.text = chests;
        }
    }
}


