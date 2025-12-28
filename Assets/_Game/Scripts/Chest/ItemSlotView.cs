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

        [Header("UI (Icon must be CHILD image, not the frame on parent)")]
        [SerializeField] private Image icon;          // child icon image
        [SerializeField] private Image rarity;        // optional badge
        [SerializeField] private TMP_Text labelText;  // optional

        [Header("Empty visuals")]
        [SerializeField] private Sprite emptyIcon; // optional: plus icon
        [SerializeField, Range(0f, 1f)] private float emptyAlpha = 0.35f;

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

            var id = GameCore.GameInstance.I.GetEquippedId(slot);

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

            // Filled
            if (icon)
            {
                icon.enabled = true;
                icon.sprite = def.Icon;
                icon.color = Color.white;
            }

            if (rarity)
            {
                rarity.sprite = def.IconRarity;
                rarity.enabled = (def.IconRarity != null);
                if (rarity.enabled) rarity.color = Color.white;
            }

            if (labelText)
                labelText.text = def.DisplayName;
        }

        private void SetEmpty()
        {
            // IMPORTANT: We do NOT touch the parent's frame image here.
            // We only change the CHILD icon.

            if (icon)
            {
                icon.enabled = true; // keep slot "alive"
                icon.sprite = emptyIcon; // if null, it will just show nothing

                var c = icon.color;
                c.a = (emptyIcon != null) ? 1f : emptyAlpha; // if no placeholder - dim
                icon.color = c;

                // if you want totally hidden icon when empty:
                // icon.enabled = (emptyIcon != null);
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




