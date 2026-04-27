using UnityEngine;

public class SpeedTimeTest : MonoBehaviour
{
    public void SpeedTime()
    {
        Time.timeScale = 5f;
    }

    public void ResetTime()
    {
        Time.timeScale = 1f;
    }
}
