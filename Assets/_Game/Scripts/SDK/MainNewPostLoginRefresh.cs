using UnityEngine;
using IDosGames;

public class MainNewPostLoginRefresh : MonoBehaviour
{
    [SerializeField] private bool refreshOnEnable = true;
    private bool _requested;

    private void OnEnable()
    {
        if (!refreshOnEnable) return;

        AuthService.LoggedIn += OnLoggedIn;
    }

    private void OnDisable()
    {
        AuthService.LoggedIn -= OnLoggedIn;
    }

    private void Start()
    {
        // Если на сцену попали уже залогиненными (например, логин был на предыдущей сцене),
        // можно просто один раз дернуть refresh.
        // Если у тебя есть явный флаг авторизации (например AuthService.IsLoggedIn) — используй его.
        TryRequestOnce();
    }

    private void OnLoggedIn()
    {
        TryRequestOnce();
    }

    private void TryRequestOnce()
    {
        if (_requested) return;
        _requested = true;

        UserDataService.RequestUserAllData();
        Debug.Log("[MainNewPostLoginRefresh] RequestUserAllData()");
    }
}

