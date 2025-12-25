using System.Collections.Generic;

namespace GameCore.Items
{
    public static class EquipmentService
    {
        public static string GetEquippedId(EquipSlot slot)
        {
            var eq = GameCore.GameInstance.I.State.Equipped;
            if (eq == null || eq.Length < 12) return "";
            return eq[(int)slot];
        }

        public static ItemDef GetEquippedDef(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            return ItemDatabase.I ? ItemDatabase.I.GetById(id) : null;
        }

        public static bool IsSlotEmpty(EquipSlot slot)
        {
            var id = GetEquippedId(slot);
            return string.IsNullOrWhiteSpace(id);
        }

        public static void Equip(EquipSlot slot, string itemId)
        {
            var s = GameCore.GameInstance.I.State;
            s.Equipped[(int)slot] = itemId ?? "";
            GameCore.GameInstance.I.MarkDirty();

            GameCore.GameInstance.I.NotifyStateChangedExternal();

            // дл€ УсейчасФ Ч сразу пишем на сервер/локал
            GameCore.GameInstance.I.SaveAllNow();
        }

        public static void Sell(ItemDef item)
        {
            if (item == null) return;
            var s = GameCore.GameInstance.I.State;
            s.Gems += item.SellGems;
            GameCore.GameInstance.I.MarkDirty();
            GameCore.GameInstance.I.NotifyStateChangedExternal();
            GameCore.GameInstance.I.SaveAllNow();
        }

        public static ItemStats GetTotalBaseStats()
        {
            var total = new ItemStats();
            var eq = GameCore.GameInstance.I.State.Equipped;

            for (int i = 0; i < eq.Length; i++)
            {
                var def = ItemDatabase.I.GetById(eq[i]);
                if (def == null) continue;
                total += def.Stats;
            }

            return total;
        }

        public static Dictionary<ExtraStatType, float> GetTotalExtraStats()
        {
            var map = new Dictionary<ExtraStatType, float>();
            var eq = GameCore.GameInstance.I.State.Equipped;

            for (int i = 0; i < eq.Length; i++)
            {
                var def = ItemDatabase.I.GetById(eq[i]);
                if (def == null) continue;

                var arr = def.ExtraStats;
                if (arr == null || arr.Length == 0) continue;

                for (int k = 0; k < arr.Length; k++)
                {
                    var s = arr[k];
                    if (map.TryGetValue(s.Type, out var v))
                        map[s.Type] = v + s.Value;
                    else
                        map[s.Type] = s.Value;
                }
            }

            return map;
        }

    }
}

