// ============================================================================
// Habis RPG — Retention Manager
// Daily login, daily quests, returning player bonus, streak tracking
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using HabisRPG.Core;

namespace HabisRPG.Managers
{

    [Serializable]
    public class RetentionData
    {
        public int LoginStreak;          // 1-7, resets weekly
        public int TotalDaysPlayed;
        public DateTime LastLoginDate;
        public List<DailyQuest> ActiveQuests = new();
        public bool DailyBossDefeated;
        public bool DailyChestOpened;
    }


    [Serializable]
    public class DailyQuest
    {
        public string Id;
        public string Description;
        public DailyQuestType Type;
        public int TargetCount;
        public int CurrentCount;
        public int GoldReward;
        public int XPReward;
        public bool IsComplete => CurrentCount >= TargetCount;
    }

    public enum DailyQuestType
    {
        DefeatEnemies,
        CraftItem,
        UseSkills,
        WinBattles,
        CollectItems
    }

    public class RetentionManager
    {
        public RetentionData Data { get; private set; }

        // ── LOGIN REWARD TABLE (GDD Section 9.1) ──
        private static readonly Dictionary<int, (int gold, int voidShards, string bonus)> LoginRewards = new()
        {
            [1] = (200, 0, "Potion x2"),
            [2] = (400, 0, "Potion x3"),
            [3] = (600, 0, "Elixir x1"),
            [4] = (800, 1, "Potion x5"),
            [5] = (1000, 1, "Elixir x2"),
            [6] = (1500, 2, "Rare Material x1"),
            [7] = (3000, 5, "Guaranteed Rare Item Box")
        };

        public RetentionManager()
        {
            Data = new RetentionData { LastLoginDate = DateTime.MinValue };
        }

        public RetentionManager(RetentionData savedData)
        {
            Data = savedData;
        }

        // ── DAILY LOGIN ──
        public class LoginResult
        {
            public bool IsNewDay;
            public int StreakDay;
            public int GoldReward;
            public int VoidShardReward;
            public string BonusItem;
            public bool IsReturningPlayer;
            public int DaysMissed;
        }

        public LoginResult ProcessLogin()
        {
            var today = DateTime.UtcNow.Date;
            var result = new LoginResult();

            if (Data.LastLoginDate.Date == today)
            {
                result.IsNewDay = false;
                result.StreakDay = Data.LoginStreak;
                return result;
            }

            result.IsNewDay = true;
            Data.TotalDaysPlayed++;

            // Check streak continuity
            var daysSinceLastLogin = (today - Data.LastLoginDate.Date).Days;

            if (daysSinceLastLogin == 1)
            {
                // Consecutive day
                Data.LoginStreak = (Data.LoginStreak % 7) + 1;
            }
            else if (daysSinceLastLogin >= 3)
            {
                // Returning player (GDD Section 9.4)
                result.IsReturningPlayer = true;
                result.DaysMissed = daysSinceLastLogin;
                Data.LoginStreak = 1;
            }
            else
            {
                // Missed 1 day — reset streak but no returning bonus
                Data.LoginStreak = 1;
            }

            // Get rewards for current streak day
            var (gold, voidShards, bonus) = LoginRewards[Data.LoginStreak];
            result.StreakDay = Data.LoginStreak;
            result.GoldReward = gold;
            result.VoidShardReward = voidShards;
            result.BonusItem = bonus;

            // Reset daily flags
            Data.DailyBossDefeated = false;
            Data.DailyChestOpened = false;
            Data.LastLoginDate = today;

            // Generate new daily quests
            GenerateDailyQuests();

            return result;
        }

        // ── DAILY QUESTS (GDD Section 9.1) ──
        private void GenerateDailyQuests()
        {
            Data.ActiveQuests.Clear();

            // Always 3 quests
            Data.ActiveQuests.Add(CreateQuest(DailyQuestType.DefeatEnemies, "Defeat 10 enemies", 10, 300, 100));
            Data.ActiveQuests.Add(CreateQuest(DailyQuestType.WinBattles, "Win 5 battles", 5, 500, 150));

            // Rotating 3rd quest
            var rotatingTypes = new[]
            {
                (DailyQuestType.CraftItem, "Craft 1 item", 1, 400, 120),
                (DailyQuestType.UseSkills, "Use 20 skills in combat", 20, 350, 100),
                (DailyQuestType.CollectItems, "Collect 5 items", 5, 300, 80),
            };

            int dayIndex = Data.TotalDaysPlayed % rotatingTypes.Length;
            var (type, desc, target, gold, xp) = rotatingTypes[dayIndex];
            Data.ActiveQuests.Add(CreateQuest(type, desc, target, gold, xp));
        }

        private DailyQuest CreateQuest(DailyQuestType type, string desc, int target, int gold, int xp)
        {
            return new DailyQuest
            {
                Id = $"daily_{type}_{Data.TotalDaysPlayed}",
                Type = type,
                Description = desc,
                TargetCount = target,
                CurrentCount = 0,
                GoldReward = gold,
                XPReward = xp
            };
        }

        /// <summary>
        /// Update quest progress. Call this when relevant game events happen.
        /// </summary>
        public List<DailyQuest> UpdateQuestProgress(DailyQuestType type, int amount = 1)
        {
            var completedQuests = new List<DailyQuest>();

            foreach (var quest in Data.ActiveQuests)
            {
                if (quest.Type == type && !quest.IsComplete)
                {
                    quest.CurrentCount += amount;
                    if (quest.IsComplete)
                        completedQuests.Add(quest);
                }
            }

            return completedQuests;
        }

        /// <summary>
        /// Get returning player bonus (GDD Section 9.4)
        /// </summary>
        public (int gold, int xpBoostPercent, string items) GetReturningPlayerBonus(int daysMissed)
        {
            // Scale bonus with days missed (capped at 7 days worth)
            int cappedDays = Mathf.Min(daysMissed, 7);
            int gold = cappedDays * 500;
            int xpBoost = 25; // 25% XP boost for returning players
            string items = cappedDays >= 5 ? "Elixir x5, Rare Material x2" : "Potion x10, Elixir x2";

            return (gold, xpBoost, items);
        }
    }
}
