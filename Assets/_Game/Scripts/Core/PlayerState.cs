using GameCore.Items;
using System;
using UnityEngine;

namespace GameCore
{
    [Serializable]
    public class PlayerState
    {
        public int Version = 3;

        // -------- Core progression --------
        public int Level = 1;
        public int Exp = 0;

        // -------- Currencies --------
        public long Gold = 100;
        public int Gems = 0;
        public int Chests = 200;

        // -------- Cosmetics --------
        public string SelectedSkinId = "default";

        // -------- Chest --------
        public int ChestLevel = 1;
        public bool AutoSellEnabled = false;

        // Stores itemId for each slot. Empty string means slot is empty.
        public string[] Equipped;

        // Unix timestamp of last save; used for "newer save wins" logic.
        public long LastSavedUnix = 0;

        // Number of slots in EquipSlot enum.
        public static int SlotCount => Enum.GetValues(typeof(EquipSlot)).Length;

        // ============================================================
        // VISUAL (Spine skin names) Ч храним строки (полные имена скинов)
        // ============================================================
        // IMPORTANT:
        // Ёто ƒќЋ∆Ќџ Ѕџ“№ реальные имена скинов из Spine, например:
        // "top/top_c_10", "boots/boots_c_3", "skin/skin_c_1", etc.
        //

        // Visual equipment slot levels (1..120)
        public int lvl_helmet = 1;
        public int lvl_top = 1;
        public int lvl_bottom = 1;
        public int lvl_boots = 1;
        public int lvl_gloves = 1;
        public int lvl_gearRight = 1;
        public int lvl_back = 1;
        public int lvl_eyewear = 1;

        public string visual_back = "";
        public string visual_beard = "";
        public string visual_boots = "";
        public string visual_bottom = "";
        public string visual_brow = "";
        public string visual_eyes = "";
        public string visual_gloves = "";

        public string visual_hair_short = "";
        public string visual_hair_hat = "";
        public string visual_helmet = "";

        public string visual_mouth = "";
        public string visual_eyewear = "";

        public string visual_gear_left = "";
        public string visual_gear_right = "";

        public string visual_top = "";
        public string visual_skin = "";

        // ============================================================
        // COLOR (skin color) Ч Color32 (RGBA) чтобы нормально сериализовалось
        // ============================================================
        public byte skinColorR = 255;
        public byte skinColorG = 255;
        public byte skinColorB = 255;
        public byte skinColorA = 255;

        // Hair
        public byte hairColorR = 255;
        public byte hairColorG = 255;
        public byte hairColorB = 255;
        public byte hairColorA = 255;

        // Beard
        public byte beardColorR = 255;
        public byte beardColorG = 255;
        public byte beardColorB = 255;
        public byte beardColorA = 255;

        // Brow
        public byte browColorR = 255;
        public byte browColorG = 255;
        public byte browColorB = 255;
        public byte browColorA = 255;

        public Color32 GetSkinColor32() => new Color32(skinColorR, skinColorG, skinColorB, skinColorA);
        public void SetSkinColor32(Color32 c)
        {
            skinColorR = c.r; skinColorG = c.g; skinColorB = c.b; skinColorA = c.a;
        }

        public Color32 GetHairColor32() => new Color32(hairColorR, hairColorG, hairColorB, hairColorA);
        public void SetHairColor32(Color32 c)
        {
            hairColorR = c.r; hairColorG = c.g; hairColorB = c.b; hairColorA = c.a;
        }

        public Color32 GetBeardColor32() => new Color32(beardColorR, beardColorG, beardColorB, beardColorA);
        public void SetBeardColor32(Color32 c)
        {
            beardColorR = c.r; beardColorG = c.g; beardColorB = c.b; beardColorA = c.a;
        }

        public Color32 GetBrowColor32() => new Color32(browColorR, browColorG, browColorB, browColorA);
        public void SetBrowColor32(Color32 c)
        {
            browColorR = c.r; browColorG = c.g; browColorB = c.b; browColorA = c.a;
        }

        /// <summary>
        /// Creates a fresh default state for first launch / dev reset.
        /// ¬ј∆Ќќ: тут задаЄм дефолтные скины сразу на все части.
        /// </summary>
        public static PlayerState CreateDefault()
        {
            var s = new PlayerState
            {
                Version = 3,
                Level = 1,
                Exp = 0,
                Gold = 100,
                Gems = 30,
                Chests = 200,
                SelectedSkinId = "default",
                ChestLevel = 1,
                AutoSellEnabled = false,
                LastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

                lvl_helmet = 1,
                lvl_top = 1,
                lvl_bottom = 1,
                lvl_boots = 1,
                lvl_gloves = 1,
                lvl_gearRight = 1,
                lvl_back = 1,
                lvl_eyewear = 1,

                // ====== DEFAULT VISUALS ======
                // ѕоставь тут свои реальные дефолтные скины.
                // ≈сли ты хочешь "все части индекс 1" Ч чаще всего это *_c_1 (но проверь имена в Spine).
                visual_back = "back/back_c_1",
                visual_beard = "",
                visual_boots = "boots/boots_c_1",
                visual_bottom = "bottom/bottom_c_1",
                visual_brow = "brow/brow_c_1",
                visual_eyes = "eyes/eyes_c_1",
                visual_gloves = "gloves/gloves_c_1",

                visual_hair_short = "hair_short/hair_short_c_1",
                visual_hair_hat = "hair_hat/hair_hat_c_1",
                visual_helmet = "",

                visual_mouth = "mouth/mouth_c_1",
                visual_eyewear = "",

                visual_gear_left = "",
                visual_gear_right = "",

                visual_top = "top/top_c_1",
                visual_skin = "skin/skin_c_1",
            };

            // ====== DEFAULT SKIN COLOR ======
            // пример УтЄплый светлыйФ Ч можешь помен€ть на любой
            s.SetSkinColor32(new Color32(255, 220, 200, 255));

            s.EnsureValid();
            return s;
        }

        /// <summary>
        /// MUST be called after loading from JSON (local/server).
        /// Fixes:
        /// - null Equipped
        /// - incorrect array length (when enum changed)
        /// - null strings inside array
        /// - null visual strings
        /// </summary>
        public void EnsureValid()
        {
            if (Equipped == null || Equipped.Length != SlotCount)
                Equipped = new string[SlotCount];

            for (int i = 0; i < Equipped.Length; i++)
                Equipped[i] ??= "";

            // на вс€кий: чтобы после загрузки не было nullТов
            visual_back ??= "";
            visual_beard ??= "";
            visual_boots ??= "";
            visual_bottom ??= "";
            visual_brow ??= "";
            visual_eyes ??= "";
            visual_gloves ??= "";

            visual_hair_short ??= "";
            visual_hair_hat ??= "";
            visual_helmet ??= "";

            visual_mouth ??= "";
            visual_eyewear ??= "";

            visual_gear_left ??= "";
            visual_gear_right ??= "";

            visual_top ??= "";
            visual_skin ??= "";

            // если вдруг кто-то сохранил A=0
            if (skinColorA == 0) skinColorA = 255;
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

        public int GetVisualSlotLevel(EquipmentType slot)
        {
            switch (slot)
            {
                case EquipmentType.Helmet: return lvl_helmet;
                case EquipmentType.Top: return lvl_top;
                case EquipmentType.Bottom: return lvl_bottom;
                case EquipmentType.Boots: return lvl_boots;
                case EquipmentType.Gloves: return lvl_gloves;
                case EquipmentType.Gear_Right: return lvl_gearRight;
                case EquipmentType.Back: return lvl_back;
                case EquipmentType.Eyewear: return lvl_eyewear;
                default: return 1; // все остальные слоты не прокачиваютс€
            }
        }

        public void SetVisualSlotLevel(EquipmentType slot, int level)
        {
            level = Mathf.Clamp(level, 1, 120);

            switch (slot)
            {
                case EquipmentType.Helmet: lvl_helmet = level; break;
                case EquipmentType.Top: lvl_top = level; break;
                case EquipmentType.Bottom: lvl_bottom = level; break;
                case EquipmentType.Boots: lvl_boots = level; break;
                case EquipmentType.Gloves: lvl_gloves = level; break;
                case EquipmentType.Gear_Right: lvl_gearRight = level; break;
                case EquipmentType.Back: lvl_back = level; break;
                case EquipmentType.Eyewear: lvl_eyewear = level; break;
                default: return;
            }
        }

        public bool UpgradeVisualSlotLevel(EquipmentType slot, int delta = 1)
        {
            int cur = GetVisualSlotLevel(slot);
            int next = Mathf.Clamp(cur + delta, 1, 120);
            if (next == cur) return false;

            SetVisualSlotLevel(slot, next);
            return true;
        }
    }
}





