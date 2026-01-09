using UnityEngine;

public class OpenPanel : MonoBehaviour
{
    [SerializeField] private GameObject Panel;

    public void ActivateGameObject()
    {
        Panel.gameObject.SetActive(true);
    }

    public void DeActivateGameObject()
    {
        Panel.gameObject.SetActive(false);
    }
}
