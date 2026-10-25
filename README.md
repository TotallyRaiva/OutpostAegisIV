# Warhammer 40k: The Siege of Outpost Aegis-IV

A mobile-first 2D Pixel Art Tower Defense game in Unity (`OutpostAegisIV`) set on an Imperial ash-waste Promethium refinery besieged by a 20-wave Ork *WAAAGH!*.

## Quick Start in Unity

1. Open **Unity Hub** and click **Add > Add project from disk**, selecting `/Users/Reiwa/CODE/OutpostAegisIV`.
2. Open the project in your Unity Editor (2022.3 LTS or Unity 6 LTS with iOS Build Support).
3. In the top Unity menu bar, click:
   **`Tools > Outpost Aegis-IV > Generate Data & Prototype Scene`**
4. Press **Play**!

## Included Gameplay Systems

- **Dual Converging Canyon Map**: *The Scrap-Road* (North, crossing the Narrow Promethium Bridge) and *The Ash-Gully* (South trench with partial cover) converging onto the wide Causeway before the Central Plasma Reactor.
- **Kingdom Rush–Style Tactical Build Nodes**:
  - `BunkerPad`: Low-ground canyon & bridge pads for Heavy Bolters, Hellhound Flame-Bunkers, and Astropathic Relays.
  - `CliffLedge`: High-ground overlooks granting **+15% bonus range** to Lascannons, Heavy Bolters, and Relays.
  - `ArtilleryEmplacement`: Rear reinforced pits for global-range Basilisk Earthshaker batteries.
- **5 Imperial Towers (3 Upgrade Tiers + Target Priority Cycling)**:
  - **Heavy Bolter Bunker** (`Kinetic` — bonus vs Swarm & Fast Warbikers/Stormboyz)
  - **Lascannon Turret** (`ArmorPiercing` — 200% damage vs Killa Kans & Deff Dreads, Tier 3 melts armor)
  - **Hellhound Flame-Bunker** (`Flame` — continuous AoE cone, melts Gretchin/Boyz swarms and ignores cover)
  - **Basilisk Artillery Pit** (`Explosive` — global splash + concussive slow)
  - **Astropathic Relay** (`Support` — buffs adjacent tower range & fire rate, and reveals stealth Kommandos)
- **Command Point Abilities**:
  - **Orbital Bombardment**: High-damage Imperial Navy lance strike with warning lock-on.
  - **Drop Pod Assault**: Deploys 3 Adeptus Astartes Space Marines onto the path for 15 seconds to physically block and fight Orks in melee.
