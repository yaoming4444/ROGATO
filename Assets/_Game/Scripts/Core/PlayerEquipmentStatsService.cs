using System;
using UnityEngine;
using GameCore.Items; // EquipmentService
using GameCore.UI;    // VisualEquipmentService (если он тут)

namespace GameCore
{
    public class PlayerEquipmentStatsService : MonoBehaviour
    {
        public static PlayerEquipmentStatsService I { get; private set; }

        public event Action Changed;

        public int BonusAtk { get; private set; }
        public int BonusHp { get; private set; }
        public int BonusDef { get; private set; } // на будущее

        [SerializeField] private VisualEquipmentService visual; // можно не назначать

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }

            I = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.StateChanged += OnStateChanged;

            if (visual == null)
                visual = VisualEquipmentService.I;

            if (visual != null)
                visual.OnChanged += OnVisualChanged;

            Recalc();
        }

        private void OnDisable()
        {
            var gi = GameCore.GameInstance.I;
            if (gi != null)
                gi.StateChanged -= OnStateChanged;

            if (visual != null)
                visual.OnChanged -= OnVisualChanged;
        }

        private void OnStateChanged(GameCore.PlayerState _)
        {
            Recalc();
        }

        private void OnVisualChanged()
        {
            Recalc();
        }

        public void Recalc()
        {
            var total = EquipmentService.GetTotalBaseStats_Combined(visual);

            BonusAtk = total.Atk;
            BonusHp = total.Hp;
            BonusDef = total.Def;

            Changed?.Invoke();
        }
    }
}

