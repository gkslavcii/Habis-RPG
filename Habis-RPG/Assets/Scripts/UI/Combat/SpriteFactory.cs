// ============================================================================
// Habis RPG — Procedural Sprite Factory
// Generates simple silhouette/shape sprites at runtime so we can ship with
// zero art assets. Real sprites can later be dropped into Resources/Art/...
// and SpriteFactory.LoadOrGenerate() will prefer them automatically.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using HabisRPG.Core;

namespace HabisRPG.UI.Combat
{
    public static class SpriteFactory
    {
        private static readonly Dictionary<string, Sprite> _cache = new();

        // ---- Public API ----

        public static Sprite GetCharacterSprite(CharacterClass cls, bool isEnemy)
        {
            string key = $"{(isEnemy ? "enemy_" : "hero_")}{cls}";
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            // Try real asset first
            var loaded = Resources.Load<Sprite>($"Art/Characters/{key}");
            if (loaded != null) { _cache[key] = loaded; return loaded; }

            // Generate silhouette
            Color body = isEnemy ? GetEnemyColor(cls) : GetHeroColor(cls);
            var tex = BuildSilhouette(body, isEnemy);
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.0f), 100f);
            sprite.name = key;
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetVFXSprite(string name, Color tint)
        {
            string key = $"vfx_{name}_{ColorToHex(tint)}";
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            var loaded = Resources.Load<Sprite>($"Art/VFX/{name}");
            if (loaded != null) { _cache[key] = loaded; return loaded; }

            var tex = BuildRadialGlow(tint);
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetSlashSprite(Color tint)
        {
            string key = $"slash_{ColorToHex(tint)}";
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            var tex = BuildSlash(tint);
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetBackground(Region region)
        {
            string key = $"bg_{region}";
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            var loaded = Resources.Load<Sprite>($"Art/Backgrounds/{region}");
            if (loaded != null) { _cache[key] = loaded; return loaded; }

            var tex = BuildBackground(region);
            var sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetSolidSprite(Color color)
        {
            string key = $"solid_{ColorToHex(color)}";
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = color;
            tex.SetPixels(px);
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            sp.name = key;
            _cache[key] = sp;
            return sp;
        }

        // ---- Generators ----

        private static Texture2D BuildSilhouette(Color body, bool isEnemy)
        {
            const int W = 128, H = 192;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var clear = new Color(0, 0, 0, 0);
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            Color outline = new Color(0.05f, 0.05f, 0.05f, 1f);
            Color shade = new Color(body.r * 0.6f, body.g * 0.6f, body.b * 0.6f, 1f);

            // Head
            DrawCircle(px, W, H, W / 2, H - 30, 22, body, outline);
            // Torso (trapezoid via columns)
            for (int y = H - 55; y > H - 120; y--)
            {
                int width = 28 + (H - 55 - y) / 4;
                for (int x = W / 2 - width; x <= W / 2 + width; x++)
                {
                    if (x < 0 || x >= W) continue;
                    bool isEdge = (x == W / 2 - width || x == W / 2 + width);
                    px[y * W + x] = isEdge ? outline :
                        (x < W / 2 ? shade : body);
                }
            }
            // Legs
            for (int y = H - 120; y > 10; y--)
            {
                for (int dx = -24; dx <= -4; dx++)
                {
                    int x = W / 2 + dx;
                    if (x >= 0 && x < W) px[y * W + x] = shade;
                }
                for (int dx = 4; dx <= 24; dx++)
                {
                    int x = W / 2 + dx;
                    if (x >= 0 && x < W) px[y * W + x] = body;
                }
            }
            // Enemy: add spikes / horns
            if (isEnemy)
            {
                DrawTriangle(px, W, H, W / 2 - 18, H - 12, 6, 14, outline);
                DrawTriangle(px, W, H, W / 2 + 18, H - 12, 6, 14, outline);
            }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildRadialGlow(Color tint)
        {
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[S * S];
            Vector2 c = new Vector2(S / 2f, S / 2f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (S / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    px[y * S + x] = new Color(tint.r, tint.g, tint.b, a * tint.a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildSlash(Color tint)
        {
            const int W = 256, H = 96;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
            // Diagonal slash arc: two curves
            for (int x = 0; x < W; x++)
            {
                float t = x / (float)W;
                int yCenter = (int)(H * 0.5f + Mathf.Sin(t * Mathf.PI) * 12f);
                for (int dy = -6; dy <= 6; dy++)
                {
                    int y = yCenter + dy;
                    if (y < 0 || y >= H) continue;
                    float a = (1f - Mathf.Abs(dy) / 6f) * Mathf.Sin(t * Mathf.PI);
                    px[y * W + x] = new Color(tint.r, tint.g, tint.b, a * tint.a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Texture2D BuildBackground(Region region)
        {
            const int W = 512, H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[W * H];
            (Color top, Color bot) = GetRegionGradient(region);
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)H;
                Color c = Color.Lerp(bot, top, t);
                for (int x = 0; x < W; x++)
                {
                    // Vignette
                    float vx = (x - W / 2f) / (W / 2f);
                    float vy = (y - H / 2f) / (H / 2f);
                    float v = 1f - Mathf.Clamp01(vx * vx + vy * vy);
                    px[y * W + x] = new Color(c.r * v, c.g * v, c.b * v, 1f);
                }
            }
            // Ground line
            for (int x = 0; x < W; x++)
            {
                int y = 60;
                px[y * W + x] = new Color(0.05f, 0.05f, 0.05f, 1f);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ---- Color helpers ----

        public static Color GetHeroColor(CharacterClass cls) => cls switch
        {
            CharacterClass.Warrior => new Color(0.75f, 0.25f, 0.25f),
            CharacterClass.Mage => new Color(0.35f, 0.45f, 0.95f),
            CharacterClass.Rogue => new Color(0.30f, 0.75f, 0.40f),
            _ => Color.gray
        };

        public static Color GetEnemyColor(CharacterClass cls) => cls switch
        {
            CharacterClass.Warrior => new Color(0.45f, 0.20f, 0.30f),
            CharacterClass.Mage => new Color(0.40f, 0.25f, 0.55f),
            CharacterClass.Rogue => new Color(0.30f, 0.40f, 0.25f),
            _ => new Color(0.4f, 0.4f, 0.4f)
        };

        public static Color GetRarityColor(ItemRarity r) => r switch
        {
            ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),
            ItemRarity.Rare => new Color(0.3f, 0.55f, 1f),
            ItemRarity.Epic => new Color(0.75f, 0.3f, 0.95f),
            ItemRarity.Legendary => new Color(1f, 0.78f, 0.15f),
            _ => Color.white
        };

        private static (Color top, Color bot) GetRegionGradient(Region r) => r switch
        {
            Region.CursedForest => (new Color(0.10f, 0.13f, 0.10f), new Color(0.04f, 0.06f, 0.04f)),
            Region.RottingSwamp => (new Color(0.18f, 0.20f, 0.10f), new Color(0.06f, 0.08f, 0.04f)),
            Region.ShatteredMountain => (new Color(0.20f, 0.18f, 0.22f), new Color(0.05f, 0.05f, 0.07f)),
            Region.AbyssalDungeon => (new Color(0.15f, 0.10f, 0.18f), new Color(0.03f, 0.02f, 0.05f)),
            Region.AncientRuins => (new Color(0.22f, 0.18f, 0.12f), new Color(0.06f, 0.05f, 0.03f)),
            Region.VoidAbyss => (new Color(0.12f, 0.05f, 0.20f), new Color(0.02f, 0.0f, 0.05f)),
            _ => (new Color(0.12f, 0.10f, 0.12f), new Color(0.03f, 0.03f, 0.04f))
        };

        // ---- Drawing primitives ----

        private static void DrawCircle(Color[] px, int W, int H, int cx, int cy, int r, Color fill, Color outline)
        {
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    int dist2 = x * x + y * y;
                    if (dist2 > r * r) continue;
                    int px_ = cx + x, py_ = cy + y;
                    if (px_ < 0 || px_ >= W || py_ < 0 || py_ >= H) continue;
                    bool edge = dist2 > (r - 2) * (r - 2);
                    px[py_ * W + px_] = edge ? outline : fill;
                }
        }

        private static void DrawTriangle(Color[] px, int W, int H, int cx, int cy, int halfBase, int height, Color color)
        {
            for (int y = 0; y < height; y++)
            {
                int row = cy + y;
                if (row < 0 || row >= H) continue;
                int half = (int)(halfBase * (1f - y / (float)height));
                for (int x = -half; x <= half; x++)
                {
                    int col = cx + x;
                    if (col < 0 || col >= W) continue;
                    px[row * W + col] = color;
                }
            }
        }

        private static string ColorToHex(Color c) =>
            ((int)(c.r * 255)).ToString("X2") +
            ((int)(c.g * 255)).ToString("X2") +
            ((int)(c.b * 255)).ToString("X2") +
            ((int)(c.a * 255)).ToString("X2");
    }
}
