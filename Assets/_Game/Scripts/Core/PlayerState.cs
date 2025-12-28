using System;
using GameCore.Items;

namespace GameCore
{
    /// <summary>
    /// Serializable player save data (local + server).
    ///
    /// Stored fields:
    /// - progression: Level, Exp
    /// - currencies: Gold, Gems, Chests
    /// - cosmetics: SelectedSkinId
    /// - chest progression: ChestLevel
    /// - equipment: Equipped[] holds itemIds (empty string means "no item")
    /// - LastSavedUnix: unix timestamp used to decide which save is newer
    ///
    /// IMPORTANT:
    /// Equipped array indexes must match EquipSlot enum values.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public int Version = 2;

        // -------- Core progression --------
        public int Level = 1;
        public int Exp = 0;

        // -------- Currencies --------
        public long Gold = 100;
        public int Gems = 0;
        public int Chests = 10;

        // -------- Cosmetics --------
        public string SelectedSkinId = "default";

        // -------- Equipment & Chest --------
        public int ChestLevel = 1;

        // Stores itemId for each slot. Empty string means slot is empty.
        public string[] Equipped;

        // Unix timestamp of last save; used for "newer save wins" logic.
        public long LastSavedUnix = 0;

        // Number of slots in EquipSlot enum.
        public static int SlotCount => Enum.GetValues(typeof(EquipSlot)).Length;

        /// <summary>
        /// Creates a fresh default state for first launch / dev reset.
        /// </summary>
        public static PlayerState CreateDefault()
        {
            var s = new PlayerState
            {
                Version = 2,
                Level = 1,
                Exp = 0,
                Gold = 100,
                Gems = 0,
                Chests = 10,
                SelectedSkinId = "default",
                ChestLevel = 1,
                LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            s.EnsureValid();
            return s;
        }

        /// <summary>
        /// MUST be called after loading from JSON (local/server).
        /// Fixes:
        /// - null Equipped
        /// - incorrect array length (when enum changed)
        /// - null strings inside array
        /// </summary>
        public void EnsureValid()
        {
            if (Equipped == null || Equipped.Length != SlotCount)
                Equipped = new string[SlotCount];

            for (int i = 0; i < Equipped.Length; i++)
                Equipped[i] ??= "";
        }

        /// <summary>
        /// Reads equipped itemId for a slot.
        /// Returns "" if empty.
        /// </summary>
        public string GetEquippedId(EquipSlot slot)
        {
            EnsureValid();
            return Equipped[(int)slot] ?? "";
        }

        /// <summary>
        /// Sets equipped itemId for a slot.
        /// Use "" (or null) to clear slot.
        /// </summary>
        public void SetEquippedId(EquipSlot slot, string itemId)
        {
            EnsureValid();
            Equipped[(int)slot] = itemId ?? "";
        }
    }
}




