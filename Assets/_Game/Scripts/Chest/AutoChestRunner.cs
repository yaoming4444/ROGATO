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

        StopAuto(); // standard stop (also resets chest visuals)
        _c = StartCoroutine(Loop());
        RunningChanged?.Invoke(true);
    }

    /// <summary>
    /// Standard stop: stops loop AND resets pending+visuals (FinalizePending).
    /// </summary>
    public void StopAuto()
    {
        bool wasRunning = (_c != null);

        if (_c != null) StopCoroutine(_c);
        _c = null;

        // standard behavior: reset chest (clears pending)
        if (chest != null)
            chest.FinalizePending();

        if (wasRunning)
            RunningChanged?.Invoke(false);
    }

    /// <summary>
    /// FULL OFF (your current): stops loop + clears pause + resets chest visuals (FinalizePending).
    /// </summary>
    public void DisableAuto()
    {
        StopAuto();
        ClearPause();
    }

    /// <summary>
    /// ? IMPORTANT: stops auto loop, but DOES NOT clear pending reward and DOES NOT reset chest visuals.
    /// Use this when user closes comparison popup (X), so they can click chest later and decide.
    /// </summary>
    public void DisableAutoKeepPending()
    {
        bool wasRunning = (_c != null);

        if (_c != null) StopCoroutine(_c);
        _c = null;

        // keep pending as-is (NO chest.FinalizePending here)
        ClearPause();

        if (wasRunning)
            RunningChanged?.Invoke(false);
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

            if (chest.IsBusy)
            {
                yield return null;
                continue;
            }

            ItemDef opened = null;

            bool started = chest.TryOpenChestAuto(it => opened = it);
            if (!started)
            {
                DisableAuto();
                yield break;
            }

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

            var cur = gi.GetEquippedDef(opened.Slot);

            bool slotEmpty = (cur == null);
            bool improvesPower = (!slotEmpty && opened.Power > cur.Power);

            int cls = GetRarityRank(opened);

            // if slot empty OR improves power => show comparison popup
            if (_s.IncreasePower && (slotEmpty || improvesPower))
            {
                chest.ShowPendingPopup();
                while (chest.IsBusy || IsPaused) yield return null;

                yield return WaitWithPause(_s.OpenInterval);
                continue;
            }

            // below MinClass => auto-sell
            if (cls < _s.MinClass)
            {
                gi.SellItem(opened, immediateSave: false);
                gi.SaveAllNow();

                if (postAutoDecisionVisualHold > 0f)
                    yield return WaitWithPause(postAutoDecisionVisualHold);

                chest.FinalizePending();
                yield return WaitWithPause(_s.OpenInterval);
                continue;
            }

            // IncreasePower enabled and NOT better => auto-sell
            if (_s.IncreasePower && !slotEmpty)
            {
                if (opened.Power <= cur.Power)
                {
                    gi.SellItem(opened, immediateSave: false);
                    gi.SaveAllNow();

                    if (postAutoDecisionVisualHold > 0f)
                        yield return WaitWithPause(postAutoDecisionVisualHold);

                    chest.FinalizePending();
                    yield return WaitWithPause(_s.OpenInterval);
                    continue;
                }
            }

            // otherwise show popup
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
