// ============================================================================
// Habis RPG — Combat Engine
// Turn-based combat: turn order, damage calc, actions, victory/defeat
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HabisRPG.Core;
using HabisRPG.Character;

namespace HabisRPG.Combat
{
    // ── COMBAT EVENTS ──
    public class CombatEventArgs : EventArgs
    {
        public string Message;
        public HabisCharacter Source;
        public HabisCharacter Target;
    }

    public class DamageEventArgs : CombatEventArgs
    {
        public int Damage;
        public bool IsCritical;
        public bool IsMiss;
        public DamageType Type;
    }

    // ── COMBAT RESULT ──
    public enum CombatResult
    {
        InProgress,
        Victory,
        Defeat,
        Fled
    }

    // ── COMBAT UNIT (wrapper for turn order) ──
    public class CombatUnit
    {
        public HabisCharacter Character;
        public bool IsPlayerSide;
        public bool HasActed;
        public int TurnOrder;

        // Skill cooldown tracking
        public Dictionary<string, int> SkillCooldowns = new();

        public bool CanAct => Character.IsAlive &&
            !Character.Data.ActiveEffects.Any(e => e.Type == StatusEffectType.Stun);
    }

    // ── MAIN COMBAT ENGINE ──
    public class CombatEngine
    {
        // State
        public List<CombatUnit> AllUnits { get; private set; } = new();
        public List<CombatUnit> TurnQueue { get; private set; } = new();
        public CombatUnit CurrentUnit { get; private set; }
        public int RoundNumber { get; private set; } = 1;
        public CombatResult Result { get; private set; } = CombatResult.InProgress;

        // Events
        public event EventHandler<CombatEventArgs> OnCombatStart;
        public event EventHandler<CombatEventArgs> OnTurnStart;
        public event EventHandler<DamageEventArgs> OnDamageDealt;
        public event EventHandler<CombatEventArgs> OnStatusApplied;
        public event EventHandler<CombatEventArgs> OnUnitDefeated;
        public event EventHandler<CombatEventArgs> OnCombatEnd;

        // ── INITIALIZATION ──
        public void InitCombat(
            List<HabisCharacter> playerParty,
            List<HabisCharacter> enemies)
        {
            AllUnits.Clear();
            RoundNumber = 1;
            Result = CombatResult.InProgress;

            foreach (var pc in playerParty)
            {
                pc.PrepareForBattle();
                AllUnits.Add(new CombatUnit
                {
                    Character = pc,
                    IsPlayerSide = true
                });
            }

            foreach (var enemy in enemies)
            {
                enemy.PrepareForBattle();
                AllUnits.Add(new CombatUnit
                {
                    Character = enemy,
                    IsPlayerSide = false
                });
            }

            BuildTurnQueue();
            OnCombatStart?.Invoke(this, new CombatEventArgs
            {
                Message = $"Combat begins! Round {RoundNumber}"
            });
        }

        // ── TURN ORDER (GDD Section 3.1: Speed-based) ──
        private void BuildTurnQueue()
        {
            TurnQueue = AllUnits
                .Where(u => u.Character.IsAlive)
                .OrderByDescending(u => u.Character.GetEffectiveStats().SPD)
                .ThenBy(u => u.Character.Data.Id) // Tie-breaker: consistent by ID
                .ToList();

            foreach (var unit in TurnQueue)
                unit.HasActed = false;
        }

        // ── ADVANCE TURN ──
        public CombatUnit AdvanceToNextUnit()
        {
            if (Result != CombatResult.InProgress) return null;

            // Find next unit that hasn't acted
            CurrentUnit = TurnQueue.FirstOrDefault(u => !u.HasActed && u.Character.IsAlive);

            if (CurrentUnit == null)
            {
                // All units acted → new round
                RoundNumber++;
                BuildTurnQueue();
                CurrentUnit = TurnQueue.FirstOrDefault(u => u.Character.IsAlive);
            }

            if (CurrentUnit == null) return null;

            // Tick status effects at turn start
            CurrentUnit.Character.TickStatusEffects();

            // Energy regen
            int bonus = GetEnergyBonus(CurrentUnit);
            CurrentUnit.Character.Data.Energy.Regenerate(bonus);

            // Reduce cooldowns
            foreach (var key in CurrentUnit.SkillCooldowns.Keys.ToList())
            {
                CurrentUnit.SkillCooldowns[key]--;
                if (CurrentUnit.SkillCooldowns[key] <= 0)
                    CurrentUnit.SkillCooldowns.Remove(key);
            }

            OnTurnStart?.Invoke(this, new CombatEventArgs
            {
                Source = CurrentUnit.Character,
                Message = $"{CurrentUnit.Character.Data.Name}'s turn"
            });

            // Handle Confuse
            if (CurrentUnit.Character.Data.ActiveEffects.Any(e => e.Type == StatusEffectType.Confuse))
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    // Attack random ally instead
                    var allies = AllUnits.Where(u => u.IsPlayerSide == CurrentUnit.IsPlayerSide
                        && u != CurrentUnit && u.Character.IsAlive).ToList();
                    if (allies.Count > 0)
                    {
                        var target = allies[UnityEngine.Random.Range(0, allies.Count)];
                        ExecuteBasicAttack(target);
                        return CurrentUnit;
                    }
                }
            }

            // Handle Stun
            if (!CurrentUnit.CanAct)
            {
                CurrentUnit.HasActed = true;
                return CurrentUnit; // Skip turn
            }

            return CurrentUnit;
        }

        private int GetEnergyBonus(CombatUnit unit)
        {
            var flavor = unit.Character.Data.Energy.Flavor;
            return flavor switch
            {
                // Warrior Rage: bonus energy when damaged (handled in TakeDamage)
                EnergyFlavor.Rage => 0,
                // Mage Arcana: passive regen is already base, crit bonus handled elsewhere
                EnergyFlavor.Arcana => 2,
                // Rogue Combo: earned by attacks (handled in attack methods)
                EnergyFlavor.ComboPoints => 0,
                _ => 0
            };
        }

        // ── ACTIONS ──

        /// <summary>
        /// Basic Attack: No energy cost, guaranteed hit, standard damage
        /// </summary>
        public DamageEventArgs ExecuteBasicAttack(CombatUnit target)
        {
            if (CurrentUnit == null || !CurrentUnit.CanAct) return null;

            var attackerStats = CurrentUnit.Character.GetEffectiveStats();
            var defenderStats = target.Character.GetEffectiveStats();

            // Accuracy check
            float hitChance = Mathf.Clamp(
                (attackerStats.ACC - defenderStats.EVA + 80f) / 100f,
                0.1f, 0.95f
            );

            // Blind debuff
            if (CurrentUnit.Character.Data.ActiveEffects.Any(e => e.Type == StatusEffectType.Blind))
                hitChance *= 0.5f;

            bool isMiss = UnityEngine.Random.value > hitChance;

            if (isMiss)
            {
                CurrentUnit.HasActed = true;
                var missResult = new DamageEventArgs
                {
                    Source = CurrentUnit.Character,
                    Target = target.Character,
                    Damage = 0,
                    IsMiss = true,
                    Message = "MISS!"
                };
                OnDamageDealt?.Invoke(this, missResult);
                CheckCombatEnd();
                return missResult;
            }

            // GDD Section 3.3: Physical Damage formula
            int baseDmg = 10 + attackerStats.STR / 2;
            int damage = CalculatePhysicalDamage(baseDmg, attackerStats, defenderStats, out bool isCrit);

            // Rogue Combo Point generation
            if (CurrentUnit.Character.Data.Energy.Flavor == EnergyFlavor.ComboPoints)
                CurrentUnit.Character.Data.Energy.Add(10);

            target.Character.TakeDamage(damage);

            // Warrior Rage generation on enemy hit
            if (target.Character.Data.Energy.Flavor == EnergyFlavor.Rage && target.Character.IsAlive)
                target.Character.Data.Energy.Add(5);

            CurrentUnit.HasActed = true;

            var result = new DamageEventArgs
            {
                Source = CurrentUnit.Character,
                Target = target.Character,
                Damage = damage,
                IsCritical = isCrit,
                Type = DamageType.Physical,
                Message = isCrit ? $"CRITICAL! {damage} damage!" : $"{damage} damage"
            };

            OnDamageDealt?.Invoke(this, result);

            if (!target.Character.IsAlive)
                OnUnitDefeated?.Invoke(this, new CombatEventArgs
                {
                    Source = CurrentUnit.Character,
                    Target = target.Character,
                    Message = $"{target.Character.Data.Name} defeated!"
                });

            CheckCombatEnd();
            return result;
        }

        /// <summary>
        /// Use Skill: Costs energy, various effects
        /// </summary>
        public DamageEventArgs ExecuteSkill(SkillData skill, CombatUnit target)
        {
            if (CurrentUnit == null || !CurrentUnit.CanAct) return null;

            // Check cooldown
            if (CurrentUnit.SkillCooldowns.ContainsKey(skill.Id)) return null;

            // Check energy
            if (!CurrentUnit.Character.Data.Energy.TrySpend(skill.EnergyCost)) return null;

            var attackerStats = CurrentUnit.Character.GetEffectiveStats();
            var defenderStats = target.Character.GetEffectiveStats();

            int damage = 0;
            bool isCrit = false;

            if (skill.BaseDamage > 0)
            {
                float scaledDamage = skill.GetScaledDamage();

                if (skill.DamageType == DamageType.Physical)
                    damage = CalculatePhysicalDamage(scaledDamage, attackerStats, defenderStats, out isCrit);
                else if (skill.DamageType == DamageType.Magical)
                    damage = CalculateMagicalDamage(scaledDamage, attackerStats, defenderStats, out isCrit);

                // Berserk buff: +40% damage
                if (CurrentUnit.Character.Data.ActiveEffects.Any(e => e.Type == StatusEffectType.Berserk))
                    damage = Mathf.RoundToInt(damage * 1.4f);

                // Weaken debuff: -30% damage
                if (CurrentUnit.Character.Data.ActiveEffects.Any(e => e.Type == StatusEffectType.Weaken))
                    damage = Mathf.RoundToInt(damage * 0.7f);

                // AoE: damage all enemies
                if (skill.IsAoE)
                {
                    var targets = AllUnits.Where(u =>
                        u.IsPlayerSide != CurrentUnit.IsPlayerSide && u.Character.IsAlive).ToList();

                    foreach (var t in targets)
                    {
                        var tStats = t.Character.GetEffectiveStats();
                        int aoeDmg = skill.DamageType == DamageType.Physical
                            ? CalculatePhysicalDamage(scaledDamage, attackerStats, tStats, out _)
                            : CalculateMagicalDamage(scaledDamage, attackerStats, tStats, out _);

                        t.Character.TakeDamage(aoeDmg);
                        if (!t.Character.IsAlive)
                            OnUnitDefeated?.Invoke(this, new CombatEventArgs
                            {
                                Target = t.Character,
                                Message = $"{t.Character.Data.Name} defeated!"
                            });
                    }
                }
                else
                {
                    target.Character.TakeDamage(damage);
                }
            }

            // Apply status effect
            if (skill.HasEffect && UnityEngine.Random.value <= skill.EffectChance)
            {
                var effectTarget = skill.TargetsSelf || skill.TargetsAllies
                    ? CurrentUnit.Character
                    : target.Character;

                var effect = StatusEffect.Create(
                    skill.AppliesEffect,
                    skill.EffectDuration,
                    GetEffectMagnitude(skill.AppliesEffect)
                );
                effectTarget.Data.ActiveEffects.Add(effect);

                OnStatusApplied?.Invoke(this, new CombatEventArgs
                {
                    Source = CurrentUnit.Character,
                    Target = effectTarget,
                    Message = $"{skill.AppliesEffect} applied!"
                });
            }

            // Set cooldown
            if (skill.CooldownTurns > 0)
                CurrentUnit.SkillCooldowns[skill.Id] = skill.CooldownTurns;

            // Mage Arcana: crit spells give bonus energy
            if (isCrit && CurrentUnit.Character.Data.Energy.Flavor == EnergyFlavor.Arcana)
                CurrentUnit.Character.Data.Energy.Add(8);

            CurrentUnit.HasActed = true;

            var result = new DamageEventArgs
            {
                Source = CurrentUnit.Character,
                Target = target.Character,
                Damage = damage,
                IsCritical = isCrit,
                Type = skill.DamageType,
                Message = $"{skill.Name}: {damage} damage{(isCrit ? " (CRIT!)" : "")}"
            };

            OnDamageDealt?.Invoke(this, result);

            if (!target.Character.IsAlive)
                OnUnitDefeated?.Invoke(this, new CombatEventArgs
                {
                    Target = target.Character,
                    Message = $"{target.Character.Data.Name} defeated!"
                });

            CheckCombatEnd();
            return result;
        }

        /// <summary>
        /// Defend: -50% incoming damage + 15 bonus energy regen
        /// </summary>
        public void ExecuteDefend()
        {
            if (CurrentUnit == null) return;

            CurrentUnit.Character.Data.ActiveEffects.Add(
                StatusEffect.Create(StatusEffectType.Barrier, 1, 0.5f));
            CurrentUnit.Character.Data.Energy.Add(EnergyState.DEFEND_BONUS_REGEN);
            CurrentUnit.HasActed = true;
        }

        /// <summary>
        /// Flee: Success based on SPD difference
        /// </summary>
        public bool ExecuteFlee()
        {
            if (CurrentUnit == null) return false;

            int playerSpd = CurrentUnit.Character.GetEffectiveStats().SPD;
            int avgEnemySpd = (int)AllUnits
                .Where(u => u.IsPlayerSide != CurrentUnit.IsPlayerSide && u.Character.IsAlive)
                .Average(u => u.Character.GetEffectiveStats().SPD);

            float fleeChance = Mathf.Clamp(0.3f + (playerSpd - avgEnemySpd) * 0.02f, 0.1f, 0.9f);
            bool success = UnityEngine.Random.value <= fleeChance;

            if (success)
                Result = CombatResult.Fled;

            CurrentUnit.HasActed = true;
            return success;
        }

        // ── DAMAGE FORMULAS (GDD Section 3.3) ──

        private int CalculatePhysicalDamage(float baseDmg, CharacterStats attacker,
            CharacterStats defender, out bool isCrit)
        {
            float damage = baseDmg * (attacker.STR / 10f);

            // Crit check
            isCrit = UnityEngine.Random.Range(0, 100) < attacker.CRI;
            if (isCrit) damage *= 1.5f;

            // Defense reduction
            float defReduction = 1f - (defender.DEF / 200f);
            damage *= Mathf.Max(defReduction, 0.1f); // Min 10% damage

            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        private int CalculateMagicalDamage(float baseDmg, CharacterStats attacker,
            CharacterStats defender, out bool isCrit)
        {
            float damage = baseDmg * (attacker.INT / 10f);

            // Crit (lower chance for magic)
            isCrit = UnityEngine.Random.Range(0, 100) < (attacker.CRI / 2);
            if (isCrit) damage *= 1.5f;

            // Magic resistance (using DEF as proxy; could add RES stat later)
            float resReduction = 1f - (defender.DEF / 250f);
            damage *= Mathf.Max(resReduction, 0.1f);

            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        private float GetEffectMagnitude(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Haste => 0.30f,
                StatusEffectType.Barrier => 0.50f,
                StatusEffectType.Berserk => 0.40f,
                StatusEffectType.Regen => 0.10f,
                StatusEffectType.CriticalBoost => 0.50f,
                StatusEffectType.Poison => 0.05f,
                StatusEffectType.Burn => 0.08f,
                StatusEffectType.Slow => 0.40f,
                StatusEffectType.Weaken => 0.30f,
                StatusEffectType.Blind => 0.50f,
                StatusEffectType.Curse => 0.20f,
                StatusEffectType.Confuse => 0.50f,
                _ => 0f
            };
        }

        // ── COMBAT END CHECK ──
        private void CheckCombatEnd()
        {
            bool allPlayersDead = AllUnits
                .Where(u => u.IsPlayerSide)
                .All(u => !u.Character.IsAlive);

            bool allEnemiesDead = AllUnits
                .Where(u => !u.IsPlayerSide)
                .All(u => !u.Character.IsAlive);

            if (allPlayersDead)
                Result = CombatResult.Defeat;
            else if (allEnemiesDead)
                Result = CombatResult.Victory;

            if (Result != CombatResult.InProgress)
            {
                OnCombatEnd?.Invoke(this, new CombatEventArgs
                {
                    Message = Result == CombatResult.Victory ? "Victory!" : "Defeat..."
                });
            }
        }

        // ── QUERIES ──
        public List<CombatUnit> GetPlayerUnits() =>
            AllUnits.Where(u => u.IsPlayerSide && u.Character.IsAlive).ToList();

        public List<CombatUnit> GetEnemyUnits() =>
            AllUnits.Where(u => !u.IsPlayerSide && u.Character.IsAlive).ToList();
    }
}
