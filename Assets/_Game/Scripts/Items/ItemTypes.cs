using System;
using UnityEngine;

namespace GameCore.Items
{
    public enum Rarity { G, F, E, D, C, B, A, S, SS }

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
        Cape = 9,
        Relic = 10,
        Artifact = 11
    }

    [Serializable]
    public struct ItemStats
    {
        public int Atk;
        public int Def;
        public int Hp;

        public static ItemStats operator +(ItemStats a, ItemStats b)
            => new ItemStats { Atk = a.Atk + b.Atk, Def = a.Def + b.Def, Hp = a.Hp + b.Hp };
    }

    // Дополнительные статы (как в Bombie)
    public enum ExtraStatType
    {
        CritChance,      // крит шанс (%)
        CritDamage,      // крит урон (%)
        DodgeChance,     // додж (%)
        ComboChance,     // комбо (%)
        IgnoreArmor,     // игнор защиты (%)
        SkillCritChance, // крит навыка (%)
        SkillCritDamage, // крит урона навыка (%)
        IgnoreDodge,     // игнор доджа (%)
        HpPercent,       // +HP (%)
        AtkPercent,      // +ATK (%)
        DefPercent       // +DEF (%)
    }

    // Один бонус: тип + значение.
    // Для процентов храним в "процентах" (1.5 = 1.5%)
    [Serializable]
    public struct ExtraStat
    {
        public ExtraStatType Type;
        public float Value;
    }
}


