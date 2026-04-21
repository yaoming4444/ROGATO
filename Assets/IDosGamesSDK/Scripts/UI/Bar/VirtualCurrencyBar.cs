using UnityEngine;

namespace IDosGames
{
    public class VirtualCurrencyBar : CurrencyBar
    {
        [SerializeField] private VirtualCurrencyID virtualCurrencyId = VirtualCurrencyID.CO;

        private void OnEnable()
        {
            UpdateAmount();
            UserInventory.InventoryUpdated += UpdateAmount;
            UserDataService.VirtualCurrencyUpdated += UpdateAmount;
        }

        private void OnDisable()
        {
            UserInventory.InventoryUpdated -= UpdateAmount;
            UserDataService.VirtualCurrencyUpdated -= UpdateAmount;
        }

        public override void UpdateAmount()
        {
            Amount = UserInventory.GetVirtualCurrencyAmount(virtualCurrencyId);
        }
    }
}