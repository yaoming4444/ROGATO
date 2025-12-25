using UnityEngine;

namespace GameCore.Items
{
    public struct RolledItem
    {
        public EquipSlot Slot;
        public Rarity Rarity;
        public ItemDef Item;
    }

    public static class ChestService
    {
        // подкинь в инспекторе (например в ChestController)
        public static RolledItem Roll(ChestDropTable table)
        {
            var state = GameCore.GameInstance.I.State;

            // 1) рандомный слот
            var slot = (EquipSlot)Random.Range(0, 12);

            // 2) рандомная редкость по уровню сундука
            var rarity = table.RollRarity(state.ChestLevel);

            // 3) берем предмет из базы
            var db = ItemDatabase.I;
            ItemDef item = db != null ? db.GetRandomFromPool(slot, rarity) : null;

            // если на этой редкости нет предметов — пробуем вниз по редкости
            if (item == null && db != null)
            {
                for (int tries = 0; tries < 9 && item == null; tries++)
                {
                    rarity = Downgrade(rarity);
                    item = db.GetRandomFromPool(slot, rarity);
                }
            }

            return new RolledItem { Slot = slot, Rarity = rarity, Item = item };
        }

        private static Rarity Downgrade(Rarity r)
        {
            // SS -> S -> A -> ... -> G
            int v = (int)r;
            v = Mathf.Max(0, v - 1);
            return (Rarity)v;
        }
    }
}

