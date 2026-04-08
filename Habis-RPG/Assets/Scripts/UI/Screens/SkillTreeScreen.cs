// ============================================================================
// Habis RPG — Skill Tree Screen
// Visualizes the player's skills as nodes grouped by branch tier (1, 2, 3).
// Tap unlocked + prerequisites met → spend a free skill point to unlock/upgrade.
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;
using HabisRPG.Skills;
using HabisRPG.Managers;
using HabisRPG.UI.Combat;

namespace HabisRPG.UI.Screens
{
    public class SkillTreeScreen : MonoBehaviour
    {
        private RectTransform _rt;
        private Text _pointsText;
        private Text _detailName;
        private Text _detailDesc;
        private Button _unlockButton;
        private SkillData _selected;
        private System.Action _onClose;
        private Transform _treeContainer;

        public static SkillTreeScreen Create(Transform parent, System.Action onClose)
        {
            var go = new GameObject("SkillTreeScreen");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<SkillTreeScreen>();
            s._onClose = onClose;
            s.Build();
            return s;
        }

        private void Build()
        {
            _rt = gameObject.AddComponent<RectTransform>();
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.one;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.05f, 0.10f, 1f);
            bg.raycastTarget = true;

            CreateText(transform, "SKILL TREE", 44, FontStyle.Bold,
                new Color(0.95f, 0.85f, 0.6f), new Vector2(0, 830), new Vector2(800, 60));

            _pointsText = CreateText(transform, "Points: 0", 26, FontStyle.Normal,
                new Color(0.9f, 0.7f, 1f), new Vector2(0, 770), new Vector2(600, 40));

            // Tree container
            var container = new GameObject("Tree");
            container.transform.SetParent(transform, false);
            var cRt = container.AddComponent<RectTransform>();
            cRt.sizeDelta = new Vector2(960, 1100);
            cRt.anchoredPosition = new Vector2(0, 100);
            var cImg = container.AddComponent<Image>();
            cImg.color = new Color(0.10f, 0.08f, 0.14f, 0.9f);
            _treeContainer = container.transform;

            // Detail panel
            BuildDetailPanel();

            // Back button
            CreateButton(transform, "Back", new Vector2(0, -880),
                new Vector2(380, 80), () => _onClose?.Invoke());

            Refresh();
        }

        private void BuildDetailPanel()
        {
            var panel = new GameObject("Detail");
            panel.transform.SetParent(transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(960, 240);
            rt.anchoredPosition = new Vector2(0, -550);
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.14f, 0.10f, 0.18f, 0.95f);

            _detailName = CreateText(panel.transform, "Tap a skill", 26, FontStyle.Bold,
                Color.white, new Vector2(0, 80), new Vector2(900, 40));
            _detailDesc = CreateText(panel.transform, "", 18, FontStyle.Normal,
                new Color(0.8f, 0.8f, 0.9f), new Vector2(0, 0), new Vector2(900, 100));

            _unlockButton = CreateButton(panel.transform, "UNLOCK / UPGRADE",
                new Vector2(0, -80), new Vector2(360, 60), TryUnlockOrUpgrade);
            _unlockButton.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            // Clear nodes
            for (int i = _treeContainer.childCount - 1; i >= 0; i--)
            {
                var child = _treeContainer.GetChild(i);
                if (child.name.StartsWith("Node_"))
                    Destroy(child.gameObject);
            }

            var pc = GameManager.Instance?.PlayerCharacter;
            if (pc == null) return;

            _pointsText.text = $"Skill Points: {pc.Data.FreeSkillPoints}";

            var skills = SkillDatabase.GetForClass(pc.Data.PrimaryClass);

            // Group by branch
            var branches = skills.GroupBy(s => s.Branch).ToList();
            float startX = -340f, dx = 340f;
            int branchIdx = 0;
            foreach (var branch in branches)
            {
                // Tier rows
                var byTier = branch.OrderBy(s => s.UnlockLevel).ToList();
                int rowIdx = 0;
                foreach (var skill in byTier)
                {
                    Vector2 pos = new Vector2(startX + branchIdx * dx, 380f - rowIdx * 180f);
                    CreateNode(skill, pos);
                    rowIdx++;
                }
                branchIdx++;
            }
        }

        private void CreateNode(SkillData skill, Vector2 pos)
        {
            var pc = GameManager.Instance.PlayerCharacter;
            var go = new GameObject($"Node_{skill.Id}");
            go.transform.SetParent(_treeContainer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 140);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();

            bool unlocked = pc.Data.UnlockedSkillIds.Contains(skill.Id);
            bool prereqMet = SkillDatabase.ArePrerequisitesMet(skill, pc.Data.UnlockedSkillIds);
            bool levelMet = pc.Data.Level >= skill.UnlockLevel;
            bool canUnlock = !unlocked && prereqMet && levelMet;

            if (unlocked)
                img.color = new Color(0.25f, 0.35f, 0.55f, 0.95f);
            else if (canUnlock)
                img.color = new Color(0.30f, 0.20f, 0.40f, 0.95f);
            else
                img.color = new Color(0.10f, 0.10f, 0.14f, 0.85f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Select(skill));

            int level = pc.Data.SkillLevels.TryGetValue(skill.Id, out var l) ? l : 0;
            CreateText(go.transform,
                $"{skill.Name}\n[{level}/{skill.MaxUpgradeLevel}]\nLv.{skill.UnlockLevel}",
                16, FontStyle.Normal,
                unlocked ? new Color(0.85f, 0.95f, 1f)
                         : canUnlock ? new Color(0.95f, 0.85f, 1f)
                                     : new Color(0.45f, 0.45f, 0.55f),
                Vector2.zero, new Vector2(270, 130));
        }

        private void Select(SkillData skill)
        {
            _selected = skill;
            var pc = GameManager.Instance.PlayerCharacter;
            int level = pc.Data.SkillLevels.TryGetValue(skill.Id, out var l) ? l : 0;
            _detailName.text = $"{skill.Name}  ({level}/{skill.MaxUpgradeLevel})";
            string typeStr = skill.IsAoE ? "AoE  " : "";
            string elemStr = skill.Element != ElementType.None ? $"{skill.Element}  " : "";
            _detailDesc.text =
                $"{skill.Description}\n" +
                $"{typeStr}{elemStr}Energy: {skill.EnergyCost}   Damage: {skill.BaseDamage}   " +
                $"CD: {skill.CooldownTurns}   Required Lv: {skill.UnlockLevel}";

            bool prereqMet = SkillDatabase.ArePrerequisitesMet(skill, pc.Data.UnlockedSkillIds);
            bool levelMet = pc.Data.Level >= skill.UnlockLevel;
            bool hasPoint = pc.Data.FreeSkillPoints > 0;
            bool canUpgrade = level < skill.MaxUpgradeLevel;
            _unlockButton.gameObject.SetActive(prereqMet && levelMet && hasPoint && canUpgrade);
        }

        private void TryUnlockOrUpgrade()
        {
            if (_selected == null) return;
            var pc = GameManager.Instance.PlayerCharacter;
            if (pc.Data.FreeSkillPoints <= 0) return;

            int level = pc.Data.SkillLevels.TryGetValue(_selected.Id, out var l) ? l : 0;
            if (level >= _selected.MaxUpgradeLevel) return;

            if (level == 0)
            {
                pc.Data.UnlockedSkillIds.Add(_selected.Id);
                pc.Data.SkillLevels[_selected.Id] = 1;
            }
            else
            {
                pc.Data.SkillLevels[_selected.Id] = level + 1;
            }
            pc.Data.FreeSkillPoints--;
            Refresh();
            Select(_selected);
        }

        // ---- helpers ----
        private static Text CreateText(Transform parent, string text, int size, FontStyle style,
            Color color, Vector2 pos, Vector2 dimensions)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = dimensions;
            rt.anchoredPosition = pos;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos,
            Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.4f, 0.18f, 0.5f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            CreateText(go.transform, label, 22, FontStyle.Bold, Color.white,
                Vector2.zero, size);
            return btn;
        }
    }
}
