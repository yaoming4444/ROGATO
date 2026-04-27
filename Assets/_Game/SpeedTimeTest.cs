using UnityEngine;

public class SpeedTimeTest : MonoBehaviour
{
    public void SpeedTime()
    {
        Time.timeScale = 8f;
    }

    public void ResetTime()
    {
        Time.timeScale = 1f;
    }
}
