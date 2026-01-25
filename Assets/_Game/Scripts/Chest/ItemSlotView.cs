using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameCore.Items;
using Unity.VisualScripting;

namespace GameCore.UI
{
    public class ItemSlotView : MonoBehaviour
    {
        [Header("Slot")]
        [SerializeField] private EquipSlot slot;

        [Header("UI (Icon must be CHILD image, not the frame on parent)")]
        [SerializeField] private Image icon;          // child icon image
        [SerializeField] private Image rarity;        // badge image (sprite set in prefab)
        [SerializeField] private TMP_Text levelText;  // optional
        [SerializeField] private GameObject levelTextParent;  // optional (ONLY the level container)

        [Header("Empty visuals")]
        [SerializeField] private Sprite emptyIcon; // optional: plus icon

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
                // НЕ трогаем icon.color
            }

            if (rarity)
            {
                // ? НЕ меняем rarity.sprite (он должен быть задан в префабе)
                var c = def.RarityColor;

                // если хочешь скрывать бейдж для "Common" — ставь alpha=0 в дефолте
                rarity.enabled = (c.a > 0.001f);
                if (rarity.enabled)
                    rarity.color = c;
            }

            if (levelTextParent)
                levelTextParent.SetActive(true);

            if (levelText)
                levelText.text = def.Level.ToString();
        }

        private void SetEmpty()
        {
            // Работаем только с child icon и бейджем, фон/рамку не трогаем.

            if (icon)
            {
                if (emptyIcon != null)
                {
                    icon.enabled = true;
                    icon.sprite = emptyIcon;
                }
                else
                {
                    icon.enabled = false;
                    icon.sprite = null;
                }
            }

            if (rarity)
                rarity.color = Color.white;

            if (levelText)
                levelText.text = "";

            if (levelTextParent)
                levelTextParent.SetActive(false);
        }
    }
}
