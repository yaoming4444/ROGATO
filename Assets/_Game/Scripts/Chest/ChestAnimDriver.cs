using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChestAnimDriver : MonoBehaviour
{
    [Header("Anim")]
    [SerializeField] private Animator anim;
    [SerializeField] private string idleState = "ChestIdle";
    [SerializeField] private string openState = "ChestOpen";
    [SerializeField] private float openDuration = 0.6f;

    [Header("Drop Icon (overlay)")]
    [SerializeField] private Image dropIcon;          // UI Image поверх сундука
    [SerializeField] private bool showIconAfterOpen = true;

    private Coroutine _c;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        HideDropIcon();
    }

    /// <summary>
    /// Запускаем анимацию открытия и (опционально) покажем иконку после openDuration.
    /// В конце мы НЕ возвращаемся в idle — остаёмся на последнем кадре Open.
    /// </summary>
    public void PlayOpen(Sprite droppedSprite)
    {
        if (!anim) return;

        if (_c != null) StopCoroutine(_c);
        _c = StartCoroutine(CoOpen(droppedSprite));
    }

    private IEnumerator CoOpen(Sprite droppedSprite)
    {
        HideDropIcon();

        // Жёсткий reset, чтобы open стартовал с нуля
        anim.Rebind();
        anim.Update(0f);

        anim.Play(openState, 0, 0f);

        // ждём конец открытия
        yield return new WaitForSecondsRealtime(openDuration);

        // останемся на последнем кадре ChestOpen (ничего не делаем)
        // и покажем иконку поверх сундука
        if (showIconAfterOpen)
            ShowDropIcon(droppedSprite);
    }

    public void ResetToIdle()
    {
        if (_c != null)
        {
            StopCoroutine(_c);
            _c = null;
        }

        HideDropIcon();

        if (!anim) return;

        // Сразу в Idle с 0 кадра
        anim.Play(idleState, 0, 0f);
    }

    private void ShowDropIcon(Sprite s)
    {
        if (!dropIcon) return;

        dropIcon.sprite = s;
        dropIcon.enabled = (s != null);
        dropIcon.color = Color.white;
    }

    private void HideDropIcon()
    {
        if (!dropIcon) return;

        dropIcon.enabled = false;
        dropIcon.sprite = null;
    }
}


