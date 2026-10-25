#if UNITY_EDITOR
using System.Collections.Generic;
using OutpostAegis.Abilities;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Map;
using OutpostAegis.UI;
using OutpostAegis.Visuals;
using OutpostAegis.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OutpostAegis.Editor
{
    /// <summary>
    /// One-click Unity Editor builder that populates all ScriptableObject assets
    /// (5 Towers x 3 Tiers, 8 Ork Enemies, 20 Waves, 2 Command Abilities) and constructs
    /// the playable Outpost Aegis-IV scene with dual converging lanes, Promethium Bridge, and Cliff Ledges.
    /// </summary>
    [InitializeOnLoad]
    public static class AegisSceneSetupEditor
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const string ScenePath = "Assets/_Project/Scenes/OutpostAegisIV.unity";

        static AegisSceneSetupEditor()
        {
            EditorApplication.delayCall += AutoInitializeIfNeeded;
        }

        private static void AutoInitializeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (!sceneExists)
            {
                GenerateAllDataAndSceneInternal(showDialog: false);
            }
            else if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        [MenuItem("Tools/Outpost Aegis-IV/Generate Data & Prototype Scene", priority = 1)]
        public static void GenerateAllDataAndScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Exit Play Mode First",
                    "Please click the Play button at the top center to exit Play Mode before generating the scene.",
                    "OK");
                return;
            }

            GenerateAllDataAndSceneInternal(showDialog: true);
        }

        private static void GenerateAllDataAndSceneInternal(bool showDialog)
        {
            EnsureFolders();

            List<TowerDataSO> towers = GenerateTowers();
            Dictionary<EnemyCategory, EnemyDataSO> enemies = GenerateEnemies();
            List<WaveDataSO> waves = Generate20Waves(enemies);
            List<AbilityDataSO> abilities = GenerateAbilities();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildOutpostAegisScene(towers, waves, abilities);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Debug.Log("[Outpost Aegis-IV] Generated all ScriptableObject assets and playable scene at " + ScenePath);
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Outpost Aegis-IV Ready!",
                    "Generated:\n• 5 Imperial Towers (3 Upgrade Tiers each)\n• 8 Ork Enemy Classes (including Wave 20 Gargant Boss)\n• 20 WAAAGH! Waves\n• 2 Command Abilities (Orbital Bombardment & Drop Pod Assault)\n• OutpostAegisIV.unity Scene\n\nPress PLAY in the Unity Editor to test the siege!",
                    "For the Emperor!");
            }
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/_Project",
                "Assets/_Project/Data",
                "Assets/_Project/Data/Towers",
                "Assets/_Project/Data/Enemies",
                "Assets/_Project/Data/Waves",
                "Assets/_Project/Data/Abilities",
                "Assets/_Project/Scenes"
            };

            foreach (string path in folders)
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    int split = path.LastIndexOf('/');
                    string parent = path.Substring(0, split);
                    string folderName = path.Substring(split + 1);
                    AssetDatabase.CreateFolder(parent, folderName);
                }
            }
        }

        private static List<TowerDataSO> GenerateTowers()
        {
            List<TowerDataSO> list = new List<TowerDataSO>();

            // 1. Heavy Bolter Bunker
            TowerDataSO bolter = CreateOrReplaceAsset<TowerDataSO>($"{DataRoot}/Towers/Tower_HeavyBolter.asset");
            bolter.towerId = "heavy_bolter";
            bolter.displayName = "Heavy Bolter Bunker";
            bolter.category = TowerCategory.HeavyBolterBunker;
            bolter.damageType = DamageType.Kinetic;
            bolter.allowedNodeTypes = BuildNodeType.BunkerPad | BuildNodeType.CliffLedge;
            bolter.defaultPriority = TargetPriority.First;
            bolter.projectileSpeed = 22f;
            bolter.accentColor = new Color(1f, 0.85f, 0.25f);
            bolter.tiers = new[]
            {
                new TowerTierData
                {
                    tierName = "Mk-IV Heavy Bolter Nest",
                    description = "Fast-firing kinetic rounds. Strong vs Gretchin, Boyz & Fast Bikers.",
                    cost = 75, damage = 20f, fireRate = 3.2f, range = 3.8f
                },
                new TowerTierData
                {
                    tierName = "Twin-Linked Heavy Bolter",
                    description = "Doubled rate of fire with Kraken penetrator bolts.",
                    cost = 110, damage = 28f, fireRate = 4.4f, range = 4.2f
                },
                new TowerTierData
                {
                    tierName = "Vendetta Autocannon Bunker",
                    description = "High-caliber shells shred infantry and fast walkers across wide range.",
                    cost = 165, damage = 44f, fireRate = 5.2f, range = 4.7f
                }
            };
            EditorUtility.SetDirty(bolter);
            list.Add(bolter);

            // 2. Lascannon Turret
            TowerDataSO lascannon = CreateOrReplaceAsset<TowerDataSO>($"{DataRoot}/Towers/Tower_Lascannon.asset");
            lascannon.towerId = "lascannon";
            lascannon.displayName = "Lascannon Turret";
            lascannon.category = TowerCategory.LascannonTurret;
            lascannon.damageType = DamageType.ArmorPiercing;
            lascannon.allowedNodeTypes = BuildNodeType.CliffLedge | BuildNodeType.BunkerPad;
            lascannon.defaultPriority = TargetPriority.Strongest;
            lascannon.accentColor = new Color(0.25f, 0.9f, 1.0f);
            lascannon.tiers = new[]
            {
                new TowerTierData
                {
                    tierName = "Godhammer Lascannon",
                    description = "Slow sniper beam dealing 200% damage to Killa Kans & Deff Dreads.",
                    cost = 120, damage = 135f, fireRate = 0.55f, range = 5.8f
                },
                new TowerTierData
                {
                    tierName = "Overcharged Las-Talon",
                    description = "Amplified capacitor array increases range and armor-piercing damage.",
                    cost = 160, damage = 230f, fireRate = 0.68f, range = 6.5f
                },
                new TowerTierData
                {
                    tierName = "Twin Magna-Lascannon",
                    description = "Sunders target armor (+20% damage from all sources for 4s).",
                    cost = 240, damage = 380f, fireRate = 0.80f, range = 7.2f,
                    armorVulnerabilityBonus = 0.20f, armorVulnerabilityDuration = 4.0f
                }
            };
            EditorUtility.SetDirty(lascannon);
            list.Add(lascannon);

            // 3. Hellhound Flame-Bunker
            TowerDataSO hellhound = CreateOrReplaceAsset<TowerDataSO>($"{DataRoot}/Towers/Tower_Hellhound.asset");
            hellhound.towerId = "hellhound_flame";
            hellhound.displayName = "Hellhound Flame-Bunker";
            hellhound.category = TowerCategory.HellhoundFlameBunker;
            hellhound.damageType = DamageType.Flame;
            hellhound.allowedNodeTypes = BuildNodeType.BunkerPad;
            hellhound.defaultPriority = TargetPriority.Closest;
            hellhound.isContinuousCone = true;
            hellhound.ignoresCover = true;
            hellhound.accentColor = new Color(1f, 0.45f, 0.1f);
            hellhound.tiers = new[]
            {
                new TowerTierData
                {
                    tierName = "Inferno Flame-Bunker",
                    description = "Continuous promethium cone. Melts swarms on the Bridge and ignores cover.",
                    cost = 100, damage = 14f, fireRate = 5.0f, range = 2.8f, areaParameter = 28f
                },
                new TowerTierData
                {
                    tierName = "Pressurized Promethium Jet",
                    description = "Wider flame cone and hotter promethium mixture.",
                    cost = 140, damage = 23f, fireRate = 5.5f, range = 3.2f, areaParameter = 35f
                },
                new TowerTierData
                {
                    tierName = "Bane-Wolf Chem-Inferno",
                    description = "Incinerates entire Ork mobs instantly and slows survivors.",
                    cost = 210, damage = 36f, fireRate = 6.0f, range = 3.6f, areaParameter = 42f
                }
            };
            EditorUtility.SetDirty(hellhound);
            list.Add(hellhound);

            // 4. Basilisk Artillery Pit
            TowerDataSO basilisk = CreateOrReplaceAsset<TowerDataSO>($"{DataRoot}/Towers/Tower_Basilisk.asset");
            basilisk.towerId = "basilisk_artillery";
            basilisk.displayName = "Basilisk Artillery Pit";
            basilisk.category = TowerCategory.BasiliskArtilleryPit;
            basilisk.damageType = DamageType.Explosive;
            basilisk.allowedNodeTypes = BuildNodeType.ArtilleryEmplacement | BuildNodeType.CliffLedge;
            basilisk.defaultPriority = TargetPriority.First;
            basilisk.isGlobalArtillery = true;
            basilisk.ignoresCover = true;
            basilisk.projectileSpeed = 11f;
            basilisk.accentColor = new Color(1f, 0.35f, 0.15f);
            basilisk.tiers = new[]
            {
                new TowerTierData
                {
                    tierName = "Earthshaker Battery",
                    description = "Global range explosive shells that splash and slow Ork advances by 30%.",
                    cost = 150, damage = 95f, fireRate = 0.32f, range = 25f,
                    areaParameter = 1.8f, slowFactor = 0.30f, slowDuration = 2.2f
                },
                new TowerTierData
                {
                    tierName = "Medusa Siege Mortar",
                    description = "Larger blast radius and heavier concussive shockwaves (40% slow).",
                    cost = 200, damage = 155f, fireRate = 0.38f, range = 25f,
                    areaParameter = 2.3f, slowFactor = 0.40f, slowDuration = 2.8f
                },
                new TowerTierData
                {
                    tierName = "Tremor Quake Cannon",
                    description = "Massive seismic shells devastate clustered waves and cripple movement (50% slow).",
                    cost = 280, damage = 245f, fireRate = 0.45f, range = 25f,
                    areaParameter = 2.8f, slowFactor = 0.50f, slowDuration = 3.5f
                }
            };
            EditorUtility.SetDirty(basilisk);
            list.Add(basilisk);

            // 5. Astropathic Relay
            TowerDataSO relay = CreateOrReplaceAsset<TowerDataSO>($"{DataRoot}/Towers/Tower_AstropathicRelay.asset");
            relay.towerId = "astropathic_relay";
            relay.displayName = "Astropathic Relay";
            relay.category = TowerCategory.AstropathicRelay;
            relay.damageType = DamageType.SupportNone;
            relay.allowedNodeTypes = BuildNodeType.All;
            relay.isSupportTower = true;
            relay.accentColor = new Color(0.68f, 0.38f, 1.0f);
            relay.tiers = new[]
            {
                new TowerTierData
                {
                    tierName = "Sanctioned Psyker Spire",
                    description = "Reveals Stealth Kommandos and buffs nearby towers (+15% Range & Fire Rate).",
                    cost = 90, range = 4.2f, auraRangeBonusPercent = 0.15f, auraFireRateBonusPercent = 0.15f,
                    revealsStealth = true
                },
                new TowerTierData
                {
                    tierName = "Astropathic Choir Conduit",
                    description = "Expands psychic detection radius and buffs nearby towers (+22% Range & Fire Rate).",
                    cost = 130, range = 5.2f, auraRangeBonusPercent = 0.22f, auraFireRateBonusPercent = 0.22f,
                    revealsStealth = true
                },
                new TowerTierData
                {
                    tierName = "Librarius Telepathy Shrine",
                    description = "Map-dominating detection aura and +30% Range & Fire Rate to adjacent towers.",
                    cost = 190, range = 6.4f, auraRangeBonusPercent = 0.30f, auraFireRateBonusPercent = 0.30f,
                    revealsStealth = true
                }
            };
            EditorUtility.SetDirty(relay);
            list.Add(relay);

            return list;
        }

        private static Dictionary<EnemyCategory, EnemyDataSO> GenerateEnemies()
        {
            Dictionary<EnemyCategory, EnemyDataSO> dict = new Dictionary<EnemyCategory, EnemyDataSO>();

            dict[EnemyCategory.SwarmGretchin] = CreateEnemy(
                "Enemy_Gretchin", "Gretchin Runts", EnemyCategory.SwarmGretchin, ArmorType.Unarmored,
                maxHP: 45f, speed: 2.3f, reward: 5, reactorDmg: 1, scale: 0.85f);

            dict[EnemyCategory.SwarmOrkBoyz] = CreateEnemy(
                "Enemy_OrkBoyz", "Ork Boyz Choppa Mob", EnemyCategory.SwarmOrkBoyz, ArmorType.Unarmored,
                maxHP: 115f, speed: 1.85f, reward: 10, reactorDmg: 1, scale: 1.0f);

            dict[EnemyCategory.FastWarbiker] = CreateEnemy(
                "Enemy_Warbiker", "Speed Freeks Warbiker", EnemyCategory.FastWarbiker, ArmorType.FastLight,
                maxHP: 150f, speed: 3.35f, reward: 16, reactorDmg: 2, scale: 1.1f, bypassTerrain: true);

            dict[EnemyCategory.FastStormboy] = CreateEnemy(
                "Enemy_Stormboy", "Rokkit Stormboy", EnemyCategory.FastStormboy, ArmorType.FastLight,
                maxHP: 130f, speed: 3.6f, reward: 15, reactorDmg: 1, scale: 1.0f, bypassTerrain: true);

            dict[EnemyCategory.StealthKommando] = CreateEnemy(
                "Enemy_Kommando", "Blood Axe Kommando", EnemyCategory.StealthKommando, ArmorType.FastLight,
                maxHP: 165f, speed: 2.15f, reward: 18, reactorDmg: 2, scale: 1.0f, isStealth: true);

            dict[EnemyCategory.ArmoredKillaKan] = CreateEnemy(
                "Enemy_KillaKan", "Killa Kan Walker", EnemyCategory.ArmoredKillaKan, ArmorType.HeavyVehicle,
                maxHP: 520f, speed: 1.25f, reward: 32, reactorDmg: 3, scale: 1.25f);

            dict[EnemyCategory.ArmoredDeffDread] = CreateEnemy(
                "Enemy_DeffDread", "Deff Dread Heavy Walker", EnemyCategory.ArmoredDeffDread, ArmorType.HeavyVehicle,
                maxHP: 1150f, speed: 1.05f, reward: 55, reactorDmg: 5, scale: 1.4f);

            EnemyDataSO boss = CreateEnemy(
                "Enemy_BossGargant", "Warboss Ghaz-Drakka in Mega Armor", EnemyCategory.BossGargantWarboss,
                ArmorType.BossMegaArmor, maxHP: 6500f, speed: 0.78f, reward: 300, reactorDmg: 20, scale: 1.65f);
            boss.isBoss = true;
            boss.bossRoarInterval = 11f;
            boss.bossRoarRadius = 5.5f;
            boss.bossRoarSpeedMultiplier = 1.45f;
            boss.bossRoarDuration = 4.0f;
            boss.bossTowerDisableInterval = 13f;
            boss.bossTowerDisableRadius = 4.8f;
            boss.bossTowerDisableDuration = 4.5f;
            boss.bossMaxTowersDisabled = 2;
            EditorUtility.SetDirty(boss);
            dict[EnemyCategory.BossGargantWarboss] = boss;

            return dict;
        }

        private static EnemyDataSO CreateEnemy(
            string assetName,
            string displayName,
            EnemyCategory category,
            ArmorType armor,
            float maxHP,
            float speed,
            int reward,
            int reactorDmg,
            float scale,
            bool isStealth = false,
            bool bypassTerrain = false)
        {
            EnemyDataSO e = CreateOrReplaceAsset<EnemyDataSO>($"{DataRoot}/Enemies/{assetName}.asset");
            e.enemyId = assetName.ToLowerInvariant();
            e.displayName = displayName;
            e.category = category;
            e.armorType = armor;
            e.maxHealth = maxHP;
            e.moveSpeed = speed;
            e.requisitionReward = reward;
            e.reactorDamage = reactorDmg;
            e.visualScale = scale;
            e.isStealthUnit = isStealth;
            e.bypassesTerrainObstacles = bypassTerrain;
            EditorUtility.SetDirty(e);
            return e;
        }

        private static List<WaveDataSO> Generate20Waves(Dictionary<EnemyCategory, EnemyDataSO> enemies)
        {
            List<WaveDataSO> waves = new List<WaveDataSO>();

            string[] titles =
            {
                "Gretchin Foragers", "Scrap-Road Boyz", "Dual-Canyon Probe", "Swarm on the Bridge",
                "Speed Freeks Outriders", "Kommando Infiltration", "Iron & Smoke (Killa Kans)",
                "Rokkit Stormboyz Blitz", "Shadow & Flame", "Armored Spearhead",
                "Green Tide Rising", "Deff Dread Sighted", "Stealth Stalkers", "Thunder on the Causeway",
                "Dread Mob March", "Blitzkrieg in Ash-Gully", "Iron Wall of Gork", "Kommando Nightfall",
                "Apocalyptic Vanguard", "WAAAGH! Warboss in Mega Armor"
            };

            for (int w = 1; w <= 20; w++)
            {
                WaveDataSO wave = CreateOrReplaceAsset<WaveDataSO>($"{DataRoot}/Waves/Wave_{w:D2}.asset");
                wave.waveNumber = w;
                wave.waveTitle = titles[w - 1];
                wave.completionRequisitionBonus = 45 + w * 10;
                wave.completionCommandPointBonus = (w % 2 == 0) ? 1 : 1;
                wave.spawnGroups = new List<WaveSpawnGroup>();

                // Scale swarm numbers across waves 1..20
                if (w <= 4)
                {
                    wave.incomingTransmissionText = "Ork infantry mobs advancing along the Scrap-Road and Ash-Gully!";
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.SwarmGretchin], LaneId.ScrapRoadNorth, 6 + w * 3, 0.55f, 0f));
                    if (w >= 2)
                        wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.SwarmOrkBoyz], LaneId.AshGullySouth, 3 + w * 2, 0.85f, 2f));
                }
                else if (w == 5 || w == 8)
                {
                    wave.incomingTransmissionText = "Fast Warbikers & Stormboyz bypassing terrain obstacles! Use Heavy Bolters!";
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.FastWarbiker], LaneId.ScrapRoadNorth, 4 + w, 0.75f, 0f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.FastStormboy], LaneId.AshGullySouth, 4 + w, 0.65f, 2f));
                }
                else if (w == 6 || w == 9 || w == 13 || w == 18)
                {
                    wave.incomingTransmissionText = "STEALTH ALERT: Ork Kommandos detected! Deploy Astropathic Relays or Drop Pods!";
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.SwarmOrkBoyz], LaneId.ScrapRoadNorth, 8 + w, 0.6f, 0f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.StealthKommando], LaneId.AshGullySouth, 4 + w / 2, 1.1f, 1.5f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.StealthKommando], LaneId.ScrapRoadNorth, 3 + w / 2, 1.1f, 2.0f));
                }
                else if (w == 7 || w == 10 || w == 12 || w == 15 || w == 17)
                {
                    wave.incomingTransmissionText = "HEAVY ARMOR: Killa Kans and Deff Dreads approaching! Power up Lascannon Turrets!";
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.SwarmGretchin], LaneId.ScrapRoadNorth, 12 + w, 0.4f, 0f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.ArmoredKillaKan], LaneId.ScrapRoadNorth, 2 + w / 3, 2.2f, 1.5f));
                    if (w >= 12)
                        wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.ArmoredDeffDread], LaneId.AshGullySouth, 1 + (w - 10) / 3, 3.0f, 2.5f));
                }
                else if (w < 20)
                {
                    wave.incomingTransmissionText = "Massive combined-arms Ork assault across both canyons!";
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.SwarmOrkBoyz], LaneId.ScrapRoadNorth, 10 + w, 0.5f, 0f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.FastStormboy], LaneId.AshGullySouth, 6 + w / 2, 0.6f, 1.5f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.ArmoredKillaKan], LaneId.AshGullySouth, 3 + w / 4, 2.0f, 2.0f));
                }
                else
                {
                    // Wave 20 Boss Finale
                    wave.incomingTransmissionText = "FINAL WAVE: Warboss Ghaz-Drakka in Mega Armor leads the Gargant WAAAGH! Protect the Plasma Reactor at all costs!";
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.SwarmOrkBoyz], LaneId.ScrapRoadNorth, 18, 0.45f, 0f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.StealthKommando], LaneId.AshGullySouth, 8, 0.9f, 1.5f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.ArmoredDeffDread], LaneId.AshGullySouth, 3, 2.8f, 2.0f));
                    wave.spawnGroups.Add(MakeGroup(enemies[EnemyCategory.BossGargantWarboss], LaneId.ScrapRoadNorth, 1, 1.0f, 3.5f));
                }

                EditorUtility.SetDirty(wave);
                waves.Add(wave);
            }

            return waves;
        }

        private static WaveSpawnGroup MakeGroup(EnemyDataSO enemy, LaneId lane, int count, float interval, float delay)
        {
            return new WaveSpawnGroup
            {
                groupNote = $"{count}x {enemy.displayName} ({lane})",
                enemyData = enemy,
                lane = lane,
                count = count,
                spawnInterval = interval,
                delayBeforeGroup = delay
            };
        }

        private static List<AbilityDataSO> GenerateAbilities()
        {
            List<AbilityDataSO> list = new List<AbilityDataSO>();

            AbilityDataSO orbital = CreateOrReplaceAsset<AbilityDataSO>($"{DataRoot}/Abilities/Ability_OrbitalBombardment.asset");
            orbital.abilityId = "orbital_bombardment";
            orbital.displayName = "Orbital Bombardment";
            orbital.abilityType = CommandAbilityType.OrbitalBombardment;
            orbital.description = "High-damage Imperial Navy lance strike on a targeted area.";
            orbital.commandPointCost = 2;
            orbital.cooldownSeconds = 45f;
            orbital.effectRadius = 3.2f;
            orbital.impactDamage = 550f;
            orbital.damageType = DamageType.Explosive;
            orbital.strikeWarningDelay = 1.1f;
            orbital.indicatorColor = new Color(1.0f, 0.3f, 0.2f, 0.65f);
            EditorUtility.SetDirty(orbital);
            list.Add(orbital);

            AbilityDataSO dropPod = CreateOrReplaceAsset<AbilityDataSO>($"{DataRoot}/Abilities/Ability_DropPodAssault.asset");
            dropPod.abilityId = "drop_pod_assault";
            dropPod.displayName = "Drop Pod Assault";
            dropPod.abilityType = CommandAbilityType.DropPodAssault;
            dropPod.description = "Deploys 3 Space Marines onto the path for 15s to physically block and fight Orks.";
            dropPod.commandPointCost = 2;
            dropPod.cooldownSeconds = 35f;
            dropPod.effectRadius = 2.4f;
            dropPod.impactDamage = 180f;
            dropPod.squadLifetimeSeconds = 15f;
            dropPod.marineCount = 3;
            dropPod.maxBlockedEnemies = 4;
            dropPod.squadMeleeDPS = 55f;
            dropPod.squadBolterDPS = 35f;
            dropPod.squadEngagementRadius = 2.8f;
            dropPod.indicatorColor = new Color(0.25f, 0.65f, 1.0f, 0.65f);
            EditorUtility.SetDirty(dropPod);
            list.Add(dropPod);

            return list;
        }

        private static void BuildOutpostAegisScene(
            List<TowerDataSO> towers,
            List<WaveDataSO> waves,
            List<AbilityDataSO> abilities)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Tactical Camera (Orthographic 2D)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
            cam.backgroundColor = new Color(0.14f, 0.12f, 0.11f); // Ash-waste dark slate
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CameraController>();

            // 2. Environment Visual Backdrop (Promethium River, Narrow Bridge, Convergence Causeway, Reactor Gate)
            GameObject envRoot = new GameObject("--- ENVIRONMENT & CHOKPOINTS ---");
            CreateVisualStrip(envRoot.transform, "Promethium_River", new Vector3(-2.2f, 0f, 0f), new Vector3(1.1f, 13f, 1f), new Color(0.95f, 0.35f, 0.08f, 0.45f), 0);
            CreateVisualStrip(envRoot.transform, "Narrow_Bridge_Deck", new Vector3(-2.2f, 2.2f, 0f), new Vector3(1.8f, 1.3f, 1f), new Color(0.35f, 0.34f, 0.32f, 0.9f), 1);
            CreateVisualStrip(envRoot.transform, "Convergence_Causeway", new Vector3(4.2f, 0.2f, 0f), new Vector3(5.2f, 2.0f, 1f), new Color(0.28f, 0.26f, 0.24f, 0.85f), 0);
            CreateVisualStrip(envRoot.transform, "Central_Plasma_Reactor_Core", new Vector3(7.6f, 0.2f, 0f), new Vector3(1.6f, 2.6f, 1f), new Color(0.2f, 0.75f, 1.0f, 0.9f), 3);

            // 3. Cover Zone in Ash-Gully (ignored by Hellhound Flame-Bunker)
            GameObject coverObj = new GameObject("AshGully_CoverTrench");
            coverObj.transform.SetParent(envRoot.transform, false);
            coverObj.transform.position = new Vector3(-3.0f, -2.5f, 0f);
            CoverZone cz = coverObj.AddComponent<CoverZone>();
            cz.zoneName = "Ash-Gully Scrap Trench";
            cz.radius = 2.0f;

            // 4. Two Converging Canyon Lanes
            GameObject lanesRoot = new GameObject("--- CANYON LANES ---");

            // North Lane: The Scrap-Road (crosses the Narrow Bridge over the Promethium River)
            WaypointPath scrapRoad = CreateLane(
                lanesRoot.transform,
                "Lane_North_ScrapRoad",
                LaneId.ScrapRoadNorth,
                "The Scrap-Road",
                new Color(1f, 0.6f, 0.2f),
                new[]
                {
                    new Vector3(-8.5f, 3.2f, 0f),
                    new Vector3(-5.5f, 3.2f, 0f),
                    new Vector3(-3.6f, 2.2f, 0f),
                    new Vector3(-0.8f, 2.2f, 0f), // Narrow Bridge Chokepoint
                    new Vector3(1.5f, 0.2f, 0f),  // Convergence Point
                    new Vector3(4.5f, 0.2f, 0f),  // Wide Causeway
                    new Vector3(7.4f, 0.2f, 0f)   // Base Gate / Central Plasma Reactor
                },
                roughSegmentIndex: 1);

            // South Lane: The Ash-Gully (winds through southern trench then converges)
            WaypointPath ashGully = CreateLane(
                lanesRoot.transform,
                "Lane_South_AshGully",
                LaneId.AshGullySouth,
                "The Ash-Gully",
                new Color(0.75f, 0.85f, 0.25f),
                new[]
                {
                    new Vector3(-8.5f, -3.2f, 0f),
                    new Vector3(-5.2f, -3.2f, 0f),
                    new Vector3(-2.2f, -2.2f, 0f),
                    new Vector3(1.5f, 0.2f, 0f),  // Convergence Point
                    new Vector3(4.5f, 0.2f, 0f),  // Wide Causeway
                    new Vector3(7.4f, 0.2f, 0f)   // Base Gate / Central Plasma Reactor
                },
                roughSegmentIndex: 2);

            // 5. Fixed Tactical Build Nodes (Kingdom Rush style)
            GameObject nodesRoot = new GameObject("--- TACTICAL BUILD NODES ---");
            CreateBuildNode(nodesRoot.transform, "Bridge Chokepoint North Pad", BuildNodeType.BunkerPad, new Vector3(-2.2f, 3.55f, 0f));
            CreateBuildNode(nodesRoot.transform, "Bridge Chokepoint South Pad", BuildNodeType.BunkerPad, new Vector3(-2.2f, 0.85f, 0f));
            CreateBuildNode(nodesRoot.transform, "Scrap-Road Forward Bunker", BuildNodeType.BunkerPad, new Vector3(-5.2f, 1.9f, 0f));
            CreateBuildNode(nodesRoot.transform, "Ash-Gully Trench Bunker", BuildNodeType.BunkerPad, new Vector3(-4.2f, -1.8f, 0f));
            CreateBuildNode(nodesRoot.transform, "Ash-Gully Bend Pad", BuildNodeType.BunkerPad, new Vector3(-1.2f, -3.4f, 0f));

            // High Cliff Ledges overlooking the Convergence Point
            CreateBuildNode(nodesRoot.transform, "High Cliff Ledge (North Overlook)", BuildNodeType.CliffLedge, new Vector3(1.3f, 2.1f, 0f));
            CreateBuildNode(nodesRoot.transform, "High Cliff Ledge (South Overlook)", BuildNodeType.CliffLedge, new Vector3(1.3f, -1.7f, 0f));

            // Causeway & Rear Artillery Emplacements
            CreateBuildNode(nodesRoot.transform, "Causeway North Guard Pad", BuildNodeType.BunkerPad, new Vector3(4.2f, 1.65f, 0f));
            CreateBuildNode(nodesRoot.transform, "Causeway South Guard Pad", BuildNodeType.BunkerPad, new Vector3(4.2f, -1.25f, 0f));
            CreateBuildNode(nodesRoot.transform, "Rear Basilisk Artillery Pit Alpha", BuildNodeType.ArtilleryEmplacement, new Vector3(6.2f, 3.1f, 0f));
            CreateBuildNode(nodesRoot.transform, "Rear Basilisk Artillery Pit Beta", BuildNodeType.ArtilleryEmplacement, new Vector3(6.2f, -2.7f, 0f));

            // 6. Core Managers & UI
            GameObject managersObj = new GameObject("--- GAME MANAGERS & HUD ---");
            GameManager gm = managersObj.AddComponent<GameManager>();
            gm.availableTowers = towers;
            gm.availableAbilities = abilities;

            WaveManager wm = managersObj.AddComponent<WaveManager>();
            wm.scrapRoadNorthLane = scrapRoad;
            wm.ashGullySouthLane = ashGully;
            wm.waves = waves;

            managersObj.AddComponent<CommandAbilityManager>();
            managersObj.AddComponent<HUDController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static WaypointPath CreateLane(
            Transform parent,
            string objName,
            LaneId id,
            string displayName,
            Color color,
            Vector3[] points,
            int roughSegmentIndex)
        {
            GameObject laneObj = new GameObject(objName);
            laneObj.transform.SetParent(parent, false);
            WaypointPath path = laneObj.AddComponent<WaypointPath>();
            path.laneId = id;
            path.laneDisplayName = displayName;
            path.gizmoColor = color;
            path.roughTerrainWaypointIndices.Add(roughSegmentIndex);

            LineRenderer lr = laneObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 1;
            lr.positionCount = points.Length;
            lr.startWidth = 0.55f;
            lr.endWidth = 0.55f;
            Color roadCol = new Color(0.32f, 0.28f, 0.24f, 0.85f);
            lr.startColor = roadCol;
            lr.endColor = roadCol;

            for (int i = 0; i < points.Length; i++)
            {
                GameObject wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(laneObj.transform, false);
                wp.transform.position = points[i];
                path.waypoints.Add(wp.transform);
                lr.SetPosition(i, points[i]);
            }

            return path;
        }

        private static void CreateBuildNode(Transform parent, string name, BuildNodeType type, Vector3 pos)
        {
            GameObject nodeObj = new GameObject(name);
            nodeObj.transform.SetParent(parent, false);
            nodeObj.transform.position = pos;
            BuildNode node = nodeObj.AddComponent<BuildNode>();
            node.nodeName = name;
            node.nodeType = type;
        }

        private static void CreateVisualStrip(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = scale * 4f;
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSpriteGenerator.GetWhiteSquareSprite();
            sr.color = color;
            sr.sortingOrder = sortOrder;
        }

        private static T CreateOrReplaceAsset<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }
    }
}
#endif
