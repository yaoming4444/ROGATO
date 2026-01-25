using UnityEngine;

public class DisableAutoOpenWhileActive : MonoBehaviour
{
    [SerializeField] private AutoChestRunner auto;

    private void Awake()
    {
        if (!auto) auto = FindObjectOfType<AutoChestRunner>(true);
    }

    private void OnEnable()
    {
        if (auto) auto.DisableAuto();
    }
}
