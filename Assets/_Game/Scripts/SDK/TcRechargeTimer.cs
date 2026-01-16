using System;
using System.Collections;
using TMPro;
using UnityEngine;
using IDosGames.ClientModels;

namespace IDosGames
{
    public class TcRechargeTimer : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject timerRoot;          // объект, который скрываем/показываем (контейнер Text)
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Config")]
        [SerializeField] private int cooldownSeconds = 120;
        [SerializeField] private int maxTc = 5;                 // фулл значение

        private DateTime _readyTimeUtc;
        private bool _initialized;
        private int _currentTc;

        private Coroutine _refreshCoroutine;
        private bool _refreshStarted;

        private const string CurrencyId = "TC";

        public void InitFromUserInventory(GetUserInventoryResult inv)
        {
            if (inv == null) { Debug.LogError("[TcRechargeTimer] inv == null"); return; }
            if (timerText == null) { Debug.LogError("[TcRechargeTimer] timerText == null"); return; }
            if (timerRoot == null) timerRoot = timerText.gameObject; // фоллбек: скрываем сам текст

            // --- TC баланс ---
            if (inv.VirtualCurrency == null || !inv.VirtualCurrency.TryGetValue(CurrencyId, out _currentTc))
            {
                timerRoot.SetActive(true);
                timerText.text = "TC...";
                _initialized = false;
                return;
            }

            // ≈сли фулл Ч таймер пр€чем
            if (_currentTc >= maxTc)
            {
                StopRefresh();
                _initialized = false;
                timerRoot.SetActive(false);
                return;
            }

            // ≈сли не фулл Ч таймер показываем
            timerRoot.SetActive(true);

            // --- RechargeTime нужен только если TC < maxTc ---
            if (inv.VirtualCurrencyRechargeTimes == null ||
                !inv.VirtualCurrencyRechargeTimes.TryGetValue(CurrencyId, out var tcData))
            {
                timerText.text = "TC...";
                _initialized = false;
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var rechargeUtc = ToUtc(tcData.RechargeTime);

            _readyTimeUtc = (rechargeUtc > nowUtc) ? rechargeUtc : rechargeUtc.AddSeconds(cooldownSeconds);
            _initialized = true;

            // —брасываем, чтобы после нового rechargeTime можно было снова запросить при окончании таймера
            _refreshStarted = false;

            Render(nowUtc);
        }

        private void Update()
        {
            if (!_initialized || timerText == null) return;
            Render(DateTime.UtcNow);
        }

        // ====== ¬ј∆Ќќ: обработка сворачивани€/возврата ======

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) TryHandleResume();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused) TryHandleResume();
        }

        private void TryHandleResume()
        {
            if (!_initialized || timerText == null) return;

            // если фулл Ч пр€чем
            if (_currentTc >= maxTc)
            {
                StopRefresh();
                _initialized = false;
                if (timerRoot != null) timerRoot.SetActive(false);
                return;
            }

            // сразу обновим визуал
            var nowUtc = DateTime.UtcNow;
            Render(nowUtc);

            // если таймер уже истЄк во врем€ сворачивани€ Ч сделаем 1 запрос (и только если ещЄ не делали)
            if (!_refreshStarted && (_readyTimeUtc - nowUtc).TotalSeconds <= 0)
            {
                _refreshStarted = true;
                Debug.Log("[TcRechargeTimer] Resume & timer ended -> RequestUserAllData()");
                UserDataService.RequestUserAllData();
            }
        }

        private void Render(DateTime nowUtc)
        {
            // если стало фулл Ч скрываем
            if (_currentTc >= maxTc)
            {
                StopRefresh();
                _initialized = false;
                if (timerRoot != null) timerRoot.SetActive(false);
                return;
            }

            var remaining = _readyTimeUtc - nowUtc;

            if (remaining.TotalSeconds <= 0)
            {
                timerText.text = "Refreshing...";

                if (!_refreshStarted)
                {
                    _refreshStarted = true;
                    _refreshCoroutine = StartCoroutine(RefreshOnce());
                }
                return;
            }

            // MM:SS (минуты общие)
            var totalSeconds = Mathf.Max(0, (int)Math.Floor(remaining.TotalSeconds));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;

            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private IEnumerator RefreshOnce()
        {
            Debug.Log("[TcRechargeTimer] Timer ended -> RequestUserAllData()");
            UserDataService.RequestUserAllData();
            yield break; // флаг не сбрасываем, чтобы не спамить
        }

        private void StopRefresh()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }
            // ¬ажно: _refreshStarted не трогаем тут, он сбрасываетс€ при новом InitFromUserInventory
        }

        private static DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc) return dt;
            if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
    }
}








