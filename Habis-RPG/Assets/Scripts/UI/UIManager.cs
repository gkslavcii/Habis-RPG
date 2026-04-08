using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;
using HabisRPG.Managers;
using HabisRPG.Character;
using HabisRPG.Combat;
using HabisRPG.Items;
using HabisRPG.Skills;
using HabisRPG.UI.Combat;
using HabisRPG.UI.Popups;
using HabisRPG.UI.Screens;

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

        // New combat scene + state
        private CombatSceneView _sceneView;
        private GameObject _skillSelectPanel;
        private bool _animating;
        private int _pendingDamage;
        private bool _pendingCrit;
        private bool _pendingMiss;
        private List<ItemData> _pendingLoot = new();
        private int _pendingXP;
        private int _pendingGold;

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
                new Vector2(600, 100), () => OpenInventoryScreen());
            CreateButton(_gameHudScreen.transform, "Skill Tree", new Vector2(0, btnY - btnSpacing * 2),
                new Vector2(600, 100), () => OpenSkillTreeScreen());
            CreateButton(_gameHudScreen.transform, "Travel", new Vector2(0, btnY - btnSpacing * 3),
                new Vector2(600, 100), () => ShowScreen(_regionScreen));
            CreateButton(_gameHudScreen.transform, "Save Game", new Vector2(0, btnY - btnSpacing * 4),
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
        // COMBAT SCREEN (Sonny-style scene + animated)
        // ============================================================
        private void CreateCombatScreen()
        {
            _combatScreen = CreateScreen("Combat");
            // Replace solid background — scene view will provide its own.
            var bgImg = _combatScreen.GetComponent<Image>();
            if (bgImg != null) bgImg.color = Color.black;

            // Combat scene (background + units + vfx)
            _sceneView = CombatSceneView.Create(_combatScreen.transform);

            // Turn indicator (top)
            _turnIndicator = CreateText(_combatScreen.transform, "", 32, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 820), new Vector2(900, 50));

            // Energy / HP compact strip (above buttons)
            _playerHPText = CreateText(_combatScreen.transform, "Hero", 22, TextAnchor.MiddleLeft,
                TEXT_WHITE, new Vector2(-360, -380), new Vector2(500, 30));
            _energyText = CreateText(_combatScreen.transform, "Energy: 40/100", 22, TextAnchor.MiddleRight,
                ACCENT_LIGHT, new Vector2(360, -380), new Vector2(500, 30));

            // Combat log (compact, 3 lines)
            var logPanel = CreatePanel(_combatScreen.transform, new Vector2(0, -440), new Vector2(960, 100));
            logPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
            _combatLog = CreateText(logPanel.transform, "Battle begins!", 18, TextAnchor.MiddleCenter,
                TEXT_WHITE, Vector2.zero, new Vector2(940, 90));

            // Action buttons (4 buttons in row)
            _combatButtons = new GameObject("CombatButtons");
            _combatButtons.transform.SetParent(_combatScreen.transform, false);
            var btnRT = _combatButtons.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(960, 220);
            btnRT.anchoredPosition = new Vector2(0, -660);

            CreateButton(_combatButtons.transform, "Attack", new Vector2(-330, 50),
                new Vector2(280, 90), () => StartCoroutine(DoBasicAttack()));
            CreateButton(_combatButtons.transform, "Skill", new Vector2(-30, 50),
                new Vector2(280, 90), () => OpenSkillSelect());
            CreateButton(_combatButtons.transform, "Defend", new Vector2(270, 50),
                new Vector2(280, 90), () => StartCoroutine(DoDefend()));
            CreateButton(_combatButtons.transform, "Flee", new Vector2(0, -60),
                new Vector2(280, 90), () => StartCoroutine(DoFlee()));

            // Skill select panel (hidden by default)
            BuildSkillSelectPanel();

            // Hidden vars (no longer used directly, kept null-safe)
            _enemyNameText = null;
            _enemyHPBar = null;
            _enemyHPText = null;
            _playerHPBar = null;
        }

        private void BuildSkillSelectPanel()
        {
            _skillSelectPanel = new GameObject("SkillSelectPanel");
            _skillSelectPanel.transform.SetParent(_combatScreen.transform, false);
            var rt = _skillSelectPanel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900, 540);
            rt.anchoredPosition = new Vector2(0, -200);
            var img = _skillSelectPanel.AddComponent<Image>();
            img.color = new Color(0.08f, 0.06f, 0.12f, 0.96f);
            _skillSelectPanel.SetActive(false);

            CreateText(_skillSelectPanel.transform, "Choose a Skill", 30, TextAnchor.MiddleCenter,
                ACCENT_LIGHT, new Vector2(0, 230), new Vector2(800, 50));

            CreateButton(_skillSelectPanel.transform, "Cancel",
                new Vector2(0, -230), new Vector2(280, 70), () => _skillSelectPanel.SetActive(false));
        }

        private void OpenSkillSelect()
        {
            if (_animating) return;
            // Clear old skill buttons
            var pc = GameManager.Instance.PlayerCharacter;
            for (int i = _skillSelectPanel.transform.childCount - 1; i >= 0; i--)
            {
                var c = _skillSelectPanel.transform.GetChild(i);
                if (c.name.StartsWith("SkillBtn_")) Destroy(c.gameObject);
            }

            float y = 160;
            int count = 0;
            foreach (var id in pc.Data.UnlockedSkillIds)
            {
                var skill = SkillDatabase.Get(id);
                if (skill == null) continue;
                var go = CreateButton(_skillSelectPanel.transform,
                    $"{skill.Name}  ({skill.EnergyCost} EN)",
                    new Vector2(0, y - count * 80), new Vector2(720, 70),
                    () => StartCoroutine(DoSkill(skill)));
                go.name = $"SkillBtn_{id}";
                count++;
                if (count >= 5) break;
            }

            if (count == 0)
            {
                var lbl = CreateText(_skillSelectPanel.transform, "No skills available",
                    22, TextAnchor.MiddleCenter, TEXT_DIM,
                    new Vector2(0, 0), new Vector2(700, 40));
                lbl.gameObject.name = "SkillBtn_empty";
            }

            _skillSelectPanel.SetActive(true);
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

            // Setup the visual scene
            _sceneView.SetRegion(gm.CurrentRegion);
            _sceneView.Populate(gm.Party.GetCombatParty(), _currentEnemies);

            _combatLog.text = "Battle begins!";
            _pendingLoot.Clear();
            _pendingXP = 0;
            _pendingGold = 0;
            ShowScreen(_combatScreen);
            UpdateCombatUI();

            // Start first turn
            gm.Combat.AdvanceToNextUnit();
            UpdateCombatUI();
            StartCoroutine(ProcessAITurnsCoroutine());
        }

        private void UpdateCombatUI()
        {
            var gm = GameManager.Instance;
            if (gm?.PlayerCharacter == null) return;

            var pc = gm.PlayerCharacter;
            _playerHPText.text = $"{pc.Data.Name}  HP: {pc.Data.CurrentHP}/{pc.Data.MaxHP}";
            _energyText.text = $"Energy: {pc.Data.Energy.Current}/{pc.Data.Energy.Max}";

            if (_sceneView != null) _sceneView.RefreshAllHP();

            // Turn indicator
            var current = gm.Combat.CurrentUnit;
            if (current != null)
            {
                _turnIndicator.text = current.IsPlayerSide
                    ? $">> {current.Character.Data.Name}'s Turn <<"
                    : $"Enemy: {current.Character.Data.Name}";
                _combatButtons.SetActive(current.IsPlayerSide &&
                    current.Character == gm.PlayerCharacter && !_animating);
            }
        }

        private IEnumerator ProcessAITurnsCoroutine()
        {
            var gm = GameManager.Instance;
            if (gm.Combat.Result != CombatResult.InProgress) yield break;

            var current = gm.Combat.CurrentUnit;

            // Loop while it's not the player's turn
            while (current != null && (
                !current.IsPlayerSide ||
                (current.IsPlayerSide && current.Character != gm.PlayerCharacter)))
            {
                if (gm.Combat.Result != CombatResult.InProgress) yield break;

                if (!current.CanAct)
                {
                    AddCombatLog($"{current.Character.Data.Name} is stunned!");
                    current.HasActed = true;
                }
                else if (!current.IsPlayerSide)
                {
                    // Enemy AI
                    var players = gm.Combat.GetPlayerUnits();
                    if (players.Count > 0)
                    {
                        var target = players[Random.Range(0, players.Count)];
                        var attackerView = _sceneView.FindView(current.Character);
                        var targetView = _sceneView.FindView(target.Character);
                        yield return _sceneView.PlayBasicAttack(attackerView, targetView);
                        gm.Combat.ExecuteBasicAttack(target);
                    }
                }
                else
                {
                    // Companion AI: basic attack
                    var enemies = gm.Combat.GetEnemyUnits();
                    if (enemies.Count > 0)
                    {
                        var attackerView = _sceneView.FindView(current.Character);
                        var targetView = _sceneView.FindView(enemies[0].Character);
                        yield return _sceneView.PlayBasicAttack(attackerView, targetView);
                        gm.Combat.ExecuteBasicAttack(enemies[0]);
                    }
                }

                UpdateCombatUI();
                if (gm.Combat.Result != CombatResult.InProgress) yield break;

                current = gm.Combat.AdvanceToNextUnit();
                UpdateCombatUI();
                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator DoBasicAttack()
        {
            if (_animating) yield break;
            _animating = true;
            _combatButtons.SetActive(false);

            var gm = GameManager.Instance;
            var enemies = gm.Combat.GetEnemyUnits();
            if (enemies.Count == 0) { _animating = false; yield break; }

            var attackerView = _sceneView.FindView(gm.PlayerCharacter);
            var targetView = _sceneView.FindView(enemies[0].Character);
            yield return _sceneView.PlayBasicAttack(attackerView, targetView);
            gm.Combat.ExecuteBasicAttack(enemies[0]);
            UpdateCombatUI();

            yield return AfterPlayerAction();
        }

        private IEnumerator DoDefend()
        {
            if (_animating) yield break;
            _animating = true;
            _combatButtons.SetActive(false);

            var gm = GameManager.Instance;
            var pcView = _sceneView.FindView(gm.PlayerCharacter);
            // Pulse + barrier glow
            VFXRunner.Instance.StartCoroutine(VFXPlayer.Burst(pcView.VFXAnchor,
                new Color(0.55f, 0.85f, 1f, 0.9f), 200f));
            yield return UITween.PunchScale(pcView.transform, 0.1f, 0.3f);

            gm.Combat.ExecuteDefend();
            AddCombatLog($"{gm.PlayerCharacter.Data.Name} defends!");
            UpdateCombatUI();

            yield return AfterPlayerAction();
        }

        private IEnumerator DoSkill(SkillData skill)
        {
            if (_animating) yield break;
            _skillSelectPanel.SetActive(false);
            _animating = true;
            _combatButtons.SetActive(false);

            var gm = GameManager.Instance;
            var pc = gm.PlayerCharacter;
            if (pc.Data.Energy.Current < skill.EnergyCost)
            {
                AddCombatLog("Not enough energy!");
                _animating = false;
                _combatButtons.SetActive(true);
                yield break;
            }

            var attackerView = _sceneView.FindView(pc);
            var enemies = gm.Combat.GetEnemyUnits();
            UnitView targetView = enemies.Count > 0 ? _sceneView.FindView(enemies[0].Character) : null;

            Color tint = GetSkillTint(skill);
            bool isProjectile = skill.DamageType == DamageType.Magical && !skill.IsAoE;
            yield return _sceneView.PlaySkill(attackerView, targetView, tint, isProjectile, skill.IsAoE);

            // Apply skill via engine
            if (targetView != null)
            {
                var targetUnit = gm.Combat.GetEnemyUnits().FirstOrDefault();
                if (targetUnit != null)
                    gm.Combat.ExecuteSkill(skill, targetUnit);
            }
            else
            {
                // Self-buff
                pc.Data.Energy.TrySpend(skill.EnergyCost);
                if (skill.HasEffect)
                {
                    pc.Data.ActiveEffects.Add(StatusEffect.Create(
                        skill.AppliesEffect, skill.EffectDuration, 0.3f));
                }
                gm.Combat.CurrentUnit.HasActed = true;
            }

            AddCombatLog($"{pc.Data.Name} uses {skill.Name}!");
            UpdateCombatUI();

            yield return AfterPlayerAction();
        }

        private IEnumerator DoFlee()
        {
            if (_animating) yield break;
            _animating = true;
            _combatButtons.SetActive(false);

            var gm = GameManager.Instance;
            bool fled = gm.Combat.ExecuteFlee();
            if (fled)
            {
                AddCombatLog("Fled from battle!");
                yield return new WaitForSeconds(0.3f);
                EndCombatCleanup();
                ShowScreen(_gameHudScreen);
                UpdateHUD();
                _animating = false;
                yield break;
            }
            else
            {
                AddCombatLog("Failed to flee!");
                gm.Combat.AdvanceToNextUnit();
                UpdateCombatUI();
                yield return ProcessAITurnsCoroutine();
                _animating = false;
                _combatButtons.SetActive(true);
            }
        }

        private IEnumerator AfterPlayerAction()
        {
            var gm = GameManager.Instance;
            yield return new WaitForSeconds(0.15f);
            if (gm.Combat.Result != CombatResult.InProgress)
            {
                _animating = false;
                yield break;
            }
            gm.Combat.AdvanceToNextUnit();
            UpdateCombatUI();
            yield return ProcessAITurnsCoroutine();
            _animating = false;
            UpdateCombatUI();
        }

        private static Color GetSkillTint(SkillData skill)
        {
            if (skill.Element == ElementType.Fire) return new Color(1f, 0.45f, 0.15f);
            if (skill.Element == ElementType.Ice) return new Color(0.55f, 0.85f, 1f);
            if (skill.Element == ElementType.Lightning) return new Color(0.9f, 0.85f, 1f);
            if (skill.Element == ElementType.Poison) return new Color(0.45f, 0.95f, 0.3f);
            if (skill.Element == ElementType.Void) return new Color(0.65f, 0.3f, 0.95f);
            if (skill.DamageType == DamageType.Magical) return new Color(0.5f, 0.5f, 1f);
            return new Color(1f, 0.85f, 0.5f);
        }

        private void OnDamageDealt(object sender, DamageEventArgs e)
        {
            AddCombatLog(e.Message);
            // Spawn damage popup over the target
            if (_sceneView != null && e.Target != null)
            {
                var view = _sceneView.FindView(e.Target);
                if (view != null)
                {
                    VFXPlayer.SpawnDamagePopup(view.VFXAnchor,
                        e.Damage, e.IsCritical, false, e.IsMiss);
                }
            }
        }

        private void OnUnitDefeated(object sender, CombatEventArgs e)
        {
            AddCombatLog(e.Message);
            if (_sceneView != null && e.Target != null)
            {
                var view = _sceneView.FindView(e.Target);
                if (view != null)
                    StartCoroutine(_sceneView.PlayDeath(view));
            }
        }

        private void OnCombatEnd(object sender, CombatEventArgs e)
        {
            var gm = GameManager.Instance;
            _combatButtons.SetActive(false);

            if (gm.Combat.Result == CombatResult.Victory)
            {
                // Compute rewards (mirror GameManager logic so popup matches)
                _pendingXP = 0;
                _pendingGold = 0;
                _pendingLoot.Clear();
                foreach (var unit in gm.Combat.AllUnits)
                {
                    if (unit.IsPlayerSide) continue;
                    int level = unit.Character.Data.Level;
                    _pendingXP += 10 + level * 3;
                    _pendingGold += gm.Economy.CalculateGoldDrop(level, EnemyTier.Basic);
                    if (Random.value < 0.7f)
                        _pendingLoot.Add(LootGenerator.GenerateDrop(EnemyTier.Basic, level));
                }
                StartCoroutine(VictoryFlow());
            }
            else
            {
                StartCoroutine(DefeatFlow());
            }
        }

        private IEnumerator VictoryFlow()
        {
            var gm = GameManager.Instance;
            AddCombatLog("VICTORY!");
            yield return VFXPlayer.ScreenFlash(_canvas.transform,
                new Color(1f, 0.9f, 0.4f, 0.4f), 0.4f);
            yield return new WaitForSeconds(0.4f);

            // Show loot popup (registers items into inventory automatically via AddItem)
            foreach (var item in _pendingLoot) gm.PlayerInventory.AddItem(item);
            yield return LootPopup.Show(_canvas.transform, _pendingLoot, _pendingGold, _pendingXP);

            EndCombatCleanup();
            ShowScreen(_gameHudScreen);
            UpdateHUD();
            _animating = false;
        }

        private IEnumerator DefeatFlow()
        {
            var gm = GameManager.Instance;
            AddCombatLog("DEFEAT...");
            yield return VFXPlayer.ScreenFlash(_canvas.transform,
                new Color(0.6f, 0.1f, 0.1f, 0.6f), 0.6f);
            yield return new WaitForSeconds(0.6f);

            gm.PlayerCharacter.Heal(gm.PlayerCharacter.Data.MaxHP);
            EndCombatCleanup();
            ShowScreen(_gameHudScreen);
            UpdateHUD();
            _animating = false;
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

        private InventoryScreen _inventoryScreenInstance;
        private SkillTreeScreen _skillTreeScreenInstance;

        private void OpenInventoryScreen()
        {
            // Destroy any existing instance to refresh contents
            if (_inventoryScreenInstance != null) Destroy(_inventoryScreenInstance.gameObject);
            _inventoryScreenInstance = InventoryScreen.Create(_canvas.transform, () =>
            {
                if (_inventoryScreenInstance != null)
                {
                    Destroy(_inventoryScreenInstance.gameObject);
                    _inventoryScreenInstance = null;
                }
                ShowScreen(_gameHudScreen);
                UpdateHUD();
            });
        }

        private void OpenSkillTreeScreen()
        {
            if (_skillTreeScreenInstance != null) Destroy(_skillTreeScreenInstance.gameObject);
            _skillTreeScreenInstance = SkillTreeScreen.Create(_canvas.transform, () =>
            {
                if (_skillTreeScreenInstance != null)
                {
                    Destroy(_skillTreeScreenInstance.gameObject);
                    _skillTreeScreenInstance = null;
                }
                ShowScreen(_gameHudScreen);
                UpdateHUD();
            });
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
                StartCoroutine(TravelTransition(region));
            }
            else
            {
                Debug.Log($"[Habis RPG] Level too low for {region}");
            }
        }

        private IEnumerator TravelTransition(Region region)
        {
            yield return RegionTransition.Play(_canvas.transform, region);
            ShowScreen(_gameHudScreen);
            UpdateHUD();
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
