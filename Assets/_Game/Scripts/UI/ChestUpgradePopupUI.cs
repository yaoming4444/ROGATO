using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;

namespace GameCore.UI
{
    public class ChestUpgradePopupUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ChestDropTable dropTable;

        [Header("Header")]
        [SerializeField] private TMP_Text leftTitle;   // "LVL 1"
        [SerializeField] private TMP_Text rightTitle;  // "LVL 2" / "MAX"

        [Header("Rows (same order on both sides)")]
        [SerializeField] private List<RarityRowUI> leftRows = new();
        [SerializeField] private List<RarityRowUI> rightRows = new();

        [Header("Upgrade")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonText; // optional (если хочешь "UPGRADE (50)")

        [Serializable]
        public class RarityRowUI
        {
            public Rarity rarity;
            public TMP_Text percentText;     // "69%" справа
            public Image fillImage;          // зеленая полоска (Image type = Filled)
            public GameObject root;          // optional: вся строка
        }

        private void Awake()
        {
            if (upgradeButton)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnEnable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            Redraw();
        }

        private void OnDisable()
        {
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameCore.PlayerState s) => Redraw();

        private void OnUpgradeClicked()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || dropTable == null) return;

            gi.TryUpgradeChest(dropTable, immediateSave: true);

            // если TryUpgradeChest внутри делает StateChanged — это не обязательно,
            // но пусть будет для мгновенного апдейта
            Redraw();
        }

        private void Redraw()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null || dropTable == null) return;

            int curLevel = Mathf.Max(1, gi.State.ChestLevel);
            int nextLevel = curLevel + 1;

            // Titles
            if (leftTitle) leftTitle.text = $"LVL {curLevel}";

            bool isMax = dropTable.IsMaxLevel(curLevel);
            if (rightTitle) rightTitle.text = isMax ? "MAX" : $"LVL {nextLevel}";

            // Rows
            DrawColumn(leftRows, curLevel, isMaxColumn: false);

            if (isMax)
                DrawMaxColumn(rightRows);
            else
                DrawColumn(rightRows, nextLevel, isMaxColumn: false);

            // Button state + cost
            if (upgradeButton)
                upgradeButton.interactable = !isMax;

            if (upgradeButtonText)
            {
                if (isMax)
                {
                    upgradeButtonText.text = "MAX";
                }
                else
                {
                    int cost = dropTable.GetUpgradeCostGems(nextLevel);
                    upgradeButtonText.text = cost > 0 ? $"UPGRADE ({cost})" : "UPGRADE";
                }
            }
        }

        private void DrawColumn(List<RarityRowUI> rows, int level, bool isMaxColumn)
        {
            if (rows == null) return;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null) continue;

                float pct = dropTable.GetChancePercent(level, r.rarity); // 0..100

                if (r.root) r.root.SetActive(true);

                if (r.percentText)
                    r.percentText.text = $"{Mathf.RoundToInt(pct)}%";

                if (r.fillImage)
                {
                    // важно: fillImage.type = Filled, fillMethod Horizontal
                    r.fillImage.fillAmount = Mathf.Clamp01(pct / 100f);
                }
            }
        }

        private void DrawMaxColumn(List<RarityRowUI> rows)
        {
            if (rows == null) return;

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null) continue;

                if (r.root) r.root.SetActive(true);

                if (r.percentText) r.percentText.text = "-";
                if (r.fillImage) r.fillImage.fillAmount = 0f;
            }
        }
    }
}

