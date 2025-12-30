using System;
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

    public bool IsPlayingOpen { get; private set; }

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        HideDropIcon();
    }

    /// <summary>
    /// Запускаем анимацию открытия.
    /// После openDuration:
    /// - показываем иконку (если включено)
    /// - вызываем onOpened()
    /// В idle НЕ возвращаемся — остаёмся на последнем кадре Open.
    /// </summary>
    public void PlayOpen(Sprite droppedSprite, Action onOpened = null)
    {
        if (!anim) return;

        if (_c != null) StopCoroutine(_c);
        _c = StartCoroutine(CoOpen(droppedSprite, onOpened));
    }

    private IEnumerator CoOpen(Sprite droppedSprite, Action onOpened)
    {
        IsPlayingOpen = true;

        HideDropIcon();

        // Жёсткий reset, чтобы open стартовал с нуля
        anim.Rebind();
        anim.Update(0f);

        anim.Play(openState, 0, 0f);

        // ждём конец открытия
        yield return new WaitForSecondsRealtime(openDuration);

        // останемся на последнем кадре ChestOpen
        if (showIconAfterOpen)
            ShowDropIcon(droppedSprite);

        IsPlayingOpen = false;
        _c = null;

        // сигнал наружу: "анимация закончилась, можно показывать попап"
        onOpened?.Invoke();
    }

    public void ResetToIdle()
    {
        if (_c != null)
        {
            StopCoroutine(_c);
            _c = null;
        }

        IsPlayingOpen = false;
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



