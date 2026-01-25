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

    [Header("Timing (fallback)")]
    [SerializeField] private WaitMode waitMode = WaitMode.AnimatorStateLength;

    [Tooltip("Extra padding added to the wait time (helps if transitions cut early).")]
    [SerializeField] private float extraWait = 0.05f;

    [Header("Popup delay")]
    [Tooltip("Delay AFTER animation end before calling onOpened (popup show).")]
    [SerializeField] private float popupDelay = 0.35f;

    [Tooltip("Use realtime/unscaled time for popup delay too.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Drop Icon (overlay)")]
    [SerializeField] private Image dropIcon;                 // UI Image поверх сундука
    [SerializeField] private bool showIconAfterOpen = true;

    [Header("Rarity VFX (UI)")]
    [SerializeField] private Transform itemVFXRoot;          // куда спавнить VFX (RectTransform/Transform над иконкой)
    [SerializeField] private bool showVFXAfterOpen = true;

    private Coroutine _c;
    private Coroutine _popupDelayCoroutine;
    private GameObject _spawnedVFX;

    // Cached data for events
    private Sprite _pendingSprite;
    private GameObject _pendingVfxPrefab;
    private Action _pendingOnOpened;

    private bool _dropShownThisOpen;
    private bool _openFinishedThisOpen;

    public bool IsPlayingOpen { get; private set; }

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        HideDropIcon();
    }

    public void PlayOpen(Sprite droppedSprite, GameObject rarityVFXPrefab, Action onOpened = null)
    {
        if (!anim) return;

        _pendingSprite = droppedSprite;
        _pendingVfxPrefab = rarityVFXPrefab;
        _pendingOnOpened = onOpened;

        _dropShownThisOpen = false;
        _openFinishedThisOpen = false;

        if (_popupDelayCoroutine != null)
        {
            StopCoroutine(_popupDelayCoroutine);
            _popupDelayCoroutine = null;
        }

        if (_c != null) StopCoroutine(_c);
        _c = StartCoroutine(CoOpen());
    }

    /// <summary>
    /// Animation Event #1 (place near the end of ChestOpen):
    /// Show drop icon/VFX exactly at chosen frame.
    /// </summary>
    public void AnimEvent_ShowDrop()
    {
        if (_dropShownThisOpen) return;
        _dropShownThisOpen = true;

        if (!showIconAfterOpen) return;
        ShowDropIcon(_pendingSprite, _pendingVfxPrefab);
    }

    /// <summary>
    /// Animation Event #2 (place at the very end of ChestOpen):
    /// Mark open finished and start delayed callback (popup).
    /// </summary>
    public void AnimEvent_OpenFinished()
    {
        if (_openFinishedThisOpen) return;
        _openFinishedThisOpen = true;

        // If ShowDrop event missing, show it here so it never ends empty.
        if (!_dropShownThisOpen && showIconAfterOpen)
        {
            ShowDropIcon(_pendingSprite, _pendingVfxPrefab);
            _dropShownThisOpen = true;
        }

        IsPlayingOpen = false;

        // Delayed callback (popup)
        if (_popupDelayCoroutine != null)
            StopCoroutine(_popupDelayCoroutine);

        _popupDelayCoroutine = StartCoroutine(CoDelayAndCallOpened());
    }

    private IEnumerator CoDelayAndCallOpened()
    {
        float d = Mathf.Max(0f, popupDelay);
        if (d > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(d);
            else
                yield return new WaitForSeconds(d);
        }

        var cb = _pendingOnOpened;
        _pendingOnOpened = null;

        _popupDelayCoroutine = null;
        cb?.Invoke();
    }

    public void SetOpenedStatic(Sprite droppedSprite, GameObject rarityVFXPrefab)
    {
        if (_c != null)
        {
            StopCoroutine(_c);
            _c = null;
        }

        if (_popupDelayCoroutine != null)
        {
            StopCoroutine(_popupDelayCoroutine);
            _popupDelayCoroutine = null;
        }

        IsPlayingOpen = false;

        if (!anim) return;

        _pendingSprite = droppedSprite;
        _pendingVfxPrefab = rarityVFXPrefab;

        anim.Rebind();
        anim.Update(0f);
        anim.Play(openState, 0, 1f);
        anim.Update(0f);

        _dropShownThisOpen = true;
        _openFinishedThisOpen = true;

        if (showIconAfterOpen)
            ShowDropIcon(droppedSprite, rarityVFXPrefab);
    }

    private IEnumerator CoOpen()
    {
        IsPlayingOpen = true;
        HideDropIcon();

        anim.Rebind();
        anim.Update(0f);

        anim.Play(openState, 0, 0f);
        anim.Update(0f);

        yield return null;

        // Wait for OpenFinished event; fallback by time if event missing.
        float wait = GetOpenWaitSeconds() + Mathf.Max(0f, extraWait);

        float startRealtime = Time.realtimeSinceStartup;
        while (!_openFinishedThisOpen && (Time.realtimeSinceStartup - startRealtime) < wait)
            yield return null;

        if (!_openFinishedThisOpen)
        {
            // fallback if events were not added
            if (!_dropShownThisOpen && showIconAfterOpen)
                AnimEvent_ShowDrop();

            AnimEvent_OpenFinished();
        }

        // Clamp to last frame for visuals
        if (anim)
        {
            anim.Play(openState, 0, 1f);
            anim.Update(0f);
        }

        _c = null;
    }

    private float GetOpenWaitSeconds()
    {
        if (!anim) return Mathf.Max(0.01f, openDuration);

        if (waitMode == WaitMode.FixedDuration)
            return Mathf.Max(0.01f, openDuration);

        try
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(openState) && info.length > 0.01f)
                return info.length;
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

        if (_popupDelayCoroutine != null)
        {
            StopCoroutine(_popupDelayCoroutine);
            _popupDelayCoroutine = null;
        }

        IsPlayingOpen = false;

        _pendingOnOpened = null;
        _dropShownThisOpen = false;
        _openFinishedThisOpen = false;

        HideDropIcon();

        if (!anim) return;

        anim.Play(idleState, 0, 0f);
        anim.Update(0f);
    }

    private void ShowDropIcon(Sprite s, GameObject rarityVFXPrefab)
    {
        if (dropIcon)
        {
            dropIcon.sprite = s;
            dropIcon.enabled = (s != null);
            dropIcon.color = Color.white;
        }

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

        if (_popupDelayCoroutine != null)
        {
            StopCoroutine(_popupDelayCoroutine);
            _popupDelayCoroutine = null;
        }

        IsPlayingOpen = false;

        if (_spawnedVFX)
        {
            Destroy(_spawnedVFX);
            _spawnedVFX = null;
        }
    }
}
