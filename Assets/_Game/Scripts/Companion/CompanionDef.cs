using UnityEngine;

namespace GameCore.Companions
{
    [CreateAssetMenu(fileName = "CompanionDef", menuName = "GameCore/Companions/Companion Def")]
    public class CompanionDef : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [TextArea]
        public string description;

        [Header("Card/UI")]
        public Sprite cardIcon;
        public GameObject uiPrefab;

        [Header("World")]
        public GameObject worldPrefab;

        [Header("Economy")]
        public int unlockCost = 1000;
        public int upgradeCostBase = 250;

        [Header("Progression")]
        public int maxLevel = 25;
    }
}