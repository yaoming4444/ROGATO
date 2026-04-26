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

        [Header("Unlock / Hard Currency")]
        [Tooltip("Unlock cost in hard/server currency (for now CO).")]
        public int hardCurrencyUnlockCost = 100;

        [Header("Platform Purchase Product IDs")]
        public string tgMiniAppProductId;
        public string appStoreProductId;
        public string googlePlayProductId;

        [Header("Upgrade")]
        public int upgradeCostBase = 250;

        [Header("Progression")]
        public int maxLevel = 25;
    }
}