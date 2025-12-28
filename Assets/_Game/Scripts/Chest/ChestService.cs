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
            // Random slot (currently hard-coded 0..12).
            // NOTE: If you add/remove slots, it is safer to use PlayerState.SlotCount.
            var slot = (EquipSlot)Random.Range(0, 12);

            // Chest level comes from PlayerState.
            var chestLevel = GameCore.GameInstance.I.State.ChestLevel;

            // Roll rarity based on chest level.
            var rarity = table.RollRarity(chestLevel);

            // Try to get random item from the DB pool (slot+rarity).
            // If pool is empty, downgrade rarity and retry up to 9 times.
            var item = ItemDatabase.I.GetRandomFromPool(slot, rarity);

            for (int i = 0; i < 9 && item == null; i++)
            {
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


