using System.Collections.Generic;
using UnityEngine; // Object.FindFirstObjectByType / FindObjectOfType

namespace GameCore.Items
{
    /// <summary>
    /// Stat helpers on top of PlayerState + ItemDatabase (CORE),
    /// plus VisualEquipmentService (VISUAL, 8 slots with slot-level scaling).
    /// </summary>
    public static class EquipmentService
    {
        // ===================== CORE =====================

        public static string GetEquippedId(EquipSlot slot)
        {
            var gi = GameCore.GameInstance.I;
            var st = gi != null ? gi.State : null;
            if (st == null) return "";

            var eq = st.Equipped;
            if (eq == null || eq.Length < 12) return "";

            return eq[(int)slot];
        }

        public static ItemDef GetEquippedDef(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            if (string.IsNullOrWhiteSpace(id)) return null;

            var db = ItemDatabase.I;
            if (db == null) return null;

            return db.GetById(id);
        }

        public static ItemStats GetTotalBaseStats()
        {
            var total = new ItemStats();

            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                var def = GetEquippedDef(slot);
                if (def == null) continue;

                total += def.Stats;
            }

            return total;
        }

        public static Dictionary<ExtraStatType, float> GetTotalExtraStats()
        {
            var map = new Dictionary<ExtraStatType, float>();

            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                var def = GetEquippedDef(slot);
                if (def == null) continue;

                var arr = def.ExtraStats;
                if (arr == null || arr.Length == 0) continue;

                for (int k = 0; k < arr.Length; k++)
                {
                    var s = arr[k];

                    if (map.TryGetValue(s.Type, out var v)) map[s.Type] = v + s.Value;
                    else map[s.Type] = s.Value;
                }
            }

            return map;
        }

        // ===================== VISUAL =====================

        /// <summary>
        /// Считает ATK/DEF/HP от VisualEquipment (8 слотов) по стату предмета и уровню слота (1..120).
        /// Если visual == null -> попытается найти активный VisualEquipmentService в сцене.
        /// </summary>
        public static ItemStats GetTotalVisualStats(VisualEquipmentService visual)
        {
            var total = new ItemStats();

            var st = GameCore.GameInstance.I?.State;
            if (st == null) return total;

            visual ??= FindVisualService();
            if (visual == null) return total;

            Add(EquipmentType.Helmet);
            Add(EquipmentType.Top);
            Add(EquipmentType.Bottom);
            Add(EquipmentType.Boots);
            Add(EquipmentType.Gloves);
            Add(EquipmentType.Gear_Right);
            Add(EquipmentType.Back);
            Add(EquipmentType.Eyewear);

            return total;

            void Add(EquipmentType slot)
            {
                var item = visual.GetEquipped(slot);
                if (item == null) return;
                if (item.statType == EquipStatType.None) return;

                int slotLevel = Mathf.Clamp(st.GetVisualSlotLevel(slot), 1, 120);
                int value = item.GetStatValueForSlotLevel(slotLevel);
                if (value == 0) return;

                switch (item.statType)
                {
                    case EquipStatType.ATK: total.Atk += value; break;
                    case EquipStatType.DEF: total.Def += value; break;
                    case EquipStatType.HP: total.Hp += value; break;
                }
            }
        }

        /// <summary>
        /// Полные базовые статы: Core(12) + Visual(8).
        /// </summary>
        public static ItemStats GetTotalBaseStats_Combined(VisualEquipmentService visual)
        {
            var core = GetTotalBaseStats();
            var vis = GetTotalVisualStats(visual);

            core.Atk += vis.Atk;
            core.Def += vis.Def;
            core.Hp += vis.Hp;

            return core;
        }

        /// <summary>
        /// Удобный оверлоад: Core + Visual, без передачи ссылки (сам найдёт VisualEquipmentService).
        /// </summary>
        public static ItemStats GetTotalBaseStats_Combined()
        {
            return GetTotalBaseStats_Combined(FindVisualService());
        }

        private static VisualEquipmentService FindVisualService()
        {
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            return Object.FindFirstObjectByType<VisualEquipmentService>();
#else
            return Object.FindObjectOfType<VisualEquipmentService>();
#endif
        }
    }
}



