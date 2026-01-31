using System;
using System.Collections;
using UnityEngine;
using GameCore.Items;
using GameCore.UI;

public class AutoChestRunner : MonoBehaviour
{
    [SerializeField] private ChestController chest;

    [Header("Visual")]
    [SerializeField] private float postAutoDecisionVisualHold = 0.15f; // 0 = no hold

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

        // Stop only the loop (pending stays)
        StopAuto();

        _c = StartCoroutine(Loop());
        RunningChanged?.Invoke(true);
    }

    /// <summary>
    /// Stop ONLY the loop.
    /// Keeps pending reward + chest visuals (opened chest + drop icon) intact.
    /// </summary>
    public void StopAuto()
    {
        bool wasRunning = (_c != null);

        if (_c != null) StopCoroutine(_c);
        _c = null;

        // Do NOT call chest.FinalizePending() here!

        if (wasRunning)
            RunningChanged?.Invoke(false);
    }

    /// <summary>
    /// FULL OFF:
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
    /// For compatibility with previous calls.
    /// Stops loop and keeps pending.
    /// </summary>
    public void DisableAutoKeepPending()
    {
        DisableAuto();
    }

    /// <summary>
    /// Use ONLY when you intentionally want to clear pending reward + reset chest to idle.
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

            // wait while other flow is busy (anim/popup)
            if (chest.IsBusy)
            {
                yield return null;
                continue;
            }

            ItemDef opened = null;

            bool started = chest.TryOpenChestAuto(it => opened = it);
            if (!started)
            {
                // no chests / can't open. stop loop, keep pending untouched
                DisableAuto();
                yield break;
            }

            // wait for animation callback to set opened
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

            // current equipped in that slot
            var cur = gi.GetEquippedDef(opened.Slot);
            bool slotEmpty = (cur == null);

            // IMPORTANT (Variant A):
            // compare computed Power using item.GetPower(level) so:
            // - low level item can still win if its base stats are higher
            // - rarity is a multiplier, not an absolute rule
            int openedLevel = Mathf.Max(1, gi.PendingChestItemLevel);
            int curLevel = slotEmpty ? 1 : Mathf.Max(1, gi.GetEquippedLevel(opened.Slot));

            int openedPwr = opened.GetPower(openedLevel);
            int curPwr = slotEmpty ? 0 : cur.GetPower(curLevel);

            bool improvesPower = (!slotEmpty && openedPwr > curPwr);

            int cls = GetRarityRank(opened);

            // =========================
            // Rule: if IncreasePower is ON and item improves power OR slot empty => show popup (let player decide)
            // =========================
            if (_s.IncreasePower && (slotEmpty || improvesPower))
            {
                chest.ShowPendingPopup();
                while (chest.IsBusy || IsPaused) yield return null;

                yield return WaitWithPause(_s.OpenInterval);
                continue;
            }

            // =========================
            // Rule: if below MinClass => auto-sell
            // =========================
            if (cls < _s.MinClass)
            {
                gi.SellItem(opened, immediateSave: false);
                gi.SaveAllNow();

                if (postAutoDecisionVisualHold > 0f)
                    yield return WaitWithPause(postAutoDecisionVisualHold);

                chest.FinalizePending(); // auto decision -> clear pending + reset chest
                yield return WaitWithPause(_s.OpenInterval);
                continue;
            }

            // =========================
            // Rule: IncreasePower hint AND slot has item AND new is NOT better => auto-sell
            // =========================
            if (_s.IncreasePower && !slotEmpty)
            {
                if (openedPwr <= curPwr)
                {
                    gi.SellItem(opened, immediateSave: false);
                    gi.SaveAllNow();

                    if (postAutoDecisionVisualHold > 0f)
                        yield return WaitWithPause(postAutoDecisionVisualHold);

                    chest.FinalizePending(); // auto decision
                    yield return WaitWithPause(_s.OpenInterval);
                    continue;
                }
            }

            // =========================
            // Otherwise show popup
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
