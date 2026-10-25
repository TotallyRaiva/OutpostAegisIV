using UnityEngine;

namespace OutpostAegis.Data
{
    [CreateAssetMenu(fileName = "AbilityData_", menuName = "Outpost Aegis-IV/Command Ability Data")]
    public class AbilityDataSO : ScriptableObject
    {
        public string abilityId;
        public string displayName;
        public CommandAbilityType abilityType;
        [TextArea(2, 3)]
        public string description;

        [Header("Cost & Cooldown")]
        public int commandPointCost = 2;
        public float cooldownSeconds = 45f;

        [Header("Strike / Impact Parameters")]
        public float effectRadius = 3.2f;
        public float impactDamage = 450f;
        public DamageType damageType = DamageType.Explosive;
        public float strikeWarningDelay = 1.2f;

        [Header("Drop Pod Space Marine Squad (DropPodAssault only)")]
        public float squadLifetimeSeconds = 15f;
        public int marineCount = 3;
        public int maxBlockedEnemies = 4;
        public float squadMeleeDPS = 48f;
        public float squadBolterDPS = 30f;
        public float squadEngagementRadius = 2.8f;

        [Header("Visuals")]
        public Color indicatorColor = new Color(0.2f, 0.6f, 1.0f, 0.6f);
    }
}
