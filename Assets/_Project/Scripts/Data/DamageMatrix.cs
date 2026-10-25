namespace OutpostAegis.Data
{
    /// <summary>
    /// Evaluates Warhammer 40k damage effectiveness against Ork armor classes and canyon cover.
    /// </summary>
    public static class DamageMatrix
    {
        public static float GetDamageMultiplier(DamageType damageType, ArmorType armorType)
        {
            switch (damageType)
            {
                case DamageType.Kinetic:
                    switch (armorType)
                    {
                        case ArmorType.Unarmored:     return 1.25f; // Strong vs light infantry
                        case ArmorType.FastLight:     return 1.50f; // Warbikers & Stormboyz vulnerable to Heavy Bolters
                        case ArmorType.HeavyVehicle:  return 0.35f; // Killa Kans & Deff Dreads resist standard gunfire
                        case ArmorType.BossMegaArmor: return 0.50f;
                    }
                    break;

                case DamageType.ArmorPiercing:
                    switch (armorType)
                    {
                        case ArmorType.Unarmored:     return 1.00f;
                        case ArmorType.FastLight:     return 1.00f;
                        case ArmorType.HeavyVehicle:  return 2.00f; // Lascannons shred Killa Kans & Deff Dreads
                        case ArmorType.BossMegaArmor: return 1.75f; // High effectiveness vs Gargant / Mega Armor
                    }
                    break;

                case DamageType.Flame:
                    switch (armorType)
                    {
                        case ArmorType.Unarmored:     return 1.75f; // Melts Gretchin & Boyz swarms
                        case ArmorType.FastLight:     return 1.10f;
                        case ArmorType.HeavyVehicle:  return 0.40f;
                        case ArmorType.BossMegaArmor: return 0.60f;
                    }
                    break;

                case DamageType.Explosive:
                    switch (armorType)
                    {
                        case ArmorType.Unarmored:     return 1.50f;
                        case ArmorType.FastLight:     return 1.00f;
                        case ArmorType.HeavyVehicle:  return 1.00f;
                        case ArmorType.BossMegaArmor: return 1.00f;
                    }
                    break;
            }

            return 1.0f;
        }

        public static float CalculateFinalDamage(
            float baseDamage,
            DamageType damageType,
            ArmorType armorType,
            bool targetInCover,
            bool attackIgnoresCover,
            float targetDamageTakenMultiplier = 1f)
        {
            float mult = GetDamageMultiplier(damageType, armorType);

            // Canyon rock cover reduces non-flame / non-artillery damage by 30%
            if (targetInCover && !attackIgnoresCover && damageType != DamageType.Flame && damageType != DamageType.Explosive)
            {
                mult *= 0.70f;
            }

            return baseDamage * mult * targetDamageTakenMultiplier;
        }
    }
}
