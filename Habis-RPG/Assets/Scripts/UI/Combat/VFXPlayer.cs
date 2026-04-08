// ============================================================================
// Habis RPG — VFX Player
// Spawns short-lived visual effects (slash, glow, explosion, projectile, heal)
// over a target UnitView. All procedural — no asset dependency.
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Core;

namespace HabisRPG.UI.Combat
{
    public static class VFXPlayer
    {
        // ---- Slash (melee swing) ----
        public static IEnumerator Slash(Transform anchor, Color tint)
        {
            var go = new GameObject("VFX_Slash");
            go.transform.SetParent(anchor, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 90);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-25f, 25f));
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.GetSlashSprite(tint);
            img.raycastTarget = false;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            float t = 0f;
            const float dur = 0.28f;
            Vector3 startScale = new Vector3(0.5f, 0.5f, 1f);
            Vector3 endScale = new Vector3(1.4f, 1.1f, 1f);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                go.transform.localScale = Vector3.Lerp(startScale, endScale, k);
                cg.alpha = 1f - k;
                yield return null;
            }
            Object.Destroy(go);
        }

        // ---- Burst (explosion / arcane impact) ----
        public static IEnumerator Burst(Transform anchor, Color tint, float size = 220f)
        {
            var go = new GameObject("VFX_Burst");
            go.transform.SetParent(anchor, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.GetVFXSprite("burst", tint);
            img.raycastTarget = false;

            var cg = go.AddComponent<CanvasGroup>();
            float t = 0f;
            const float dur = 0.45f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.6f, UITween.EaseOutQuad(k));
                cg.alpha = 1f - k;
                yield return null;
            }
            Object.Destroy(go);
        }

        // ---- Projectile (mage bolt, arrow) ----
        public static IEnumerator Projectile(Transform parent, Vector2 from, Vector2 to,
            Color tint, float duration = 0.35f)
        {
            var go = new GameObject("VFX_Projectile");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);
            rt.anchoredPosition = from;
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.GetVFXSprite("bolt", tint);
            img.raycastTarget = false;

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = UITween.EaseOutQuad(Mathf.Clamp01(t / duration));
                rt.anchoredPosition = Vector2.Lerp(from, to, k);
                rt.localRotation = Quaternion.Euler(0, 0, t * 720f);
                yield return null;
            }
            Object.Destroy(go);
        }

        // ---- Heal sparkle ----
        public static IEnumerator Heal(Transform anchor)
        {
            var tint = new Color(0.55f, 1f, 0.7f, 1f);
            yield return Burst(anchor, tint, 180f);
        }

        // ---- Screen flash (full canvas overlay) ----
        public static IEnumerator ScreenFlash(Transform canvasRoot, Color color, float duration = 0.2f)
        {
            var go = new GameObject("ScreenFlash");
            go.transform.SetParent(canvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                img.color = new Color(color.r, color.g, color.b, color.a * (1f - t / duration));
                yield return null;
            }
            Object.Destroy(go);
        }

        // ---- Damage popup ----
        public static void SpawnDamagePopup(Transform anchor, int damage, bool isCrit, bool isHeal = false, bool isMiss = false)
        {
            var go = new GameObject("DmgPopup");
            go.transform.SetParent(anchor, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 60);
            rt.anchoredPosition = new Vector2(Random.Range(-30f, 30f), 40f);

            var txt = go.AddComponent<Text>();
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            if (isMiss)
            {
                txt.text = "MISS";
                txt.fontSize = 38;
                txt.color = new Color(0.7f, 0.7f, 0.7f);
            }
            else if (isHeal)
            {
                txt.text = $"+{damage}";
                txt.fontSize = 42;
                txt.color = new Color(0.4f, 1f, 0.55f);
            }
            else if (isCrit)
            {
                txt.text = $"{damage}!";
                txt.fontSize = 60;
                txt.color = new Color(1f, 0.78f, 0.15f);
                txt.fontStyle = FontStyle.Bold;
            }
            else
            {
                txt.text = damage.ToString();
                txt.fontSize = 38;
                txt.color = new Color(1f, 0.95f, 0.85f);
            }

            // Outline for readability
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(2, -2);

            // Animate via runner
            VFXRunner.Instance.StartCoroutine(AnimatePopup(go, rt, txt));
        }

        private static IEnumerator AnimatePopup(GameObject go, RectTransform rt, Text txt)
        {
            Vector2 start = rt.anchoredPosition;
            Vector2 end = start + new Vector2(0, 110f);
            // Pop scale
            yield return UITween.PunchScale(go.transform, 0.4f, 0.18f);
            float t = 0f;
            const float dur = 0.85f;
            Color baseColor = txt.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                rt.anchoredPosition = Vector2.Lerp(start, end, UITween.EaseOutQuad(k));
                txt.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
                yield return null;
            }
            Object.Destroy(go);
        }
    }

    // Helper MonoBehaviour to host coroutines from static contexts
    public class VFXRunner : MonoBehaviour
    {
        private static VFXRunner _instance;
        public static VFXRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("VFXRunner");
                    Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<VFXRunner>();
                }
                return _instance;
            }
        }
    }
}
