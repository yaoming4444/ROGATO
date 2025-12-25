using System.Text;
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

        [Header("Extras TMP (optional)")]
        [SerializeField] private TMP_Text extrasText;

        private void OnEnable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameCore.PlayerState _)
        {
            Refresh();
        }

        public void Refresh()
        {
            // 1) базовые
            var baseStats = EquipmentService.GetTotalBaseStats();
            if (atkText) atkText.text = baseStats.Atk.ToString();
            if (defText) defText.text = baseStats.Def.ToString();
            if (hpText) hpText.text = baseStats.Hp.ToString();

            // 2) доп. статы
            if (!extrasText) return;

            var extras = EquipmentService.GetTotalExtraStats();
            if (extras == null || extras.Count == 0)
            {
                extrasText.text = "";
                return;
            }

            var sb = new StringBuilder(128);
            foreach (var kv in extras)
            {
                sb.AppendLine($"{kv.Key}: {kv.Value:0.##}");
            }
            extrasText.text = sb.ToString();
        }
    }
}

