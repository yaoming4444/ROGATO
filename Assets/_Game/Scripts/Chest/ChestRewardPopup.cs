using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;

namespace GameCore.UI
{
    /// <summary>
    /// UI popup that shows:
    /// - the newly dropped item
    /// - the currently equipped item in the same slot (if any)
    /// - action buttons: Equip/Replace, Sell, Close
    ///
    /// This class does NOT roll items and does NOT spend chests.
    /// It only displays a given ItemDef and calls GameInstance methods on button clicks.
    ///
    /// How to use:
    /// 1) Create a popup GameObject under Canvas
    /// 2) Hook all serialized fields in Inspector
    /// 3) Keep popup GameObject inactive by default
    /// 4) Call popup.Show(itemDef) when a chest is opened and an item is rolled
    /// </summary>
    public class ChestRewardPopup : MonoBehaviour
    {
        [Header("New Item UI")]
        [SerializeField] private Image newIcon;
        [SerializeField] private Image newRarity;
        [SerializeField] private TMP_Text newName;
        [SerializeField] private TMP_Text newStats;

        [Header("Current Equipped UI")]
        // Whole right-side block (so we can hide it when slot is empty)
        [SerializeField] private GameObject currentBlock;
        [SerializeField] private Image curIcon;
        [SerializeField] private Image curRarity;
        [SerializeField] private TMP_Text curName;
        [SerializeField] private TMP_Text curStats;

        [Header("Buttons")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button closeButton;

        // Item currently shown by this popup
        private ItemDef _newItem;

        private void Awake()
        {
            // Wire up button callbacks once.
            if (equipButton) equipButton.onClick.AddListener(OnEquip);
            if (sellButton) sellButton.onClick.AddListener(OnSell);
            if (closeButton) closeButton.onClick.AddListener(Hide);

            // Popup should start hidden.
            Hide();
        }

        /// <summary>
        /// Shows the popup for the given item.
        /// Also checks what is currently equipped in the same slot and shows comparison UI.
        /// </summary>
        public void Show(ItemDef newItem)
        {
            _newItem = newItem;
            if (_newItem == null) return;

            gameObject.SetActive(true);

            // ----- NEW ITEM UI -----
            if (newIcon)
            {
                newIcon.enabled = true;
                newIcon.sprite = _newItem.Icon;
                newIcon.color = Color.white;
            }

            if (newRarity)
            {
                // Rarity badge is optional
                newRarity.enabled = _newItem.IconRarity != null;
                newRarity.sprite = _newItem.IconRarity;
                newRarity.color = Color.white;
            }

            if (newName)
                newName.text = $"{_newItem.DisplayName} ({_newItem.Rarity})";

            if (newStats)
                newStats.text = FormatStats(_newItem);

            // ----- CURRENT EQUIPPED ITEM UI -----
            var gi = GameCore.GameInstance.I;
            var cur = gi != null ? gi.GetEquippedDef(_newItem.Slot) : null;

            bool empty = (cur == null);
            if (currentBlock) currentBlock.SetActive(!empty);

            if (!empty)
            {
                if (curIcon)
                {
                    curIcon.enabled = true;
                    curIcon.sprite = cur.Icon;
                    curIcon.color = Color.white;
                }

                if (curRarity)
                {
                    curRarity.enabled = cur.IconRarity != null;
                    curRarity.sprite = cur.IconRarity;
                    curRarity.color = Color.white;
                }

                if (curName)
                    curName.text = $"{cur.DisplayName} ({cur.Rarity})";

                if (curStats)
                    curStats.text = FormatStats(cur);
            }

            // ----- BUTTON TEXT -----
            // If slot empty -> "EQUIP", else -> "REPLACE"
            if (equipButton)
            {
                var t = equipButton.GetComponentInChildren<TMP_Text>();
                if (t) t.text = empty ? "EQUIP" : "REPLACE";
            }

            if (sellButton)
            {
                var t = sellButton.GetComponentInChildren<TMP_Text>();
                if (t) t.text = $"SELL (+{_newItem.SellGems} gems)";
            }
        }

        /// <summary>
        /// Equip/Replace action:
        /// - Writes itemId into the slot in PlayerState
        /// - Triggers UI refresh events
        /// - Saves local+server immediately (immediateSave=true)
        /// </summary>
        private void OnEquip()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            gi.EquipItem(_newItem.Slot, _newItem.Id, immediateSave: true);
            Hide();
        }

        /// <summary>
        /// Sell action:
        /// - Adds gems to the player
        /// - Saves immediately
        /// </summary>
        private void OnSell()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            gi.SellItem(_newItem, immediateSave: true);
            Hide();
        }

        /// <summary>
        /// Hide popup and clear current item reference.
        /// </summary>
        public void Hide()
        {
            _newItem = null;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Simple formatting for base stats.
        /// In the future you can add:
        /// - stat deltas (green/red)
        /// - power score
        /// - extra stats
        /// </summary>
        private string FormatStats(ItemDef it)
        {
            if (it == null) return "";
            var s = it.Stats;
            return $"ATK: {s.Atk}\nDEF: {s.Def}\nHP: {s.Hp}";
        }
    }
}


