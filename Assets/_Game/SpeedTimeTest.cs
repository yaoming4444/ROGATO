using UnityEngine;

public class SpeedTimeTest : MonoBehaviour
{
    public void SpeedTime()
    {
        Time.timeScale = 3f;
    }

    public void ResetTime()
    {
        Time.timeScale = 1f;
    }
}
