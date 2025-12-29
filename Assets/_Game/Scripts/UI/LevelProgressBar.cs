using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Progression;

namespace GameCore.UI
{
    public class LevelProgressBar : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image fillImage;        // Image (Type = Filled)
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text progressText;

        private LevelProgression _prog;

        private void Awake()
        {
            // Resources/LevelProgression.asset
            _prog = Resources.Load<LevelProgression>("LevelProgression");
        }

        private void OnEnable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            OnStateChanged(GameCore.GameInstance.I?.State);
        }

        private void OnDisable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameCore.PlayerState s)
        {
            if (s == null || _prog == null)
                return;

            int lvl = Mathf.Max(_prog.StartLevel, s.Level);
            int totalExp = Mathf.Max(0, s.Exp);

            if (levelText) levelText.text = $"LVL {lvl}";

            if (_prog.IsMaxLevel(lvl))
            {
                if (fillImage) fillImage.fillAmount = 1f;
                if (progressText) progressText.text = "MAX";
                return;
            }

            int prevReq = _prog.GetRequiredTotalExpForLevel(lvl);
            int nextReq = _prog.GetRequiredTotalExpForLevel(lvl + 1);

            int cur = Mathf.Max(0, totalExp - prevReq);
            int need = Mathf.Max(1, nextReq - prevReq);

            float fill = Mathf.Clamp01(cur / (float)need);

            if (fillImage) fillImage.fillAmount = fill;
            if (progressText) progressText.text = $"{cur}/{need}";
        }
    }
}



