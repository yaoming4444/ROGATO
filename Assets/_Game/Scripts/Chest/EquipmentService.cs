using UnityEngine; // Object.FindFirstObjectByType / FindObjectOfType

namespace GameCore.Items
{
    /// <summary>
    /// Stat helpers only for VisualEquipment (8 slots with slot-level scaling).
    /// </summary>
    public static class EquipmentService
    {
        /// <summary>
        /// Calculates total ATK/DEF/HP from VisualEquipment (8 slots)
        /// using item stat + visual slot level (1..120).
        /// If visual == null -> tries to find active VisualEquipmentService in scene.
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
                    case EquipStatType.ATK:
                        total.Atk += value;
                        break;

                    case EquipStatType.DEF:
                        total.Def += value;
                        break;

                    case EquipStatType.HP:
                        total.Hp += value;
                        break;
                }
            }
        }

        /// <summary>
        /// Convenience overload: finds VisualEquipmentService automatically.
        /// </summary>
        public static ItemStats GetTotalVisualStats()
        {
            return GetTotalVisualStats(FindVisualService());
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