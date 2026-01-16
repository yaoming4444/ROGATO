using IDosGames;
using UnityEngine;

public class LoginWatcher : MonoBehaviour
{
    private void OnEnable()
    {
        AuthService.LoggedIn += OnLoggedIn;
        UserDataService.DataUpdated += OnAllDataReady;
    }

    private void OnDisable()
    {
        AuthService.LoggedIn -= OnLoggedIn;
        UserDataService.DataUpdated -= OnAllDataReady;
    }

    private void OnLoggedIn()
    {
        // После логина запрашиваем ВСЕ данные
        UserDataService.RequestUserAllData();
    }

    private void OnAllDataReady()
    {
        // Все данные загружены, VirtualCurrencyRechargeTimes уже готовы
        var timers = FindObjectsOfType<IDosGames.TcRechargeTimer>();

        foreach (var timer in timers)
        {
            timer.InitFromUserInventory(IGSUserData.UserInventory);
        }
    }
}
