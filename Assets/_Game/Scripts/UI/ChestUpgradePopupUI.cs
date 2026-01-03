using System;
using System.Collections.Generic;
using System.Globalization;
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

        [Header("Rows (exactly 9 on both sides)")]
        [SerializeField] private List<RowUI> leftRows = new();
        [SerializeField] private List<RowUI> rightRows = new();

        [Header("Upgrade")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonText; // optional

        [Header("Colors by rarity")]
        [SerializeField] private RarityColor[] rarityColors;
        [SerializeField] private Color emptyRowColor = new Color(1f, 1f, 1f, 0.15f);

        [Serializable]
        public class RowUI
        {
            public TMP_Text rarityText;   // "E"
            public TMP_Text percentText;  // "0.2%"
            public Image bgImage;         // плашка строки
            public GameObject root;       // если хочешь отключать строку (не обязательно)
        }

        [Serializable]
        public class RarityColor
        {
            public Rarity rarity;
            public Color color;
        }

        private Dictionary<Rarity, Color> _colorMap;

        private void Awake()
        {
            BuildColorMap();

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
            Redraw();
        }

        private void Redraw()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || gi.State == null || dropTable == null) return;

            int curLevel = Mathf.Max(1, gi.State.ChestLevel);
            int nextLevel = curLevel + 1;

            if (leftTitle) leftTitle.text = $"LVL {curLevel}";

            bool isMax = dropTable.IsMaxLevel(curLevel);
            if (rightTitle) rightTitle.text = isMax ? "MAX" : $"LVL {nextLevel}";

            DrawColumn(leftRows, curLevel);

            if (isMax) DrawMax(rightRows);
            else DrawColumn(rightRows, nextLevel);

            if (upgradeButton) upgradeButton.interactable = !isMax;

            if (upgradeButtonText)
            {
                if (isMax) upgradeButtonText.text = "MAX";
                else
                {
                    int cost = dropTable.GetUpgradeCostGems(nextLevel);
                    upgradeButtonText.text = cost > 0 ? $"UPGRADE ({cost})" : "UPGRADE";
                }
            }
        }

        private void DrawColumn(List<RowUI> rows, int level)
        {
            if (rows == null || rows.Count == 0) return;

            // Список рарностей начиная с minRarity и вверх по enum.
            var list = BuildRarityListFromMin(level);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;

                if (row.root) row.root.SetActive(true);

                if (i < list.Count)
                {
                    var rarity = list[i];
                    float pct = dropTable.GetChancePercent(level, rarity); // 0..100 (нормализованный)

                    if (row.rarityText) row.rarityText.text = rarity.ToString();
                    if (row.percentText) row.percentText.text = FormatPercent(pct);

                    if (row.bgImage) row.bgImage.color = GetColor(rarity);
                }
                else
                {
                    // “лишние” строки (внизу)
                    if (row.rarityText) row.rarityText.text = "-";
                    if (row.percentText) row.percentText.text = "-";
                    if (row.bgImage) row.bgImage.color = emptyRowColor;
                }
            }
        }

        private void DrawMax(List<RowUI> rows)
        {
            if (rows == null || rows.Count == 0) return;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;

                if (row.root) row.root.SetActive(true);

                if (row.rarityText) row.rarityText.text = "-";
                if (row.percentText) row.percentText.text = "-";
                if (row.bgImage) row.bgImage.color = emptyRowColor;
            }
        }

        private List<Rarity> BuildRarityListFromMin(int level)
        {
            var result = new List<Rarity>(16);

            var min = dropTable.GetMinRarity(level);

            // ВАЖНО: предполагаем что enum упорядочен: G(0) ... SS(8) и т.д.
            int minV = (int)min;
            int maxV = GetMaxRarityEnumValue();

            for (int v = minV; v <= maxV; v++)
                result.Add((Rarity)v);

            return result;
        }

        private int GetMaxRarityEnumValue()
        {
            // Берём максимум по enum (на будущее если добавишь SSS и т.п.)
            Array values = Enum.GetValues(typeof(Rarity));
            int max = 0;
            foreach (var x in values)
                max = Mathf.Max(max, (int)x);
            return max;
        }

        private static string FormatPercent(float pct)
        {
            pct = Mathf.Max(0f, pct);

            // Примеры:
            // 0.2 -> "0.2%"
            // 1.25 -> "1.3%"
            // 12.6 -> "13%"
            if (pct < 1f) return pct.ToString("0.0#", CultureInfo.InvariantCulture) + "%";
            if (pct < 10f) return pct.ToString("0.#", CultureInfo.InvariantCulture) + "%";
            return Mathf.RoundToInt(pct) + "%";
        }

        private void BuildColorMap()
        {
            _colorMap = new Dictionary<Rarity, Color>();
            if (rarityColors == null) return;

            foreach (var rc in rarityColors)
                _colorMap[rc.rarity] = rc.color;
        }

        private Color GetColor(Rarity r)
        {
            if (_colorMap != null && _colorMap.TryGetValue(r, out var c))
                return c;

            return Color.white;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Redraw(); // чтобы сразу обновилось при показе
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}




