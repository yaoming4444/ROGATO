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
        [SerializeField] private TMP_Text newStats;

        [Header("CURRENT item block (only if comparison)")]
        [SerializeField] private GameObject currentBlock;
        [SerializeField] private Image curIcon;
        [SerializeField] private Image curRarity;
        [SerializeField] private TMP_Text curName;
        [SerializeField] private TMP_Text curStats;

        [Header("Buttons")]
        [SerializeField] private Button equipButton; // always visible
        [SerializeField] private Button sellButton;  // only when comparison

        private ItemDef _newItem;

        private void Awake()
        {
            // ВАЖНО: НЕ Hide() здесь!
            // Иначе при первом SetActive(true) Awake вызовется и обнулит _newItem.

            if (equipButton) equipButton.onClick.AddListener(OnEquip);
            if (sellButton) sellButton.onClick.AddListener(OnSell);

            // Просто сбросим вид, но не выключаем объект.
            ResetView();
        }

        private void ResetView()
        {
            if (currentBlock) currentBlock.SetActive(false);
            if (sellButton) sellButton.gameObject.SetActive(false);

            // можно почистить тексты, чтобы не мигали старые
            if (newName) newName.text = "";
            if (newStats) newStats.text = "";
            if (curName) curName.text = "";
            if (curStats) curStats.text = "";
        }

        public void Show(ItemDef newItem)
        {
            _newItem = newItem;
            if (_newItem == null) return;

            // если объект был выключен - включаем
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ResetView();

            // ===================== NEW (ALWAYS) =====================
            if (newIcon)
            {
                newIcon.enabled = true;
                newIcon.sprite = _newItem.Icon; // может быть null - это нормально
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

            if (newStats)
                newStats.text = FormatStats(_newItem);

            // ===================== CURRENT (ONLY IF EXISTS) =====================
            var gi = GameCore.GameInstance.I;
            var cur = gi != null ? gi.GetEquippedDef(_newItem.Slot) : null;

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

                if (curStats)
                    curStats.text = FormatStats(cur);
            }

            // ===================== BUTTONS =====================
            if (equipButton)
            {
                var t = equipButton.GetComponentInChildren<TMP_Text>(true);
                if (t) t.text = hasCurrent ? "REPLACE" : "EQUIP";
            }

            if (sellButton)
            {
                sellButton.gameObject.SetActive(hasCurrent);

                var t = sellButton.GetComponentInChildren<TMP_Text>(true);
                if (t) t.text = $"SELL (+{_newItem.SellGems} gems)";
            }
        }

        public void Hide()
        {
            _newItem = null;
            gameObject.SetActive(false);
        }

        private void OnEquip()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            gi.EquipItem(_newItem.Slot, _newItem.Id, immediateSave: true);
            Hide();
        }

        private void OnSell()
        {
            var gi = GameCore.GameInstance.I;
            if (gi == null || _newItem == null) return;

            gi.SellItem(_newItem, immediateSave: true);
            Hide();
        }

        private string FormatStats(ItemDef it)
        {
            var s = it.Stats;
            return $"ATK: {s.Atk}\nDEF: {s.Def}\nHP: {s.Hp}";
        }
    }
}





