using System.Collections.Generic;
using OutpostAegis.Data;
using UnityEngine;

namespace OutpostAegis.Visuals
{
    /// <summary>
    /// High-detail 32 PPU Grimdark Pixel-Art Sprite & Animation Synthesizer.
    /// Generates multi-frame Ork walk cycles, Imperial tower turrets with muzzle flashes/recoil,
    /// animated molten promethium river tiles, pulsing Central Plasma Reactor coils,
    /// and a full dithered Ash-Waste & Cliff-Ledge battlefield tilemap backdrop.
    /// </summary>
    public static class PixelSpriteGenerator
    {
        public const float PixelsPerUnit = 32f;
        private static readonly Dictionary<string, Sprite> SingleCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> ArrayCache = new Dictionary<string, Sprite[]>();

        // =====================================================================
        // 1. IMPERIAL TOWERS (DETAILED BASES + IDLE/FIRING TURRET FRAMES)
        // =====================================================================

        public static Sprite GetTowerBaseSprite(TowerCategory category, int tierIndex)
        {
            string key = $"tower_base_v2_{category}_{tierIndex}";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 32;
            Texture2D tex = CreateBlankTexture(size, size);

            Color shadow = new Color(0.08f, 0.08f, 0.10f, 0.75f);
            Color ferroDark = new Color(0.18f, 0.20f, 0.22f);
            Color ferroMid = new Color(0.30f, 0.33f, 0.36f);
            Color ferroLight = new Color(0.44f, 0.47f, 0.50f);
            Color sandbag = new Color(0.48f, 0.42f, 0.30f);
            Color sandbagDark = new Color(0.34f, 0.29f, 0.20f);
            Color brass = new Color(0.85f, 0.68f, 0.24f);
            Color hazardYellow = new Color(0.92f, 0.74f, 0.15f);

            // Drop shadow
            DrawOctagon(tex, 16, 14, 13, shadow);

            // Outer sandbag / blast-wall ring for bunkers & artillery
            if (category == TowerCategory.HeavyBolterBunker ||
                category == TowerCategory.HellhoundFlameBunker ||
                category == TowerCategory.BasiliskArtilleryPit)
            {
                DrawOctagon(tex, 16, 16, 13, sandbagDark);
                DrawOctagon(tex, 16, 16, 12, sandbag);
            }

            // Octagonal ferrocrete bunker foundation
            DrawOctagon(tex, 16, 16, 11, ferroDark);
            DrawOctagon(tex, 16, 16, 10, ferroMid);

            // Corner rivets & top bevel highlight
            for (int x = 9; x <= 22; x++)
            {
                tex.SetPixel(x, 24, ferroLight);
                tex.SetPixel(x, 7, ferroDark);
            }
            tex.SetPixel(9, 9, ferroLight);
            tex.SetPixel(22, 9, ferroLight);
            tex.SetPixel(9, 22, ferroLight);
            tex.SetPixel(22, 22, ferroLight);

            // Tier 2+ hazard trim & Tier chevrons
            if (tierIndex >= 1)
            {
                for (int x = 10; x <= 21; x++)
                {
                    Color stripe = ((x / 2) % 2 == 0) ? hazardYellow : ferroDark;
                    tex.SetPixel(x, 8, stripe);
                }
            }

            // Golden Imperial tier chevrons at bottom of bunker
            for (int t = 0; t <= tierIndex && t < 3; t++)
            {
                int bx = 11 + t * 4;
                DrawRect(tex, bx, 5, bx + 2, 6, brass);
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            SingleCache[key] = s;
            return s;
        }

        /// <summary>
        /// Returns [0] = Idle Turret Sprite, [1] = Firing Recoil + Muzzle Flash Sprite
        /// (or 4-frame psychic wave loop for Astropathic Relay).
        /// </summary>
        public static Sprite[] GetTowerTurretFrames(TowerCategory category, int tierIndex)
        {
            string key = $"tower_turret_frames_v2_{category}_{tierIndex}";
            if (ArrayCache.TryGetValue(key, out Sprite[] cached) && cached != null) return cached;

            if (category == TowerCategory.AstropathicRelay)
            {
                Sprite[] relayFrames = new Sprite[4];
                for (int f = 0; f < 4; f++)
                {
                    relayFrames[f] = BuildAstropathicTurretFrame(tierIndex, f);
                }
                ArrayCache[key] = relayFrames;
                return relayFrames;
            }

            Sprite idle = BuildCombatTurretSprite(category, tierIndex, isFiring: false);
            Sprite firing = BuildCombatTurretSprite(category, tierIndex, isFiring: true);
            Sprite[] result = { idle, firing };
            ArrayCache[key] = result;
            return result;
        }

        public static Sprite GetTowerTurretSprite(TowerCategory category, int tierIndex)
        {
            return GetTowerTurretFrames(category, tierIndex)[0];
        }

        private static Sprite BuildCombatTurretSprite(TowerCategory category, int tierIndex, bool isFiring)
        {
            int size = 36;
            Texture2D tex = CreateBlankTexture(size, size);
            int c = 18;
            int recoil = isFiring ? -2 : 0; // Barrel kicks backward on firing frame

            Color steelDark = new Color(0.14f, 0.15f, 0.18f);
            Color steelMid = new Color(0.28f, 0.31f, 0.35f);
            Color steelBright = new Color(0.52f, 0.56f, 0.62f);
            Color brass = new Color(0.86f, 0.68f, 0.22f);
            Color hullMain = GetTowerPrimaryColor(category);
            Color hullShade = hullMain * 0.72f;
            hullShade.a = 1f;

            // Armored gunhouse cupola
            DrawOctagon(tex, c + recoil / 2, c, 7 + (tierIndex >= 2 ? 1 : 0), steelDark);
            DrawOctagon(tex, c + recoil / 2, c, 6 + (tierIndex >= 2 ? 1 : 0), hullShade);
            DrawOctagon(tex, c + recoil / 2, c + 1, 5, hullMain);

            // Golden Aquila / sight optics on turret roof
            DrawRect(tex, c - 2, c - 1, c + 1, c + 1, brass);
            tex.SetPixel(c + 3, c + 2, Color.red); // Auspex targeting lens

            if (category == TowerCategory.HeavyBolterBunker)
            {
                // Brass ammo feed belt
                DrawRect(tex, c - 1, c - 6, c + 2, c - 4, brass);

                // Barrels (Single -> Twin -> Quad/Autocannon)
                int offset = tierIndex == 0 ? 0 : 2;
                int bStart = c + 5 + recoil;
                int bEnd = c + 13 + (tierIndex >= 2 ? 2 : 0) + recoil;

                DrawRect(tex, bStart, c - offset - 1, bEnd, c - offset + 1, steelDark);
                DrawRect(tex, bStart, c - offset, bEnd - 1, c - offset, steelMid);

                if (tierIndex >= 1)
                {
                    DrawRect(tex, bStart, c + offset - 1, bEnd, c + offset + 1, steelDark);
                    DrawRect(tex, bStart, c + offset, bEnd - 1, c + offset, steelMid);
                }

                // Muzzle flash star on firing frame
                if (isFiring)
                {
                    DrawMuzzleFlash(tex, bEnd + 2, c - offset, new Color(1f, 0.95f, 0.4f), new Color(1f, 0.5f, 0.1f));
                    if (tierIndex >= 1)
                        DrawMuzzleFlash(tex, bEnd + 2, c + offset, new Color(1f, 0.95f, 0.4f), new Color(1f, 0.5f, 0.1f));
                }
            }
            else if (category == TowerCategory.LascannonTurret)
            {
                // Long sniper barrel with glowing cyan plasma-capacitor ribs
                int bStart = c + 4 + recoil;
                int bEnd = c + 15 + recoil;
                DrawRect(tex, bStart, c - 2, bEnd, c + 2, steelDark);
                DrawRect(tex, bStart, c - 1, bEnd, c + 1, steelBright);

                Color coilColor = isFiring
                    ? new Color(0.85f, 1.0f, 1.0f)
                    : new Color(0.15f, 0.82f, 1.0f);

                for (int x = bStart + 2; x < bEnd - 1; x += 3)
                {
                    tex.SetPixel(x, c - 1, coilColor);
                    tex.SetPixel(x, c, coilColor);
                    tex.SetPixel(x, c + 1, coilColor);
                }

                if (isFiring)
                {
                    DrawMuzzleFlash(tex, bEnd + 2, c, new Color(0.8f, 1f, 1f), new Color(0.1f, 0.75f, 1f));
                }
            }
            else if (category == TowerCategory.HellhoundFlameBunker)
            {
                // Twin pressurized red promethium fuel tanks on rear of turret
                Color tankRed = new Color(0.78f, 0.18f, 0.12f);
                Color tankHighlight = new Color(0.95f, 0.35f, 0.22f);
                DrawRect(tex, c - 8, c - 6, c - 3, c - 2, tankRed);
                DrawRect(tex, c - 8, c + 2, c - 3, c + 6, tankRed);
                tex.SetPixel(c - 6, c - 4, tankHighlight);
                tex.SetPixel(c - 6, c + 4, tankHighlight);

                // Heavy ventilated Inferno nozzle
                int bStart = c + 4;
                int bEnd = c + 12;
                DrawRect(tex, bStart, c - 3, bEnd, c + 3, steelDark);
                DrawRect(tex, bStart + 1, c - 2, bEnd - 1, c + 2, brass);

                if (isFiring)
                {
                    DrawMuzzleFlash(tex, bEnd + 3, c, new Color(1f, 0.9f, 0.3f), new Color(1f, 0.35f, 0.05f));
                }
                else
                {
                    // Blue-orange pilot light at nozzle tip
                    tex.SetPixel(bEnd + 1, c, new Color(0.3f, 0.8f, 1f));
                    tex.SetPixel(bEnd + 2, c, new Color(1f, 0.6f, 0.1f));
                }
            }
            else if (category == TowerCategory.BasiliskArtilleryPit)
            {
                // Heavy Earthshaker gun shield + long siege howitzer barrel
                DrawRect(tex, c + 1, c - 7, c + 4, c + 7, steelDark);
                DrawRect(tex, c + 2, c - 6, c + 3, c + 6, hullMain);

                int bStart = c + 4 + recoil;
                int bEnd = c + 16 + recoil;
                DrawRect(tex, bStart, c - 3, bEnd, c + 3, steelDark);
                DrawRect(tex, bStart, c - 1, bEnd - 1, c + 1, steelMid);
                DrawRect(tex, bEnd - 2, c - 4, bEnd, c + 4, brass); // Muzzle brake

                if (isFiring)
                {
                    DrawMuzzleFlash(tex, bEnd + 2, c, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.4f, 0.1f));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static Sprite BuildAstropathicTurretFrame(int tierIndex, int frame)
        {
            int size = 36;
            Texture2D tex = CreateBlankTexture(size, size);
            int c = 18;

            Color spireDark = new Color(0.24f, 0.15f, 0.36f);
            Color spireGold = new Color(0.88f, 0.72f, 0.25f);
            Color warpCore = new Color(0.75f, 0.45f, 1.0f);
            Color warpPulse = new Color(0.45f, 0.92f, 1.0f);

            DrawOctagon(tex, c, c, 6, spireDark);
            DrawPixelCircleOutline(tex, c, c, 6, spireGold);
            DrawOctagon(tex, c, c, 3, warpCore);

            // Animated expanding psychic ring
            int pulseRadius = 4 + frame * 2 + (tierIndex >= 1 ? 1 : 0);
            DrawPixelCircleOutline(tex, c, c, pulseRadius, warpPulse);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // =====================================================================
        // 2. ORK WAAAGH! MULTI-FRAME ANIMATED WALK CYCLES (4 FRAMES EACH)
        // =====================================================================

        public static Sprite[] GetEnemyWalkFrames(EnemyCategory category)
        {
            string key = $"enemy_walk_v2_{category}";
            if (ArrayCache.TryGetValue(key, out Sprite[] cached) && cached != null) return cached;

            Sprite[] frames = new Sprite[4];
            for (int f = 0; f < 4; f++)
            {
                frames[f] = BuildOrkFrame(category, f);
            }
            ArrayCache[key] = frames;
            return frames;
        }

        public static Sprite GetEnemySprite(EnemyCategory category)
        {
            return GetEnemyWalkFrames(category)[0];
        }

        private static Sprite BuildOrkFrame(EnemyCategory category, int frame)
        {
            int size = category == EnemyCategory.BossGargantWarboss ? 56 : 36;
            Texture2D tex = CreateBlankTexture(size, size);
            int c = size / 2;

            // Walk cycle offsets: frame 0,1,2,3 -> leg stride & body bob
            int bobY = (frame % 2 == 1) ? 1 : 0;
            int leftLegX = (frame == 1) ? -2 : ((frame == 3) ? 2 : 0);
            int rightLegX = -leftLegX;
            int armSwingY = (frame == 1) ? 1 : ((frame == 3) ? -1 : 0);

            Color orkSkinDark = new Color(0.16f, 0.38f, 0.11f);
            Color orkSkinMid = new Color(0.28f, 0.62f, 0.18f);
            Color orkSkinLight = new Color(0.42f, 0.76f, 0.26f);
            Color leather = new Color(0.38f, 0.25f, 0.15f);
            Color ironDark = new Color(0.22f, 0.23f, 0.26f);
            Color ironMid = new Color(0.46f, 0.48f, 0.52f);
            Color ironBright = new Color(0.72f, 0.75f, 0.80f);
            Color evilSunzRed = new Color(0.84f, 0.16f, 0.12f);
            Color flameOrange = new Color(1.0f, 0.58f, 0.12f);
            Color flameYellow = new Color(1.0f, 0.92f, 0.30f);

            // Ground shadow
            DrawRect(tex, c - 6, 3, c + 6, 5, new Color(0f, 0f, 0f, 0.4f));

            switch (category)
            {
                case EnemyCategory.SwarmGretchin:
                {
                    // Animated boots
                    DrawRect(tex, c - 4 + leftLegX, 4, c - 2 + leftLegX, 7, leather);
                    DrawRect(tex, c + 1 + rightLegX, 4, c + 3 + rightLegX, 7, leather);
                    // Hunched loincloth & torso
                    DrawRect(tex, c - 4, 8 + bobY, c + 3, 13 + bobY, leather);
                    DrawRect(tex, c - 3, 11 + bobY, c + 3, 16 + bobY, orkSkinMid);
                    // Big pointy ears + goblin nose + red eyes
                    DrawRect(tex, c - 7, 16 + bobY, c + 6, 18 + bobY, orkSkinDark);
                    DrawRect(tex, c - 3, 15 + bobY, c + 5, 21 + bobY, orkSkinLight);
                    tex.SetPixel(c + 3, 18 + bobY, evilSunzRed);
                    // Rusty pistol in front hand
                    DrawRect(tex, c + 4, 11 + bobY + armSwingY, c + 9, 13 + bobY + armSwingY, ironMid);
                    break;
                }

                case EnemyCategory.SwarmOrkBoyz:
                {
                    // Heavy stomping boots
                    DrawRect(tex, c - 5 + leftLegX, 4, c - 2 + leftLegX, 8, ironDark);
                    DrawRect(tex, c + 2 + rightLegX, 4, c + 5 + rightLegX, 8, ironDark);
                    // Muscular torso & leather harness
                    DrawRect(tex, c - 6, 9 + bobY, c + 5, 19 + bobY, orkSkinDark);
                    DrawRect(tex, c - 5, 10 + bobY, c + 4, 18 + bobY, orkSkinMid);
                    DrawRect(tex, c - 5, 9 + bobY, c + 4, 12 + bobY, leather);
                    // Iron shoulder pauldron with red trim
                    DrawRect(tex, c - 7, 16 + bobY, c - 3, 21 + bobY, evilSunzRed);
                    // Jutting jaw + iron helmet + tusks
                    DrawRect(tex, c - 3, 18 + bobY, c + 5, 25 + bobY, orkSkinLight);
                    DrawRect(tex, c - 4, 23 + bobY, c + 4, 27 + bobY, ironMid);
                    tex.SetPixel(c + 3, 22 + bobY, evilSunzRed);
                    tex.SetPixel(c + 5, 19 + bobY, Color.white); // Tusk
                    // Swinging Choppa axe in right hand!
                    int axeY = 13 + bobY + armSwingY * 2;
                    DrawRect(tex, c + 5, axeY, c + 10, axeY + 2, leather);
                    DrawRect(tex, c + 8, axeY - 2, c + 12, axeY + 5, ironBright);
                    break;
                }

                case EnemyCategory.FastWarbiker:
                {
                    // Front & Rear knobby tires with animated spinning spokes
                    DrawOctagon(tex, c - 8, 9, 5, ironDark);
                    DrawOctagon(tex, c + 8, 9, 5, ironDark);
                    int spokeOffset = frame % 2;
                    tex.SetPixel(c - 8 + spokeOffset, 9, ironBright);
                    tex.SetPixel(c + 8 + spokeOffset, 9, ironBright);
                    // Red Speed Freeks chassis & twin dakkaguns
                    DrawRect(tex, c - 8, 10 + bobY, c + 9, 16 + bobY, evilSunzRed);
                    DrawRect(tex, c + 7, 13 + bobY, c + 15, 15 + bobY, ironMid);
                    // Ork biker torso & goggles
                    DrawRect(tex, c - 4, 16 + bobY, c + 2, 23 + bobY, orkSkinMid);
                    DrawRect(tex, c - 2, 22 + bobY, c + 3, 24 + bobY, flameYellow);
                    // Exhaust smoke puff on frames 0 & 2
                    if (frame % 2 == 0)
                        DrawOctagon(tex, c - 13, 12 + frame, 3, new Color(0.6f, 0.6f, 0.6f, 0.8f));
                    break;
                }

                case EnemyCategory.FastStormboy:
                {
                    // Airborne dangling boots
                    DrawRect(tex, c - 4 + leftLegX, 6 + bobY, c - 2 + leftLegX, 10 + bobY, leather);
                    DrawRect(tex, c + 1 + rightLegX, 6 + bobY, c + 3 + rightLegX, 10 + bobY, leather);
                    // Strapped steel rokkit pack on back with flickering jet plume!
                    DrawRect(tex, c - 9, 13 + bobY, c - 4, 25 + bobY, ironMid);
                    DrawRect(tex, c - 8, 25 + bobY, c - 5, 28 + bobY, evilSunzRed);
                    int flameLen = 4 + (frame % 3) * 2;
                    DrawRect(tex, c - 9, 13 + bobY - flameLen, c - 4, 12 + bobY, flameOrange);
                    DrawRect(tex, c - 8, 13 + bobY - flameLen / 2, c - 5, 12 + bobY, flameYellow);
                    // Stormboy body & aviator helmet
                    DrawRect(tex, c - 4, 11 + bobY, c + 4, 21 + bobY, orkSkinMid);
                    DrawRect(tex, c - 2, 20 + bobY, c + 5, 26 + bobY, orkSkinLight);
                    tex.SetPixel(c + 3, 23 + bobY, evilSunzRed);
                    break;
                }

                case EnemyCategory.StealthKommando:
                {
                    // Sneaky purple-camo gear + green night-vision goggles
                    Color camoPurple = new Color(0.42f, 0.20f, 0.56f);
                    Color camoDark = new Color(0.25f, 0.12f, 0.35f);
                    Color nvgGreen = new Color(0.2f, 1.0f, 0.35f);

                    DrawRect(tex, c - 5 + leftLegX, 4, c - 2 + leftLegX, 8, camoDark);
                    DrawRect(tex, c + 2 + rightLegX, 4, c + 5 + rightLegX, 8, camoDark);
                    DrawRect(tex, c - 6, 9 + bobY, c + 4, 19 + bobY, camoPurple);
                    DrawRect(tex, c - 3, 18 + bobY, c + 5, 24 + bobY, orkSkinDark);
                    // Glowing green splinter-cell / kommando goggles
                    DrawRect(tex, c + 1, 21 + bobY, c + 5, 22 + bobY, nvgGreen);
                    // Twin throat-slitta daggers
                    DrawRect(tex, c + 5, 13 + bobY + armSwingY, c + 11, 14 + bobY + armSwingY, ironBright);
                    break;
                }

                case EnemyCategory.ArmoredKillaKan:
                {
                    // Mechanical piston legs
                    DrawRect(tex, c - 6 + leftLegX, 4, c - 2 + leftLegX, 10, ironDark);
                    DrawRect(tex, c + 2 + rightLegX, 4, c + 6 + rightLegX, 10, ironDark);
                    // Cylindrical riveted scrap-can hull
                    DrawRect(tex, c - 9, 10 + bobY, c + 8, 26 + bobY, ironDark);
                    DrawRect(tex, c - 8, 11 + bobY, c + 7, 25 + bobY, ironMid);
                    DrawRect(tex, c - 8, 11 + bobY, c + 7, 15 + bobY, evilSunzRed); // Lower jaw plate
                    // Glowing red armored vision slit
                    DrawRect(tex, c - 2, 20 + bobY, c + 6, 22 + bobY, Color.black);
                    DrawRect(tex, c + 1, 20 + bobY, c + 5, 21 + bobY, evilSunzRed);
                    // Spinning buzzsaw arm
                    DrawOctagon(tex, c + 10, 15 + bobY + armSwingY, 4, ironBright);
                    break;
                }

                case EnemyCategory.ArmoredDeffDread:
                {
                    // Heavy dreadnought stompers
                    DrawRect(tex, c - 8 + leftLegX, 4, c - 3 + leftLegX, 11, ironDark);
                    DrawRect(tex, c + 3 + rightLegX, 4, c + 8 + rightLegX, 11, ironDark);
                    // Massive black & red armored hull
                    DrawRect(tex, c - 11, 11 + bobY, c + 10, 29 + bobY, ironDark);
                    DrawRect(tex, c - 9, 12 + bobY, c + 8, 27 + bobY, new Color(0.28f, 0.30f, 0.34f));
                    // Checkered skull jaw & red eyes
                    DrawRect(tex, c - 4, 14 + bobY, c + 8, 19 + bobY, Color.white);
                    DrawRect(tex, c + 2, 22 + bobY, c + 7, 24 + bobY, evilSunzRed);
                    // Upper & lower Dread Klaws
                    DrawRect(tex, c + 9, 20 + bobY + armSwingY, c + 15, 24 + bobY + armSwingY, ironBright);
                    DrawRect(tex, c + 9, 12 + bobY - armSwingY, c + 15, 16 + bobY - armSwingY, ironBright);
                    break;
                }

                case EnemyCategory.BossGargantWarboss:
                {
                    // Colossal 56x56 Mega-Armored Warboss / Gargant
                    DrawRect(tex, c - 12 + leftLegX, 4, c - 4 + leftLegX, 14, ironDark);
                    DrawRect(tex, c + 4 + rightLegX, 4, c + 12 + rightLegX, 14, ironDark);

                    // Mega-Armor torso with hazard stripes & red plating
                    DrawRect(tex, c - 16, 14 + bobY, c + 15, 40 + bobY, ironDark);
                    DrawRect(tex, c - 14, 16 + bobY, c + 13, 38 + bobY, evilSunzRed);

                    for (int x = c - 13; x <= c + 12; x++)
                    {
                        Color check = ((x / 3) % 2 == 0) ? flameYellow : Color.black;
                        DrawRect(tex, x, 16 + bobY, x, 19 + bobY, check);
                    }

                    // Dorsal exhaust stacks belching fire
                    DrawRect(tex, c - 12, 40 + bobY, c - 8, 48 + bobY, ironMid);
                    DrawRect(tex, c - 4, 40 + bobY, c, 48 + bobY, ironMid);
                    if (frame % 2 == 0)
                    {
                        DrawOctagon(tex, c - 10, 50 + bobY, 3, flameOrange);
                        DrawOctagon(tex, c - 2, 50 + bobY, 3, flameYellow);
                    }

                    // Warboss head & Iron Gob
                    DrawRect(tex, c - 4, 30 + bobY, c + 9, 41 + bobY, orkSkinMid);
                    DrawRect(tex, c - 3, 28 + bobY, c + 11, 34 + bobY, ironBright); // Iron Gob jaw
                    DrawRect(tex, c + 4, 36 + bobY, c + 8, 38 + bobY, flameYellow); // Glowing eyes

                    // Giant crushing Power Klaw
                    int klawY = 20 + bobY + armSwingY * 2;
                    DrawRect(tex, c + 12, klawY, c + 24, klawY + 10, ironDark);
                    DrawRect(tex, c + 14, klawY + 2, c + 25, klawY + 8, ironBright);
                    break;
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // =====================================================================
        // 3. ASH-WASTE TILEMAP, MOLTEN PROMETHIUM RIVER & REFINERY WALLS
        // =====================================================================

        /// <summary>
        /// Generates a 4-frame scrolling Molten Promethium River tile (32x32 at 32 PPU).
        /// </summary>
        public static Sprite[] GetPromethiumRiverFrames()
        {
            const string key = "promethium_river_frames_v2";
            if (ArrayCache.TryGetValue(key, out Sprite[] cached) && cached != null) return cached;

            Sprite[] frames = new Sprite[4];
            Color slagBank = new Color(0.16f, 0.12f, 0.10f);
            Color lavaDeep = new Color(0.72f, 0.18f, 0.04f);
            Color lavaMid = new Color(0.96f, 0.42f, 0.06f);
            Color lavaBright = new Color(1.0f, 0.85f, 0.22f);

            for (int f = 0; f < 4; f++)
            {
                Texture2D tex = CreateBlankTexture(32, 32);
                int scrollY = f * 8;

                for (int y = 0; y < 32; y++)
                {
                    int wy = (y + scrollY) % 32;
                    for (int x = 0; x < 32; x++)
                    {
                        // Charred slag banks on left/right edges
                        if (x < 3 || x > 28)
                        {
                            tex.SetPixel(x, y, slagBank);
                            continue;
                        }

                        // Wavy molten currents
                        float wave = Mathf.Sin((wy * 0.25f) + (x * 0.35f));
                        Color c = lavaDeep;
                        if (wave > 0.15f) c = lavaMid;
                        if (wave > 0.65f && x > 7 && x < 24) c = lavaBright;

                        tex.SetPixel(x, y, c);
                    }
                }

                tex.Apply();
                frames[f] = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            }

            ArrayCache[key] = frames;
            return frames;
        }

        /// <summary>
        /// Generates a 4-frame pulsing Central Plasma Reactor fortress core (64x96 pixels).
        /// </summary>
        public static Sprite[] GetPlasmaReactorFrames()
        {
            const string key = "plasma_reactor_frames_v2";
            if (ArrayCache.TryGetValue(key, out Sprite[] cached) && cached != null) return cached;

            Sprite[] frames = new Sprite[4];
            Color wallDark = new Color(0.16f, 0.18f, 0.22f);
            Color wallMid = new Color(0.28f, 0.31f, 0.36f);
            Color gold = new Color(0.86f, 0.70f, 0.24f);

            for (int f = 0; f < 4; f++)
            {
                int w = 64, h = 96;
                Texture2D tex = CreateBlankTexture(w, h);

                // Gothic armoured reactor housing
                DrawRect(tex, 4, 4, w - 5, h - 5, wallDark);
                DrawRect(tex, 8, 8, w - 9, h - 9, wallMid);

                // Gold buttresses & top trim
                DrawRect(tex, 6, h - 14, w - 7, h - 10, gold);
                DrawRect(tex, 6, 10, w - 7, 14, gold);

                // Central Plasma Containment Chamber
                DrawRect(tex, 16, 20, w - 17, h - 21, new Color(0.06f, 0.12f, 0.20f));

                float pulse = 0.7f + 0.3f * Mathf.Sin(f * Mathf.PI * 0.5f);
                Color plasmaOuter = new Color(0.15f * pulse, 0.65f * pulse, 1.0f);
                Color plasmaCore = new Color(0.75f * pulse, 0.96f, 1.0f);

                for (int ry = 24; ry < h - 24; ry += 8)
                {
                    bool activeCoil = ((ry / 8 + f) % 2 == 0);
                    DrawRect(tex, 20, ry, w - 21, ry + 5, plasmaOuter);
                    if (activeCoil)
                        DrawRect(tex, 24, ry + 1, w - 25, ry + 4, plasmaCore);
                }

                tex.Apply();
                frames[f] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            }

            ArrayCache[key] = frames;
            return frames;
        }

        /// <summary>
        /// Generates the Narrow Promethium Bridge deck with steel girders and hazard chevrons (64x48 pixels).
        /// </summary>
        public static Sprite GetNarrowBridgeSprite()
        {
            const string key = "narrow_bridge_sprite_v2";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 64, h = 48;
            Texture2D tex = CreateBlankTexture(w, h);
            Color girder = new Color(0.18f, 0.20f, 0.23f);
            Color deck = new Color(0.34f, 0.36f, 0.40f);
            Color plankLine = new Color(0.24f, 0.26f, 0.29f);
            Color hazardYellow = new Color(0.92f, 0.74f, 0.16f);

            DrawRect(tex, 0, 4, w - 1, h - 5, girder);
            DrawRect(tex, 0, 10, w - 1, h - 11, deck);

            // Hazard stripes along top and bottom bridge railings
            for (int x = 0; x < w; x++)
            {
                Color stripe = ((x / 4) % 2 == 0) ? hazardYellow : Color.black;
                DrawRect(tex, x, 6, x, 9, stripe);
                DrawRect(tex, x, h - 10, x, h - 7, stripe);

                if (x % 8 == 0)
                {
                    DrawRect(tex, x, 11, x, h - 12, plankLine);
                }
            }

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            SingleCache[key] = s;
            return s;
        }

        /// <summary>
        /// Generates a full-battlefield dithered Ash-Waste & Cliff Ledge pixel-art tilemap texture (576x352 = 18x11 units).
        /// </summary>
        public static Sprite GetAshWasteBattlefieldSprite()
        {
            const string key = "ash_waste_battlefield_v2";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int w = 576;
            int h = 352;
            Texture2D tex = CreateBlankTexture(w, h);

            Color ashBase = new Color(0.18f, 0.16f, 0.14f);
            Color ashDark = new Color(0.14f, 0.12f, 0.11f);
            Color ashLight = new Color(0.23f, 0.20f, 0.18f);
            Color cliffTop = new Color(0.28f, 0.30f, 0.34f);
            Color cliffFace = new Color(0.12f, 0.13f, 0.16f);
            Color fortressFloor = new Color(0.24f, 0.25f, 0.28f);

            // 1. Base dithered volcanic ash-waste ground
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int hash = ((x * 73856093) ^ (y * 19349663)) & 0xFF;
                    Color c = ashBase;
                    if (hash < 35) c = ashDark;
                    else if (hash > 225) c = ashLight;

                    // Eastern Refinery Fortress paved perimeter
                    if (x > 460)
                    {
                        c = ((x % 16 == 0) || (y % 16 == 0)) ? ashDark : fortressFloor;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            // 2. Elevated High Cliff Ledge Plateaus (North & South overlooking Convergence)
            DrawCliffPlateau(tex, 310, 225, 76, 56, cliffTop, cliffFace);
            DrawCliffPlateau(tex, 310, 102, 76, 56, cliffTop, cliffFace);

            // 3. Ash-Gully Scrap Trench Cover Zone barricades
            DrawCliffPlateau(tex, 165, 85, 68, 44, new Color(0.26f, 0.21f, 0.16f), ashDark);

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            SingleCache[key] = s;
            return s;
        }

        private static void DrawCliffPlateau(Texture2D tex, int cx, int cy, int rw, int rh, Color topCol, Color faceCol)
        {
            int x0 = cx - rw / 2;
            int x1 = cx + rw / 2;
            int y0 = cy - rh / 2;
            int y1 = cy + rh / 2;

            // Cliff vertical striated rock wall below plateau
            DrawRect(tex, x0, y0 - 10, x1, y0, faceCol);
            DrawRect(tex, x0, y0, x1, y1, topCol);
            // Highlight rim
            DrawRect(tex, x0, y1 - 2, x1, y1, topCol * 1.25f);
        }

        // =====================================================================
        // 4. BUILD NODES, SPACE MARINES & UTILITY SPRITES
        // =====================================================================

        public static Sprite GetBuildNodeSprite(BuildNodeType nodeType)
        {
            string key = $"node_v2_{nodeType}";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 36;
            Texture2D tex = CreateBlankTexture(size, size);
            Color padColor = nodeType switch
            {
                BuildNodeType.CliffLedge => new Color(0.32f, 0.44f, 0.58f, 0.90f),
                BuildNodeType.ArtilleryEmplacement => new Color(0.50f, 0.40f, 0.24f, 0.90f),
                _ => new Color(0.34f, 0.35f, 0.34f, 0.90f)
            };
            Color borderColor = new Color(0.88f, 0.74f, 0.26f, 0.98f);
            Color darkInner = new Color(0.16f, 0.17f, 0.18f, 0.90f);

            DrawRect(tex, 2, 2, size - 3, size - 3, darkInner);
            DrawRect(tex, 4, 4, size - 5, size - 5, padColor);

            // Imperial Aquila / Crosshair emblem in center of empty build pad
            DrawRect(tex, 13, 17, 22, 18, borderColor);
            DrawRect(tex, 17, 13, 18, 22, borderColor);

            // Corner tactical brackets
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
            SingleCache[key] = s;
            return s;
        }

        public static Sprite GetSpaceMarineSprite()
        {
            const string key = "space_marine_astartes_v2";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int size = 28;
            Texture2D tex = CreateBlankTexture(size, size);
            Color ceramiteDark = new Color(0.08f, 0.22f, 0.55f);
            Color ceramiteBlue = new Color(0.16f, 0.42f, 0.88f);
            Color goldTrim = new Color(0.92f, 0.78f, 0.25f);
            Color eyeRed = new Color(1.0f, 0.15f, 0.15f);

            // Power armor boots & greaves
            DrawRect(tex, 9, 3, 12, 8, ceramiteDark);
            DrawRect(tex, 15, 3, 18, 8, ceramiteDark);
            // Chestplate & Golden Aquila
            DrawRect(tex, 8, 9, 19, 18, ceramiteBlue);
            DrawRect(tex, 10, 14, 17, 16, goldTrim);
            // Iconic oversized Astartes pauldrons
            DrawRect(tex, 5, 14, 9, 20, ceramiteBlue);
            DrawRect(tex, 18, 14, 22, 20, ceramiteBlue);
            DrawRect(tex, 5, 14, 9, 15, goldTrim);
            DrawRect(tex, 18, 14, 22, 15, goldTrim);
            // Mk-X Helmet + glowing red eye lenses
            DrawRect(tex, 11, 18, 16, 23, ceramiteBlue);
            tex.SetPixel(14, 20, eyeRed);
            tex.SetPixel(16, 20, eyeRed);
            // Godwyn-pattern Bolter rifle
            DrawRect(tex, 16, 11, 25, 14, new Color(0.18f, 0.18f, 0.20f));
            tex.SetPixel(25, 13, goldTrim);

            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            SingleCache[key] = s;
            return s;
        }

        public static Sprite GetCircleSprite(int size = 64, bool hollowRing = false)
        {
            string key = $"circle_{size}_{hollowRing}";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

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
            SingleCache[key] = s;
            return s;
        }

        public static Sprite GetWhiteSquareSprite()
        {
            const string key = "white_square_8";
            if (SingleCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Texture2D tex = CreateBlankTexture(8, 8);
            DrawRect(tex, 0, 0, 7, 7, Color.white);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            SingleCache[key] = s;
            return s;
        }

        // =====================================================================
        // PIXEL DRAWING HELPERS
        // =====================================================================

        private static Color GetTowerPrimaryColor(TowerCategory cat)
        {
            return cat switch
            {
                TowerCategory.HeavyBolterBunker => new Color(0.30f, 0.42f, 0.28f),
                TowerCategory.LascannonTurret => new Color(0.24f, 0.36f, 0.52f),
                TowerCategory.HellhoundFlameBunker => new Color(0.56f, 0.24f, 0.16f),
                TowerCategory.BasiliskArtilleryPit => new Color(0.44f, 0.42f, 0.28f),
                TowerCategory.AstropathicRelay => new Color(0.38f, 0.25f, 0.55f),
                _ => Color.gray
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

        private static void DrawOctagon(Texture2D tex, int cx, int cy, int radius, Color col)
        {
            int cut = Mathf.RoundToInt(radius * 1.45f);
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= cut)
                    {
                        tex.SetPixel(x, y, col);
                    }
                }
            }
        }

        private static void DrawMuzzleFlash(Texture2D tex, int cx, int cy, Color core, Color outer)
        {
            DrawOctagon(tex, cx, cy, 3, outer);
            DrawRect(tex, cx - 1, cy - 1, cx + 2, cy + 1, core);
        }

        private static void DrawPixelCircleOutline(Texture2D tex, int cx, int cy, int r, Color col)
        {
            for (int angle = 0; angle < 360; angle += 8)
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
