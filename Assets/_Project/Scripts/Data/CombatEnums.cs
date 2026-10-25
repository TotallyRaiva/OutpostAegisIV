using System;

namespace OutpostAegis.Data
{
    public enum DamageType
    {
        Kinetic,        // Heavy Bolter & Space Marine Bolters - strong vs Swarm & Fast
        ArmorPiercing,  // Lascannon - strong vs Heavy Vehicles & Boss Mega Armor
        Flame,          // Hellhound Flame-Bunker - continuous AoE cone, melts swarms, ignores cover
        Explosive,      // Basilisk Artillery & Orbital Bombardment - splash AoE + slow
        SupportNone     // Astropathic Relay
    }

    public enum ArmorType
    {
        Unarmored,      // Gretchin, Ork Boyz
        FastLight,      // Warbikers, Stormboyz, Kommandos
        HeavyVehicle,   // Killa Kans, Deff Dreads
        BossMegaArmor   // Gargant / Warboss in Mega Armor
    }

    public enum TargetPriority
    {
        First,          // Closest to the Central Plasma Reactor
        Strongest,      // Highest current HP (default for Lascannon)
        Closest,        // Closest to the tower
        Last            // Furthest from the Reactor
    }

    [Flags]
    public enum BuildNodeType
    {
        None = 0,
        BunkerPad = 1 << 0,            // Low ground pads (Bridge & Canyon edges)
        CliffLedge = 1 << 1,           // High ground overlooking convergence (+15% range bonus)
        ArtilleryEmplacement = 1 << 2, // Reinforced rear pits for Basilisk Artillery
        All = BunkerPad | CliffLedge | ArtilleryEmplacement
    }

    public enum TowerCategory
    {
        HeavyBolterBunker,
        LascannonTurret,
        HellhoundFlameBunker,
        BasiliskArtilleryPit,
        AstropathicRelay
    }

    public enum EnemyCategory
    {
        SwarmGretchin,
        SwarmOrkBoyz,
        FastWarbiker,
        FastStormboy,
        StealthKommando,
        ArmoredKillaKan,
        ArmoredDeffDread,
        BossGargantWarboss
    }

    public enum LaneId
    {
        ScrapRoadNorth = 0, // Winds over the Narrow Promethium Bridge
        AshGullySouth = 1   // Winds through the southern canyon trench
    }

    public enum CommandAbilityType
    {
        OrbitalBombardment,
        DropPodAssault
    }

    public enum GameState
    {
        Preparing,
        WaveInProgress,
        Victory,
        Defeat
    }
}
