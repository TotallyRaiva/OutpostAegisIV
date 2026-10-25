using System.Collections.Generic;
using OutpostAegis.Data;
using UnityEngine;

namespace OutpostAegis.Visuals
{
    /// <summary>
    /// Synthesizes crisp 32 PPU (FilterMode.Point) grimdark pixel-art sprites at runtime/editor
    /// so every tower, Ork unit, Drop Pod, and build node has a clear silhouette out of the box.
    /// Custom imported PNG sprites assigned on TowerDataSO / EnemyDataSO automatically override these.
    /// </summary>
    public static class PixelSpriteGenerator
    {
        public const float PixelsPerUnit = 32f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite GetTowerBaseSprite(TowerCategory category, int tierIndex)
        {
            string key = $"tower_base_{category}_{tierIndex}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 32;
            Texture2D tex = CreateBlankTexture(size, size);
            Color hullDark = new Color(0.18f, 0.20f, 0.22f);
            Color hullMid = new Color(0.32f, 0.35f, 0.38f);
            Color goldTrim = new Color(0.78f, 0.64f, 0.22f);

            // Octagonal bunker base
            for (int y = 4; y < 28; y++)
            {
                for (int x = 4; x < 28; x++)
                {
                    int dx = Mathf.Abs(x - 15);
                    int dy = Mathf.Abs(y - 15);
                    if (dx + dy < 18)
                    {
                        bool isBorder = dx == 11 || dy == 11 || dx + dy >= 16;
                        tex.SetPixel(x, y, isBorder ? hullDark : hullMid);
                    }
                }
            }

            // Tier chevrons in gold trim
            for (int t = 0; t <= tierIndex && t < 3; t++)
            {
                int cy = 6 + t * 2;
                for (int x = 12; x <= 19; x++)
                {
                    tex.SetPixel(x, cy, goldTrim);
                }
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetTowerTurretSprite(TowerCategory category, int tierIndex)
        {
            string key = $"tower_turret_{category}_{tierIndex}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 32;
            Texture2D tex = CreateBlankTexture(size, size);

            Color primary = GetTowerPrimaryColor(category);
            Color barrel = new Color(0.15f, 0.16f, 0.18f);
            Color muzzle = GetTowerAccentColor(category);

            // Turret housing (center 10..21)
            for (int y = 9; y <= 22; y++)
            {
                for (int x = 9; x <= 22; x++)
                {
                    if ((x - 15) * (x - 15) + (y - 15) * (y - 15) <= 42)
                    {
                        tex.SetPixel(x, y, primary);
                    }
                }
            }

            // Weapon barrel pointing right (+X is forward in 2D turret rotation)
            if (category == TowerCategory.AstropathicRelay)
            {
                // Psychic dish / cogitator spire rings
                for (int r = 3; r <= 9; r += 3)
                {
                    DrawPixelCircleOutline(tex, 16, 16, r, muzzle);
                }
            }
            else if (category == TowerCategory.HeavyBolterBunker)
            {
                // Twin-linked heavy bolter barrels
                int barrelOffset = tierIndex >= 1 ? 2 : 0;
                DrawRect(tex, 18, 15 - barrelOffset, 29, 16 - barrelOffset, barrel);
                if (tierIndex >= 1)
                    DrawRect(tex, 18, 15 + barrelOffset, 29, 16 + barrelOffset, barrel);
                tex.SetPixel(29, 15 - barrelOffset, muzzle);
            }
            else if (category == TowerCategory.LascannonTurret)
            {
                // Long sniper las-barrel with glowing capacitor coils
                DrawRect(tex, 16, 14, 31, 17, barrel);
                for (int x = 20; x <= 28; x += 3)
                {
                    tex.SetPixel(x, 15, muzzle);
                    tex.SetPixel(x, 16, muzzle);
                }
            }
            else if (category == TowerCategory.HellhoundFlameBunker)
            {
                // Wide promethium nozzle + fuel tanks
                DrawRect(tex, 8, 9, 14, 22, new Color(0.75f, 0.22f, 0.1f));
                DrawRect(tex, 18, 13, 27, 18, barrel);
                DrawRect(tex, 26, 12, 29, 19, muzzle);
            }
            else if (category == TowerCategory.BasiliskArtilleryPit)
            {
                // Heavy Earthshaker cannon barrel
                DrawRect(tex, 14, 12, 30, 19, barrel);
                DrawRect(tex, 28, 11, 31, 20, muzzle);
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetEnemySprite(EnemyCategory category)
        {
            string key = $"enemy_{category}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = category == EnemyCategory.BossGargantWarboss ? 48 : 32;
            Texture2D tex = CreateBlankTexture(size, size);

            Color orkSkin = new Color(0.28f, 0.62f, 0.20f);
            Color orkDark = new Color(0.14f, 0.35f, 0.10f);
            Color scrapIron = new Color(0.42f, 0.40f, 0.38f);
            Color hazardRed = new Color(0.82f, 0.18f, 0.14f);
            Color stealthPurple = new Color(0.48f, 0.22f, 0.62f);

            int c = size / 2;
            int radius = category switch
            {
                EnemyCategory.SwarmGretchin => 6,
                EnemyCategory.SwarmOrkBoyz => 9,
                EnemyCategory.FastWarbiker => 10,
                EnemyCategory.FastStormboy => 9,
                EnemyCategory.StealthKommando => 9,
                EnemyCategory.ArmoredKillaKan => 12,
                EnemyCategory.ArmoredDeffDread => 14,
                EnemyCategory.BossGargantWarboss => 20,
                _ => 9
            };

            Color bodyColor = category switch
            {
                EnemyCategory.StealthKommando => stealthPurple,
                EnemyCategory.ArmoredKillaKan => scrapIron,
                EnemyCategory.ArmoredDeffDread => new Color(0.32f, 0.32f, 0.36f),
                EnemyCategory.BossGargantWarboss => hazardRed,
                EnemyCategory.FastWarbiker => hazardRed,
                _ => orkSkin
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int distSq = (x - c) * (x - c) + (y - c) * (y - c);
                    if (distSq <= radius * radius)
                    {
                        bool rim = distSq >= (radius - 2) * (radius - 2);
                        tex.SetPixel(x, y, rim ? orkDark : bodyColor);
                    }
                }
            }

            // Distinct pixel details (eyes, iron jaw, saws, or rokkit pack)
            tex.SetPixel(c + 3, c + 2, hazardRed);
            tex.SetPixel(c + 3, c - 2, hazardRed);

            if (category == EnemyCategory.ArmoredKillaKan || category == EnemyCategory.ArmoredDeffDread || category == EnemyCategory.BossGargantWarboss)
            {
                // Dread klaws / iron jaw teeth
                DrawRect(tex, c + radius - 3, c - 5, c + radius + 2, c - 3, scrapIron);
                DrawRect(tex, c + radius - 3, c + 3, c + radius + 2, c + 5, scrapIron);
            }
            else if (category == EnemyCategory.FastStormboy)
            {
                // Rokkit pack flame on back
                DrawRect(tex, c - radius - 2, c - 2, c - radius + 1, c + 2, new Color(1f, 0.6f, 0.1f));
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetBuildNodeSprite(BuildNodeType nodeType)
        {
            string key = $"node_{nodeType}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 36;
            Texture2D tex = CreateBlankTexture(size, size);
            Color padColor = nodeType switch
            {
                BuildNodeType.CliffLedge => new Color(0.35f, 0.45f, 0.58f, 0.85f),
                BuildNodeType.ArtilleryEmplacement => new Color(0.52f, 0.42f, 0.25f, 0.85f),
                _ => new Color(0.38f, 0.38f, 0.36f, 0.85f)
            };
            Color borderColor = new Color(0.85f, 0.72f, 0.28f, 0.95f);

            DrawRect(tex, 2, 2, size - 3, size - 3, padColor);
            // Corner brackets
            for (int i = 2; i < 10; i++)
            {
                tex.SetPixel(i, 2, borderColor);
                tex.SetPixel(2, i, borderColor);
                tex.SetPixel(size - 1 - i, 2, borderColor);
                tex.SetPixel(size - 3, i, borderColor);
                tex.SetPixel(i, size - 3, borderColor);
                tex.SetPixel(2, size - 1 - i, borderColor);
                tex.SetPixel(size - 1 - i, size - 3, borderColor);
                tex.SetPixel(size - 3, size - 1 - i, borderColor);
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetSpaceMarineSprite()
        {
            const string key = "space_marine_astartes";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 24;
            Texture2D tex = CreateBlankTexture(size, size);
            Color ultramarineBlue = new Color(0.15f, 0.38f, 0.85f);
            Color gold = new Color(0.92f, 0.78f, 0.25f);

            // Pauldrons + power armor torso
            DrawRect(tex, 6, 5, 17, 18, ultramarineBlue);
            DrawRect(tex, 5, 13, 18, 17, gold); // Shoulder trim
            DrawRect(tex, 15, 10, 22, 13, new Color(0.2f, 0.2f, 0.22f)); // Bolter

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetCircleSprite(int size = 64, bool hollowRing = false)
        {
            string key = $"circle_{size}_{hollowRing}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D tex = CreateBlankTexture(size, size);
            float c = (size - 1) * 0.5f;
            float rMax = size * 0.48f;
            float rMin = hollowRing ? rMax - 2.2f : 0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    if (d <= rMax && d >= rMin)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        public static Sprite GetWhiteSquareSprite()
        {
            const string key = "white_square_8";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D tex = CreateBlankTexture(8, 8);
            DrawRect(tex, 0, 0, 7, 7, Color.white);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        private static Color GetTowerPrimaryColor(TowerCategory cat)
        {
            return cat switch
            {
                TowerCategory.HeavyBolterBunker => new Color(0.32f, 0.42f, 0.30f),
                TowerCategory.LascannonTurret => new Color(0.25f, 0.36f, 0.52f),
                TowerCategory.HellhoundFlameBunker => new Color(0.55f, 0.24f, 0.16f),
                TowerCategory.BasiliskArtilleryPit => new Color(0.44f, 0.42f, 0.28f),
                TowerCategory.AstropathicRelay => new Color(0.38f, 0.25f, 0.55f),
                _ => Color.gray
            };
        }

        private static Color GetTowerAccentColor(TowerCategory cat)
        {
            return cat switch
            {
                TowerCategory.HeavyBolterBunker => new Color(1.0f, 0.85f, 0.3f),
                TowerCategory.LascannonTurret => new Color(0.3f, 0.9f, 1.0f),
                TowerCategory.HellhoundFlameBunker => new Color(1.0f, 0.48f, 0.1f),
                TowerCategory.BasiliskArtilleryPit => new Color(1.0f, 0.35f, 0.2f),
                TowerCategory.AstropathicRelay => new Color(0.65f, 0.4f, 1.0f),
                _ => Color.yellow
            };
        }

        private static Texture2D CreateBlankTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] clear = new Color[width * height];
            tex.SetPixels(clear);
            return tex;
        }

        private static void DrawRect(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
        {
            x0 = Mathf.Clamp(x0, 0, tex.width - 1);
            x1 = Mathf.Clamp(x1, 0, tex.width - 1);
            y0 = Mathf.Clamp(y0, 0, tex.height - 1);
            y1 = Mathf.Clamp(y1, 0, tex.height - 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
        }

        private static void DrawPixelCircleOutline(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int angle = 0; angle < 360; angle += 10)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = Mathf.RoundToInt(cx + Mathf.Cos(rad) * r);
                int y = Mathf.RoundToInt(cy + Mathf.Sin(rad) * r);
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    tex.SetPixel(x, y, col);
            }
        }
    }
}
