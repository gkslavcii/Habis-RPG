// ============================================================================
// Habis RPG — Core Data Structures
// Serializable structs/classes for character stats, items, skills
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HabisRPG.Core
{
    // ── CHARACTER STATS ──
    [Serializable]
    public class CharacterStats
    {
        public int STR;
        public int INT;
        public int DEX;
        public int VIT;
        public int SPD;
        public int CRI;
        public int MAG;
        public int DEF;
        public int ACC;
        public int EVA;

        public int GetStat(StatType type)
        {
            return type switch
            {
                StatType.STR => STR,
                StatType.INT => INT,
                StatType.DEX => DEX,
                StatType.VIT => VIT,
                StatType.SPD => SPD,
                StatType.CRI => CRI,
                StatType.MAG => MAG,
                StatType.DEF => DEF,
                StatType.ACC => ACC,
                StatType.EVA => EVA,
                _ => 0
            };
        }

        public void SetStat(StatType type, int value)
        {
            switch (type)
            {
                case StatType.STR: STR = value; break;
                case StatType.INT: INT = value; break;
                case StatType.DEX: DEX = value; break;
                case StatType.VIT: VIT = value; break;
                case StatType.SPD: SPD = value; break;
                case StatType.CRI: CRI = value; break;
                case StatType.MAG: MAG = value; break;
                case StatType.DEF: DEF = value; break;
                case StatType.ACC: ACC = value; break;
                case StatType.EVA: EVA = value; break;
            }
        }

        public void AddStat(StatType type, int amount)
        {
            SetStat(type, GetStat(type) + amount);
        }

        /// <summary>
        /// Merges another stat block into this one (for equipment bonuses etc.)
        /// </summary>
        public CharacterStats Combined(CharacterStats other)
        {
            return new CharacterStats
            {
                STR = STR + other.STR,
                INT = INT + other.INT,
                DEX = DEX + other.DEX,
                VIT = VIT + other.VIT,
                SPD = SPD + other.SPD,
                CRI = CRI + other.CRI,
                MAG = MAG + other.MAG,
                DEF = DEF + other.DEF,
                ACC = ACC + other.ACC,
                EVA = EVA + other.EVA
            };
        }

        public CharacterStats Clone()
        {
            return new CharacterStats
            {
                STR = STR, INT = INT, DEX = DEX, VIT = VIT, SPD = SPD,
                CRI = CRI, MAG = MAG, DEF = DEF, ACC = ACC, EVA = EVA
            };
        }
    }

    // ── CLASS CONFIGURATION ──
    [Serializable]
    public class ClassConfig
    {
        public CharacterClass Class;
        public EnergyFlavor EnergyType;
        public CharacterStats BaseBonusPercent;  // e.g., STR +15 means 15% bonus
        public StatType PrimaryStatBonus;        // +2 per level to this stat
        public string[] Masteries;
    }

    // ── ENERGY SYSTEM ──
    [Serializable]
    public class EnergyState
    {
        public const int MAX_ENERGY = 100;
        public const int BATTLE_START_ENERGY = 40;
        public const int PASSIVE_REGEN = 8;
        public const int DEFEND_BONUS_REGEN = 15;

        public int Current;
        public int Max => MAX_ENERGY;
        public EnergyFlavor Flavor;

        public EnergyState() { }

        public EnergyState(EnergyFlavor flavor)
        {
            Flavor = flavor;
            Current = BATTLE_START_ENERGY;
        }

        public void Regenerate(int bonus = 0)
        {
            Current = Mathf.Min(Current + PASSIVE_REGEN + bonus, MAX_ENERGY);
        }

        public bool CanSpend(int amount) => Current >= amount;

        public bool TrySpend(int amount)
        {
            if (Current < amount) return false;
            Current -= amount;
            return true;
        }

        public void Add(int amount)
        {
            Current = Mathf.Min(Current + amount, MAX_ENERGY);
        }

        public void Reset()
        {
            Current = BATTLE_START_ENERGY;
        }
    }

    // ── STATUS EFFECT ──
    [Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        public StatusCategory Category;
        public int RemainingTurns;
        public float Magnitude;  // e.g., 0.3 for +30% SPD

        public bool IsExpired => RemainingTurns <= 0;

        public void Tick()
        {
            RemainingTurns--;
        }

        public static StatusEffect Create(StatusEffectType type, int duration, float magnitude)
        {
            return new StatusEffect
            {
                Type = type,
                Category = type switch
                {
                    StatusEffectType.Haste or
                    StatusEffectType.Barrier or
                    StatusEffectType.Berserk or
                    StatusEffectType.Regen or
                    StatusEffectType.ArcaneShield or
                    StatusEffectType.CriticalBoost => StatusCategory.Buff,
                    _ => StatusCategory.Debuff
                },
                RemainingTurns = duration,
                Magnitude = magnitude
            };
        }
    }

    // ── SKILL DEFINITION ──
    [Serializable]
    public class SkillData
    {
        public string Id;
        public string Name;
        public string Description;
        public CharacterClass RequiredClass;
        public SkillTreeBranch Branch;
        public int EnergyCost;           // 15-50
        public int CooldownTurns;        // 0 = no cooldown
        public DamageType DamageType;
        public float BaseDamage;
        public ElementType Element;
        public int MaxUpgradeLevel;      // 5-10
        public int CurrentLevel;
        public int UnlockLevel;          // character level required
        public string[] PrerequisiteSkillIds;

        // Status effect application
        public bool HasEffect;
        public StatusEffectType AppliesEffect;
        public float EffectChance;       // 0-1
        public int EffectDuration;

        // Targeting
        public bool IsAoE;
        public bool TargetsSelf;
        public bool TargetsAllies;

        public float GetScaledDamage()
        {
            // Each upgrade level adds 15% base damage
            return BaseDamage * (1f + (CurrentLevel - 1) * 0.15f);
        }
    }

    // ── SKILL TREE NODE ──
    [Serializable]
    public class SkillTreeNode
    {
        public SkillData Skill;
        public List<string> ChildSkillIds = new();
        public bool IsUnlocked;
        public int PointsInvested;
    }
}
