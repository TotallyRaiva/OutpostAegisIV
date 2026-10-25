using UnityEngine;

namespace OutpostAegis.Data
{
    [CreateAssetMenu(fileName = "EnemyData_", menuName = "Outpost Aegis-IV/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        public string enemyId;
        public string displayName;
        public EnemyCategory category;
        public ArmorType armorType;

        [Header("Core Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 2.0f;
        public float meleeDPS = 12f;
        public int requisitionReward = 10;
        public int reactorDamage = 1;

        [Header("Special Traits")]
        [Tooltip("Kommandos: Invisible until revealed by an Astropathic Relay or engaged in melee.")]
        public bool isStealthUnit;
        [Tooltip("Warbikers & Stormboyz: Ignore canyon rough terrain slowdowns and take shortcut jumps.")]
        public bool bypassesTerrainObstacles;

        [Header("Boss Abilities (Wave 20 Gargant / Warboss in Mega Armor)")]
        public bool isBoss;
        public float bossRoarInterval = 12f;
        public float bossRoarRadius = 5.5f;
        public float bossRoarSpeedMultiplier = 1.45f;
        public float bossRoarDuration = 4.0f;

        public float bossTowerDisableInterval = 14f;
        public float bossTowerDisableRadius = 5.0f;
        public float bossTowerDisableDuration = 4.5f;
        public int bossMaxTowersDisabled = 2;

        [Header("Visuals")]
        public Color tintColor = new Color(0.25f, 0.65f, 0.2f);
        public float visualScale = 1.0f;
        public Sprite sprite;
    }
}
