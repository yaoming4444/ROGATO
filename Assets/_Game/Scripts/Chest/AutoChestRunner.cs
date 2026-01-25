using System;
using System.Collections;
using UnityEngine;
using GameCore.Items;
using GameCore.UI;

public class AutoChestRunner : MonoBehaviour
{
    [SerializeField] private ChestController chest;

    [Header("Visual")]
    [SerializeField] private float postAutoDecisionVisualHold = 0.15f; // 0 = сразу сброс

    private Coroutine _c;
    private AutoOpenSettingsPopup.Settings _s;

    // pause/resume while any other UI is open
    private int _pauseCounter;

    public bool IsRunning => _c != null;
    public bool IsPaused => _pauseCounter > 0;

    // Optional UI hook (button glow/vfx)
    public event Action<bool> RunningChanged;

    public void PauseAuto() => _pauseCounter++;
    public void ResumeAuto() => _pauseCounter = Mathf.Max(0, _pauseCounter - 1);
    public void ClearPause() => _pauseCounter = 0;

    public void StartAuto(AutoOpenSettingsPopup.Settings settings)
    {
        _s = settings;

        // ? ВАЖНО: старт авто НЕ должен сбрасывать pending.
        StopAuto(); // stop loop only (pending stays)

        _c = StartCoroutine(Loop());
        RunningChanged?.Invoke(true);
    }

    /// <summary>
    /// ? Stop ONLY the loop.
    /// Keeps pending reward + chest visuals (opened chest + drop icon) intact.
    /// </summary>
    public void StopAuto()
    {
        bool wasRunning = (_c != null);

        if (_c != null) StopCoroutine(_c);
        _c = null;

        // ? НЕ делаем chest.FinalizePending() здесь!

        if (wasRunning)
            RunningChanged?.Invoke(false);
    }

    /// <summary>
    /// ? FULL OFF:
    /// - stop loop
    /// - clear pause counter
    /// - keep pending intact
    /// </summary>
    public void DisableAuto()
    {
        StopAuto();
        ClearPause();
    }

    /// <summary>
    /// ? For compatibility with your previous calls.
    /// Stops loop and keeps pending.
    /// </summary>
    public void DisableAutoKeepPending()
    {
        DisableAuto();
    }

    /// <summary>
    /// ? Use ONLY when you intentionally want to clear pending reward + reset chest to idle.
    /// Example: user manually clicks "Cancel reward" (if you ever add), or emergency cleanup.
    /// </summary>
    public void ClearPendingNow()
    {
        if (chest != null)
            chest.FinalizePending();
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            if (chest == null)
            {
                DisableAuto();
                yield break;
            }

            if (IsPaused)
            {
                yield return null;
                continue;
            }

            // ждём, если занято (попап/анимация/ожидание решения)
            if (chest.IsBusy)
            {
                yield return null;
                continue;
            }

            ItemDef opened = null;

            bool started = chest.TryOpenChestAuto(it => opened = it);
            if (!started)
            {
                // сундуков нет/не удалось открыть — просто выключаем луп, pending не трогаем
                DisableAuto();
                yield break;
            }

            // ждём пока анимация закончится (callback приходит после PlayOpen)
            while (opened == null)
            {
                if (IsPaused) { yield return null; continue; }
                yield return null;
            }

            var gi = GameCore.GameInstance.I;
            if (gi == null)
            {
                DisableAuto();
                yield break;
            }

            // Текущая вещь в слоте
            var cur = gi.GetEquippedDef(opened.Slot);

            bool slotEmpty = (cur == null);
            bool improvesPower = (!slotEmpty && opened.Power > cur.Power);

            int cls = GetRarityRank(opened);

            // =========================
            // правило: если слот пуст ИЛИ повышает power — показываем сравнение
            // =========================
            if (_s.IncreasePower && (slotEmpty || improvesPower))
            {
                chest.ShowPendingPopup();
                while (chest.IsBusy || IsPaused) yield return null;

                yield return WaitWithPause(_s.OpenInterval);
                continue;
            }

            // =========================
            // фильтр по классу: ниже MinClass => auto-sell
            // (здесь мы ОСОЗНАННО закрываем pending, потому что решение принято автоматически)
            // =========================
            if (cls < _s.MinClass)
            {
                gi.SellItem(opened, immediateSave: false);
                gi.SaveAllNow();

                if (postAutoDecisionVisualHold > 0f)
                    yield return WaitWithPause(postAutoDecisionVisualHold);

                chest.FinalizePending(); // ? auto decision -> clear pending + reset chest
                yield return WaitWithPause(_s.OpenInterval);
                continue;
            }

            // =========================
            // IncreasePower включен и предмет НЕ лучше текущего => auto-sell
            // =========================
            if (_s.IncreasePower && !slotEmpty)
            {
                if (opened.Power <= cur.Power)
                {
                    gi.SellItem(opened, immediateSave: false);
                    gi.SaveAllNow();

                    if (postAutoDecisionVisualHold > 0f)
                        yield return WaitWithPause(postAutoDecisionVisualHold);

                    chest.FinalizePending(); // ? auto decision
                    yield return WaitWithPause(_s.OpenInterval);
                    continue;
                }
            }

            // =========================
            // иначе просто показываем попап
            // =========================
            chest.ShowPendingPopup();
            while (chest.IsBusy || IsPaused) yield return null;
            yield return WaitWithPause(_s.OpenInterval);
        }
    }

    private IEnumerator WaitWithPause(float seconds)
    {
        float t = 0f;
        seconds = Mathf.Max(0f, seconds);

        while (t < seconds)
        {
            if (!IsPaused)
                t += Time.unscaledDeltaTime;

            yield return null;
        }
    }

    private int GetRarityRank(ItemDef item)
    {
        if (item == null) return 0;
        string r = item.Rarity.ToString().Trim().ToUpperInvariant();

        if (r.Contains("SS")) return 8;
        if (r.Contains("S")) return 7;
        if (r.Contains("A")) return 6;
        if (r.Contains("B")) return 5;
        if (r.Contains("C")) return 4;
        if (r.Contains("D")) return 3;
        if (r.Contains("E")) return 2;
        if (r.Contains("F")) return 1;
        if (r.Contains("G")) return 0;

        return 0;
    }
}
