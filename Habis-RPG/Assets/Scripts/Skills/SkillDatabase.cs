// ============================================================================
// Habis RPG — Skill Database
// Static catalog of all skills + per-class skill tree definitions.
// Used by SkillTree UI and CombatEngine to resolve skill data by Id.
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using HabisRPG.Core;

namespace HabisRPG.Skills
{
    public static class SkillDatabase
    {
        private static readonly Dictionary<string, SkillData> _all = new();
        private static readonly Dictionary<CharacterClass, List<SkillData>> _byClass = new();

        static SkillDatabase()
        {
            BuildWarrior();
            BuildMage();
            BuildRogue();
        }

        // ---- Public API ----

        public static SkillData Get(string id) =>
            _all.TryGetValue(id, out var s) ? s : null;

        public static List<SkillData> GetForClass(CharacterClass cls) =>
            _byClass.TryGetValue(cls, out var list) ? list : new List<SkillData>();

        public static IEnumerable<SkillData> All => _all.Values;

        /// <summary>
        /// Returns true if the prerequisite skills for this skill have been unlocked
        /// by the character (i.e. exist in their UnlockedSkillIds list).
        /// </summary>
        public static bool ArePrerequisitesMet(SkillData skill, ICollection<string> unlocked)
        {
            if (skill.PrerequisiteSkillIds == null || skill.PrerequisiteSkillIds.Length == 0)
                return true;
            foreach (var id in skill.PrerequisiteSkillIds)
                if (!unlocked.Contains(id)) return false;
            return true;
        }

        // ---- Builders ----

        private static void Add(SkillData s)
        {
            _all[s.Id] = s;
            if (!_byClass.ContainsKey(s.RequiredClass))
                _byClass[s.RequiredClass] = new List<SkillData>();
            _byClass[s.RequiredClass].Add(s);
        }

        private static void BuildWarrior()
        {
            // ---- Tier 1 ----
            Add(new SkillData {
                Id = "war_powerstrike", Name = "Power Strike",
                Description = "A heavy blow that deals 150% physical damage.",
                RequiredClass = CharacterClass.Warrior,
                Branch = SkillTreeBranch.MeleeMastery,
                EnergyCost = 20, BaseDamage = 18,
                DamageType = DamageType.Physical,
                MaxUpgradeLevel = 5, UnlockLevel = 1,
                PrerequisiteSkillIds = new string[0]
            });
            Add(new SkillData {
                Id = "war_guard", Name = "Iron Guard",
                Description = "Raise your defense by 50% for 2 turns.",
                RequiredClass = CharacterClass.Warrior,
                Branch = SkillTreeBranch.TankDefensive,
                EnergyCost = 15, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.Barrier,
                EffectChance = 1f, EffectDuration = 2,
                TargetsSelf = true,
                PrerequisiteSkillIds = new string[0]
            });
            Add(new SkillData {
                Id = "war_rage", Name = "Battle Rage",
                Description = "Enter berserk: +40% damage for 3 turns.",
                RequiredClass = CharacterClass.Warrior,
                Branch = SkillTreeBranch.BerserkAggressive,
                EnergyCost = 25, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.Berserk,
                EffectChance = 1f, EffectDuration = 3,
                TargetsSelf = true,
                PrerequisiteSkillIds = new string[0]
            });

            // ---- Tier 2 ----
            Add(new SkillData {
                Id = "war_cleave", Name = "Cleave",
                Description = "Wide swing hits all enemies for 90% damage.",
                RequiredClass = CharacterClass.Warrior,
                Branch = SkillTreeBranch.MeleeMastery,
                EnergyCost = 35, BaseDamage = 14,
                DamageType = DamageType.Physical, IsAoE = true,
                MaxUpgradeLevel = 5, UnlockLevel = 5,
                PrerequisiteSkillIds = new[] { "war_powerstrike" }
            });
            Add(new SkillData {
                Id = "war_taunt", Name = "Taunt",
                Description = "Force enemies to focus you and weakens their next hit.",
                RequiredClass = CharacterClass.Warrior,
                Branch = SkillTreeBranch.TankDefensive,
                EnergyCost = 20, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 5,
                HasEffect = true, AppliesEffect = StatusEffectType.Weaken,
                EffectChance = 1f, EffectDuration = 2,
                PrerequisiteSkillIds = new[] { "war_guard" }
            });

            // ---- Tier 3 ----
            Add(new SkillData {
                Id = "war_execute", Name = "Execute",
                Description = "Devastating finisher: 250% damage, more if target is below 30% HP.",
                RequiredClass = CharacterClass.Warrior,
                Branch = SkillTreeBranch.BerserkAggressive,
                EnergyCost = 50, BaseDamage = 32, CooldownTurns = 2,
                DamageType = DamageType.Physical,
                MaxUpgradeLevel = 5, UnlockLevel = 12,
                PrerequisiteSkillIds = new[] { "war_rage" }
            });
        }

        private static void BuildMage()
        {
            // ---- Tier 1 ----
            Add(new SkillData {
                Id = "mage_fireball", Name = "Fireball",
                Description = "Hurl a fireball for magical damage. Chance to Burn.",
                RequiredClass = CharacterClass.Mage,
                Branch = SkillTreeBranch.Elemental,
                EnergyCost = 20, BaseDamage = 22,
                DamageType = DamageType.Magical, Element = ElementType.Fire,
                MaxUpgradeLevel = 5, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.Burn,
                EffectChance = 0.4f, EffectDuration = 3,
                PrerequisiteSkillIds = new string[0]
            });
            Add(new SkillData {
                Id = "mage_frostbolt", Name = "Frost Bolt",
                Description = "An icy bolt that may slow the target.",
                RequiredClass = CharacterClass.Mage,
                Branch = SkillTreeBranch.Elemental,
                EnergyCost = 18, BaseDamage = 18,
                DamageType = DamageType.Magical, Element = ElementType.Ice,
                MaxUpgradeLevel = 5, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.Slow,
                EffectChance = 0.5f, EffectDuration = 2,
                PrerequisiteSkillIds = new string[0]
            });
            Add(new SkillData {
                Id = "mage_arcaneshield", Name = "Arcane Shield",
                Description = "Shimmering barrier reduces incoming damage.",
                RequiredClass = CharacterClass.Mage,
                Branch = SkillTreeBranch.Support,
                EnergyCost = 22, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.ArcaneShield,
                EffectChance = 1f, EffectDuration = 3,
                TargetsSelf = true,
                PrerequisiteSkillIds = new string[0]
            });

            // ---- Tier 2 ----
            Add(new SkillData {
                Id = "mage_chainlight", Name = "Chain Lightning",
                Description = "Lightning arcs through all enemies for magical damage.",
                RequiredClass = CharacterClass.Mage,
                Branch = SkillTreeBranch.ArcaneArtillery,
                EnergyCost = 40, BaseDamage = 20,
                DamageType = DamageType.Magical, Element = ElementType.Lightning,
                IsAoE = true,
                MaxUpgradeLevel = 5, UnlockLevel = 5,
                PrerequisiteSkillIds = new[] { "mage_fireball" }
            });
            Add(new SkillData {
                Id = "mage_regen", Name = "Restoration",
                Description = "Heal-over-time on caster for 4 turns.",
                RequiredClass = CharacterClass.Mage,
                Branch = SkillTreeBranch.Support,
                EnergyCost = 25, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 5,
                HasEffect = true, AppliesEffect = StatusEffectType.Regen,
                EffectChance = 1f, EffectDuration = 4,
                TargetsSelf = true,
                PrerequisiteSkillIds = new[] { "mage_arcaneshield" }
            });

            // ---- Tier 3 ----
            Add(new SkillData {
                Id = "mage_meteor", Name = "Meteor",
                Description = "A massive meteor crashes down. AoE 200% damage.",
                RequiredClass = CharacterClass.Mage,
                Branch = SkillTreeBranch.ArcaneArtillery,
                EnergyCost = 50, BaseDamage = 40, CooldownTurns = 3,
                DamageType = DamageType.Magical, Element = ElementType.Fire,
                IsAoE = true,
                MaxUpgradeLevel = 5, UnlockLevel = 12,
                PrerequisiteSkillIds = new[] { "mage_chainlight" }
            });
        }

        private static void BuildRogue()
        {
            // ---- Tier 1 ----
            Add(new SkillData {
                Id = "rog_backstab", Name = "Backstab",
                Description = "A precise strike with high crit chance.",
                RequiredClass = CharacterClass.Rogue,
                Branch = SkillTreeBranch.Assassination,
                EnergyCost = 20, BaseDamage = 22,
                DamageType = DamageType.Physical,
                MaxUpgradeLevel = 5, UnlockLevel = 1,
                PrerequisiteSkillIds = new string[0]
            });
            Add(new SkillData {
                Id = "rog_smokebomb", Name = "Smoke Bomb",
                Description = "Blind enemies, reducing their hit chance for 2 turns.",
                RequiredClass = CharacterClass.Rogue,
                Branch = SkillTreeBranch.Evasion,
                EnergyCost = 22, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.Blind,
                EffectChance = 1f, EffectDuration = 2, IsAoE = true,
                PrerequisiteSkillIds = new string[0]
            });
            Add(new SkillData {
                Id = "rog_hamstring", Name = "Hamstring",
                Description = "Slow target and deal moderate damage.",
                RequiredClass = CharacterClass.Rogue,
                Branch = SkillTreeBranch.Control,
                EnergyCost = 18, BaseDamage = 14,
                DamageType = DamageType.Physical,
                MaxUpgradeLevel = 5, UnlockLevel = 1,
                HasEffect = true, AppliesEffect = StatusEffectType.Slow,
                EffectChance = 0.7f, EffectDuration = 3,
                PrerequisiteSkillIds = new string[0]
            });

            // ---- Tier 2 ----
            Add(new SkillData {
                Id = "rog_poisonblade", Name = "Poison Blade",
                Description = "Coats blade in poison: chance to apply Poison on hits.",
                RequiredClass = CharacterClass.Rogue,
                Branch = SkillTreeBranch.Assassination,
                EnergyCost = 28, BaseDamage = 18,
                DamageType = DamageType.Physical,
                MaxUpgradeLevel = 5, UnlockLevel = 5,
                HasEffect = true, AppliesEffect = StatusEffectType.Poison,
                EffectChance = 0.9f, EffectDuration = 4,
                PrerequisiteSkillIds = new[] { "rog_backstab" }
            });
            Add(new SkillData {
                Id = "rog_evadeup", Name = "Quickstep",
                Description = "Boost SPD significantly for 3 turns.",
                RequiredClass = CharacterClass.Rogue,
                Branch = SkillTreeBranch.Evasion,
                EnergyCost = 20, BaseDamage = 0,
                MaxUpgradeLevel = 3, UnlockLevel = 5,
                HasEffect = true, AppliesEffect = StatusEffectType.Haste,
                EffectChance = 1f, EffectDuration = 3,
                TargetsSelf = true,
                PrerequisiteSkillIds = new[] { "rog_smokebomb" }
            });

            // ---- Tier 3 ----
            Add(new SkillData {
                Id = "rog_shadowdance", Name = "Shadow Dance",
                Description = "Strike all enemies twice. Heavy energy cost.",
                RequiredClass = CharacterClass.Rogue,
                Branch = SkillTreeBranch.Assassination,
                EnergyCost = 50, BaseDamage = 16, CooldownTurns = 3,
                DamageType = DamageType.Physical, IsAoE = true,
                MaxUpgradeLevel = 5, UnlockLevel = 12,
                PrerequisiteSkillIds = new[] { "rog_poisonblade" }
            });
        }
    }
}
