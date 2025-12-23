using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDebug : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLoaded;
    }

    void OnLoaded(Scene s, LoadSceneMode m)
    {
        Debug.Log($"[SceneDebug] Loaded scene: {s.name} mode={m}");
        Debug.Log($"[SceneDebug] Stack:\n{System.Environment.StackTrace}");
    }
}
