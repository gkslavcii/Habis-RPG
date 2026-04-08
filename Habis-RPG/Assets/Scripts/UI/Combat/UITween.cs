// ============================================================================
// Habis RPG — Lightweight UI Tween Helpers
// Coroutine-based, no third-party dependencies. Used by combat animations.
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HabisRPG.UI.Combat
{
    public static class UITween
    {
        // ---- Easing curves ----
        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseInQuad(float t) => t * t;
        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        // ---- Move ----
        public static IEnumerator MoveAnchored(RectTransform rt, Vector2 from, Vector2 to,
            float duration, Func<float, float> ease = null)
        {
            ease ??= EaseOutQuad;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float k = ease(Mathf.Clamp01(t / duration));
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = to;
        }

        // ---- Scale ----
        public static IEnumerator ScaleTo(Transform tr, Vector3 from, Vector3 to,
            float duration, Func<float, float> ease = null)
        {
            ease ??= EaseOutBack;
            float t = 0f;
            while (t < duration && tr != null)
            {
                t += Time.deltaTime;
                float k = ease(Mathf.Clamp01(t / duration));
                tr.localScale = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
            if (tr != null) tr.localScale = to;
        }

        // ---- Color (Image / Graphic) ----
        public static IEnumerator ColorTo(Graphic g, Color from, Color to,
            float duration, Func<float, float> ease = null)
        {
            ease ??= EaseInOutQuad;
            float t = 0f;
            while (t < duration && g != null)
            {
                t += Time.deltaTime;
                float k = ease(Mathf.Clamp01(t / duration));
                g.color = Color.LerpUnclamped(from, to, k);
                yield return null;
            }
            if (g != null) g.color = to;
        }

        // ---- Fade ----
        public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to,
            float duration, Func<float, float> ease = null)
        {
            ease ??= EaseInOutQuad;
            float t = 0f;
            while (t < duration && cg != null)
            {
                t += Time.deltaTime;
                float k = ease(Mathf.Clamp01(t / duration));
                cg.alpha = Mathf.LerpUnclamped(from, to, k);
                yield return null;
            }
            if (cg != null) cg.alpha = to;
        }

        // ---- Shake (anchored position) ----
        public static IEnumerator ShakeAnchored(RectTransform rt, float duration, float magnitude)
        {
            if (rt == null) yield break;
            Vector2 origin = rt.anchoredPosition;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float falloff = 1f - (t / duration);
                Vector2 offset = new Vector2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)) * magnitude * falloff;
                rt.anchoredPosition = origin + offset;
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = origin;
        }

        // ---- Punch scale (Sonny-style hit pop) ----
        public static IEnumerator PunchScale(Transform tr, float magnitude, float duration)
        {
            if (tr == null) yield break;
            Vector3 origin = tr.localScale;
            Vector3 peak = origin * (1f + magnitude);
            float half = duration * 0.5f;
            yield return ScaleTo(tr, origin, peak, half, EaseOutQuad);
            yield return ScaleTo(tr, peak, origin, half, EaseInQuad);
        }
    }
}
