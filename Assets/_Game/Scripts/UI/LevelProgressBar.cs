using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Progression;

namespace GameCore.UI
{
    public class LevelProgressBar : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Slider slider;          // <- Slider на родителе (как у тебя)
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text progressText;

        private LevelProgression _prog;

        private void Awake()
        {
            _prog = Resources.Load<LevelProgression>("LevelProgression");

            // На всякий: если забыли проставить
            if (slider == null) slider = GetComponent<Slider>();

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;

                // чтобы игрок не мог двигать (если это просто индикатор)
                slider.interactable = false;
            }
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

            if (levelText) levelText.text = $"{lvl}";

            if (_prog.IsMaxLevel(lvl))
            {
                SetFill01(1f);
                if (progressText) progressText.text = "MAX";
                return;
            }

            int prevReq = _prog.GetRequiredTotalExpForLevel(lvl);
            int nextReq = _prog.GetRequiredTotalExpForLevel(lvl + 1);

            int cur = Mathf.Max(0, totalExp - prevReq);
            int need = Mathf.Max(1, nextReq - prevReq);

            float fill01 = Mathf.Clamp01(cur / (float)need);

            SetFill01(fill01);
            if (progressText) progressText.text = $"{cur}/{need}";
        }

        private void SetFill01(float v)
        {
            v = Mathf.Clamp01(v);

            if (slider != null)
            {
                // Можно так:
                slider.normalizedValue = v;

                // или так (одно и то же при min=0 max=1):
                // slider.value = v;
            }
        }
    }
}




