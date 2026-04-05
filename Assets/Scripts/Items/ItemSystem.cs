// ============================================================================
// Habis RPG — Item System
// Item definitions, inventory, equipment, loot generation
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HabisRPG.Core;

namespace HabisRPG.Items
{
    // ── ITEM EFFECT ──
    [Serializable]
    public class ItemEffect
    {
        public string Name;
        public string Description;
        public bool IsPassive;          // true = always active, false = triggered
        public StatusEffectType? AppliesStatus;
        public float TriggerChance;     // 0-1 for passive on-hit effects
        public float Value;             // damage, heal amount, etc.
    }

    // ── ITEM DATA ──
    [Serializable]
    public class ItemData
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemType Type;
        public WeaponType WeaponSubType;
        public ItemRarity Rarity;
        public EquipSlot Slot;
        public int Level;
        public int UpgradeLevel;        // +0 to +10

        // Stats
        public int BaseDamage;          // Weapons only
        public int BaseDefense;         // Armor only
        public CharacterStats BonusStats = new();

        // Effects
        public List<ItemEffect> Effects = new();

        // Economy
        public int BuyPrice;
        public int SellPrice => BuyPrice / 3;

        // Crafting
        public bool IsBlueprint;        // Legendary crafting blueprint

        /// <summary>
        /// Returns stats with upgrade bonus applied (+5% per upgrade level)
        /// </summary>
        public CharacterStats GetUpgradedStats()
        {
            float multiplier = 1f + (UpgradeLevel * 0.05f);
            var stats = BonusStats.Clone();
            stats.STR = Mathf.RoundToInt(stats.STR * multiplier);
            stats.INT = Mathf.RoundToInt(stats.INT * multiplier);
            stats.DEX = Mathf.RoundToInt(stats.DEX * multiplier);
            stats.VIT = Mathf.RoundToInt(stats.VIT * multiplier);
            stats.SPD = Mathf.RoundToInt(stats.SPD * multiplier);
            stats.CRI = Mathf.RoundToInt(stats.CRI * multiplier);
            stats.MAG = Mathf.RoundToInt(stats.MAG * multiplier);
            stats.DEF = Mathf.RoundToInt(stats.DEF * multiplier);
            stats.ACC = Mathf.RoundToInt(stats.ACC * multiplier);
            stats.EVA = Mathf.RoundToInt(stats.EVA * multiplier);
            return stats;
        }

        public int GetUpgradedDamage()
        {
            return Mathf.RoundToInt(BaseDamage * (1f + UpgradeLevel * 0.05f));
        }

        public int GetUpgradedDefense()
        {
            return Mathf.RoundToInt(BaseDefense * (1f + UpgradeLevel * 0.05f));
        }
    }

    // ── INVENTORY ──
    public class Inventory
    {
        public List<ItemData> Items { get; private set; } = new();
        public int MaxSlots { get; set; } = 100; // 200 for VIP

        public bool IsFull => Items.Count >= MaxSlots;

        public bool AddItem(ItemData item)
        {
            if (IsFull) return false;
            Items.Add(item);
            return true;
        }

        public bool RemoveItem(string itemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return false;
            Items.Remove(item);
            return true;
        }

        public ItemData GetItem(string itemId) =>
            Items.FirstOrDefault(i => i.Id == itemId);

        public List<ItemData> GetItemsByType(ItemType type) =>
            Items.Where(i => i.Type == type).ToList();

        public List<ItemData> GetItemsByRarity(ItemRarity rarity) =>
            Items.Where(i => i.Rarity == rarity).ToList();

        public List<ItemData> GetEquippableItems(EquipSlot slot) =>
            Items.Where(i => i.Slot == slot).ToList();

        public void SortByRarity() =>
            Items = Items.OrderByDescending(i => i.Rarity).ThenBy(i => i.Name).ToList();

        public void SortByLevel() =>
            Items = Items.OrderByDescending(i => i.Level).ThenBy(i => i.Name).ToList();
    }

    // ── LOOT GENERATOR (GDD Section 4.4) ──
    public static class LootGenerator
    {
        /// <summary>
        /// Generate a loot drop based on enemy tier and level
        /// </summary>
        public static ItemData GenerateDrop(EnemyTier tier, int enemyLevel)
        {
            ItemRarity rarity = RollRarity(tier);

            // Legendary only from Level 50+ (GDD rule)
            if (rarity == ItemRarity.Legendary && enemyLevel < 50)
                rarity = ItemRarity.Epic;

            // Item level = enemy level ± 2-5 variance
            int itemLevel = Mathf.Clamp(
                enemyLevel + UnityEngine.Random.Range(-5, 6),
                1, 99
            );

            return GenerateItem(rarity, itemLevel);
        }

        private static ItemRarity RollRarity(EnemyTier tier)
        {
            float roll = UnityEngine.Random.value * 100f;

            return tier switch
            {
                // GDD Section 4.4: Drop Rates by Enemy Difficulty
                EnemyTier.Basic => roll < 60 ? ItemRarity.Common :
                                   roll < 90 ? ItemRarity.Rare : ItemRarity.Epic,

                EnemyTier.Advanced => roll < 30 ? ItemRarity.Common :
                                     roll < 80 ? ItemRarity.Rare : ItemRarity.Epic,

                EnemyTier.Elite => roll < 10 ? ItemRarity.Rare :
                                  roll < 70 ? ItemRarity.Epic : ItemRarity.Legendary,

                EnemyTier.Boss or
                EnemyTier.SecretBoss => roll < 20 ? ItemRarity.Rare :
                                       roll < 70 ? ItemRarity.Epic : ItemRarity.Legendary,

                _ => ItemRarity.Common
            };
        }

        private static ItemData GenerateItem(ItemRarity rarity, int level)
        {
            // Random item type
            var types = new[] {
                ItemType.Weapon, ItemType.Helmet, ItemType.Chest,
                ItemType.Legs, ItemType.Feet, ItemType.Gloves,
                ItemType.Ring, ItemType.Amulet
            };
            var type = types[UnityEngine.Random.Range(0, types.Length)];

            var item = new ItemData
            {
                Id = $"item_{Guid.NewGuid().ToString("N")[..8]}",
                Rarity = rarity,
                Level = level,
                Type = type,
                UpgradeLevel = 0
            };

            // Set equip slot
            item.Slot = type switch
            {
                ItemType.Weapon => EquipSlot.MainHand,
                ItemType.Helmet => EquipSlot.Helmet,
                ItemType.Chest => EquipSlot.Chest,
                ItemType.Legs => EquipSlot.Legs,
                ItemType.Feet => EquipSlot.Feet,
                ItemType.Gloves => EquipSlot.Gloves,
                ItemType.Ring => EquipSlot.Ring1,
                ItemType.Amulet => EquipSlot.Amulet,
                _ => EquipSlot.MainHand
            };

            // Base values scale with level
            if (type == ItemType.Weapon)
            {
                item.BaseDamage = 5 + level * 2 + (int)rarity * 5;
                item.WeaponSubType = (WeaponType)UnityEngine.Random.Range(0, 6);
                item.Name = $"{rarity} {item.WeaponSubType}";
            }
            else
            {
                item.BaseDefense = 3 + level + (int)rarity * 3;
                item.Name = $"{rarity} {type}";
            }

            // Secondary stats based on rarity (GDD Section 4.3)
            int secondaryCount = rarity switch
            {
                ItemRarity.Common => UnityEngine.Random.Range(1, 3),
                ItemRarity.Rare => UnityEngine.Random.Range(2, 4),
                ItemRarity.Epic => UnityEngine.Random.Range(3, 5),
                ItemRarity.Legendary => UnityEngine.Random.Range(4, 6),
                _ => 1
            };

            var availableStats = Enum.GetValues(typeof(StatType)).Cast<StatType>().ToList();
            for (int i = 0; i < secondaryCount && availableStats.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, availableStats.Count);
                var stat = availableStats[idx];
                availableStats.RemoveAt(idx);

                int value = (level / 5 + 1) * ((int)rarity + 1);
                item.BonusStats.AddStat(stat, value);
            }

            // Price calculation
            item.BuyPrice = (level * 10 + (int)rarity * 50) * (1 + item.BonusStats.STR + item.BonusStats.INT);
            item.BuyPrice = Mathf.Max(item.BuyPrice, 10);

            return item;
        }
    }

    // ── CRAFTING SYSTEM (GDD Section 4.5) ──
    public static class CraftingSystem
    {
        public struct CraftResult
        {
            public bool Success;
            public ItemData ResultItem;
            public string Message;
        }

        /// <summary>
        /// Craft Rare: 3 Common + 500 Gold
        /// </summary>
        public static CraftResult CraftRare(List<ItemData> materials, int playerGold)
        {
            if (materials.Count < 3 || materials.Any(m => m.Rarity != ItemRarity.Common))
                return new CraftResult { Success = false, Message = "Need 3 Common items" };
            if (playerGold < 500)
                return new CraftResult { Success = false, Message = "Need 500 Gold" };

            int avgLevel = (int)materials.Average(m => m.Level);
            var result = LootGenerator.GenerateDrop(EnemyTier.Advanced, avgLevel);
            result.Rarity = ItemRarity.Rare;

            return new CraftResult { Success = true, ResultItem = result, Message = "Rare item crafted!" };
        }

        /// <summary>
        /// Craft Epic: 3 Rare + 5,000 Gold
        /// </summary>
        public static CraftResult CraftEpic(List<ItemData> materials, int playerGold)
        {
            if (materials.Count < 3 || materials.Any(m => m.Rarity != ItemRarity.Rare))
                return new CraftResult { Success = false, Message = "Need 3 Rare items" };
            if (playerGold < 5000)
                return new CraftResult { Success = false, Message = "Need 5,000 Gold" };

            int avgLevel = (int)materials.Average(m => m.Level);
            var result = LootGenerator.GenerateDrop(EnemyTier.Elite, avgLevel);
            result.Rarity = ItemRarity.Epic;

            return new CraftResult { Success = true, ResultItem = result, Message = "Epic item crafted!" };
        }

        /// <summary>
        /// Craft Legendary: Blueprint + 3 Epic (same type) + 5 Void Shards + 50,000 Gold
        /// 30% failure rate — consumes Gold + Void Shards on fail, returns Epics
        /// </summary>
        public static CraftResult CraftLegendary(
            ItemData blueprint,
            List<ItemData> epicItems,
            int voidShards,
            int playerGold,
            int blacksmithRepLevel = 0)
        {
            if (blueprint == null || !blueprint.IsBlueprint)
                return new CraftResult { Success = false, Message = "Need a Legendary Blueprint" };
            if (epicItems.Count < 3 || epicItems.Any(e => e.Rarity != ItemRarity.Epic))
                return new CraftResult { Success = false, Message = "Need 3 Epic items" };
            if (voidShards < 5)
                return new CraftResult { Success = false, Message = "Need 5 Void Shards" };
            if (playerGold < 50000)
                return new CraftResult { Success = false, Message = "Need 50,000 Gold" };

            // Failure check: 30% base, -5% per blacksmith rep level
            float failChance = Mathf.Max(0.05f, 0.30f - (blacksmithRepLevel * 0.05f));

            if (UnityEngine.Random.value < failChance)
            {
                return new CraftResult
                {
                    Success = false,
                    Message = "Crafting FAILED! Gold and Void Shards consumed. Epic items returned."
                };
            }

            int avgLevel = (int)epicItems.Average(e => e.Level);
            var result = LootGenerator.GenerateDrop(EnemyTier.SecretBoss, avgLevel);
            result.Rarity = ItemRarity.Legendary;

            return new CraftResult { Success = true, ResultItem = result, Message = "LEGENDARY item crafted!" };
        }

        /// <summary>
        /// Enchant: +5 ore → Item +1 (max +10)
        /// Failure rate increases from +7 onwards
        /// </summary>
        public static CraftResult EnchantItem(ItemData item, int oreCount)
        {
            if (item.UpgradeLevel >= 10)
                return new CraftResult { Success = false, Message = "Already at max upgrade (+10)" };
            if (oreCount < 5)
                return new CraftResult { Success = false, Message = "Need 5 ore" };

            int nextLevel = item.UpgradeLevel + 1;

            // Failure rate: 0% for +1-6, scaling from +7
            float failChance = nextLevel switch
            {
                <= 6 => 0f,
                7 => 0.10f,
                8 => 0.20f,
                9 => 0.35f,
                10 => 0.50f,
                _ => 1f
            };

            if (UnityEngine.Random.value < failChance)
            {
                return new CraftResult
                {
                    Success = false,
                    Message = $"Enchanting to +{nextLevel} FAILED! Ore consumed."
                };
            }

            item.UpgradeLevel = nextLevel;
            return new CraftResult
            {
                Success = true,
                ResultItem = item,
                Message = $"Item enchanted to +{nextLevel}!"
            };
        }
    }
}
