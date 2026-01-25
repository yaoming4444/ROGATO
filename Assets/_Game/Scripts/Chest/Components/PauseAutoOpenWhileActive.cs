using UnityEngine;

public class PauseAutoOpenWhileActive : MonoBehaviour
{
    [SerializeField] private AutoChestRunner auto;

    private void Awake()
    {
        if (!auto) auto = FindObjectOfType<AutoChestRunner>(true);
    }

    private void OnEnable()
    {
        if (auto) auto.PauseAuto();
    }

    private void OnDisable()
    {
        if (auto) auto.ResumeAuto();
    }
}
