// ============================================================================
// Habis RPG — Party Manager
// Manages active party, bench, companion recruitment, Bond system
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HabisRPG.Core;
using HabisRPG.Character;

namespace HabisRPG.Party
{

    public class CompanionTemplate
    {
        public string Id;
        public string Name;
        public CharacterClass Class;
        public int JoinLevel;
        public Region JoinRegion;
        public string RecruitMethod;  // "story" or "sidequest"
        public string Description;
    }

    public class PartyManager
    {
        // GDD Section 6.2: Active party = player + 2 (boss fights: +3)
        public const int MAX_ACTIVE_PARTY = 3;
        public const int MAX_BOSS_PARTY = 4;

        public HabisCharacter PlayerCharacter { get; private set; }
        public List<HabisCharacter> ActiveCompanions { get; private set; } = new();
        public List<HabisCharacter> BenchCompanions { get; private set; } = new();
        public List<string> RecruitedCompanionIds { get; private set; } = new();

        // ── COMPANION ROSTER (GDD Section 6.1) ──
        public static readonly List<CompanionTemplate> CompanionRoster = new()
        {
            new CompanionTemplate
            {
                Id = "comp_kael", Name = "Kael", Class = CharacterClass.Warrior,
                JoinLevel = 1, JoinRegion = Region.CursedForest,
                RecruitMethod = "story",
                Description = "A fellow Sigil-Bearer who awakens alongside you."
            },
            new CompanionTemplate
            {
                Id = "comp_lyra", Name = "Lyra", Class = CharacterClass.Mage,
                JoinLevel = 8, JoinRegion = Region.CursedForest,
                RecruitMethod = "story",
                Description = "A young mage rescued from a corrupted shrine."
            },
            new CompanionTemplate
            {
                Id = "comp_vex", Name = "Vex", Class = CharacterClass.Rogue,
                JoinLevel = 15, JoinRegion = Region.RottingSwamp,
                RecruitMethod = "sidequest",
                Description = "A cunning thief who tests your stealth abilities."
            },
            new CompanionTemplate
            {
                Id = "comp_theron", Name = "Theron", Class = CharacterClass.Warrior,
                JoinLevel = 25, JoinRegion = Region.ShatteredMountain,
                RecruitMethod = "story",
                Description = "A fallen knight seeking redemption from the Habis curse."
            },
            new CompanionTemplate
            {
                Id = "comp_selene", Name = "Selene", Class = CharacterClass.Mage,
                JoinLevel = 35, JoinRegion = Region.AbyssalDungeon,
                RecruitMethod = "sidequest",
                Description = "A reclusive scholar who guards ancient arcane knowledge."
            },
            new CompanionTemplate
            {
                Id = "comp_riven", Name = "Riven", Class = CharacterClass.Rogue,
                JoinLevel = 50, JoinRegion = Region.AncientRuins,
                RecruitMethod = "story",
                Description = "A spy with ambiguous loyalties and a hidden past."
            },
            new CompanionTemplate
            {
                Id = "comp_secret", Name = "???", Class = CharacterClass.Warrior,
                JoinLevel = 70, JoinRegion = Region.VoidAbyss,
                RecruitMethod = "sidequest",
                Description = "A mysterious figure found deep within the Void."
            }
        };

        // ── INITIALIZATION ──
        public PartyManager(HabisCharacter player)
        {
            PlayerCharacter = player;
        }

        // ── RECRUITMENT ──
        public HabisCharacter RecruitCompanion(string companionId)
        {
            if (RecruitedCompanionIds.Contains(companionId)) return null;

            var template = CompanionRoster.FirstOrDefault(c => c.Id == companionId);
            if (template == null) return null;

            var companion = new HabisCharacter(template.Name, template.Class);
            companion.Data.Id = template.Id;
            companion.Data.IsCompanion = true;

            // Set to appropriate level (match player or template minimum)
            int targetLevel = Mathf.Max(template.JoinLevel, PlayerCharacter.Data.Level - 2);
            while (companion.Data.Level < targetLevel)
            {
                companion.AddExperience(companion.GetXPRequiredForLevel(companion.Data.Level));
            }

            RecruitedCompanionIds.Add(companionId);

            // Auto-add to active if space, otherwise bench
            if (ActiveCompanions.Count < MAX_ACTIVE_PARTY - 1) // -1 for player
                ActiveCompanions.Add(companion);
            else
                BenchCompanions.Add(companion);

            return companion;
        }

        /// <summary>
        /// Returns companions available to recruit at current player level and region
        /// </summary>
        public List<CompanionTemplate> GetAvailableRecruits(int playerLevel, Region currentRegion)
        {
            return CompanionRoster
                .Where(c => !RecruitedCompanionIds.Contains(c.Id)
                    && playerLevel >= c.JoinLevel)
                .ToList();
        }

        // ── PARTY MANAGEMENT ──
        public bool SwapToActive(HabisCharacter companion)
        {
            if (!BenchCompanions.Contains(companion)) return false;
            if (ActiveCompanions.Count >= MAX_ACTIVE_PARTY - 1) return false;

            BenchCompanions.Remove(companion);
            ActiveCompanions.Add(companion);
            return true;
        }

        public bool SwapToBench(HabisCharacter companion)
        {
            if (!ActiveCompanions.Contains(companion)) return false;

            ActiveCompanions.Remove(companion);
            BenchCompanions.Add(companion);
            return true;
        }

        public bool SwapCompanions(HabisCharacter toActive, HabisCharacter toBench)
        {
            if (!BenchCompanions.Contains(toActive) || !ActiveCompanions.Contains(toBench))
                return false;

            BenchCompanions.Remove(toActive);
            ActiveCompanions.Remove(toBench);
            ActiveCompanions.Add(toActive);
            BenchCompanions.Add(toBench);
            return true;
        }

        /// <summary>
        /// Get full combat party (player + active companions)
        /// </summary>
        public List<HabisCharacter> GetCombatParty(bool isBossFight = false)
        {
            var party = new List<HabisCharacter> { PlayerCharacter };
            int maxCompanions = isBossFight ? MAX_BOSS_PARTY - 1 : MAX_ACTIVE_PARTY - 1;
            party.AddRange(ActiveCompanions.Take(maxCompanions));
            return party;
        }

        // ── POST-BATTLE XP DISTRIBUTION ──
        /// <summary>
        /// Distributes XP after combat. Bench companions get 50% (GDD Section 6.2)
        /// </summary>
        public void DistributeBattleXP(int totalXP)
        {
            // Active party gets full XP
            PlayerCharacter.AddExperience(totalXP);
            foreach (var comp in ActiveCompanions)
            {
                comp.AddExperience(totalXP);
                comp.AddBondXP(5f); // Bond XP for participating
            }

            // Bench gets 50%
            int benchXP = totalXP / 2;
            foreach (var comp in BenchCompanions)
            {
                comp.AddExperience(benchXP);
            }
        }

        // ── COMPANION AI ──
        /// <summary>
        /// Get AI-recommended action for a companion based on their preset
        /// </summary>
        public CombatAction GetAIAction(HabisCharacter companion, float hpPercent)
        {
            var preset = companion.Data.AIPreset;

            return preset switch
            {
                CompanionAIPreset.Aggressive => CombatAction.UseSkill,

                CompanionAIPreset.Defensive =>
                    hpPercent < 0.3f ? CombatAction.Defend :
                    hpPercent < 0.5f ? CombatAction.UseItem :
                    CombatAction.BasicAttack,

                CompanionAIPreset.Balanced =>
                    hpPercent < 0.25f ? CombatAction.Defend :
                    hpPercent < 0.5f ? CombatAction.BasicAttack :
                    CombatAction.UseSkill,

                CompanionAIPreset.Support =>
                    hpPercent < 0.4f ? CombatAction.UseItem :
                    CombatAction.UseSkill, // Prioritize buff/heal skills

                _ => CombatAction.BasicAttack
            };
        }
    }
}
