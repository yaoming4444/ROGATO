using System;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public class PlayerState
    {
        public int Version = 1;

        public int Level = 1;
        public long Gold = 100;
        public int Gems = 0;

        public string SelectedSkinId = "default";
        public long LastSavedUnix = 0;

        public static PlayerState CreateDefault()
        {
            return new PlayerState
            {
                Version = 1,
                Level = 1,
                Gold = 100,
                Gems = 0,
                SelectedSkinId = "default",
                LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }
    }
}

