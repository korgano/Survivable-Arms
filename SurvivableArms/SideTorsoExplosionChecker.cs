using System;
using Harmony;
using BattleTech;
using UnityEngine;
using HBS.Logging;

namespace SurvivableArms
{
    class SideTorsoExplosionChecker
    {
        [HarmonyPatch(typeof(BattleTech.Mech))]
        [HarmonyPatch("DamageLocation")]
        public static class BattleTech_Mech_DamageLocation_Prefix
        {
            static void Prefix(Mech __instance, int originalHitLoc, WeaponHitInfo hitInfo, ArmorLocation aLoc, Weapon weapon,
                float totalArmorDamage, float directStructureDamage, int hitIndex, AttackImpactQuality impactQuality, DamageType damageType)
            {
                var logger = Logger.GetLogger("SurvivableArms");
                logger.LogAtLevel(LogLevel.Debug, $"Entering DamageLocation - originalHitLoc: {originalHitLoc}, aLoc: {aLoc}, totalArmorDamage: {totalArmorDamage}, directStructureDamage: {directStructureDamage}");

                if (aLoc == ArmorLocation.None || aLoc == ArmorLocation.Invalid)
                {
                    logger.LogAtLevel(LogLevel.Debug, "Exiting DamageLocation - Invalid ArmorLocation");
                    return;
                }

                float num = totalArmorDamage;
                float currentArmor = __instance.GetCurrentArmor(aLoc);
                logger.LogAtLevel(LogLevel.Debug, $"Current Armor: {currentArmor}");

                if (currentArmor > 0f)
                {
                    num = totalArmorDamage - currentArmor;
                }
                logger.LogAtLevel(LogLevel.Debug, $"Calculated num after armor damage: {num}");

                num += directStructureDamage; // account for damage split: this should get us back where we were when we both had armour spillover damage and any damage done directly to the structure
                logger.LogAtLevel(LogLevel.Debug, $"Calculated num after structure damage: {num}");

                if (num <= 0f)
                {
                    logger.LogAtLevel(LogLevel.Debug, "Exiting DamageLocation - No effective damage");
                    return; //no need to continue if the shot doesn't do anything we care about
                }

                ChassisLocations chassisLocationFromArmorLocation = MechStructureRules.GetChassisLocationFromArmorLocation(aLoc);
                logger.LogAtLevel(LogLevel.Debug, $"Chassis Location from Armor Location: {chassisLocationFromArmorLocation}");

                float currentStructure = __instance.GetCurrentStructure(chassisLocationFromArmorLocation);
                logger.LogAtLevel(LogLevel.Debug, $"Current Structure: {currentStructure}");

                if (currentStructure > 0f)
                {
                    float num4 = Math.Min(num, currentStructure);
                    bool WasDestroyed = (currentStructure - num) <= 0; //if currentstructure minus remaining damage is less or equal to 0, then the location is destroyed.
                    logger.LogAtLevel(LogLevel.Debug, $"WasDestroyed: {WasDestroyed}");

                    num -= num4;
                    if (WasDestroyed && num4 > 0.01f) //this location was destroyed, so we now check for dependents.
                    {
                        if (chassisLocationFromArmorLocation == ChassisLocations.LeftArm)
                        {
                            Holder.LeftArmSurvived = false; //invalidate if the actual arm was destroyed
                            logger.LogAtLevel(LogLevel.Debug, "Left Arm Destroyed");
                        }
                        else if (chassisLocationFromArmorLocation == ChassisLocations.RightArm)
                        {
                            Holder.RightArmSurvived = false; //invalidate if the actual arm was destroyed
                            logger.LogAtLevel(LogLevel.Debug, "Right Arm Destroyed");
                        }
                        ChassisLocations dependentLocation = MechStructureRules.GetDependentLocation(chassisLocationFromArmorLocation);
                        logger.LogAtLevel(LogLevel.Debug, $"Dependent Location: {dependentLocation}");

                        if (dependentLocation != ChassisLocations.None && !__instance.IsLocationDestroyed(dependentLocation))
                        {
                            if (dependentLocation == ChassisLocations.LeftArm)
                            {
                                Holder.LeftArmSurvived = true; //side torso was destroyed, no reason the arm should be totally trashed.
                                logger.LogAtLevel(LogLevel.Debug, "Left Arm Survived");
                            }
                            else if (dependentLocation == ChassisLocations.RightArm)
                            {
                                Holder.RightArmSurvived = true; //side torso was destroyed, no reason the arm should be totally trashed.
                                logger.LogAtLevel(LogLevel.Debug, "Right Arm Survived");
                            }
                        }
                    }
                }
            }
        }
    }
}
