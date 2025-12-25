using System;
using GameCore.Items;

namespace GameCore
{
    [Serializable]
    public class PlayerState
    {
        public int Version = 2;

        // -------- Core progression --------
        public int Level = 1;
        public int Exp = 0;

        public long Gold = 100;
        public int Gems = 0;
        public int Chests = 0;

        // -------- Cosmetics --------
        public string SelectedSkinId = "default";

        // -------- Equipment & Chest --------
        public int ChestLevel = 1;

        // храним только itemId ("" если пусто)
        public string[] Equipped;

        public long LastSavedUnix = 0;

        public static int SlotCount => Enum.GetValues(typeof(EquipSlot)).Length;

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
        /// ВАЖНО: вызывать после загрузки из JSON (локал/сервер).
        /// Чинит null/длину массива и заполняет пустыми строками.
        /// </summary>
        public void EnsureValid()
        {
            if (Equipped == null || Equipped.Length != SlotCount)
                Equipped = new string[SlotCount];

            for (int i = 0; i < Equipped.Length; i++)
                Equipped[i] ??= "";
        }

        public string GetEquippedId(EquipSlot slot)
        {
            EnsureValid();
            return Equipped[(int)slot] ?? "";
        }

        public void SetEquippedId(EquipSlot slot, string itemId)
        {
            EnsureValid();
            Equipped[(int)slot] = itemId ?? "";
        }
    }
}



