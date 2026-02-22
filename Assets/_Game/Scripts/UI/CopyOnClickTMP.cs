using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IDosGames
{
    public class CopyOnClickTMP : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text textToCopy;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (textToCopy == null) return;

            var s = textToCopy.text;
            if (string.IsNullOrEmpty(s)) return;

            GUIUtility.systemCopyBuffer = s;   // копирование в буфер обмена
            Debug.Log($"Copied: {s}");
        }
    }
}
