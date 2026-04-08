// ============================================================================
// Habis RPG — Game Manager (Singleton)
// Central controller that initializes and coordinates all systems
// ============================================================================

using System;
using UnityEngine;
using HabisRPG.Core;
using HabisRPG.Character;
using HabisRPG.Combat;
using HabisRPG.Items;
using HabisRPG.Party;
using HabisRPG.Economy;
using HabisRPG.Skills;

namespace HabisRPG.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── SUBSYSTEMS ──
        public HabisCharacter PlayerCharacter { get; private set; }
        public PartyManager Party { get; private set; }
        public CombatEngine Combat { get; private set; }
        public EconomyManager Economy { get; private set; }
        public Inventory PlayerInventory { get; private set; }

        // ── GAME STATE ──
        public Region CurrentRegion { get; set; } = Region.CursedForest;
        public bool IsInCombat { get; private set; }
        public bool IsVIP { get; set; }

        // Events
        public event Action<Region> OnRegionChanged;
        public event Action<int> OnLevelUp;
        public event Action<string> OnCompanionRecruited;

        // ── SINGLETON SETUP ──
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── NEW GAME ──
        public void StartNewGame(string playerName, CharacterClass playerClass)
        {
            // Initialize player
            PlayerCharacter = new HabisCharacter(playerName, playerClass);
            PlayerInventory = new Inventory();
            Economy = new EconomyManager();
            Party = new PartyManager(PlayerCharacter);
            Combat = new CombatEngine();

            // VIP inventory bonus
            if (IsVIP)
                PlayerInventory.MaxSlots = 200;

            // Auto-recruit Kael (story companion at Level 1)
            Party.RecruitCompanion("comp_kael");

            // Give starting gear
            GiveStartingEquipment(playerClass);

            // Unlock initial tier-1 skills for the chosen class
            foreach (var s in SkillDatabase.GetForClass(playerClass))
            {
                if (s.UnlockLevel == 1 && (s.PrerequisiteSkillIds?.Length ?? 0) == 0)
                {
                    PlayerCharacter.Data.UnlockedSkillIds.Add(s.Id);
                    PlayerCharacter.Data.SkillLevels[s.Id] = 1;
                }
            }

            CurrentRegion = Region.CursedForest;

            Debug.Log($"[Habis RPG] New game started: {playerName} ({playerClass})");
        }

        private void GiveStartingEquipment(CharacterClass playerClass)
        {
            // Generate level-appropriate common gear
            var weapon = new ItemData
            {
                Id = "start_weapon",
                Name = playerClass switch
                {
                    CharacterClass.Warrior => "Rusty Sword",
                    CharacterClass.Mage => "Worn Staff",
                    CharacterClass.Rogue => "Dull Daggers",
                    _ => "Basic Weapon"
                },
                Type = ItemType.Weapon,
                Rarity = ItemRarity.Common,
                Slot = EquipSlot.MainHand,
                Level = 1,
                BaseDamage = 8,
                BonusStats = new CharacterStats()
            };

            var armor = new ItemData
            {
                Id = "start_armor",
                Name = "Tattered Chest",
                Type = ItemType.Chest,
                Rarity = ItemRarity.Common,
                Slot = EquipSlot.Chest,
                Level = 1,
                BaseDefense = 5,
                BonusStats = new CharacterStats()
            };

            PlayerInventory.AddItem(weapon);
            PlayerInventory.AddItem(armor);

            // Auto-equip
            PlayerCharacter.Data.EquippedItems[EquipSlot.MainHand] = weapon.Id;
            PlayerCharacter.Data.EquippedItems[EquipSlot.Chest] = armor.Id;
        }

        // ── COMBAT FLOW ──
        public void StartCombat(
            System.Collections.Generic.List<HabisCharacter> enemies,
            bool isBossFight = false)
        {
            if (IsInCombat) return;

            IsInCombat = true;
            var party = Party.GetCombatParty(isBossFight);
            Combat.InitCombat(party, enemies);

            // Subscribe to combat end
            Combat.OnCombatEnd += HandleCombatEnd;
        }

        private void HandleCombatEnd(object sender, CombatEventArgs e)
        {
            Combat.OnCombatEnd -= HandleCombatEnd;
            IsInCombat = false;

            if (Combat.Result == CombatResult.Victory)
            {
                // Calculate rewards
                int totalXP = 0;
                int totalGold = 0;

                foreach (var unit in Combat.GetEnemyUnits())
                {
                    int level = unit.Character.Data.Level;
                    totalXP += CalculateEnemyXP(level);
                    totalGold += Economy.CalculateGoldDrop(level, EnemyTier.Basic);
                }

                // Distribute XP (handles bench 50%)
                Party.DistributeBattleXP(totalXP);

                // Add gold
                Economy.AddCurrency(CurrencyType.Gold, totalGold);

                // Generate loot
                foreach (var unit in Combat.GetEnemyUnits())
                {
                    if (UnityEngine.Random.value < 0.7f) // 70% drop chance
                    {
                        var loot = LootGenerator.GenerateDrop(
                            EnemyTier.Basic,
                            unit.Character.Data.Level
                        );
                        PlayerInventory.AddItem(loot);
                    }
                }

                Debug.Log($"[Habis RPG] Victory! +{totalXP} XP, +{totalGold} Gold");
            }
            else if (Combat.Result == CombatResult.Defeat)
            {
                Debug.Log("[Habis RPG] Defeat! Returning to last checkpoint...");
                // TODO: Implement death/respawn logic
            }
        }

        private int CalculateEnemyXP(int enemyLevel)
        {
            // Base XP scales with enemy level
            return 10 + (enemyLevel * 3);
        }

        // ── REGION TRAVEL ──
        public bool TravelToRegion(Region region)
        {
            // Check level requirements
            int requiredLevel = region switch
            {
                Region.CursedForest => 1,
                Region.RottingSwamp => 10,
                Region.ShatteredMountain => 20,
                Region.AbyssalDungeon => 30,
                Region.AncientRuins => 50,
                Region.VoidAbyss => 70,
                _ => 1
            };

            if (PlayerCharacter.Data.Level < requiredLevel) return false;

            CurrentRegion = region;
            OnRegionChanged?.Invoke(region);

            // Check for companion recruitment opportunities
            var recruits = Party.GetAvailableRecruits(
                PlayerCharacter.Data.Level, region);
            foreach (var r in recruits)
            {
                if (r.JoinRegion == region && r.RecruitMethod == "story")
                {
                    Party.RecruitCompanion(r.Id);
                    OnCompanionRecruited?.Invoke(r.Name);
                }
            }

            return true;
        }

        // ── SAVE / LOAD ──
    
        [Serializable]
        public class SaveData
        {
            public CharacterData PlayerData;
            public System.Collections.Generic.List<CharacterData> ActiveCompanionData;
            public System.Collections.Generic.List<CharacterData> BenchCompanionData;
            public System.Collections.Generic.List<string> RecruitedIds;
            public System.Collections.Generic.List<ItemData> InventoryItems;
            public WalletData Wallet;
            public Region CurrentRegion;
            public bool IsVIP;
            public string SaveTime;
        }

        public string SaveGame()
        {
            var save = new SaveData
            {
                PlayerData = PlayerCharacter.Data,
                ActiveCompanionData = new(),
                BenchCompanionData = new(),
                RecruitedIds = new(Party.RecruitedCompanionIds),
                InventoryItems = new(PlayerInventory.Items),
                Wallet = Economy.Wallet,
                CurrentRegion = CurrentRegion,
                IsVIP = IsVIP,
                SaveTime = DateTime.UtcNow.ToString("O")
            };

            foreach (var c in Party.ActiveCompanions)
                save.ActiveCompanionData.Add(c.Data);
            foreach (var c in Party.BenchCompanions)
                save.BenchCompanionData.Add(c.Data);

            string json = JsonUtility.ToJson(save, true);

            // In production: write to Application.persistentDataPath
            Debug.Log("[Habis RPG] Game saved!");
            return json;
        }

        public void LoadGame(string json)
        {
            var save = JsonUtility.FromJson<SaveData>(json);

            PlayerCharacter = new HabisCharacter(save.PlayerData);
            PlayerInventory = new Inventory();
            ItemRegistry.Clear();
            foreach (var item in save.InventoryItems)
                PlayerInventory.AddItem(item);

            Economy = new EconomyManager(save.Wallet);
            Party = new PartyManager(PlayerCharacter);

            foreach (var cd in save.ActiveCompanionData)
                Party.ActiveCompanions.Add(new HabisCharacter(cd));
            foreach (var cd in save.BenchCompanionData)
                Party.BenchCompanions.Add(new HabisCharacter(cd));

            Party.RecruitedCompanionIds.AddRange(save.RecruitedIds);

            CurrentRegion = save.CurrentRegion;
            IsVIP = save.IsVIP;
            Combat = new CombatEngine();

            Debug.Log("[Habis RPG] Game loaded!");
        }
    }
}
