using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;

namespace GameCore.UI
{
    public class ItemSlotView : MonoBehaviour
    {
        [Header("Slot")]
        [SerializeField] private EquipSlot slot;

        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Image rarity;
        [SerializeField] private TMP_Text labelText;

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
            if (s == null)
            {
                SetEmpty();
                return;
            }

            var id = s.GetEquippedId(slot);
            if (string.IsNullOrWhiteSpace(id))
            {
                SetEmpty();
                return;
            }

            var def = ItemDatabase.I != null ? ItemDatabase.I.GetById(id) : null;
            if (def == null)
            {
                SetEmpty();
                return;
            }

            if (icon)
            {
                icon.enabled = true;
                icon.sprite = def.Icon;
                icon.color = Color.white;
            }

            if (rarity)
            {
                rarity.enabled = def.IconRarity != null;
                rarity.sprite = def.IconRarity;
                rarity.color = Color.white;
            }

            if (labelText)
                labelText.text = $"{def.Rarity}";
        }

        private void SetEmpty()
        {
            if (icon)
            {
                icon.sprite = null;
                icon.enabled = false; // или оставь true и ставь прозрачность
            }

            if (rarity)
            {
                rarity.sprite = null;
                rarity.enabled = false;
            }

            if (labelText)
                labelText.text = "";
        }
    }
}


