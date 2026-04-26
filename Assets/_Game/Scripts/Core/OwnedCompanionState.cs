using System;

namespace GameCore.Companions
{
    [Serializable]
    public class OwnedCompanionState
    {
        public string companionId;
        public bool unlocked;
        public int level = 1;

        public OwnedCompanionState() { }

        public OwnedCompanionState(string companionId, bool unlocked = true, int level = 1)
        {
            this.companionId = companionId;
            this.unlocked = unlocked;
            this.level = level < 1 ? 1 : level;
        }
    }
}