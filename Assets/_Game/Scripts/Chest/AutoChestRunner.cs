using System.Collections;
using UnityEngine;
using GameCore.Items;
using GameCore.UI;

public class AutoChestRunner : MonoBehaviour
{
    [SerializeField] private ChestController chest;

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
    }

    private IEnumerator Loop()
    {
        while (true)
        {
            if (chest == null) yield break;

            // ждЄм, если зан€то (попап/анимаци€/ожидание решени€)
            if (chest.IsBusy)
            {
                yield return null;
                continue;
            }

            ItemDef opened = null;

            bool started = chest.TryOpenChestAuto(it => opened = it);
            if (!started)
            {
                StopAuto();
                yield break;
            }

            // ждЄм пока анимаци€ закончитс€ (callback приходит после openDuration)
            while (opened == null)
                yield return null;

            var gi = GameCore.GameInstance.I;
            if (gi == null)
            {
                StopAuto();
                yield break;
            }

            // 1) фильтр "класс >= "
            int cls = GetRarityRank(opened);
            if (cls < _s.MinClass)
            {
                gi.SellItem(opened, immediateSave: false);
                gi.SaveAllNow();
                chest.FinalizePendingKeepVisual();
                yield return new WaitForSeconds(_s.OpenInterval);
                continue;
            }

            // 2) повышение мощи
            var cur = gi.GetEquippedDef(opened.Slot);

            // слот пуст -> авто-экип (как в твоЄм попапе) :contentReference[oaicite:8]{index=8}
            if (_s.IncreasePower && cur == null)
            {
                gi.EquipItem(opened.Slot, opened.Id, immediateSave: false);
                gi.SaveAllNow();
                chest.FinalizePendingKeepVisual();
                yield return new WaitForSeconds(_s.OpenInterval);
                continue;
            }

            if (_s.IncreasePower && cur != null)
            {
                // если хуже или равно Ч авто-продажа
                if (opened.Power <= cur.Power)
                {
                    gi.SellItem(opened, immediateSave: false);
                    gi.SaveAllNow();
                    chest.FinalizePendingKeepVisual();
                    yield return new WaitForSeconds(_s.OpenInterval);
                    continue;
                }

                // лучше Ч показываем окно сравнени€ и ждЄм решени€ игрока
                chest.ShowPendingPopup();
                while (chest.IsBusy) yield return null;
                yield return new WaitForSeconds(_s.OpenInterval);
                continue;
            }

            // если IncreasePower выключен Ч просто показываем попап всегда
            chest.ShowPendingPopup();
            while (chest.IsBusy) yield return null;
            yield return new WaitForSeconds(_s.OpenInterval);
        }
    }

    private int GetRarityRank(ItemDef item)
    {
        // ” теб€ в UI rarity печатаетс€ как (_newItem.Rarity) :contentReference[oaicite:9]{index=9}
        // ѕоэтому тут делаем простой маппинг по строке/enum.ToString().
        if (item == null || item.Rarity == null) return 0;
        string r = item.Rarity.ToString().Trim().ToLowerInvariant();

        // частые варианты
        if (r.Contains("G")) return 0;
        if (r.Contains("F")) return 1;
        if (r.Contains("E")) return 2;
        if (r.Contains("D")) return 3;
        if (r.Contains("C")) return 4;
        if (r.Contains("B")) return 5;
        if (r.Contains("A")) return 6;
        if (r.Contains("S")) return 7;
        if (r.Contains("SS")) return 8;

        return 0;
    }
}
