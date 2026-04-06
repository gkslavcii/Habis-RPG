// ============================================================================
// Habis RPG — Economy Manager
// Gold, Void Shards, Sigil Tokens, shop system, anti-inflation
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using HabisRPG.Core;

namespace HabisRPG.Economy
{

    [Serializable]
    public class WalletData
    {
        public int Gold;
        public int VoidShards;
        public int SigilTokens;

        // Daily caps (GDD Section 7.4: Anti-inflation)
        public int GoldEarnedToday;
        public int AdsWatchedToday;
        public DateTime LastResetDate;
    }

    public class EconomyManager
    {
        public WalletData Wallet { get; private set; }

        // Daily caps
        public const int MAX_ADS_PER_DAY = 15;
        public const int MAX_DAILY_GOLD_FROM_REPEATABLE = 50000;

        public EconomyManager()
        {
            Wallet = new WalletData { LastResetDate = DateTime.UtcNow.Date };
        }

        public EconomyManager(WalletData savedWallet)
        {
            Wallet = savedWallet;
            CheckDailyReset();
        }

        // ── DAILY RESET ──
        private void CheckDailyReset()
        {
            if (DateTime.UtcNow.Date > Wallet.LastResetDate)
            {
                Wallet.GoldEarnedToday = 0;
                Wallet.AdsWatchedToday = 0;
                Wallet.LastResetDate = DateTime.UtcNow.Date;
            }
        }

        // ── CURRENCY OPERATIONS ──
        public bool CanAfford(CurrencyType type, int amount)
        {
            return type switch
            {
                CurrencyType.Gold => Wallet.Gold >= amount,
                CurrencyType.VoidShards => Wallet.VoidShards >= amount,
                CurrencyType.SigilTokens => Wallet.SigilTokens >= amount,
                _ => false
            };
        }

        public bool TrySpend(CurrencyType type, int amount)
        {
            if (!CanAfford(type, amount)) return false;

            switch (type)
            {
                case CurrencyType.Gold: Wallet.Gold -= amount; break;
                case CurrencyType.VoidShards: Wallet.VoidShards -= amount; break;
                case CurrencyType.SigilTokens: Wallet.SigilTokens -= amount; break;
            }
            return true;
        }

        public void AddCurrency(CurrencyType type, int amount)
        {
            switch (type)
            {
                case CurrencyType.Gold:
                    Wallet.Gold += amount;
                    Wallet.GoldEarnedToday += amount;
                    break;
                case CurrencyType.VoidShards:
                    Wallet.VoidShards += amount;
                    break;
                case CurrencyType.SigilTokens:
                    Wallet.SigilTokens += amount;
                    break;
            }
        }

        // ── GOLD GENERATION (GDD Section 7.2) ──
        /// <summary>
        /// Calculate gold drop from enemy based on player level
        /// </summary>
        public int CalculateGoldDrop(int enemyLevel, EnemyTier tier)
        {
            int baseGold = enemyLevel * 3;

            float tierMultiplier = tier switch
            {
                EnemyTier.Basic => 1.0f,
                EnemyTier.Advanced => 1.5f,
                EnemyTier.Elite => 2.5f,
                EnemyTier.Boss => 5.0f,
                EnemyTier.SecretBoss => 8.0f,
                _ => 1.0f
            };

            int gold = Mathf.RoundToInt(baseGold * tierMultiplier);

            // Random variance ±20%
            float variance = UnityEngine.Random.Range(0.8f, 1.2f);
            gold = Mathf.RoundToInt(gold * variance);

            // Daily cap check for repeatable content
            if (Wallet.GoldEarnedToday + gold > MAX_DAILY_GOLD_FROM_REPEATABLE)
            {
                gold = Mathf.Max(0, MAX_DAILY_GOLD_FROM_REPEATABLE - Wallet.GoldEarnedToday);
            }

            return gold;
        }

        // ── RESPEC COST (GDD Section 5.3) ──
        public int GetRespecCost(int playerLevel)
        {
            if (playerLevel <= 20) return 1000;
            if (playerLevel <= 40) return 5000;
            if (playerLevel <= 60) return 15000;
            if (playerLevel <= 80) return 25000;
            return 50000;
        }

        // ── REPAIR COSTS (GDD Section 7.4) ──
        public int GetRepairCost(int itemLevel, int upgradeLevel)
        {
            int baseCost = itemLevel * 5;
            float upgradeMultiplier = 1f + (upgradeLevel * 0.3f);
            return Mathf.RoundToInt(baseCost * upgradeMultiplier);
        }

        // ── AD SYSTEM (GDD Section 8.2) ──
        public bool CanWatchAd()
        {
            CheckDailyReset();
            return Wallet.AdsWatchedToday < MAX_ADS_PER_DAY;
        }

        public void RecordAdWatch()
        {
            Wallet.AdsWatchedToday++;
        }

        /// <summary>
        /// Ad reward types and their daily limits
        /// </summary>
        public enum AdRewardType
        {
            BonusLoot,      // Max 5/day
            GoldBoost,      // Max 3/day
            Revive,         // Max 2/day
            DailyChest      // Max 1/day
        }

        private readonly Dictionary<AdRewardType, int> _adCounts = new()
        {
            [AdRewardType.BonusLoot] = 0,
            [AdRewardType.GoldBoost] = 0,
            [AdRewardType.Revive] = 0,
            [AdRewardType.DailyChest] = 0,
        };

        private static readonly Dictionary<AdRewardType, int> AdLimits = new()
        {
            [AdRewardType.BonusLoot] = 5,
            [AdRewardType.GoldBoost] = 3,
            [AdRewardType.Revive] = 2,
            [AdRewardType.DailyChest] = 1,
        };

        public bool CanWatchAdForReward(AdRewardType type)
        {
            return CanWatchAd() && _adCounts[type] < AdLimits[type];
        }

        public void RecordAdReward(AdRewardType type)
        {
            _adCounts[type]++;
            RecordAdWatch();
        }
    }
}
