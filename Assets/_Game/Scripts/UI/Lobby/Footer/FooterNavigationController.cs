using UnityEngine;

public class FooterNavigationController : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private FooterTabItem lobbyTab;
    [SerializeField] private FooterTabItem shopTab;
    [SerializeField] private FooterTabItem equipmentTab;
    [SerializeField] private FooterTabItem cardsTab;
    [SerializeField] private FooterTabItem petTab;

    [Header("Windows")]
    [SerializeField] private GameObject shopWindow;
    [SerializeField] private GameObject equipmentWindow;
    [SerializeField] private GameObject cardsWindow;
    [SerializeField] private GameObject petWindow;

    private FooterTabType currentTab = FooterTabType.Lobby;

    private void Start()
    {
        OpenLobby();
    }

    public void OnClickLobby()
    {
        OpenLobby();
    }

    public void OnClickShop()
    {
        OpenTab(FooterTabType.Shop);
    }

    public void OnClickEquipment()
    {
        OpenTab(FooterTabType.Equipment);
    }

    public void OnClickCards()
    {
        OpenTab(FooterTabType.Cards);
    }

    public void OnClickPet()
    {
        OpenTab(FooterTabType.Pet);
    }

    public void OpenLobby()
    {
        CloseAllWindows();
        SetActiveTab(FooterTabType.Lobby);
    }

    public void OpenTab(FooterTabType tab)
    {
        CloseAllWindows();

        switch (tab)
        {
            case FooterTabType.Shop:
                if (shopWindow != null) shopWindow.SetActive(true);
                break;

            case FooterTabType.Equipment:
                if (equipmentWindow != null) equipmentWindow.SetActive(true);
                break;

            case FooterTabType.Cards:
                if (cardsWindow != null) cardsWindow.SetActive(true);
                break;

            case FooterTabType.Pet:
                if (petWindow != null) petWindow.SetActive(true);
                break;
        }

        SetActiveTab(tab);
    }

    public void OnWindowClosed()
    {
        OpenLobby();
    }

    private void CloseAllWindows()
    {
        if (shopWindow != null) shopWindow.SetActive(false);
        if (equipmentWindow != null) equipmentWindow.SetActive(false);
        if (cardsWindow != null) cardsWindow.SetActive(false);
        if (petWindow != null) petWindow.SetActive(false);
    }

    private void SetActiveTab(FooterTabType tab)
    {
        currentTab = tab;

        if (lobbyTab != null) lobbyTab.SetState(tab == FooterTabType.Lobby);
        if (shopTab != null) shopTab.SetState(tab == FooterTabType.Shop);
        if (equipmentTab != null) equipmentTab.SetState(tab == FooterTabType.Equipment);
        if (cardsTab != null) cardsTab.SetState(tab == FooterTabType.Cards);
        if (petTab != null) petTab.SetState(tab == FooterTabType.Pet);
    }
}