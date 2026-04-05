using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;
using HabisRPG.Managers;
using HabisRPG.Character;
using HabisRPG.Combat;

namespace HabisRPG.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        // Root canvas
        private Canvas _canvas;
        private CanvasScaler _scaler;

        // Screens
        private GameObject _mainMenuScreen;
        private GameObject _classSelectScreen;
        private GameObject _gameHudScreen;
        private GameObject _combatScreen;
        private GameObject _inventoryScreen;
        private GameObject _regionScreen;

        // HUD elements
        private Text _hudName;
        private Text _hudLevel;
        private Text _hudHP;
        private Text _hudGold;
        private Text _hudRegion;
        private Text _hudXP;
        private Image _hpBar;
        private Image _xpBar;

        // Combat elements
        private Text _combatLog;
        private Text _playerHPText;
        private Text _enemyHPText;
        private Image _playerHPBar;
        private Image _enemyHPBar;
        private Text _enemyNameText;
        private Text _turnIndicator;
        private GameObject _combatButtons;
        private Text _energyText;

        // Colors
        private static readonly Color DARK_BG = new Color(0.14f, 0.12f, 0.13f);
        private static readonly Color PANEL_BG = new Color(0.2f, 0.18f, 0.22f);
        private static readonly Color ACCENT = new Color(0.6f, 0.2f, 0.8f);
        private static readonly Color ACCENT_LIGHT = new Color(0.7f, 0.4f, 0.9f);
        private static readonly Color GOLD_COLOR = new Color(1f, 0.84f, 0f);
        private static readonly Color HP_GREEN = new Color(0.2f, 0.8f, 0.2f);
        private static readonly Color HP_RED = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color XP_BLUE = new Color(0.3f, 0.5f, 1f);
        private static readonly Color BTN_COLOR = new Color(0.35f, 0.15f, 0.5f);
        private static readonly Color BTN_HOVER = new Color(0.45f, 0.25f, 0.6f);
        private static readonly Color TEXT_WHITE = new Color(0.95f, 0.93f, 0.9f);
        private static readonly Color TEXT_DIM = new Color(0.6f, 0.55f, 0.65f);

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize()
        {
            CreateCanvas();
            CreateMainMenu();
            CreateClassSelect();
            CreateGameHUD();
            CreateCombatScreen();
            CreateInventoryScreen();
            CreateRegionScreen();
            ShowScreen(_mainMenuScreen);
        }

        // ============================================================
        // CANVAS SETUP
        // ============================================================
        private void CreateCanvas()
        {
            var canvasGO = new GameObject("UICanvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            _scaler = canvasGO.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1080, 1920);
            _scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // ============================================================
        // MAIN MENU
        // ============================================================
        private void CreateMainMenu()
        {
            _mainMenuScreen = CreateScreen("MainMenu");

            // Title
            CreateText(_mainMenuScreen.transform, "HABIS RPG", 72, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 300), new Vector2(800, 120));

            CreateText(_mainMenuScreen.transform, "The Curse Awaits", 28, TextAnchor.MiddleCenter,
                TEXT_DIM, new Vector2(0, 220), new Vector2(600, 50));

            // Buttons
            CreateButton(_mainMenuScreen.transform, "New Game", new Vector2(0, -50),
                new Vector2(500, 90), () => ShowScreen(_classSelectScreen));

            CreateButton(_mainMenuScreen.transform, "Continue", new Vector2(0, -160),
                new Vector2(500, 90), () => TryContinueGame());

            // Version
            CreateText(_mainMenuScreen.transform, "v0.1.0", 20, TextAnchor.MiddleCenter,
                TEXT_DIM, new Vector2(0, -800), new Vector2(200, 40));
        }

        private void TryContinueGame()
        {
            string saveData = PlayerPrefs.GetString("HabisRPG_Save", "");
            if (!string.IsNullOrEmpty(saveData))
            {
                GameManager.Instance.LoadGame(saveData);
                ShowScreen(_gameHudScreen);
                UpdateHUD();
            }
            else
            {
                Debug.Log("[Habis RPG] No save data found");
            }
        }

        // ============================================================
        // CLASS SELECT
        // ============================================================
        private void CreateClassSelect()
        {
            _classSelectScreen = CreateScreen("ClassSelect");

            CreateText(_classSelectScreen.transform, "Choose Your Path", 52, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 400), new Vector2(800, 80));

            // Warrior
            var warriorPanel = CreatePanel(_classSelectScreen.transform, new Vector2(0, 150), new Vector2(700, 200));
            CreateText(warriorPanel.transform, "WARRIOR", 36, TextAnchor.MiddleCenter,
                new Color(0.9f, 0.3f, 0.3f), new Vector2(0, 50), new Vector2(600, 50));
            CreateText(warriorPanel.transform, "High STR & DEF. Energy: Rage.\nBuilds power through combat.", 22,
                TextAnchor.MiddleCenter, TEXT_DIM, new Vector2(0, -10), new Vector2(600, 60));
            CreateButton(warriorPanel.transform, "Select", new Vector2(0, -60), new Vector2(300, 60),
                () => StartGame(CharacterClass.Warrior));

            // Mage
            var magePanel = CreatePanel(_classSelectScreen.transform, new Vector2(0, -110), new Vector2(700, 200));
            CreateText(magePanel.transform, "MAGE", 36, TextAnchor.MiddleCenter,
                new Color(0.3f, 0.5f, 1f), new Vector2(0, 50), new Vector2(600, 50));
            CreateText(magePanel.transform, "High INT & MAG. Energy: Arcana.\nDevastating spells with crit bonus.", 22,
                TextAnchor.MiddleCenter, TEXT_DIM, new Vector2(0, -10), new Vector2(600, 60));
            CreateButton(magePanel.transform, "Select", new Vector2(0, -60), new Vector2(300, 60),
                () => StartGame(CharacterClass.Mage));

            // Rogue
            var roguePanel = CreatePanel(_classSelectScreen.transform, new Vector2(0, -370), new Vector2(700, 200));
            CreateText(roguePanel.transform, "ROGUE", 36, TextAnchor.MiddleCenter,
                new Color(0.2f, 0.9f, 0.4f), new Vector2(0, 50), new Vector2(600, 50));
            CreateText(roguePanel.transform, "High DEX & SPD. Energy: Combo Points.\nFast strikes with high evasion.", 22,
                TextAnchor.MiddleCenter, TEXT_DIM, new Vector2(0, -10), new Vector2(600, 60));
            CreateButton(roguePanel.transform, "Select", new Vector2(0, -60), new Vector2(300, 60),
                () => StartGame(CharacterClass.Rogue));
        }

        private void StartGame(CharacterClass playerClass)
        {
            GameManager.Instance.StartNewGame("Hero", playerClass);
            ShowScreen(_gameHudScreen);
            UpdateHUD();
        }

        // ============================================================
        // GAME HUD
        // ============================================================
        private void CreateGameHUD()
        {
            _gameHudScreen = CreateScreen("GameHUD");

            // Top bar
            var topBar = CreatePanel(_gameHudScreen.transform, new Vector2(0, 870), new Vector2(1080, 180));

            _hudName = CreateText(topBar.transform, "Hero", 30, TextAnchor.MiddleLeft,
                TEXT_WHITE, new Vector2(-300, 40), new Vector2(400, 40));
            _hudLevel = CreateText(topBar.transform, "Lv. 1", 26, TextAnchor.MiddleRight,
                ACCENT_LIGHT, new Vector2(300, 40), new Vector2(200, 40));

            // HP Bar
            CreateText(topBar.transform, "HP", 20, TextAnchor.MiddleLeft,
                TEXT_DIM, new Vector2(-420, 0), new Vector2(60, 30));
            var hpBg = CreateImage(topBar.transform, new Color(0.15f, 0.15f, 0.15f),
                new Vector2(50, 0), new Vector2(700, 30));
            _hpBar = CreateImage(hpBg.transform, HP_GREEN, Vector2.zero, Vector2.zero);
            SetFillBar(_hpBar, 1f);
            _hudHP = CreateText(topBar.transform, "155/155", 18, TextAnchor.MiddleCenter,
                TEXT_WHITE, new Vector2(50, 0), new Vector2(700, 30));

            // XP Bar
            CreateText(topBar.transform, "XP", 20, TextAnchor.MiddleLeft,
                TEXT_DIM, new Vector2(-420, -35), new Vector2(60, 25));
            var xpBg = CreateImage(topBar.transform, new Color(0.15f, 0.15f, 0.15f),
                new Vector2(50, -35), new Vector2(700, 20));
            _xpBar = CreateImage(xpBg.transform, XP_BLUE, Vector2.zero, Vector2.zero);
            SetFillBar(_xpBar, 0f);
            _hudXP = CreateText(topBar.transform, "0/100", 16, TextAnchor.MiddleCenter,
                TEXT_WHITE, new Vector2(50, -35), new Vector2(700, 20));

            // Gold display
            _hudGold = CreateText(topBar.transform, "Gold: 0", 24, TextAnchor.MiddleLeft,
                GOLD_COLOR, new Vector2(-300, -65), new Vector2(300, 35));
            _hudRegion = CreateText(topBar.transform, "Cursed Forest", 22, TextAnchor.MiddleRight,
                TEXT_DIM, new Vector2(250, -65), new Vector2(350, 35));

            // Main action buttons
            float btnY = -200;
            float btnSpacing = 130;

            CreateButton(_gameHudScreen.transform, "Explore & Fight", new Vector2(0, btnY),
                new Vector2(600, 100), () => StartRandomCombat());
            CreateButton(_gameHudScreen.transform, "Inventory", new Vector2(0, btnY - btnSpacing),
                new Vector2(600, 100), () => ShowInventory());
            CreateButton(_gameHudScreen.transform, "Travel", new Vector2(0, btnY - btnSpacing * 2),
                new Vector2(600, 100), () => ShowScreen(_regionScreen));
            CreateButton(_gameHudScreen.transform, "Save Game", new Vector2(0, btnY - btnSpacing * 3),
                new Vector2(600, 100), () => SaveGame());

            // Party info
            CreateText(_gameHudScreen.transform, "Party", 28, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, -750), new Vector2(300, 40));
        }

        public void UpdateHUD()
        {
            if (GameManager.Instance?.PlayerCharacter == null) return;

            var pc = GameManager.Instance.PlayerCharacter;
            var eco = GameManager.Instance.Economy;

            _hudName.text = pc.Data.Name;
            _hudLevel.text = $"Lv. {pc.Data.Level}  ({pc.Data.PrimaryClass})";
            _hudHP.text = $"{pc.Data.CurrentHP}/{pc.Data.MaxHP}";
            _hudGold.text = $"Gold: {eco.Wallet.Gold}";
            _hudRegion.text = GameManager.Instance.CurrentRegion.ToString();

            float hpRatio = (float)pc.Data.CurrentHP / pc.Data.MaxHP;
            SetFillBar(_hpBar, hpRatio);
            _hpBar.color = hpRatio > 0.5f ? HP_GREEN : (hpRatio > 0.25f ? GOLD_COLOR : HP_RED);

            int xpReq = pc.GetXPRequiredForLevel(pc.Data.Level);
            float xpRatio = xpReq > 0 ? (float)pc.Data.Experience / xpReq : 0;
            SetFillBar(_xpBar, xpRatio);
            _hudXP.text = $"{pc.Data.Experience}/{xpReq}";
        }

        // ============================================================
        // COMBAT SCREEN
        // ============================================================
        private void CreateCombatScreen()
        {
            _combatScreen = CreateScreen("Combat");

            // Turn indicator
            _turnIndicator = CreateText(_combatScreen.transform, "Combat!", 36, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 820), new Vector2(800, 60));

            // Enemy area
            var enemyPanel = CreatePanel(_combatScreen.transform, new Vector2(0, 550), new Vector2(800, 200));
            _enemyNameText = CreateText(enemyPanel.transform, "Enemy", 32, TextAnchor.MiddleCenter,
                HP_RED, new Vector2(0, 50), new Vector2(700, 45));
            var enemyHpBg = CreateImage(enemyPanel.transform, new Color(0.15f, 0.15f, 0.15f),
                new Vector2(0, 0), new Vector2(600, 35));
            _enemyHPBar = CreateImage(enemyHpBg.transform, HP_RED, Vector2.zero, Vector2.zero);
            SetFillBar(_enemyHPBar, 1f);
            _enemyHPText = CreateText(enemyPanel.transform, "100/100", 20, TextAnchor.MiddleCenter,
                TEXT_WHITE, new Vector2(0, 0), new Vector2(600, 35));

            // Player area
            var playerPanel = CreatePanel(_combatScreen.transform, new Vector2(0, 250), new Vector2(800, 200));
            _playerHPText = CreateText(playerPanel.transform, "Hero  HP: 100/100", 26, TextAnchor.MiddleCenter,
                TEXT_WHITE, new Vector2(0, 50), new Vector2(700, 40));
            var playerHpBg = CreateImage(playerPanel.transform, new Color(0.15f, 0.15f, 0.15f),
                new Vector2(0, 0), new Vector2(600, 35));
            _playerHPBar = CreateImage(playerHpBg.transform, HP_GREEN, Vector2.zero, Vector2.zero);
            SetFillBar(_playerHPBar, 1f);
            _energyText = CreateText(playerPanel.transform, "Energy: 40/100", 22, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, -45), new Vector2(600, 35));

            // Combat log
            var logPanel = CreatePanel(_combatScreen.transform, new Vector2(0, -50), new Vector2(900, 300));
            _combatLog = CreateText(logPanel.transform, "Battle begins!", 22, TextAnchor.UpperCenter,
                TEXT_WHITE, new Vector2(0, 0), new Vector2(850, 280));

            // Action buttons
            _combatButtons = new GameObject("CombatButtons");
            _combatButtons.transform.SetParent(_combatScreen.transform, false);
            var btnRT = _combatButtons.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(900, 300);
            btnRT.anchoredPosition = new Vector2(0, -450);

            CreateButton(_combatButtons.transform, "Attack", new Vector2(-200, 60),
                new Vector2(350, 80), () => CombatAction_Attack());
            CreateButton(_combatButtons.transform, "Defend", new Vector2(200, 60),
                new Vector2(350, 80), () => CombatAction_Defend());
            CreateButton(_combatButtons.transform, "Skill", new Vector2(-200, -40),
                new Vector2(350, 80), () => CombatAction_Skill());
            CreateButton(_combatButtons.transform, "Flee", new Vector2(200, -40),
                new Vector2(350, 80), () => CombatAction_Flee());
        }

        private List<HabisCharacter> _currentEnemies = new();

        public void StartRandomCombat()
        {
            var gm = GameManager.Instance;
            if (gm.IsInCombat) return;

            // Generate enemies
            _currentEnemies = EnemyFactory.GenerateEncounter(
                gm.CurrentRegion, gm.PlayerCharacter.Data.Level);

            gm.StartCombat(_currentEnemies);

            // Subscribe to events
            gm.Combat.OnDamageDealt += OnDamageDealt;
            gm.Combat.OnUnitDefeated += OnUnitDefeated;
            gm.Combat.OnCombatEnd += OnCombatEnd;

            _combatLog.text = "Battle begins!\n";
            ShowScreen(_combatScreen);
            UpdateCombatUI();

            // Start first turn
            gm.Combat.AdvanceToNextUnit();
            UpdateCombatUI();
            ProcessAITurns();
        }

        private void UpdateCombatUI()
        {
            var gm = GameManager.Instance;
            if (gm?.PlayerCharacter == null) return;

            var pc = gm.PlayerCharacter;
            _playerHPText.text = $"{pc.Data.Name}  HP: {pc.Data.CurrentHP}/{pc.Data.MaxHP}";
            float hpRatio = (float)pc.Data.CurrentHP / pc.Data.MaxHP;
            SetFillBar(_playerHPBar, hpRatio);
            _playerHPBar.color = hpRatio > 0.5f ? HP_GREEN : (hpRatio > 0.25f ? GOLD_COLOR : HP_RED);
            _energyText.text = $"Energy: {pc.Data.Energy.Current}/{pc.Data.Energy.Max}";

            // Show first living enemy
            var enemies = gm.Combat.GetEnemyUnits();
            if (enemies.Count > 0)
            {
                var enemy = enemies[0];
                _enemyNameText.text = $"{enemy.Character.Data.Name} (x{enemies.Count})";
                _enemyHPText.text = $"{enemy.Character.Data.CurrentHP}/{enemy.Character.Data.MaxHP}";
                float eRatio = (float)enemy.Character.Data.CurrentHP / enemy.Character.Data.MaxHP;
                SetFillBar(_enemyHPBar, eRatio);
            }

            // Turn indicator
            var current = gm.Combat.CurrentUnit;
            if (current != null)
            {
                _turnIndicator.text = current.IsPlayerSide
                    ? $">> {current.Character.Data.Name}'s Turn <<"
                    : $"Enemy: {current.Character.Data.Name}";
                _combatButtons.SetActive(current.IsPlayerSide &&
                    current.Character == gm.PlayerCharacter);
            }
        }

        private void ProcessAITurns()
        {
            var gm = GameManager.Instance;
            if (gm.Combat.Result != CombatResult.InProgress) return;

            var current = gm.Combat.CurrentUnit;
            if (current == null) return;

            // AI for enemies and companion
            while (current != null && (
                !current.IsPlayerSide ||
                (current.IsPlayerSide && current.Character != gm.PlayerCharacter)))
            {
                if (!current.CanAct)
                {
                    AddCombatLog($"{current.Character.Data.Name} is stunned!");
                    current.HasActed = true;
                }
                else if (!current.IsPlayerSide)
                {
                    // Enemy AI: basic attack on random player
                    var players = gm.Combat.GetPlayerUnits();
                    if (players.Count > 0)
                    {
                        var target = players[Random.Range(0, players.Count)];
                        gm.Combat.ExecuteBasicAttack(target);
                    }
                }
                else
                {
                    // Companion AI
                    var enemies = gm.Combat.GetEnemyUnits();
                    if (enemies.Count > 0)
                    {
                        gm.Combat.ExecuteBasicAttack(enemies[0]);
                    }
                }

                UpdateCombatUI();
                if (gm.Combat.Result != CombatResult.InProgress) return;

                current = gm.Combat.AdvanceToNextUnit();
                UpdateCombatUI();
            }
        }

        private void CombatAction_Attack()
        {
            var gm = GameManager.Instance;
            var enemies = gm.Combat.GetEnemyUnits();
            if (enemies.Count == 0) return;

            gm.Combat.ExecuteBasicAttack(enemies[0]);
            UpdateCombatUI();

            if (gm.Combat.Result != CombatResult.InProgress) return;

            gm.Combat.AdvanceToNextUnit();
            UpdateCombatUI();
            ProcessAITurns();
        }

        private void CombatAction_Defend()
        {
            var gm = GameManager.Instance;
            gm.Combat.ExecuteDefend();
            AddCombatLog($"{gm.PlayerCharacter.Data.Name} defends! (+Energy, +Barrier)");
            UpdateCombatUI();

            if (gm.Combat.Result != CombatResult.InProgress) return;

            gm.Combat.AdvanceToNextUnit();
            UpdateCombatUI();
            ProcessAITurns();
        }

        private void CombatAction_Skill()
        {
            // For now, treat as strong attack if enough energy
            var gm = GameManager.Instance;
            var pc = gm.PlayerCharacter;

            if (pc.Data.Energy.Current >= 20)
            {
                pc.Data.Energy.TrySpend(20);
                var enemies = gm.Combat.GetEnemyUnits();
                if (enemies.Count > 0)
                {
                    // Deal 1.5x damage as a skill
                    var stats = pc.GetEffectiveStats();
                    int dmg = Mathf.RoundToInt((10 + stats.STR / 2) * 1.5f);
                    enemies[0].Character.TakeDamage(dmg);
                    AddCombatLog($"{pc.Data.Name} uses Power Strike! {dmg} damage!");

                    if (!enemies[0].Character.IsAlive)
                        AddCombatLog($"{enemies[0].Character.Data.Name} defeated!");
                }

                gm.Combat.CurrentUnit.HasActed = true;
                UpdateCombatUI();

                if (gm.Combat.Result != CombatResult.InProgress) return;
                gm.Combat.AdvanceToNextUnit();
                UpdateCombatUI();
                ProcessAITurns();
            }
            else
            {
                AddCombatLog("Not enough energy! (Need 20)");
            }
        }

        private void CombatAction_Flee()
        {
            var gm = GameManager.Instance;
            bool fled = gm.Combat.ExecuteFlee();
            if (fled)
            {
                AddCombatLog("Fled from battle!");
                EndCombatCleanup();
                ShowScreen(_gameHudScreen);
                UpdateHUD();
            }
            else
            {
                AddCombatLog("Failed to flee!");
                gm.Combat.AdvanceToNextUnit();
                UpdateCombatUI();
                ProcessAITurns();
            }
        }

        private void OnDamageDealt(object sender, DamageEventArgs e)
        {
            AddCombatLog(e.Message);
        }

        private void OnUnitDefeated(object sender, CombatEventArgs e)
        {
            AddCombatLog(e.Message);
        }

        private void OnCombatEnd(object sender, CombatEventArgs e)
        {
            var gm = GameManager.Instance;
            string resultText = gm.Combat.Result == CombatResult.Victory
                ? "VICTORY! Tap to continue..."
                : "DEFEAT... Tap to continue...";

            AddCombatLog($"\n{resultText}");
            _combatButtons.SetActive(false);

            // Create a "continue" button
            GameObject continueBtn = null;
            continueBtn = CreateButton(_combatScreen.transform, "Continue",
                new Vector2(0, -700), new Vector2(400, 80), () =>
                {
                    EndCombatCleanup();
                    if (gm.Combat.Result == CombatResult.Defeat)
                    {
                        // Heal player on defeat
                        gm.PlayerCharacter.Heal(gm.PlayerCharacter.Data.MaxHP);
                    }
                    ShowScreen(_gameHudScreen);
                    UpdateHUD();
                    Destroy(continueBtn);
                });
        }

        private void EndCombatCleanup()
        {
            var gm = GameManager.Instance;
            gm.Combat.OnDamageDealt -= OnDamageDealt;
            gm.Combat.OnUnitDefeated -= OnUnitDefeated;
            gm.Combat.OnCombatEnd -= OnCombatEnd;
        }

        private void AddCombatLog(string msg)
        {
            if (_combatLog == null) return;
            string[] lines = _combatLog.text.Split('\n');
            if (lines.Length > 8)
            {
                _combatLog.text = string.Join("\n", lines, lines.Length - 8, 8);
            }
            _combatLog.text += $"\n{msg}";
        }

        // ============================================================
        // INVENTORY SCREEN
        // ============================================================
        private void CreateInventoryScreen()
        {
            _inventoryScreen = CreateScreen("Inventory");

            CreateText(_inventoryScreen.transform, "Inventory", 42, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 820), new Vector2(600, 60));

            CreateButton(_inventoryScreen.transform, "Back", new Vector2(0, -800),
                new Vector2(400, 80), () => { ShowScreen(_gameHudScreen); UpdateHUD(); });
        }

        private void ShowInventory()
        {
            ShowScreen(_inventoryScreen);

            // Clear old items
            foreach (Transform child in _inventoryScreen.transform)
            {
                if (child.name.StartsWith("Item_"))
                    Destroy(child.gameObject);
            }

            var inv = GameManager.Instance.PlayerInventory;
            if (inv == null || inv.Items.Count == 0)
            {
                CreateText(_inventoryScreen.transform, "No items yet. Go fight!",
                    26, TextAnchor.MiddleCenter, TEXT_DIM, new Vector2(0, 200), new Vector2(600, 40))
                    .gameObject.name = "Item_empty";
                return;
            }

            float yPos = 700;
            foreach (var item in inv.Items)
            {
                if (yPos < -700) break;

                Color rarityColor = item.Rarity switch
                {
                    ItemRarity.Common => TEXT_DIM,
                    ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
                    ItemRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
                    ItemRarity.Legendary => GOLD_COLOR,
                    _ => TEXT_WHITE
                };

                var txt = CreateText(_inventoryScreen.transform, $"{item.Name}  Lv.{item.Level}  [{item.Rarity}]",
                    24, TextAnchor.MiddleLeft, rarityColor, new Vector2(0, yPos), new Vector2(800, 40));
                txt.gameObject.name = $"Item_{item.Id}";
                yPos -= 50;
            }
        }

        // ============================================================
        // REGION / TRAVEL SCREEN
        // ============================================================
        private void CreateRegionScreen()
        {
            _regionScreen = CreateScreen("Regions");

            CreateText(_regionScreen.transform, "Travel", 42, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 820), new Vector2(600, 60));

            var regions = new (Region region, string name, int level)[]
            {
                (Region.CursedForest, "Cursed Forest", 1),
                (Region.RottingSwamp, "Rotting Swamp", 10),
                (Region.ShatteredMountain, "Shattered Mountain", 20),
                (Region.AbyssalDungeon, "Abyssal Dungeon", 30),
                (Region.AncientRuins, "Ancient Ruins", 50),
                (Region.VoidAbyss, "Void Abyss", 70),
            };

            float y = 550;
            foreach (var (region, name, level) in regions)
            {
                var r = region;
                CreateButton(_regionScreen.transform, $"{name}  (Lv.{level}+)",
                    new Vector2(0, y), new Vector2(700, 80), () => TravelTo(r));
                y -= 110;
            }

            CreateButton(_regionScreen.transform, "Back", new Vector2(0, -800),
                new Vector2(400, 80), () => { ShowScreen(_gameHudScreen); UpdateHUD(); });
        }

        private void TravelTo(Region region)
        {
            bool success = GameManager.Instance.TravelToRegion(region);
            if (success)
            {
                ShowScreen(_gameHudScreen);
                UpdateHUD();
            }
            else
            {
                Debug.Log($"[Habis RPG] Level too low for {region}");
            }
        }

        private void SaveGame()
        {
            string json = GameManager.Instance.SaveGame();
            PlayerPrefs.SetString("HabisRPG_Save", json);
            PlayerPrefs.Save();
            Debug.Log("[Habis RPG] Game saved to PlayerPrefs!");
        }

        // ============================================================
        // UI HELPERS
        // ============================================================
        private void ShowScreen(GameObject screen)
        {
            _mainMenuScreen?.SetActive(false);
            _classSelectScreen?.SetActive(false);
            _gameHudScreen?.SetActive(false);
            _combatScreen?.SetActive(false);
            _inventoryScreen?.SetActive(false);
            _regionScreen?.SetActive(false);
            screen.SetActive(true);
        }

        private GameObject CreateScreen(string name)
        {
            var screen = new GameObject(name);
            screen.transform.SetParent(_canvas.transform, false);
            var rt = screen.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            // Full screen dark background
            var bg = screen.AddComponent<Image>();
            bg.color = DARK_BG;

            screen.SetActive(false);
            return screen;
        }

        private GameObject CreatePanel(Transform parent, Vector2 pos, Vector2 size)
        {
            var panel = new GameObject("Panel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = panel.AddComponent<Image>();
            img.color = PANEL_BG;

            return panel;
        }

        private Text CreateText(Transform parent, string text, int fontSize,
            TextAnchor alignment, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            return txt;
        }

        private GameObject CreateButton(Transform parent, string label, Vector2 pos,
            Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var btnGO = new GameObject($"Btn_{label}");
            btnGO.transform.SetParent(parent, false);
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnGO.AddComponent<Image>();
            img.color = BTN_COLOR;

            var btn = btnGO.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = BTN_COLOR;
            colors.highlightedColor = BTN_HOVER;
            colors.pressedColor = ACCENT;
            colors.selectedColor = BTN_COLOR;
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.sizeDelta = Vector2.zero;

            var txt = labelGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = TEXT_WHITE;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            return btnGO;
        }

        private Image CreateImage(Transform parent, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = color;

            return img;
        }

        private void SetFillBar(Image bar, float ratio)
        {
            if (bar == null) return;
            var rt = bar.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
