using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;

namespace GameCore.UI
{
    /// <summary>
    /// UI view for one equipment slot.
    ///
    /// Responsibilities:
    /// - Knows which EquipSlot it represents (e.g. Head/Body/Weapon)
    /// - Subscribes to GameInstance.StateChanged
    /// - Updates icon/rarity/label based on currently equipped itemId
    ///
    /// It does not equip items by itself; it's just a view.
    /// (You can later add OnClick to open inventory or open chest UI.)
    /// </summary>
    public class ItemSlotView : MonoBehaviour
    {
        [Header("Slot")]
        // Which slot this view represents
        [SerializeField] private EquipSlot slot;

        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Image rarity;
        [SerializeField] private TMP_Text labelText;

        private void OnEnable()
        {
            // Subscribe to state changes (currencies, equipment, etc.)
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged += OnStateChanged;

            // Force an immediate refresh when enabled
            OnStateChanged(GameCore.GameInstance.I?.State);
        }

        private void OnDisable()
        {
            // Unsubscribe (avoid memory leaks / double subscriptions)
            if (GameCore.GameInstance.I != null)
                GameCore.GameInstance.I.StateChanged -= OnStateChanged;
        }

        /// <summary>
        /// Refreshes the UI when the PlayerState changes.
        /// </summary>
        private void OnStateChanged(GameCore.PlayerState s)
        {
            if (s == null)
            {
                SetEmpty();
                return;
            }

            // Resolve equipped item
            var id = GameCore.GameInstance.I.GetEquippedId(slot);

            if (string.IsNullOrWhiteSpace(id))
            {
                SetEmpty();
                return;
            }

            var def = ItemDatabase.I.GetById(id);
            if (def == null)
            {
                // If itemId is unknown (DB changed), treat as empty.
                SetEmpty();
                return;
            }

            // ----- Filled UI -----
            if (icon)
            {
                icon.sprite = def.Icon;
                icon.enabled = (def.Icon != null);
            }

            if (rarity)
            {
                rarity.sprite = def.IconRarity;
                rarity.enabled = (def.IconRarity != null);
            }

            if (labelText)
                labelText.text = def.DisplayName;
        }

        /// <summary>
        /// Empty UI state.
        ///
        /// IMPORTANT UX NOTE:
        /// Right now you disable the icon image completely.
        /// If you want "slots visible even when empty", you should:
        /// - keep a background frame always visible, OR
        /// - set an 'empty sprite' (plus icon), OR
        /// - reduce alpha instead of disabling.
        /// </summary>
        private void SetEmpty()
        {
            if (icon)
            {
                icon.sprite = null;

                // Current behavior: icon completely disappears when empty.
                // Alternative: keep it enabled and set a placeholder sprite or alpha.
                icon.enabled = false; // or keep true and set transparency
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



