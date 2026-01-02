using UnityEngine;

namespace GameCore.Items
{
    /// <summary>
    /// Result of a chest roll:
    /// - Slot: which equipment slot was targeted (in your current logic it is random)
    /// - Rarity: rolled rarity (then may be downgraded if pool is empty)
    /// - Item: the final selected item definition (can be null if the DB has no candidates)
    /// </summary>
    public struct RolledItem
    {
        public EquipSlot Slot;
        public Rarity Rarity;
        public ItemDef Item;
    }

    /// <summary>
    /// Pure static service that rolls an item from the chest system.
    ///
    /// IMPORTANT:
    /// This version picks a RANDOM SLOT first, then rarity, then tries to pick an item from
    /// ItemDatabase pool (slot+rarity). If pool is empty, it downgrades rarity (SS -> ... -> G).
    ///
    /// If you only have few items in the database, many (slot, rarity) pools will be empty,
    /// therefore "Item" might come out null.
    /// </summary>
    public static class ChestService
    {
        /// <summary>
        /// Rolls an item using the provided ChestDropTable.
        /// You should assign the table in inspector (e.g. ChestController / ChestOpenButton).
        /// </summary>
        public static RolledItem Roll(ChestDropTable table)
        {
            var slot = (EquipSlot)UnityEngine.Random.Range(0, 12);

            var gi = GameCore.GameInstance.I;
            var chestLevel = gi != null ? gi.State.ChestLevel : 1;

            var rarity = table.RollRarity(chestLevel);
            var minRarity = table.GetMinRarity(chestLevel);

            var item = ItemDatabase.I.GetRandomFromPool(slot, rarity);

            // даунгрейдим, но НЕ ниже minRarity
            for (int i = 0; i < 20 && item == null; i++)
            {
                if ((int)rarity <= (int)minRarity) break;
                rarity = Downgrade(rarity);
                item = ItemDatabase.I.GetRandomFromPool(slot, rarity);
            }

            return new RolledItem { Slot = slot, Rarity = rarity, Item = item };
        }

        /// <summary>
        /// Downgrades rarity by 1 step until we reach the minimum (G).
        /// Assumes enum ordering: G(0) ... SS(8).
        /// </summary>
        private static Rarity Downgrade(Rarity r)
        {
            // SS -> S -> A -> ... -> G
            int v = (int)r;
            v = Mathf.Max(0, v - 1);
            return (Rarity)v;
        }
    }
}


