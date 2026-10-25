using System;
using UnityEngine;

namespace OutpostAegis.Data
{
    [Serializable]
    public struct TowerTierData
    {
        public string tierName;
        [TextArea(2, 3)]
        public string description;
        public int cost;
        public float damage;
        [Tooltip("Attacks per second (or ticks per second for continuous flame).")]
        public float fireRate;
        [Tooltip("World units range. Use >= 40 for global Basilisk artillery.")]
        public float range;
        [Tooltip("Splash radius for Basilisk, or Cone half-angle in degrees for Hellhound.")]
        public float areaParameter;
        [Range(0f, 0.8f)]
        public float slowFactor;
        public float slowDuration;
        [Tooltip("Bonus damage multiplier applied to target on hit (e.g., Tier 3 Lascannon armor melt).")]
        public float armorVulnerabilityBonus;
        public float armorVulnerabilityDuration;

        [Header("Support Aura (Astropathic Relay)")]
        public float auraRangeBonusPercent;
        public float auraFireRateBonusPercent;
        public bool revealsStealth;
    }

    [CreateAssetMenu(fileName = "TowerData_", menuName = "Outpost Aegis-IV/Tower Data")]
    public class TowerDataSO : ScriptableObject
    {
        public string towerId;
        public string displayName;
        public TowerCategory category;
        public DamageType damageType;
        public BuildNodeType allowedNodeTypes = BuildNodeType.All;
        public TargetPriority defaultPriority = TargetPriority.First;

        [Header("Combat Behavior")]
        public bool isContinuousCone;
        public bool isGlobalArtillery;
        public bool isSupportTower;
        public bool ignoresCover;
        public float projectileSpeed = 18f;

        [Header("Visuals")]
        public Color primaryColor = Color.gray;
        public Color accentColor = Color.yellow;
        public Sprite icon;

        [Header("3 Upgrade Tiers (Tier 1 = Base Build, Tier 2 & 3 = Upgrades)")]
        public TowerTierData[] tiers = new TowerTierData[3];

        public int GetTotalInvestedCost(int currentTierIndex)
        {
            int total = 0;
            for (int i = 0; i <= currentTierIndex && i < tiers.Length; i++)
            {
                total += tiers[i].cost;
            }
            return total;
        }
    }
}
