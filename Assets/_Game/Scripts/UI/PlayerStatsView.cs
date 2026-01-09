using TMPro;
using UnityEngine;
using GameCore.Items;

namespace GameCore.UI
{
    public class PlayerStatsView : MonoBehaviour
    {
        [Header("Base TMP")]
        [SerializeField] private TMP_Text atkText;
        [SerializeField] private TMP_Text defText;
        [SerializeField] private TMP_Text hpText;

        [Header("Visual (optional)")]
        [SerializeField] private VisualEquipmentService visual; // можно не назначать

        private void OnEnable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            if (visual == null)
                visual = VisualEquipmentService.I;

            if (visual != null)
                visual.OnChanged += OnVisualChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;

            if (visual != null)
                visual.OnChanged -= OnVisualChanged;
        }

        private void OnStateChanged(GameCore.PlayerState _)
        {
            Refresh();
        }

        private void OnVisualChanged()
        {
            Refresh();
        }

        public void Refresh()
        {
            // core + visual вместе (если visual null Ч EquipmentService сам найдЄт VisualEquipmentService.I через твой код, либо передай visual)
            var total = EquipmentService.GetTotalBaseStats_Combined(visual);

            if (atkText) atkText.text = total.Atk.ToString();
            if (defText) defText.text = total.Def.ToString();
            if (hpText) hpText.text = total.Hp.ToString();
        }
    }
}

