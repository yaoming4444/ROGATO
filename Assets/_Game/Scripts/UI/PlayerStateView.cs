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
                Set("—", "—", "—");
                return;
            }

            Set(
                gold: s.Gold.ToString(),
                level: s.Level.ToString(),
                gems: s.Gems.ToString()
            );
        }

        private void Set(string gold, string level, string gems)
        {
            if (goldText) goldText.text = gold;
            if (levelText) levelText.text = level;
            if (gemsText) gemsText.text = gems;
        }
    }
}

