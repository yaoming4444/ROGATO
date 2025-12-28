using UnityEngine;

namespace GameCore
{
    public class DevTools : MonoBehaviour
    {
        public void ResetProgress()
        {
            GameInstance.I.DevResetProgress();
        }
    }
}

