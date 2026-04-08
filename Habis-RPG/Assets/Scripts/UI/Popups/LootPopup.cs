// ============================================================================
// Habis RPG — Loot Popup
// Animated drop notification: rarity glow, item name, scrolling reward list.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;
using HabisRPG.Items;
using HabisRPG.UI.Combat;

namespace HabisRPG.UI.Popups
{
    public static class LootPopup
    {
        public static IEnumerator Show(Transform canvasRoot, List<ItemData> items, int gold, int xp)
        {
            var go = new GameObject("LootPopup");
            go.transform.SetParent(canvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720, 760);
            rt.anchoredPosition = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.05f, 0.10f, 0.95f);
            bg.raycastTarget = true;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Border glow (color depends on best rarity)
            ItemRarity best = ItemRarity.Common;
            foreach (var i in items) if ((int)i.Rarity > (int)best) best = i.Rarity;
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(go.transform, false);
            var bRt = borderGO.AddComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(-8, -8); bRt.offsetMax = new Vector2(8, 8);
            var bImg = borderGO.AddComponent<Image>();
            bImg.color = new Color(SpriteFactory.GetRarityColor(best).r,
                                   SpriteFactory.GetRarityColor(best).g,
                                   SpriteFactory.GetRarityColor(best).b, 0.4f);

            // Title
            CreateText(go.transform, "VICTORY!", 56, FontStyle.Bold,
                new Color(1f, 0.85f, 0.3f), new Vector2(0, 320), new Vector2(700, 80));

            CreateText(go.transform, $"+{xp} XP    +{gold} Gold", 32, FontStyle.Normal,
                new Color(0.9f, 0.9f, 0.95f), new Vector2(0, 250), new Vector2(700, 50));

            // Item list (scrollable area)
            float y = 170;
            int shown = 0;
            foreach (var item in items)
            {
                if (shown >= 10) break;
                var rowGO = new GameObject($"LootRow_{shown}");
                rowGO.transform.SetParent(go.transform, false);
                var rRt = rowGO.AddComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(640, 56);
                rRt.anchoredPosition = new Vector2(0, y);
                var rImg = rowGO.AddComponent<Image>();
                rImg.color = new Color(SpriteFactory.GetRarityColor(item.Rarity).r * 0.5f,
                                       SpriteFactory.GetRarityColor(item.Rarity).g * 0.5f,
                                       SpriteFactory.GetRarityColor(item.Rarity).b * 0.5f, 0.45f);

                CreateText(rowGO.transform,
                    $"{item.Name}  Lv.{item.Level}",
                    24, FontStyle.Normal,
                    SpriteFactory.GetRarityColor(item.Rarity),
                    new Vector2(-10, 0), new Vector2(620, 50));

                y -= 64;
                shown++;
            }

            if (items.Count == 0)
            {
                CreateText(go.transform, "No items dropped this time...", 22, FontStyle.Italic,
                    new Color(0.6f, 0.6f, 0.65f), new Vector2(0, 100), new Vector2(700, 40));
            }

            // Continue button
            var btnGO = new GameObject("ContinueBtn");
            btnGO.transform.SetParent(go.transform, false);
            var btnRt = btnGO.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(380, 80);
            btnRt.anchoredPosition = new Vector2(0, -320);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.45f, 0.20f, 0.55f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            CreateText(btnGO.transform, "CONTINUE", 28, FontStyle.Bold,
                Color.white, Vector2.zero, new Vector2(380, 80));

            bool clicked = false;
            btn.onClick.AddListener(() => clicked = true);

            // Fade in + scale pop
            go.transform.localScale = Vector3.one * 0.7f;
            VFXRunner.Instance.StartCoroutine(UITween.ScaleTo(go.transform,
                Vector3.one * 0.7f, Vector3.one, 0.35f, UITween.EaseOutBack));
            yield return UITween.FadeCanvasGroup(cg, 0f, 1f, 0.3f);

            while (!clicked) yield return null;

            yield return UITween.FadeCanvasGroup(cg, 1f, 0f, 0.2f);
            Object.Destroy(go);
        }

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
            t.raycastTarget = false;
            return t;
        }
    }
}
