using System;
using UnityEngine;

namespace GameCore.Items
{
    /// <summary>
    /// Item rarity ordered from lowest to highest.
    /// IMPORTANT: Some logic assumes numeric ordering (Downgrade/Upgrade).
    /// </summary>
    public enum Rarity { G, F, E, D, C, B, A, S, SS }

    /// <summary>
    /// Equipment slots.
    ///
    /// IMPORTANT:
    /// - PlayerState.Equipped is an array indexed by (int)EquipSlot.
    /// - Do not reorder existing values after you ship, otherwise saves will break.
    /// </summary>
    public enum EquipSlot
    {
        Weapon = 0,
        Head = 1,
        Body = 2,
        Legs = 3,
        Boots = 4,
        Gloves = 5,
        Belt = 6,
        Ring = 7,
        Amulet = 8,
        Artifact = 9,
        Companion = 10,
        Badge = 11
    }

    /// <summary>
    /// Basic "flat" stats (integers).
    /// </summary>
    [Serializable]
    public struct ItemStats
    {
        public int Atk;
        public int Def;
        public int Hp;

        // Adds two stat structs together.
        public static ItemStats operator +(ItemStats a, ItemStats b)
            => new ItemStats { Atk = a.Atk + b.Atk, Def = a.Def + b.Def, Hp = a.Hp + b.Hp };
    }

    /// <summary>
    /// Extra stat types (percentage or special bonuses).
    /// </summary>
    public enum ExtraStatType
    {
        CritChance,      // +Crit chance (%)
        CritDamage,      // +Crit damage (%)
        DodgeChance,     // +Dodge chance (%)

        AtkPercent,      // +ATK (%)
        HpPercent,       // +HP (%)
        DefPercent       // +DEF (%)
    }

    /// <summary>
    /// One extra bonus: Type + Value.
    /// For percentage stats store the number in "percent units" (1.5 = 1.5%).
    /// </summary>
    [Serializable]
    public struct ExtraStat
    {
        public ExtraStatType Type;
        public float Value;
    }
}



