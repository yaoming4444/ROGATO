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
        [SerializeField] private Image newRarity;
        [SerializeField] private TMP_Text newName;
        [SerializeField] private TMP_Text newPowerText; // ONE power text here

        [Header("NEW stats (numbers only)")]
        [SerializeField] private TMP_Text newHpText;   // digits only
        [SerializeField] private TMP_Text newAtkText;  // digits only

        [Header("CURRENT item block (only if comparison)")]
        [SerializeField] private GameObject currentBlock;
        [SerializeField] private Image curIcon;
        [SerializeField] private Image curRarity;
        [SerializeField] private TMP_Text curName;

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
            if (newPowerText) newPowerText.text = "";
            if (newHpText) newHpText.text = "";
            if (newAtkText) newAtkText.text = "";

            if (curName) curName.text = "";
            if (curHpText) curHpText.text = "";
            if (curAtkText) curAtkText.text = "";
        }

        public void Show(ItemDef newItem)
        {
            _newItem = newItem;
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

            if (newRarity)
            {
                newRarity.enabled = (_newItem.IconRarity != null);
                newRarity.sprite = _newItem.IconRarity;
                newRarity.color = Color.white;
            }

            if (newName)
                newName.text = $"{_newItem.DisplayName} ({_newItem.Rarity})";

            ApplyHpAtkOnly(_newItem, newHpText, newAtkText);

            // ===================== CURRENT =====================
            var cur = (gi != null) ? gi.GetEquippedDef(_newItem.Slot) : null;
            bool hasCurrent = (cur != null);

            if (currentBlock) currentBlock.SetActive(hasCurrent);

            if (hasCurrent)
            {
                if (curIcon)
                {
                    curIcon.enabled = true;
                    curIcon.sprite = cur.Icon;
                    curIcon.color = Color.white;
                }

                if (curRarity)
                {
                    curRarity.enabled = (cur.IconRarity != null);
                    curRarity.sprite = cur.IconRarity;
                    curRarity.color = Color.white;
                }

                if (curName)
                    curName.text = $"{cur.DisplayName} ({cur.Rarity})";

                ApplyHpAtkOnly(cur, curHpText, curAtkText);
            }

            // ===================== POWER TEXT (ONLY NEW BLOCK) =====================
            if (newPowerText)
            {
                if (!hasCurrent)
                {
                    newPowerText.text = $"PWR: {_newItem.Power}";
                    newPowerText.color = powerNeutralColor;
                }
                else
                {
                    int delta = _newItem.Power - cur.Power;
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
            {
                sellButton.gameObject.SetActive(hasCurrent);
                /*if (hasCurrent)
                {
                    var t = sellButton.GetComponentInChildren<TMP_Text>(true);
                    if (t) t.text = $"SELL (+{_newItem.SellGems} gems)";
                }*/
            }
        }

        public void Hide()
        {
            _newItem = null;
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
                gi.EquipItem(_newItem.Slot, _newItem.Id, immediateSave: true);
                CloseAndNotify();
                return;
            }

            if (IsAutoSellEnabled())
            {
                // keep stronger by Power, sell weaker, close
                if (_newItem.Power >= cur.Power)
                {
                    gi.EquipItem(_newItem.Slot, _newItem.Id, immediateSave: false);
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

            // AutoSell OFF: SWAP and re-open popup
            gi.EquipItem(_newItem.Slot, _newItem.Id, immediateSave: false);
            gi.SaveAllNow();

            Show(cur);
        }

        private void OnSell()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            // sellButton is hidden when slot empty, but still safety:
            var cur = gi.GetEquippedDef(_newItem.Slot);
            if (cur == null) return;

            gi.SellItem(_newItem, immediateSave: true);
            CloseAndNotify();
        }

        private static void ApplyHpAtkOnly(ItemDef it, TMP_Text hpText, TMP_Text atkText)
        {
            if (it == null) return;
            var s = it.Stats;
            if (hpText) hpText.text = s.Hp.ToString();
            if (atkText) atkText.text = s.Atk.ToString();
        }
    }
}







