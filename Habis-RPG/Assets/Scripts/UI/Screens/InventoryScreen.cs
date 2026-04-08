// ============================================================================
// Habis RPG — Inventory Screen
// Equipment slots panel + scrollable item grid + tooltip on tap.
// Built procedurally so it slots into the existing UIManager.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;
using HabisRPG.Items;
using HabisRPG.Character;
using HabisRPG.Managers;
using HabisRPG.UI.Combat;

namespace HabisRPG.UI.Screens
{
    public class InventoryScreen : MonoBehaviour
    {
        private RectTransform _rt;
        private Transform _slotGrid;
        private Transform _itemGrid;
        private Text _detailName;
        private Text _detailStats;
        private GameObject _detailPanel;
        private Button _equipButton;
        private Button _unequipButton;
        private ItemData _selectedItem;
        private System.Action _onClose;

        public static InventoryScreen Create(Transform parent, System.Action onClose)
        {
            var go = new GameObject("InventoryScreen");
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<InventoryScreen>();
            screen._onClose = onClose;
            screen.Build();
            return screen;
        }

        private void Build()
        {
            _rt = gameObject.AddComponent<RectTransform>();
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.one;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;

            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.10f, 1f);
            bg.raycastTarget = true;

            // Title
            CreateText(transform, "INVENTORY", 44, FontStyle.Bold,
                new Color(0.95f, 0.85f, 0.6f), new Vector2(0, 830), new Vector2(800, 60));

            // Equipment slots panel (top half)
            BuildEquipSlots();

            // Item grid (mid)
            BuildItemGrid();

            // Detail panel (bottom)
            BuildDetailPanel();

            // Back button
            var backBtn = CreateButton(transform, "Back", new Vector2(0, -880),
                new Vector2(380, 80), () => _onClose?.Invoke());
        }

        private void BuildEquipSlots()
        {
            var panel = new GameObject("EquipSlots");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(960, 280);
            pRt.anchoredPosition = new Vector2(0, 580);
            var pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.12f, 0.10f, 0.16f, 0.9f);
            _slotGrid = panel.transform;

            CreateText(panel.transform, "EQUIPPED", 22, FontStyle.Bold,
                new Color(0.7f, 0.65f, 0.8f), new Vector2(0, 110), new Vector2(400, 30));

            // Equipment slot positions (4 columns × 3 rows)
            var slots = new[]
            {
                (EquipSlot.Helmet,  new Vector2(-300,  40)),
                (EquipSlot.Amulet,  new Vector2(-100,  40)),
                (EquipSlot.MainHand,new Vector2( 100,  40)),
                (EquipSlot.Chest,   new Vector2( 300,  40)),
                (EquipSlot.Gloves,  new Vector2(-300, -60)),
                (EquipSlot.Legs,    new Vector2(-100, -60)),
                (EquipSlot.Feet,    new Vector2( 100, -60)),
                (EquipSlot.Ring1,   new Vector2( 300, -60)),
            };

            foreach (var (slot, pos) in slots)
            {
                CreateEquipSlot(panel.transform, slot, pos);
            }
        }

        private void CreateEquipSlot(Transform parent, EquipSlot slot, Vector2 pos)
        {
            var go = new GameObject($"Slot_{slot}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 70);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.15f, 0.22f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            string itemId = null;
            if (GameManager.Instance?.PlayerCharacter != null)
                GameManager.Instance.PlayerCharacter.Data.EquippedItems.TryGetValue(slot, out itemId);

            var item = ItemRegistry.Get(itemId);
            string label = item != null
                ? $"<{slot}>\n{item.Name}"
                : $"<{slot}>\n(empty)";
            var color = item != null
                ? SpriteFactory.GetRarityColor(item.Rarity)
                : new Color(0.5f, 0.5f, 0.55f);

            CreateText(go.transform, label, 14, FontStyle.Normal, color,
                Vector2.zero, new Vector2(170, 60));

            if (item != null)
            {
                btn.onClick.AddListener(() => SelectItem(item));
            }
        }

        private void BuildItemGrid()
        {
            var panel = new GameObject("ItemGrid");
            panel.transform.SetParent(transform, false);
            var pRt = panel.AddComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(960, 600);
            pRt.anchoredPosition = new Vector2(0, 100);
            var pImg = panel.AddComponent<Image>();
            pImg.color = new Color(0.10f, 0.08f, 0.14f, 0.9f);
            _itemGrid = panel.transform;

            CreateText(panel.transform, "BAG", 22, FontStyle.Bold,
                new Color(0.7f, 0.65f, 0.8f), new Vector2(0, 270), new Vector2(400, 30));

            RefreshItemGrid();
        }

        private void RefreshItemGrid()
        {
            if (_itemGrid == null) return;
            // Clear old item buttons
            for (int i = _itemGrid.childCount - 1; i >= 0; i--)
            {
                var child = _itemGrid.GetChild(i);
                if (child.name.StartsWith("ItemBtn_"))
                    Destroy(child.gameObject);
            }

            var inv = GameManager.Instance?.PlayerInventory;
            if (inv == null) return;

            int col = 0, row = 0;
            const int COLS = 4;
            float startX = -345f, startY = 200f, dx = 230f, dy = 80f;

            foreach (var item in inv.Items)
            {
                var go = new GameObject($"ItemBtn_{item.Id}");
                go.transform.SetParent(_itemGrid, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(220, 70);
                rt.anchoredPosition = new Vector2(startX + col * dx, startY - row * dy);
                var img = go.AddComponent<Image>();
                var rarityColor = SpriteFactory.GetRarityColor(item.Rarity);
                img.color = new Color(rarityColor.r * 0.3f, rarityColor.g * 0.3f, rarityColor.b * 0.3f, 0.95f);
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;

                CreateText(go.transform, $"{item.Name}\nLv.{item.Level}  +{item.UpgradeLevel}",
                    14, FontStyle.Normal, rarityColor, Vector2.zero, new Vector2(210, 60));

                var captured = item;
                btn.onClick.AddListener(() => SelectItem(captured));

                col++;
                if (col >= COLS) { col = 0; row++; }
                if (row > 6) break;
            }
        }

        private void BuildDetailPanel()
        {
            _detailPanel = new GameObject("DetailPanel");
            _detailPanel.transform.SetParent(transform, false);
            var rt = _detailPanel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(960, 280);
            rt.anchoredPosition = new Vector2(0, -440);
            var img = _detailPanel.AddComponent<Image>();
            img.color = new Color(0.14f, 0.10f, 0.18f, 0.95f);

            _detailName = CreateText(_detailPanel.transform, "Select an item", 28, FontStyle.Bold,
                new Color(0.85f, 0.85f, 0.95f), new Vector2(0, 100), new Vector2(900, 40));

            _detailStats = CreateText(_detailPanel.transform, "", 20, FontStyle.Normal,
                new Color(0.75f, 0.75f, 0.85f), new Vector2(0, 0), new Vector2(900, 200));

            _equipButton = CreateButton(_detailPanel.transform, "EQUIP",
                new Vector2(-130, -100), new Vector2(220, 60), TryEquip);
            _equipButton.gameObject.SetActive(false);

            _unequipButton = CreateButton(_detailPanel.transform, "UNEQUIP",
                new Vector2(130, -100), new Vector2(220, 60), TryUnequip);
            _unequipButton.gameObject.SetActive(false);
        }

        private void SelectItem(ItemData item)
        {
            _selectedItem = item;
            if (item == null)
            {
                _detailName.text = "Select an item";
                _detailStats.text = "";
                _equipButton.gameObject.SetActive(false);
                _unequipButton.gameObject.SetActive(false);
                return;
            }

            _detailName.text = $"{item.Name}  [{item.Rarity}]  Lv.{item.Level} +{item.UpgradeLevel}";
            _detailName.color = SpriteFactory.GetRarityColor(item.Rarity);

            var stats = item.GetUpgradedStats();
            string s = "";
            if (item.BaseDamage > 0) s += $"DMG {item.GetUpgradedDamage()}    ";
            if (item.BaseDefense > 0) s += $"DEF {item.GetUpgradedDefense()}    ";
            if (stats.STR != 0) s += $"STR +{stats.STR}  ";
            if (stats.INT != 0) s += $"INT +{stats.INT}  ";
            if (stats.DEX != 0) s += $"DEX +{stats.DEX}  ";
            if (stats.VIT != 0) s += $"VIT +{stats.VIT}  ";
            if (stats.SPD != 0) s += $"SPD +{stats.SPD}  ";
            if (stats.CRI != 0) s += $"CRI +{stats.CRI}  ";
            if (stats.MAG != 0) s += $"MAG +{stats.MAG}  ";
            if (stats.DEF != 0) s += $"DEF +{stats.DEF}  ";
            if (stats.ACC != 0) s += $"ACC +{stats.ACC}  ";
            if (stats.EVA != 0) s += $"EVA +{stats.EVA}  ";
            _detailStats.text = s;

            // Show equip / unequip buttons
            var pc = GameManager.Instance?.PlayerCharacter;
            bool isEquipped = pc != null && pc.Data.EquippedItems.TryGetValue(item.Slot, out var eid)
                && eid == item.Id;
            _equipButton.gameObject.SetActive(!isEquipped);
            _unequipButton.gameObject.SetActive(isEquipped);
        }

        private void TryEquip()
        {
            if (_selectedItem == null) return;
            var pc = GameManager.Instance?.PlayerCharacter;
            if (pc == null) return;
            pc.Equip(_selectedItem);
            Rebuild();
        }

        private void TryUnequip()
        {
            if (_selectedItem == null) return;
            var pc = GameManager.Instance?.PlayerCharacter;
            if (pc == null) return;
            pc.Unequip(_selectedItem.Slot);
            Rebuild();
        }

        private void Rebuild()
        {
            // Clear and rebuild equipment panel
            if (_slotGrid != null)
            {
                for (int i = _slotGrid.childCount - 1; i >= 0; i--)
                {
                    var c = _slotGrid.GetChild(i);
                    if (c.name.StartsWith("Slot_")) Destroy(c.gameObject);
                }
                var slots = new[]
                {
                    (EquipSlot.Helmet,  new Vector2(-300,  40)),
                    (EquipSlot.Amulet,  new Vector2(-100,  40)),
                    (EquipSlot.MainHand,new Vector2( 100,  40)),
                    (EquipSlot.Chest,   new Vector2( 300,  40)),
                    (EquipSlot.Gloves,  new Vector2(-300, -60)),
                    (EquipSlot.Legs,    new Vector2(-100, -60)),
                    (EquipSlot.Feet,    new Vector2( 100, -60)),
                    (EquipSlot.Ring1,   new Vector2( 300, -60)),
                };
                foreach (var (slot, pos) in slots) CreateEquipSlot(_slotGrid, slot, pos);
            }
            RefreshItemGrid();
            SelectItem(_selectedItem); // refresh detail
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
