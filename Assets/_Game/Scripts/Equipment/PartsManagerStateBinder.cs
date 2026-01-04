using System.Collections.Generic;
using UnityEngine;
using LayerLab.ArtMaker;

namespace GameCore.Visual
{
    public class PartsManagerStateBinder : MonoBehaviour
    {
        [SerializeField] private PartsManager partsManager;

        [Header("Init")]
        [SerializeField] private bool autoInit = true;

        [Header("Optional: apply baseline once (hide everything except skin)")]
        [SerializeField] private bool applyBaselineOnce = true;
        [SerializeField] private PartsType baselineSkinType = PartsType.Skin;
        [SerializeField] private int baselineSkinIndex = 0;

        private bool _inited;
        private bool _baselineApplied;

        private void Awake()
        {
            if (!partsManager) partsManager = GetComponentInChildren<PartsManager>(true);
            if (autoInit) EnsureInit();
        }

        private void EnsureInit()
        {
            if (_inited) return;
            partsManager.Init();
            _inited = true;

            if (applyBaselineOnce)
                ApplyBaseline();
        }

        private void ApplyBaseline()
        {
            if (_baselineApplied) return;

            // делаем "голого" персонажа: всем -1, кроме базовой кожи
            var dict = new Dictionary<PartsType, int>();
            foreach (PartsType t in System.Enum.GetValues(typeof(PartsType)))
            {
                if (t == PartsType.None) continue;
                dict[t] = -1;
            }
            dict[baselineSkinType] = baselineSkinIndex;

            partsManager.SetSkinActiveIndex(dict);
            _baselineApplied = true;
        }

        public void ApplyFromState()
        {
            EnsureInit();

            var st = GameCore.GameInstance.I?.State;
            if (st == null) return;

            // ВАЖНО: тут мы НЕ вызываем SetSkinActiveIndex на весь дикт.
            // Только точечные слоты:
            ApplyOne(PartsType.Top, st.visual_top);
            ApplyOne(PartsType.Boots, st.visual_boots);

            // добавишь остальные слоты по мере надобности:
            // ApplyOne(PartsType.Helmet, st.visual_helmet);
            // ApplyOne(PartsType.Gear_Left, st.visual_left);
        }

        private void ApplyOne(PartsType type, string skinName)
        {
            if (string.IsNullOrWhiteSpace(skinName))
                return; // <-- ключевое: если пусто, НЕ ТРОГАЕМ слот (не надеваем "первый")

            var list = partsManager.GetCurrentSkinNames(type);
            if (list == null || list.Count == 0) return;

            int idx = list.IndexOf(skinName);
            if (idx < 0)
            {
                Debug.LogWarning($"[Binder] Skin '{skinName}' not found for {type}");
                return;
            }

            partsManager.EquipParts(type, idx);
        }
    }
}



