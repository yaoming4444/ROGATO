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
        [SerializeField] private Image rarity;        // badge image (sprite set in prefab)
        [SerializeField] private TMP_Text levelText;  // optional
        [SerializeField] private GameObject levelTextParent;  // optional (ONLY the level container)

        [Header("Empty visuals")]
        [SerializeField] private Sprite emptyIcon; // optional: plus icon

        [Header("Click -> Popup")]
        [Tooltip("Button on this slot (you can put it on the root object). If empty, will try GetComponent<Button>().")]
        [SerializeField] private Button clickButton;

        [Tooltip("Popup that shows item details (name/atk/hp).")]
        [SerializeField] private ItemInfoPopup infoPopup;

        private ItemDef _cachedDef;

        private void Awake()
        {
            if (clickButton == null)
                clickButton = GetComponent<Button>();

            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(OnClick);
                clickButton.onClick.AddListener(OnClick);
            }
        }

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

        private void OnClick()
        {
            // Если попап не назначен — просто ничего
            if (infoPopup == null) return;

            // Показываем только если предмет реально есть
            if (_cachedDef == null) return;

            infoPopup.Show(_cachedDef);
        }

        private void OnStateChanged(GameCore.PlayerState s)
        {
            _cachedDef = null;

            if (s == null)
            {
                SetEmpty();
                return;
            }

            var gi = GameCore.GameInstance.I;
            if (gi == null)
            {
                SetEmpty();
                return;
            }

            var id = gi.GetEquippedId(slot);

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

            _cachedDef = def;

            // Filled
            if (icon)
            {
                icon.enabled = true;
                icon.sprite = def.Icon;
                // НЕ трогаем icon.color
            }

            if (rarity)
            {
                // НЕ меняем rarity.sprite (он задан в префабе)
                var c = def.RarityColor;
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
            // слот пустой => попап по клику не показываем
            _cachedDef = null;

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
