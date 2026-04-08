// ============================================================================
// Habis RPG — UnitView
// Visual representation of a single combatant in the combat scene.
// Holds sprite, HP bar, name plate, status icons, vfx anchor.
// ============================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Character;
using HabisRPG.Core;

namespace HabisRPG.UI.Combat
{
    public class UnitView : MonoBehaviour
    {
        public HabisCharacter Character;
        public bool IsEnemy;

        private RectTransform _rt;
        private Image _bodyImage;
        private Image _shadowImage;
        private Image _hpBarBg;
        private Image _hpBarFill;
        private Text _nameText;
        private Text _hpText;
        private Transform _vfxAnchor;
        private Transform _statusRow;

        private Color _bodyOriginalColor;
        private Vector2 _homePosition;

        public Vector2 HomePosition => _homePosition;
        public RectTransform RT => _rt;
        public Transform VFXAnchor => _vfxAnchor;

        public static UnitView Create(Transform parent, HabisCharacter character,
            bool isEnemy, Vector2 anchoredPos)
        {
            var go = new GameObject($"UnitView_{character.Data.Name}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<UnitView>();
            view.Build(character, isEnemy, anchoredPos);
            return view;
        }

        private void Build(HabisCharacter character, bool isEnemy, Vector2 anchoredPos)
        {
            Character = character;
            IsEnemy = isEnemy;
            _rt = gameObject.AddComponent<RectTransform>();
            _rt.sizeDelta = new Vector2(180, 220);
            _rt.anchoredPosition = anchoredPos;
            _homePosition = anchoredPos;

            // Shadow ellipse under feet
            var shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(transform, false);
            var sRt = shadowGO.AddComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(120, 22);
            sRt.anchoredPosition = new Vector2(0, -100);
            _shadowImage = shadowGO.AddComponent<Image>();
            _shadowImage.sprite = SpriteFactory.GetVFXSprite("shadow", new Color(0, 0, 0, 0.55f));

            // Body sprite
            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(transform, false);
            var bRt = bodyGO.AddComponent<RectTransform>();
            bRt.sizeDelta = new Vector2(160, 220);
            bRt.anchoredPosition = new Vector2(0, 10);
            _bodyImage = bodyGO.AddComponent<Image>();
            _bodyImage.sprite = SpriteFactory.GetCharacterSprite(character.Data.PrimaryClass, isEnemy);
            _bodyImage.preserveAspect = true;
            _bodyOriginalColor = _bodyImage.color;
            // Mirror enemies so they face left
            if (isEnemy) bRt.localScale = new Vector3(-1f, 1f, 1f);

            // Name plate
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(transform, false);
            var nRt = nameGO.AddComponent<RectTransform>();
            nRt.sizeDelta = new Vector2(220, 28);
            nRt.anchoredPosition = new Vector2(0, 130);
            _nameText = nameGO.AddComponent<Text>();
            _nameText.text = $"{character.Data.Name}  Lv.{character.Data.Level}";
            _nameText.alignment = TextAnchor.MiddleCenter;
            _nameText.fontSize = 20;
            _nameText.color = isEnemy ? new Color(1f, 0.55f, 0.55f) : new Color(0.85f, 0.95f, 1f);
            _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nameText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // HP bar background
            var hpBgGO = new GameObject("HPBg");
            hpBgGO.transform.SetParent(transform, false);
            var hpBgRt = hpBgGO.AddComponent<RectTransform>();
            hpBgRt.sizeDelta = new Vector2(160, 12);
            hpBgRt.anchoredPosition = new Vector2(0, 108);
            _hpBarBg = hpBgGO.AddComponent<Image>();
            _hpBarBg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

            // HP bar fill
            var hpFillGO = new GameObject("HPFill");
            hpFillGO.transform.SetParent(hpBgGO.transform, false);
            var hpFillRt = hpFillGO.AddComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.offsetMin = Vector2.zero;
            hpFillRt.offsetMax = Vector2.zero;
            _hpBarFill = hpFillGO.AddComponent<Image>();
            _hpBarFill.color = isEnemy ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.3f, 0.85f, 0.35f);

            // HP text
            var hpTxtGO = new GameObject("HPText");
            hpTxtGO.transform.SetParent(transform, false);
            var hpTxtRt = hpTxtGO.AddComponent<RectTransform>();
            hpTxtRt.sizeDelta = new Vector2(160, 14);
            hpTxtRt.anchoredPosition = new Vector2(0, 108);
            _hpText = hpTxtGO.AddComponent<Text>();
            _hpText.alignment = TextAnchor.MiddleCenter;
            _hpText.fontSize = 13;
            _hpText.color = Color.white;
            _hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Status icon row
            var statusGO = new GameObject("StatusRow");
            statusGO.transform.SetParent(transform, false);
            var stRt = statusGO.AddComponent<RectTransform>();
            stRt.sizeDelta = new Vector2(160, 18);
            stRt.anchoredPosition = new Vector2(0, 88);
            _statusRow = statusGO.transform;

            // VFX anchor
            var vfxGO = new GameObject("VFXAnchor");
            vfxGO.transform.SetParent(transform, false);
            var vfxRt = vfxGO.AddComponent<RectTransform>();
            vfxRt.sizeDelta = Vector2.zero;
            vfxRt.anchoredPosition = Vector2.zero;
            _vfxAnchor = vfxGO.transform;

            UpdateHP();
            UpdateStatusIcons();
        }

        // ---- Updates ----

        public void UpdateHP()
        {
            if (Character == null) return;
            float ratio = Character.Data.MaxHP > 0
                ? (float)Character.Data.CurrentHP / Character.Data.MaxHP
                : 0f;
            ratio = Mathf.Clamp01(ratio);
            var rt = _hpBarFill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(ratio, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _hpText.text = $"{Character.Data.CurrentHP}/{Character.Data.MaxHP}";

            if (IsEnemy)
                _hpBarFill.color = new Color(0.85f, 0.25f, 0.25f);
            else
                _hpBarFill.color = ratio > 0.5f
                    ? new Color(0.3f, 0.85f, 0.35f)
                    : ratio > 0.25f
                        ? new Color(0.95f, 0.78f, 0.2f)
                        : new Color(0.9f, 0.25f, 0.25f);
        }

        public void UpdateStatusIcons()
        {
            if (_statusRow == null) return;
            // Clear
            for (int i = _statusRow.childCount - 1; i >= 0; i--)
                Destroy(_statusRow.GetChild(i).gameObject);
            if (Character == null) return;

            int x = -((Character.Data.ActiveEffects.Count - 1) * 11);
            foreach (var fx in Character.Data.ActiveEffects)
            {
                var iconGO = new GameObject("Fx");
                iconGO.transform.SetParent(_statusRow, false);
                var rt = iconGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(18, 18);
                rt.anchoredPosition = new Vector2(x, 0);
                var img = iconGO.AddComponent<Image>();
                img.sprite = SpriteFactory.GetVFXSprite("status", GetStatusColor(fx.Type));
                x += 22;
            }
        }

        // ---- Animations ----

        public IEnumerator PlayHitFlash()
        {
            yield return UITween.ColorTo(_bodyImage, _bodyOriginalColor, Color.white, 0.05f);
            yield return UITween.ColorTo(_bodyImage, Color.white, _bodyOriginalColor, 0.15f);
        }

        public IEnumerator PlayShake()
        {
            yield return UITween.ShakeAnchored(_rt, 0.25f, 12f);
        }

        public IEnumerator PlayDeath()
        {
            // Add CanvasGroup for fade
            var cg = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            float t = 0f;
            Vector2 start = _rt.anchoredPosition;
            Vector2 end = start + new Vector2(0, -40);
            const float dur = 0.55f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                cg.alpha = 1f - k;
                _rt.anchoredPosition = Vector2.Lerp(start, end, k);
                _rt.localRotation = Quaternion.Euler(0, 0, k * (IsEnemy ? -75f : 75f));
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public IEnumerator PlayLunge(Vector2 toward, float distance, float duration)
        {
            Vector2 dir = (toward - _homePosition).normalized;
            Vector2 lungePos = _homePosition + dir * distance;
            yield return UITween.MoveAnchored(_rt, _homePosition, lungePos, duration * 0.4f, UITween.EaseOutCubic);
            yield return UITween.MoveAnchored(_rt, lungePos, _homePosition, duration * 0.6f, UITween.EaseInOutQuad);
        }

        public IEnumerator PlayCastPose()
        {
            // Slight upward bob + scale punch
            yield return UITween.PunchScale(transform, 0.12f, 0.35f);
        }

        public void ResetPose()
        {
            if (_rt != null)
            {
                _rt.anchoredPosition = _homePosition;
                _rt.localRotation = Quaternion.identity;
                _rt.localScale = Vector3.one;
            }
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        private static Color GetStatusColor(StatusEffectType t) => t switch
        {
            StatusEffectType.Burn => new Color(1f, 0.4f, 0.1f),
            StatusEffectType.Poison => new Color(0.4f, 0.85f, 0.2f),
            StatusEffectType.Stun => new Color(1f, 0.95f, 0.3f),
            StatusEffectType.Slow => new Color(0.6f, 0.7f, 1f),
            StatusEffectType.Weaken => new Color(0.65f, 0.45f, 0.85f),
            StatusEffectType.Blind => new Color(0.35f, 0.35f, 0.4f),
            StatusEffectType.Curse => new Color(0.5f, 0.1f, 0.5f),
            StatusEffectType.Confuse => new Color(0.95f, 0.55f, 0.85f),
            StatusEffectType.Haste => new Color(0.55f, 0.9f, 1f),
            StatusEffectType.Barrier => new Color(0.6f, 0.85f, 1f),
            StatusEffectType.Berserk => new Color(1f, 0.3f, 0.3f),
            StatusEffectType.Regen => new Color(0.5f, 1f, 0.6f),
            StatusEffectType.ArcaneShield => new Color(0.5f, 0.5f, 1f),
            StatusEffectType.CriticalBoost => new Color(1f, 0.8f, 0.3f),
            _ => Color.gray
        };
    }
}
