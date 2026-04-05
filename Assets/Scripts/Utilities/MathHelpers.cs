// ============================================================================
// Habis RPG — Math Helpers & Utility Functions
// Common calculations used across combat, economy, progression
// ============================================================================

using UnityEngine;
using HabisRPG.Core;

namespace HabisRPG.Utilities
{
    public static class MathHelpers
    {
        // ── DAMAGE CALCULATIONS ──

        /// <summary>
        /// GDD Section 3.3: Physical Damage Formula
        /// Damage = (BaseDmg × (STR / 10)) × (1 + CritBonus) × (1 - EnemyDEF/200)
        /// </summary>
        public static float CalculatePhysicalDamage(float baseDamage, int attackerSTR, int defenderDEF, bool isCrit)
        {
            float damage = baseDamage * (attackerSTR / 10f);
            if (isCrit) damage *= 1.5f;
            float defReduction = Mathf.Max(1f - (defenderDEF / 200f), 0.1f);
            return Mathf.Max(1f, damage * defReduction);
        }

        /// <summary>
        /// GDD Section 3.3: Magical Damage Formula
        /// Damage = (BaseDmg × (INT / 10)) × (1 + ElementBonus) × (1 - EnemyRES/200)
        /// </summary>
        public static float CalculateMagicalDamage(float baseDamage, int attackerINT, int defenderRES, float elementBonus = 0f)
        {
            float damage = baseDamage * (attackerINT / 10f);
            damage *= (1f + elementBonus);
            float resReduction = Mathf.Max(1f - (defenderRES / 200f), 0.1f);
            return Mathf.Max(1f, damage * resReduction);
        }

        /// <summary>
        /// Crit chance check: CRI stat = CRI% chance
        /// </summary>
        public static bool RollCritical(int criStat) =>
            Random.Range(0, 100) < criStat;

        /// <summary>
        /// Accuracy check: (BaseACC - EnemyEVA + 80) / 100, clamped 10%-95%
        /// </summary>
        public static bool RollHit(int attackerACC, int defenderEVA)
        {
            float hitChance = Mathf.Clamp((attackerACC - defenderEVA + 80f) / 100f, 0.10f, 0.95f);
            return Random.value <= hitChance;
        }

        // ── PROGRESSION ──

        /// <summary>
        /// HP = 50 + (VIT × 10) + (Level × 5)
        /// </summary>
        public static int CalculateMaxHP(int vit, int level) =>
            50 + (vit * 10) + (level * 5);

        /// <summary>
        /// XP required per level (GDD Section 5.1)
        /// </summary>
        public static int XPForLevel(int level)
        {
            if (level <= 10) return 100;
            if (level <= 30) return 300;
            if (level <= 60) return 800;
            if (level <= 90) return 2000;
            return 5000;
        }

        /// <summary>
        /// Total XP from Level 1 to target level
        /// </summary>
        public static int TotalXPToLevel(int targetLevel)
        {
            int total = 0;
            for (int i = 1; i < targetLevel; i++)
                total += XPForLevel(i);
            return total;
        }

        /// <summary>
        /// Get level progress as 0-1 float
        /// </summary>
        public static float GetLevelProgress(int currentXP, int level) =>
            (float)currentXP / XPForLevel(level);

        // ── ECONOMY ──

        /// <summary>
        /// Item sell price = buy price / 3 (GDD Section 7.3)
        /// </summary>
        public static int GetSellPrice(int buyPrice) =>
            Mathf.Max(1, buyPrice / 3);

        /// <summary>
        /// Enchant failure rate (GDD Section 4.5)
        /// +1 to +6: 0%, +7: 10%, +8: 20%, +9: 35%, +10: 50%
        /// </summary>
        public static float GetEnchantFailRate(int targetLevel)
        {
            return targetLevel switch
            {
                <= 6 => 0f,
                7 => 0.10f,
                8 => 0.20f,
                9 => 0.35f,
                10 => 0.50f,
                _ => 1f
            };
        }

        /// <summary>
        /// Legendary craft failure rate: 30% base, -5% per blacksmith rep
        /// </summary>
        public static float GetLegendaryCraftFailRate(int blacksmithRep) =>
            Mathf.Max(0.05f, 0.30f - (blacksmithRep * 0.05f));

        // ── COMBAT UTILITY ──

        /// <summary>
        /// Flee success chance (GDD Section 3.2)
        /// Base 30% + (PlayerSPD - EnemySPD) × 2%, clamped 10%-90%
        /// </summary>
        public static float FleeChance(int playerSPD, int enemyAvgSPD) =>
            Mathf.Clamp(0.3f + (playerSPD - enemyAvgSPD) * 0.02f, 0.10f, 0.90f);

        /// <summary>
        /// Elemental effectiveness multiplier
        /// </summary>
        public static float GetElementMultiplier(ElementType attack, ElementType defense)
        {
            // Fire > Ice > Lightning > Fire (triangle)
            // Poison and Void are neutral
            if (attack == ElementType.None || defense == ElementType.None) return 1f;
            if (attack == defense) return 0.5f; // Resist

            return (attack, defense) switch
            {
                (ElementType.Fire, ElementType.Ice) => 1.5f,
                (ElementType.Ice, ElementType.Lightning) => 1.5f,
                (ElementType.Lightning, ElementType.Fire) => 1.5f,
                (ElementType.Void, _) => 1.25f, // Void is slightly effective vs all
                _ => 1f
            };
        }

        // ── GENERAL ──

        /// <summary>
        /// Weighted random selection
        /// </summary>
        public static int WeightedRandom(float[] weights)
        {
            float total = 0f;
            foreach (var w in weights) total += w;

            float roll = Random.value * total;
            float cumulative = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return i;
            }

            return weights.Length - 1;
        }

        /// <summary>
        /// Smooth curve for difficulty scaling (S-curve)
        /// Returns 0-1 based on input 0-1
        /// </summary>
        public static float SmoothStep(float t) =>
            t * t * (3f - 2f * t);
    }
}
