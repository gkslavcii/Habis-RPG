// ============================================================================
// Habis RPG — Character Controller
// Manages character state: stats, level, energy, equipment, skills
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HabisRPG.Core;

namespace HabisRPG.Character
{
    [Serializable]
    public class CharacterData
    {
        public string Id;
        public string Name;
        public CharacterClass PrimaryClass;
        public HybridClass Hybrid = HybridClass.None;

        // Level & XP
        public int Level = 1;
        public int Experience;
        public int FreeStatPoints;
        public int FreeSkillPoints;

        // Stats
        public CharacterStats BaseStats = new();
        public CharacterStats AllocatedStats = new();

        // Energy
        public EnergyState Energy;

        // Combat state
        public int CurrentHP;
        public int MaxHP;
        public List<StatusEffect> ActiveEffects = new();

        // Equipment & Skills
        public Dictionary<EquipSlot, string> EquippedItems = new();
        public List<string> UnlockedSkillIds = new();
        public Dictionary<string, int> SkillLevels = new();

        // Companion specific
        public bool IsCompanion;
        public int BondLevel;
        public float BondXP;
        public CompanionAIPreset AIPreset = CompanionAIPreset.Balanced;
    }

    public class CharacterController
    {
        public CharacterData Data { get; private set; }

        // ── XP TABLE (from GDD Section 5.1) ──
        private static readonly Dictionary<int, int> XPPerLevel = new()
        {
            // Levels 1-10: 100 XP each
            // Levels 11-30: 300 XP each
            // Levels 31-60: 800 XP each
            // Levels 61-90: 2000 XP each
            // Levels 91-99: 5000 XP each
        };

        // ── CLASS BASE STATS ──
        private static readonly Dictionary<CharacterClass, CharacterStats> ClassBaseStats = new()
        {
            [CharacterClass.Warrior] = new CharacterStats
            {
                STR = 12, INT = 5, DEX = 7, VIT = 10, SPD = 6,
                CRI = 5, MAG = 3, DEF = 8, ACC = 8, EVA = 4
            },
            [CharacterClass.Mage] = new CharacterStats
            {
                STR = 4, INT = 12, DEX = 6, VIT = 7, SPD = 8,
                CRI = 3, MAG = 12, DEF = 4, ACC = 7, EVA = 5
            },
            [CharacterClass.Rogue] = new CharacterStats
            {
                STR = 7, INT = 5, DEX = 12, VIT = 6, SPD = 12,
                CRI = 8, MAG = 3, DEF = 4, ACC = 9, EVA = 10
            }
        };

        private static readonly Dictionary<CharacterClass, StatType> PrimaryStats = new()
        {
            [CharacterClass.Warrior] = StatType.STR,
            [CharacterClass.Mage] = StatType.INT,
            [CharacterClass.Rogue] = StatType.DEX
        };

        private static readonly Dictionary<CharacterClass, EnergyFlavor> ClassEnergy = new()
        {
            [CharacterClass.Warrior] = EnergyFlavor.Rage,
            [CharacterClass.Mage] = EnergyFlavor.Arcana,
            [CharacterClass.Rogue] = EnergyFlavor.ComboPoints
        };

        // ── CONSTRUCTOR ──
        public CharacterController(string name, CharacterClass primaryClass)
        {
            Data = new CharacterData
            {
                Id = $"char_{Guid.NewGuid().ToString("N")[..8]}",
                Name = name,
                PrimaryClass = primaryClass,
                BaseStats = ClassBaseStats[primaryClass].Clone(),
                Energy = new EnergyState(ClassEnergy[primaryClass]),
                Level = 1,
                Experience = 0,
                FreeStatPoints = 0,
                FreeSkillPoints = 1
            };

            RecalculateHP();
            Data.CurrentHP = Data.MaxHP;
        }

        /// <summary>
        /// Load from saved data
        /// </summary>
        public CharacterController(CharacterData savedData)
        {
            Data = savedData;
        }

        // ── LEVELING ──
        public int GetXPRequiredForLevel(int level)
        {
            if (level <= 10) return 100;
            if (level <= 30) return 300;
            if (level <= 60) return 800;
            if (level <= 90) return 2000;
            return 5000; // 91-99
        }

        public int GetTotalXPForLevel(int targetLevel)
        {
            int total = 0;
            for (int i = 1; i < targetLevel; i++)
                total += GetXPRequiredForLevel(i);
            return total;
        }

        /// <summary>
        /// Adds XP and handles level-ups. Returns number of levels gained.
        /// </summary>
        public int AddExperience(int xp)
        {
            if (Data.Level >= 99) return 0;

            // Curse debuff reduces XP gain
            var curse = Data.ActiveEffects.Find(e => e.Type == StatusEffectType.Curse);
            if (curse != null)
                xp = Mathf.RoundToInt(xp * (1f - curse.Magnitude));

            Data.Experience += xp;
            int levelsGained = 0;

            while (Data.Level < 99)
            {
                int required = GetXPRequiredForLevel(Data.Level);
                if (Data.Experience < required) break;

                Data.Experience -= required;
                Data.Level++;
                levelsGained++;
                OnLevelUp();
            }

            return levelsGained;
        }

        private void OnLevelUp()
        {
            // GDD: 3-5 free stat points per level
            int freePoints = UnityEngine.Random.Range(3, 6);
            Data.FreeStatPoints += freePoints;
            Data.FreeSkillPoints += 1;

            // GDD: +2 to primary stat per level
            StatType primary = PrimaryStats[Data.PrimaryClass];
            Data.BaseStats.AddStat(primary, 2);

            RecalculateHP();
            Data.CurrentHP = Data.MaxHP; // Full heal on level up
        }

        // ── STAT ALLOCATION ──
        public bool AllocateStat(StatType stat, int points = 1)
        {
            if (Data.FreeStatPoints < points) return false;

            Data.AllocatedStats.AddStat(stat, points);
            Data.FreeStatPoints -= points;
            RecalculateHP();
            return true;
        }

        /// <summary>
        /// Returns the final computed stats (base + allocated + equipment bonuses)
        /// </summary>
        public CharacterStats GetEffectiveStats()
        {
            var combined = Data.BaseStats.Combined(Data.AllocatedStats);
            // TODO: Add equipment stat bonuses from ItemDatabase
            // TODO: Add status effect modifiers
            return combined;
        }

        private void RecalculateHP()
        {
            var stats = GetEffectiveStats();
            // HP = 50 + (VIT * 10) + (Level * 5)
            Data.MaxHP = 50 + (stats.VIT * 10) + (Data.Level * 5);
        }

        // ── HYBRID / SUBCLASS ──
        public bool CanUnlockHybrid() => Data.Level >= 30 && Data.Hybrid == HybridClass.None;

        public bool UnlockHybrid(CharacterClass secondaryClass)
        {
            if (!CanUnlockHybrid()) return false;
            if (secondaryClass == Data.PrimaryClass) return false;

            Data.Hybrid = (Data.PrimaryClass, secondaryClass) switch
            {
                (CharacterClass.Warrior, CharacterClass.Mage) or
                (CharacterClass.Mage, CharacterClass.Warrior) => HybridClass.Spellknight,

                (CharacterClass.Warrior, CharacterClass.Rogue) or
                (CharacterClass.Rogue, CharacterClass.Warrior) => HybridClass.Duelist,

                (CharacterClass.Mage, CharacterClass.Rogue) or
                (CharacterClass.Rogue, CharacterClass.Mage) => HybridClass.Trickster,

                _ => HybridClass.None
            };

            return Data.Hybrid != HybridClass.None;
        }

        // ── RESPEC ──
        public int GetRespecCost()
        {
            // GDD: 1,000 Gold (early) → 50,000 Gold (late)
            if (Data.Level <= 20) return 1000;
            if (Data.Level <= 40) return 5000;
            if (Data.Level <= 60) return 15000;
            if (Data.Level <= 80) return 25000;
            return 50000;
        }

        public void Respec()
        {
            // Refund all allocated stat points
            int totalAllocated = Data.AllocatedStats.STR + Data.AllocatedStats.INT +
                Data.AllocatedStats.DEX + Data.AllocatedStats.VIT +
                Data.AllocatedStats.SPD + Data.AllocatedStats.CRI +
                Data.AllocatedStats.MAG + Data.AllocatedStats.DEF +
                Data.AllocatedStats.ACC + Data.AllocatedStats.EVA;

            Data.AllocatedStats = new CharacterStats();
            Data.FreeStatPoints += totalAllocated;

            // Refund skill points
            int totalSkillPoints = Data.SkillLevels.Values.Sum();
            Data.SkillLevels.Clear();
            Data.FreeSkillPoints += totalSkillPoints;

            RecalculateHP();
        }

        // ── COMBAT HELPERS ──
        public bool IsAlive => Data.CurrentHP > 0;

        public void TakeDamage(int damage)
        {
            Data.CurrentHP = Mathf.Max(0, Data.CurrentHP - damage);
        }

        public void Heal(int amount)
        {
            Data.CurrentHP = Mathf.Min(Data.MaxHP, Data.CurrentHP + amount);
        }

        public void PrepareForBattle()
        {
            Data.Energy.Reset();
            Data.ActiveEffects.Clear();
        }

        public void TickStatusEffects()
        {
            foreach (var effect in Data.ActiveEffects)
            {
                ApplyEffectTick(effect);
                effect.Tick();
            }

            Data.ActiveEffects.RemoveAll(e => e.IsExpired);
        }

        private void ApplyEffectTick(StatusEffect effect)
        {
            switch (effect.Type)
            {
                case StatusEffectType.Poison:
                    TakeDamage(Mathf.RoundToInt(Data.MaxHP * 0.05f));
                    break;
                case StatusEffectType.Burn:
                    TakeDamage(Mathf.RoundToInt(Data.MaxHP * 0.08f));
                    break;
                case StatusEffectType.Regen:
                    Heal(Mathf.RoundToInt(Data.MaxHP * 0.10f));
                    break;
            }
        }

        // ── COMPANION BOND ──
        public void AddBondXP(float amount)
        {
            if (!Data.IsCompanion) return;
            Data.BondXP += amount;

            // Bond levels: 100 XP each, max level 5
            while (Data.BondXP >= 100f && Data.BondLevel < 5)
            {
                Data.BondXP -= 100f;
                Data.BondLevel++;
            }
        }
    }
}
