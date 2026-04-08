// ============================================================================
// Habis RPG — CombatSceneView
// Container for the visual battle scene: background, party slots, enemy slots,
// and the orchestrator for hit / skill / death animations.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HabisRPG.Character;
using HabisRPG.Core;

namespace HabisRPG.UI.Combat
{
    public class CombatSceneView : MonoBehaviour
    {
        // Slot positions (anchored to scene center)
        private static readonly Vector2[] PlayerSlots =
        {
            new Vector2(-360, -40),
            new Vector2(-260, -120),
            new Vector2(-160, -200),
            new Vector2(-450, 30),
        };

        private static readonly Vector2[] EnemySlots =
        {
            new Vector2(360, -40),
            new Vector2(260, -120),
            new Vector2(160, -200),
            new Vector2(450, 30),
        };

        private RectTransform _rt;
        private Image _backgroundImage;
        private Transform _unitsContainer;
        private Transform _vfxContainer;

        public List<UnitView> PlayerViews { get; private set; } = new();
        public List<UnitView> EnemyViews { get; private set; } = new();

        public Transform VFXContainer => _vfxContainer;

        public static CombatSceneView Create(Transform parent)
        {
            var go = new GameObject("CombatScene");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CombatSceneView>();
            view.Build();
            return view;
        }

        private void Build()
        {
            _rt = gameObject.AddComponent<RectTransform>();
            _rt.anchorMin = new Vector2(0, 0);
            _rt.anchorMax = new Vector2(1, 1);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(transform, false);
            var bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            _backgroundImage = bgGO.AddComponent<Image>();
            _backgroundImage.color = Color.white;
            _backgroundImage.preserveAspect = false;

            // Units container (centered)
            var unitsGO = new GameObject("Units");
            unitsGO.transform.SetParent(transform, false);
            var uRt = unitsGO.AddComponent<RectTransform>();
            uRt.anchorMin = new Vector2(0.5f, 0.5f);
            uRt.anchorMax = new Vector2(0.5f, 0.5f);
            uRt.sizeDelta = Vector2.zero;
            uRt.anchoredPosition = new Vector2(0, 200);
            _unitsContainer = unitsGO.transform;

            // VFX container (above units)
            var vfxGO = new GameObject("VFX");
            vfxGO.transform.SetParent(transform, false);
            var vRt = vfxGO.AddComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0.5f, 0.5f);
            vRt.anchorMax = new Vector2(0.5f, 0.5f);
            vRt.sizeDelta = Vector2.zero;
            vRt.anchoredPosition = new Vector2(0, 200);
            _vfxContainer = vfxGO.transform;
        }

        public void SetRegion(Region region)
        {
            _backgroundImage.sprite = SpriteFactory.GetBackground(region);
        }

        public void Populate(List<HabisCharacter> players, List<HabisCharacter> enemies)
        {
            ClearUnits();
            for (int i = 0; i < players.Count; i++)
            {
                var slot = PlayerSlots[Mathf.Min(i, PlayerSlots.Length - 1)];
                var v = UnitView.Create(_unitsContainer, players[i], false, slot);
                PlayerViews.Add(v);
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                var slot = EnemySlots[Mathf.Min(i, EnemySlots.Length - 1)];
                var v = UnitView.Create(_unitsContainer, enemies[i], true, slot);
                EnemyViews.Add(v);
            }
        }

        public void ClearUnits()
        {
            foreach (var v in PlayerViews) if (v != null) Destroy(v.gameObject);
            foreach (var v in EnemyViews) if (v != null) Destroy(v.gameObject);
            PlayerViews.Clear();
            EnemyViews.Clear();
        }

        public UnitView FindView(HabisCharacter character)
        {
            foreach (var v in PlayerViews) if (v.Character == character) return v;
            foreach (var v in EnemyViews) if (v.Character == character) return v;
            return null;
        }

        public void RefreshAllHP()
        {
            foreach (var v in PlayerViews) v.UpdateHP();
            foreach (var v in EnemyViews) v.UpdateHP();
            foreach (var v in PlayerViews) v.UpdateStatusIcons();
            foreach (var v in EnemyViews) v.UpdateStatusIcons();
        }

        // ---- Animation orchestration ----

        public IEnumerator PlayBasicAttack(UnitView attacker, UnitView target)
        {
            if (attacker == null || target == null) yield break;
            // 1) Lunge toward target
            yield return attacker.PlayLunge(target.HomePosition, 110f, 0.35f);
            // 2) Slash + hit reaction simultaneously
            VFXRunner.Instance.StartCoroutine(VFXPlayer.Slash(target.VFXAnchor,
                attacker.IsEnemy ? new Color(1f, 0.4f, 0.4f) : new Color(1f, 0.95f, 0.8f)));
            VFXRunner.Instance.StartCoroutine(target.PlayHitFlash());
            VFXRunner.Instance.StartCoroutine(target.PlayShake());
            VFXRunner.Instance.StartCoroutine(CameraShake(0.18f, 6f));
            yield return new WaitForSeconds(0.2f);
        }

        public IEnumerator PlaySkill(UnitView attacker, UnitView target,
            Color tint, bool isProjectile, bool isAoE)
        {
            if (attacker == null) yield break;

            // 1) Cast pose
            yield return attacker.PlayCastPose();

            if (isProjectile && target != null)
            {
                Vector2 from = attacker.RT.anchoredPosition;
                Vector2 to = target.RT.anchoredPosition;
                yield return VFXPlayer.Projectile(_unitsContainer, from, to, tint, 0.35f);
            }

            if (isAoE)
            {
                // Hit all enemies opposite to the caster
                var targets = attacker.IsEnemy ? PlayerViews : EnemyViews;
                foreach (var t in targets)
                {
                    if (t == null) continue;
                    VFXRunner.Instance.StartCoroutine(VFXPlayer.Burst(t.VFXAnchor, tint, 240f));
                    VFXRunner.Instance.StartCoroutine(t.PlayHitFlash());
                    VFXRunner.Instance.StartCoroutine(t.PlayShake());
                }
                yield return VFXPlayer.ScreenFlash(transform.parent, new Color(tint.r, tint.g, tint.b, 0.4f), 0.25f);
                yield return CameraShake(0.35f, 14f);
            }
            else if (target != null)
            {
                yield return VFXPlayer.Burst(target.VFXAnchor, tint, 220f);
                VFXRunner.Instance.StartCoroutine(target.PlayHitFlash());
                VFXRunner.Instance.StartCoroutine(target.PlayShake());
                yield return CameraShake(0.22f, 9f);
            }
        }

        public IEnumerator PlayDeath(UnitView view)
        {
            if (view == null) yield break;
            yield return view.PlayDeath();
        }

        public IEnumerator PlayHeal(UnitView target, int amount)
        {
            if (target == null) yield break;
            yield return VFXPlayer.Heal(target.VFXAnchor);
            VFXPlayer.SpawnDamagePopup(target.VFXAnchor, amount, false, true);
        }

        // ---- Camera (canvas) shake ----
        public IEnumerator CameraShake(float duration, float magnitude)
        {
            if (_rt == null) yield break;
            Vector2 origin = _rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float falloff = 1f - (t / duration);
                _rt.anchoredPosition = origin + new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)) * magnitude * falloff;
                yield return null;
            }
            _rt.anchoredPosition = origin;
        }
    }
}
