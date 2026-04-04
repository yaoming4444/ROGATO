using UnityEngine;

public class FooterTabsController : MonoBehaviour
{
    [SerializeField] private FooterTabItem[] tabs;
    [SerializeField] private int startIndex = 0;

    private int currentIndex = -1;

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].Init(this, i);
        }

        SelectTab(startIndex);
    }

    public void SelectTab(int index)
    {
        if (index == currentIndex) return;

        currentIndex = index;

        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].SetState(i == currentIndex);
        }
    }
}