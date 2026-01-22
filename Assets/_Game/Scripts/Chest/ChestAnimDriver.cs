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

    [Tooltip("Fallback duration if clip length can't be read. Also used when WaitMode = FixedDuration.")]
    [SerializeField] private float openDuration = 0.6f;

    public enum WaitMode
    {
        FixedDuration,      // wait openDuration
        AnimatorStateLength // wait actual animator state length (more reliable)
    }

    [Header("Timing")]
    [SerializeField] private WaitMode waitMode = WaitMode.AnimatorStateLength;

    [Tooltip("If true uses realtime/unscaled time (ignores Time.timeScale).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("Extra padding added to the wait time (helps if transitions cut early).")]
    [SerializeField] private float extraWait = 0.05f;

    [Header("Drop Icon (overlay)")]
    [SerializeField] private Image dropIcon;                 // UI Image поверх сундука
    [SerializeField] private bool showIconAfterOpen = true;

    [Header("Rarity VFX (UI)")]
    [SerializeField] private Transform itemVFXRoot;          // куда спавнить VFX (RectTransform/Transform над иконкой)
    [SerializeField] private bool showVFXAfterOpen = true;

    private Coroutine _c;
    private GameObject _spawnedVFX;

    // Cached data for AnimEvent_ShowDrop
    private Sprite _pendingSprite;
    private GameObject _pendingVfxPrefab;

    private bool _dropShownThisOpen;

    public bool IsPlayingOpen { get; private set; }

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        HideDropIcon();
    }

    /// <summary>
    /// Запускаем анимацию открытия.
    /// - Визуал (иконка/VFX) показывается по Animation Event: AnimEvent_ShowDrop()
    /// - onOpened() вызывается после ПОЛНОГО окончания открытия (по duration/length)
    /// В idle НЕ возвращаемся — остаёмся на последнем кадре Open.
    /// </summary>
    public void PlayOpen(Sprite droppedSprite, GameObject rarityVFXPrefab, Action onOpened = null)
    {
        if (!anim) return;

        // запоминаем, что показать когда придёт event
        _pendingSprite = droppedSprite;
        _pendingVfxPrefab = rarityVFXPrefab;

        _dropShownThisOpen = false;

        if (_c != null) StopCoroutine(_c);
        _c = StartCoroutine(CoOpen(onOpened));
    }

    /// <summary>
    /// Animation Event (добавь в клип ChestOpen).
    /// Поставь его в последний/предпоследний кадр, где сундук уже открыт.
    /// </summary>
    public void AnimEvent_ShowDrop()
    {
        if (_dropShownThisOpen) return; // защита если event случайно стоит 2 раза
        _dropShownThisOpen = true;

        if (!showIconAfterOpen) return;

        ShowDropIcon(_pendingSprite, _pendingVfxPrefab);
    }

    /// <summary>
    /// Instantly forces the chest into the OPEN state (last frame) and shows icon/VFX.
    /// Use this when you restore a pending chest reward after restart.
    /// </summary>
    public void SetOpenedStatic(Sprite droppedSprite, GameObject rarityVFXPrefab)
    {
        if (_c != null)
        {
            StopCoroutine(_c);
            _c = null;
        }

        IsPlayingOpen = false;

        if (!anim) return;

        // remember to show
        _pendingSprite = droppedSprite;
        _pendingVfxPrefab = rarityVFXPrefab;

        // Go to open state and clamp to the last frame
        anim.Rebind();
        anim.Update(0f);
        anim.Play(openState, 0, 1f);
        anim.Update(0f);

        _dropShownThisOpen = true; // уже показали
        if (showIconAfterOpen)
            ShowDropIcon(droppedSprite, rarityVFXPrefab);
    }

    private IEnumerator CoOpen(Action onOpened)
    {
        IsPlayingOpen = true;
        HideDropIcon();

        // Жёсткий reset, чтобы open стартовал с нуля
        anim.Rebind();
        anim.Update(0f);

        // Стартуем open
        anim.Play(openState, 0, 0f);
        anim.Update(0f);

        // Даем Animator 1 кадр, чтобы state info обновился корректно
        yield return null;

        float wait = GetOpenWaitSeconds() + Mathf.Max(0f, extraWait);

        // ? ЖДЁМ ПОЛНОСТЬЮ ОТКРЫТИЕ (иконка появится по Animation Event)
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(wait);
        else
            yield return new WaitForSeconds(wait);

        // На случай если ты забыл поставить Animation Event:
        // покажем дроп в конце, чтобы не было "пусто"
        if (!_dropShownThisOpen && showIconAfterOpen)
        {
            ShowDropIcon(_pendingSprite, _pendingVfxPrefab);
            _dropShownThisOpen = true;
        }

        // Останемся на последнем кадре ChestOpen (на всякий случай)
        anim.Play(openState, 0, 1f);
        anim.Update(0f);

        IsPlayingOpen = false;
        _c = null;

        onOpened?.Invoke();
    }

    /// <summary>
    /// Returns seconds to wait for open animation.
    /// </summary>
    private float GetOpenWaitSeconds()
    {
        if (!anim) return Mathf.Max(0.01f, openDuration);

        if (waitMode == WaitMode.FixedDuration)
            return Mathf.Max(0.01f, openDuration);

        try
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);

            // если реально на этом стейте — берём length
            if (info.IsName(openState) && info.length > 0.01f)
                return info.length;

            // иногда IsName может не сработать из-за разных путей,
            // поэтому аккуратно fallback
        }
        catch { }

        return Mathf.Max(0.01f, openDuration);
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

        anim.Play(idleState, 0, 0f);
        anim.Update(0f);
    }

    private void ShowDropIcon(Sprite s, GameObject rarityVFXPrefab)
    {
        // Иконка
        if (dropIcon)
        {
            dropIcon.sprite = s;
            dropIcon.enabled = (s != null);
            dropIcon.color = Color.white;
        }

        // VFX
        if (!showVFXAfterOpen) return;

        if (_spawnedVFX)
        {
            Destroy(_spawnedVFX);
            _spawnedVFX = null;
        }

        if (rarityVFXPrefab != null && itemVFXRoot != null)
        {
            _spawnedVFX = Instantiate(rarityVFXPrefab, itemVFXRoot);
            _spawnedVFX.SetActive(true);
        }
    }

    private void HideDropIcon()
    {
        if (dropIcon)
        {
            dropIcon.enabled = false;
            dropIcon.sprite = null;
        }

        if (_spawnedVFX)
        {
            Destroy(_spawnedVFX);
            _spawnedVFX = null;
        }
    }

    private void OnDisable()
    {
        if (_c != null)
        {
            StopCoroutine(_c);
            _c = null;
        }

        IsPlayingOpen = false;

        if (_spawnedVFX)
        {
            Destroy(_spawnedVFX);
            _spawnedVFX = null;
        }
    }
}

