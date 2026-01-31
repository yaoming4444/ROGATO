using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;

namespace GameCore.UI
{
    public class ChestRewardPopup : MonoBehaviour
    {
        [Header("NEW item block (always visible)")]
        [SerializeField] private Image newIcon;
        [SerializeField] private Image newRarity;      // badge sprite set in prefab (we only tint)
        [SerializeField] private TMP_Text newName;
        [SerializeField] private TMP_Text newLevelText; // NEW: level separate
        [SerializeField] private TMP_Text newPowerText; // ONE power text here

        [Header("NEW stats (numbers only)")]
        [SerializeField] private TMP_Text newHpText;   // digits only
        [SerializeField] private TMP_Text newAtkText;  // digits only

        [Header("CURRENT item block (only if comparison)")]
        [SerializeField] private GameObject currentBlock;
        [SerializeField] private Image curIcon;
        [SerializeField] private Image curRarity;      // badge sprite set in prefab (we only tint)
        [SerializeField] private TMP_Text curName;
        [SerializeField] private TMP_Text curLevelText; // NEW: level separate

        [Header("CURRENT stats (numbers only)")]
        [SerializeField] private TMP_Text curHpText;   // digits only
        [SerializeField] private TMP_Text curAtkText;  // digits only

        [Header("QoL")]
        [SerializeField] private Toggle autoSellToggle;

        [Header("Buttons")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button sellButton;

        [Header("Power colors")]
        [SerializeField] private Color powerPlusColor = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color powerMinusColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color powerNeutralColor = Color.white;

        private ItemDef _newItem;
        private int _newItemLevel = 1;

        public Action OnDecisionMade;

        private void Awake()
        {
            if (equipButton) equipButton.onClick.AddListener(OnEquip);
            if (sellButton) sellButton.onClick.AddListener(OnSell);
            if (autoSellToggle) autoSellToggle.onValueChanged.AddListener(OnAutoSellChanged);

            ResetView();
        }

        private void OnAutoSellChanged(bool enabled)
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null) return;

            gi.SetAutoSellEnabled(enabled, immediateSave: true);
        }

        private void ResetView()
        {
            if (currentBlock) currentBlock.SetActive(false);

            if (newName) newName.text = "";
            if (newLevelText) newLevelText.text = "";
            if (newPowerText) { newPowerText.text = ""; newPowerText.color = powerNeutralColor; }
            if (newHpText) newHpText.text = "";
            if (newAtkText) newAtkText.text = "";

            if (curName) curName.text = "";
            if (curLevelText) curLevelText.text = "";
            if (curHpText) curHpText.text = "";
            if (curAtkText) curAtkText.text = "";

            // Optional: hide rarity badges by default
            if (newRarity) { newRarity.enabled = false; newRarity.color = Color.white; }
            if (curRarity) { curRarity.enabled = false; curRarity.color = Color.white; }
        }

        /// <summary>
        /// Backward compatible entry point:
        /// берЄм level из GameInstance.PendingChestItemLevel, иначе 1.
        /// </summary>
        public void Show(ItemDef newItem)
        {
            var gi = GameCore.GameInstance.I;
            int lvl = (gi != null) ? gi.PendingChestItemLevel : 1;
            Show(newItem, lvl);
        }

        /// <summary>
        /// ќсновной Show с €вным itemLevel.
        /// </summary>
        public void Show(ItemDef newItem, int newItemLevel)
        {
            _newItem = newItem;
            _newItemLevel = Mathf.Max(1, newItemLevel);

            if (_newItem == null) return;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ResetView();

            var gi = GameCore.GameInstance.I;

            // sync autosell toggle from save
            if (autoSellToggle && gi != null && gi.State != null)
                autoSellToggle.SetIsOnWithoutNotify(gi.State.AutoSellEnabled);

            // ===================== NEW =====================
            if (newIcon)
            {
                newIcon.enabled = true;
                newIcon.sprite = _newItem.Icon;
                newIcon.color = Color.white;
            }

            // NEW RARITY: COLOR ONLY (sprite set in prefab)
            ApplyRarityColor(newRarity, _newItem);

            if (newName)
                newName.text = $"{_newItem.DisplayName} ({_newItem.Rarity})";

            if (newLevelText)
                newLevelText.text = $"LVL {_newItemLevel}";

            ApplyHpAtkOnly(_newItem, _newItemLevel, newHpText, newAtkText);

            // ===================== CURRENT =====================
            var cur = (gi != null) ? gi.GetEquippedDef(_newItem.Slot) : null;
            bool hasCurrent = (cur != null);
            int curLevel = hasCurrent && gi != null ? Mathf.Max(1, gi.GetEquippedLevel(_newItem.Slot)) : 1;

            if (currentBlock) currentBlock.SetActive(hasCurrent);

            if (hasCurrent)
            {
                if (curIcon)
                {
                    curIcon.enabled = true;
                    curIcon.sprite = cur.Icon;
                    curIcon.color = Color.white;
                }

                // CURRENT RARITY: COLOR ONLY
                ApplyRarityColor(curRarity, cur);

                if (curName)
                    curName.text = $"{cur.DisplayName} ({cur.Rarity})";

                if (curLevelText)
                    curLevelText.text = $"LVL {curLevel}";

                ApplyHpAtkOnly(cur, curLevel, curHpText, curAtkText);
            }

            // ===================== POWER TEXT (ONLY NEW BLOCK) =====================
            if (newPowerText)
            {
                int newPwr = _newItem.GetPower(_newItemLevel);

                if (!hasCurrent)
                {
                    newPowerText.text = $"PWR: {newPwr}";
                    newPowerText.color = powerNeutralColor;
                }
                else
                {
                    int curPwr = cur.GetPower(curLevel);
                    int delta = newPwr - curPwr;

                    if (delta > 0)
                    {
                        newPowerText.text = $"+{delta} PWR";
                        newPowerText.color = powerPlusColor;
                    }
                    else if (delta < 0)
                    {
                        newPowerText.text = $"{delta} PWR";
                        newPowerText.color = powerMinusColor;
                    }
                    else
                    {
                        newPowerText.text = "±0 PWR";
                        newPowerText.color = powerNeutralColor;
                    }
                }
            }

            // ===================== BUTTONS =====================
            if (equipButton)
            {
                var t = equipButton.GetComponentInChildren<TMP_Text>(true);
                if (t) t.text = hasCurrent ? "REPLACE" : "EQUIP";
            }

            // IMPORTANT: если слот пуст Ч SELL Ќ≈Ћ№«я
            if (sellButton)
                sellButton.gameObject.SetActive(hasCurrent);
        }

        public void Hide()
        {
            _newItem = null;
            _newItemLevel = 1;
            gameObject.SetActive(false);
        }

        private void CloseAndNotify()
        {
            Hide();
            OnDecisionMade?.Invoke();
        }

        private bool IsAutoSellEnabled()
        {
            if (autoSellToggle != null) return autoSellToggle.isOn;

            var gi = GameCore.GameInstance.I;
            return gi != null && gi.State != null && gi.State.AutoSellEnabled;
        }

        private void OnEquip()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            var cur = gi.GetEquippedDef(_newItem.Slot);
            bool hasCurrent = (cur != null);

            // slot empty -> just equip, close
            if (!hasCurrent)
            {
                gi.EquipItemWithLevel(_newItem.Slot, _newItem.Id, _newItemLevel, immediateSave: true);
                CloseAndNotify();
                return;
            }

            int curLevelBeforeSwap = Mathf.Max(1, gi.GetEquippedLevel(_newItem.Slot));

            if (IsAutoSellEnabled())
            {
                // keep stronger by computed PWR (variant A), sell weaker, close
                int newPwr = _newItem.GetPower(_newItemLevel);
                int curPwr = cur.GetPower(curLevelBeforeSwap);

                if (newPwr >= curPwr)
                {
                    gi.EquipItemWithLevel(_newItem.Slot, _newItem.Id, _newItemLevel, immediateSave: false);
                    gi.SellItem(cur, immediateSave: false);
                }
                else
                {
                    gi.SellItem(_newItem, immediateSave: false);
                }

                gi.SaveAllNow();
                CloseAndNotify();
                return;
            }

            // AutoSell OFF: SWAP and re-open popup (show previous item)
            gi.EquipItemWithLevel(_newItem.Slot, _newItem.Id, _newItemLevel, immediateSave: false);
            gi.SaveAllNow();

            Show(cur, curLevelBeforeSwap);
        }

        private void OnSell()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            // sellButton hidden when slot empty, but safety:
            var cur = gi.GetEquippedDef(_newItem.Slot);
            if (cur == null) return;

            gi.SellItem(_newItem, immediateSave: true);
            CloseAndNotify();
        }

        // ===================== Stats display =====================

        private static void ApplyHpAtkOnly(ItemDef it, int itemLevel, TMP_Text hpText, TMP_Text atkText)
        {
            if (it == null) return;

            ItemStats s = it.GetStats(itemLevel);

            if (hpText) hpText.text = s.Hp.ToString();
            if (atkText) atkText.text = s.Atk.ToString();
        }

        // ===================== UI helpers =====================

        private static void ApplyRarityColor(Image rarityImg, ItemDef def)
        {
            if (!rarityImg || def == null) return;

            // Sprite должен быть задан в префабе!
            Color c = def.RarityColor;

            bool show = c.a > 0.001f;
            rarityImg.enabled = show;

            rarityImg.color = show ? c : Color.white;
        }
    }
}
