using IDosGames;
using TMPro;
using UnityEngine;
using static NBitcoin.RPC.SignRawTransactionRequest;



    public class UserIdText : MonoBehaviour
    {
        [SerializeField] private TMP_Text userIdText;

        private void OnEnable()
        {
            // если уже залогинен Ч покажем сразу
            Refresh();

            // если логин произойдЄт позже Ч обновим по событию
            AuthService.LoggedIn += OnLoggedIn;
        }

        private void OnDisable()
        {
            AuthService.LoggedIn -= OnLoggedIn;
        }

        private void OnLoggedIn()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (userIdText == null) return;

            var id = AuthService.UserID;

            userIdText.text = $"{id}";
        }
    }
