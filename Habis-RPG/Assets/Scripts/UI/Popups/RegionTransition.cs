// ============================================================================
// Habis RPG — Region Transition
// Cinematic full-screen wipe with region name, used when traveling.
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;
using HabisRPG.UI.Combat;

namespace HabisRPG.UI.Popups
{
    public static class RegionTransition
    {
        public static IEnumerator Play(Transform canvasRoot, Region region, string subtitle = null)
        {
            var go = new GameObject("RegionTransition");
            go.transform.SetParent(canvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = Color.black;
            bg.raycastTarget = true;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(go.transform, false);
            var tRt = titleGO.AddComponent<RectTransform>();
            tRt.sizeDelta = new Vector2(900, 120);
            tRt.anchoredPosition = new Vector2(0, 60);
            var title = titleGO.AddComponent<Text>();
            title.text = FormatRegionName(region).ToUpper();
            title.fontSize = 64;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.85f, 0.65f);
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.raycastTarget = false;

            // Subtitle
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(go.transform, false);
            var sRt = subGO.AddComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(900, 60);
            sRt.anchoredPosition = new Vector2(0, -20);
            var sub = subGO.AddComponent<Text>();
            sub.text = subtitle ?? GetFlavorText(region);
            sub.fontSize = 26;
            sub.fontStyle = FontStyle.Italic;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.7f, 0.65f, 0.55f);
            sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sub.horizontalOverflow = HorizontalWrapMode.Overflow;
            sub.raycastTarget = false;

            // Decorative line
            var lineGO = new GameObject("Line");
            lineGO.transform.SetParent(go.transform, false);
            var lRt = lineGO.AddComponent<RectTransform>();
            lRt.sizeDelta = new Vector2(0, 2);
            lRt.anchoredPosition = new Vector2(0, 18);
            var line = lineGO.AddComponent<Image>();
            line.color = new Color(0.95f, 0.85f, 0.65f, 0.7f);
            line.raycastTarget = false;

            // Phase 1: fade to black
            yield return UITween.FadeCanvasGroup(cg, 0f, 1f, 0.45f);

            // Phase 2: line expand
            float t = 0f;
            const float lineDur = 0.5f;
            while (t < lineDur)
            {
                t += Time.deltaTime;
                float k = UITween.EaseOutCubic(t / lineDur);
                lRt.sizeDelta = new Vector2(700f * k, 2f);
                yield return null;
            }

            // Phase 3: hold
            yield return new WaitForSeconds(1.1f);

            // Phase 4: fade out
            yield return UITween.FadeCanvasGroup(cg, 1f, 0f, 0.55f);
            Object.Destroy(go);
        }

        private static string FormatRegionName(Region r) => r switch
        {
            Region.CursedForest => "Cursed Forest",
            Region.RottingSwamp => "Rotting Swamp",
            Region.ShatteredMountain => "Shattered Mountain",
            Region.AbyssalDungeon => "Abyssal Dungeon",
            Region.AncientRuins => "Ancient Ruins",
            Region.VoidAbyss => "Void Abyss",
            _ => r.ToString()
        };

        private static string GetFlavorText(Region r) => r switch
        {
            Region.CursedForest => "Where the trees whisper of forgotten sins...",
            Region.RottingSwamp => "Each breath thickens with disease.",
            Region.ShatteredMountain => "Stones remember the breaking of the world.",
            Region.AbyssalDungeon => "Light surrenders below.",
            Region.AncientRuins => "Glory lies buried beneath ash.",
            Region.VoidAbyss => "Beyond this place, nothing remains.",
            _ => ""
        };
    }
}
