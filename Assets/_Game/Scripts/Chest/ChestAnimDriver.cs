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
    [SerializeField] private Image dropIcon;                 // UI Image поверх сундука
    [SerializeField] private bool showIconAfterOpen = true;

    [Header("Rarity VFX (UI)")]
    [SerializeField] private Transform itemVFXRoot;          // куда спавнить VFX (RectTransform/Transform над иконкой)
    [SerializeField] private bool showVFXAfterOpen = true;

    private Coroutine _c;
    private GameObject _spawnedVFX;

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
    /// - спавним VFX префаб (если включено)
    /// - вызываем onOpened()
    /// В idle НЕ возвращаемся — остаёмся на последнем кадре Open.
    /// </summary>
    /// <param name="droppedSprite">Спрайт дропа для иконки.</param>
    /// <param name="rarityVFXPrefab">Префаб VFX (UI), который будет заинстансен в itemVFXRoot. Может быть null.</param>
    /// <param name="onOpened">Коллбек после завершения открытия.</param>
    public void PlayOpen(Sprite droppedSprite, GameObject rarityVFXPrefab, Action onOpened = null)
    {
        if (!anim) return;

        if (_c != null) StopCoroutine(_c);
        _c = StartCoroutine(CoOpen(droppedSprite, rarityVFXPrefab, onOpened));
    }

    private IEnumerator CoOpen(Sprite droppedSprite, GameObject rarityVFXPrefab, Action onOpened)
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
            ShowDropIcon(droppedSprite, rarityVFXPrefab);

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

        // Удаляем прошлый VFX, если был
        if (_spawnedVFX)
        {
            Destroy(_spawnedVFX);
            _spawnedVFX = null;
        }

        // Спавним новый
        if (rarityVFXPrefab != null && itemVFXRoot != null)
        {
            _spawnedVFX = Instantiate(rarityVFXPrefab, itemVFXRoot);
            _spawnedVFX.SetActive(true);
        }
        else
        {
            // Если префаб/рут не задан — просто ничего не делаем (без ошибок).
            // Можно добавить Debug.LogWarning если хочешь.
        }
    }

    private void HideDropIcon()
    {
        // Иконка
        if (dropIcon)
        {
            dropIcon.enabled = false;
            dropIcon.sprite = null;
        }

        // VFX
        if (_spawnedVFX)
        {
            Destroy(_spawnedVFX);
            _spawnedVFX = null;
        }
    }

    private void OnDisable()
    {
        // На всякий случай, чтобы не оставались VFX при выключении объекта/канваса
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




