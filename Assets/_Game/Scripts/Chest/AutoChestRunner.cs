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

    public bool IsRunning => _c != null;

    public void StartAuto(AutoOpenSettingsPopup.Settings settings)
    {
        _s = settings;
        StopAuto();
        _c = StartCoroutine(Loop());
    }

    public void StopAuto()
    {
        if (_c != null) StopCoroutine(_c);
        _c = null;

        // если остановились вручную — сбросим визуал, чтобы не залипало
        if (chest != null)
            chest.FinalizePending();
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            if (chest == null) yield break;

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
                chest.FinalizePending();
                StopAuto();
                yield break;
            }

            // ждём пока анимация закончится (callback приходит после openDuration / длины стейта)
            while (opened == null)
                yield return null;

            var gi = GameCore.GameInstance.I;
            if (gi == null)
            {
                chest.FinalizePending();
                StopAuto();
                yield break;
            }

            // Текущая вещь в слоте
            var cur = gi.GetEquippedDef(opened.Slot);

            // Вычисляем "улучшает ли power"
            bool slotEmpty = (cur == null);
            bool improvesPower = (!slotEmpty && opened.Power > cur.Power);

            // Ранг/класс предмета
            int cls = GetRarityRank(opened);

            // =========================
            // НОВОЕ ПРАВИЛО:
            // если слот пуст ИЛИ предмет повышает power — всегда показываем окно сравнения
            // (даже если рарность ниже MinClass)
            // =========================
            if (_s.IncreasePower && (slotEmpty || improvesPower))
            {
                chest.ShowPendingPopup();
                while (chest.IsBusy) yield return null;

                yield return new WaitForSecondsRealtime(_s.OpenInterval);
                continue;
            }

            // =========================
            // Если НЕ выполняется правило выше, дальше работаем как авто-решение:
            // - ниже MinClass => auto-sell
            // - либо если IncreasePower и не улучшает (<=) => auto-sell
            // - иначе просто показываем попап (если IncreasePower выключен)
            // =========================

            // 1) Фильтр по классу (теперь он НЕ мешает улучшениям)
            if (cls < _s.MinClass)
            {
                gi.SellItem(opened, immediateSave: false);
                gi.SaveAllNow();

                if (postAutoDecisionVisualHold > 0f)
                    yield return new WaitForSecondsRealtime(postAutoDecisionVisualHold);

                chest.FinalizePending(); // сбросить сундук в idle
                yield return new WaitForSecondsRealtime(_s.OpenInterval);
                continue;
            }

            // 2) Если IncreasePower включен и предмет НЕ лучше текущего (или слот пуст уже обработан выше)
            if (_s.IncreasePower && !slotEmpty)
            {
                if (opened.Power <= cur.Power)
                {
                    gi.SellItem(opened, immediateSave: false);
                    gi.SaveAllNow();

                    if (postAutoDecisionVisualHold > 0f)
                        yield return new WaitForSecondsRealtime(postAutoDecisionVisualHold);

                    chest.FinalizePending(); // сбросить сундук в idle
                    yield return new WaitForSecondsRealtime(_s.OpenInterval);
                    continue;
                }

                // opened.Power > cur.Power сюда уже не попадёт (мы выше показываем попап)
            }

            // 3) Если IncreasePower выключен — просто показываем попап всегда
            chest.ShowPendingPopup();
            while (chest.IsBusy) yield return null;
            yield return new WaitForSecondsRealtime(_s.OpenInterval);
        }
    }

    private int GetRarityRank(ItemDef item)
    {
        if (item == null || item.Rarity == null) return 0;
        string r = item.Rarity.ToString().Trim().ToUpperInvariant();

        // ВАЖНО: оставляю твою текущую систему буквенных классов
        // (но порядок SS/S — SS раньше S)
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
