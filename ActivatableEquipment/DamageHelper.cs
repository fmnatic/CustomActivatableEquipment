﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using HBS;
using CustomComponents;
using CustomAmmoCategoriesPatches;

namespace CustomActivatableEquipment.DamageHelpers
{
    [HarmonyPatch(typeof(Mech), "AddExternalHeat")]
    public static class Mech_AddExternalHeat_Patch
    {

        private static void Postfix(Mech __instance,string reason,int amt)
        {
            if (__instance == null)
            {
                Log.LogWrite("No mech\n");
                return;
            }
            Log.LogWrite($"{new string('═', 46)}\n");
            Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took {amt} Heat Damage from {reason ?? "null"}\n");
            DamageHelper.ActivateComponentsBasedOnHeatDamage(__instance, amt);

        }
    }
    [HarmonyPatch(typeof(Mech))]
    [HarmonyPatch("TakeWeaponDamage")]
    [HarmonyPatch(MethodType.Normal)]
#if BT1_8
    [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
#else
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(int), typeof(DamageType) })]
#endif
    public static class Mech_TakeWeaponDamage
    {
#if BT1_8
        public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType)
        {
#else
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, int hitIndex, DamageType damageType) {
#endif
            if (__instance == null)
            {
                Log.LogWrite("No mech\n");
                return;
            }
            Log.LogWrite($"{new string('═', 46)}\n");
            string wname = (weapon!=null) ? (weapon.Name?? "null") : "null";
            Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took Damage from {wname} - {damageType.ToString()}\n");
            DamageHelper.ActivateComponentsBasedOnDamage(__instance, damageAmount, directStructureDamage);
        }
    }

        public class DamageHelper
        {

            internal static float MaxArmorForLocation(Mech mech, int Location)
            {
                if (mech != null)
                {
                    Statistic stat = mech.StatCollection.GetStatistic(mech.GetStringForArmorLocation((ArmorLocation)Location));
                    if (stat == null)
                    {
                        Log.LogWrite($"Can't get armor stat  { mech.DisplayName } location:{ Location.ToString()}\n");
                        return 0;
                    }
                    //Log.LogWrite($"armor stat  { mech.DisplayName } location:{ Location.ToString()} :{stat.DefaultValue<float>()}");
                    return stat.DefaultValue<float>();
                }
                Log.LogWrite($"Mech null\n");
                return 0;
            }
            internal static float MaxStructureForLocation(Mech mech, int Location)
            {
                if (mech != null)
                {
                    Statistic stat = mech.StatCollection.GetStatistic(mech.GetStringForStructureLocation((ChassisLocations)Location));
                    if (stat == null)
                    {
                        Log.LogWrite($"Can't get structure stat  { mech.DisplayName } location:{ Location.ToString()}\n");
                        return 0;
                    }
                    //Log.LogWrite($"structure stat  { mech.DisplayName } location:{ Location.ToString()}:{stat.DefaultValue<float>()}");
                    return stat.DefaultValue<float>();
                }
                Log.LogWrite($"Mech null\n");
                return 0;
            }

            public static void ActivateComponentsBasedOnHeatDamage(Mech defender, int heatDamage)
            {
                if (heatDamage<=0)
                {
                    Log.LogWrite("No heat damage\n");
                    return;
                }

                if (defender==null)
                {
                    Log.LogWrite("No mech\n");
                    return;
                }

                if (defender.IsDead || defender.IsFlaggedForDeath || defender.IsShutDown)
                {
                    Log.LogWrite($"{defender.DisplayName} dead or shutdown.\n");//<check> do we need to handle incoming damage when shutdown on startup?
                    return;
                }

            foreach (MechComponent component in defender.allComponents)
                {
                    ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
                    if (tactivatable == null) { continue; }
                    if (ActivatableComponent.canBeDamageActivated(component) == false) { continue; };
                    if (defender.IsLocationDestroyed((ChassisLocations)component.Location))
                    {
                        Log.LogWrite($"Ignored {component.Name} installed in destroyed {((ChassisLocations)component.Location).ToString()}\n");
                        continue;
                    };
                    if (defender is Mech mech)
                    {
                        Log.LogWrite($"Damage >>> D: {0:F3} DS: {0:F3} H: {heatDamage}\n");                            
                    }
                    else
                    {
                        Log.LogWrite($"Not a mech, somethings broken\n");
                    }

                    if (defender.isHasHeat() )
                    {//if not battle armor 
                        ActivatableComponent.ActivateOnIncomingHeat(component, heatDamage);
                    }
                    else
                    {
                        Log.LogWrite($" { defender.DisplayName } can't have incoming heat damage\n");
                    }
                }

            }

            public static void ActivateComponentsBasedOnDamage(Mech defender, float damageAmount, float directStructureDamage)
            {
                if (defender == null)
                {
                    Log.LogWrite("No mech\n");
                    return;
                }
                if (defender.IsDead || defender.IsFlaggedForDeath || defender.IsShutDown)
                {
                    Log.LogWrite($"{defender.DisplayName} dead or shutdown.\n");//<check> do we need to handle incoming damage when shutdown on startup?
                    return;
                }
                if((damageAmount+directStructureDamage)<=0)
                {
                    Log.LogWrite("No damage\n");
                    return;
                }
                bool gotdamagevalues = false;

                float Head_s = 0;
                float LeftArm_s = 0;
                float LeftTorso_s = 0;
                float CenterTorso_s = 0;
                float RightTorso_s = 0;
                float RightArm_s = 0;
                float LeftLeg_s = 0;
                float RightLeg_s = 0;

                float Head_a = 0;
                float LeftArm_a = 0;
                float LeftTorso_a = 0;
                float CenterTorso_a = 0;
                float RightTorso_a = 0;
                float RightArm_a = 0;
                float LeftLeg_a = 0;
                float RightLeg_a = 0;

                foreach (MechComponent component in defender.allComponents)
                {
                    ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
                    if (tactivatable == null) { continue; }
                    if (ActivatableComponent.canBeDamageActivated(component) == false) { continue; };
                    if (defender.IsLocationDestroyed((ChassisLocations)component.Location)) {
                        Log.LogWrite($"Ignored {component.Name} installed in destroyed {((ChassisLocations)component.Location).ToString()}\n");
                        continue; 
                    };
                    if (!gotdamagevalues)
                    {//have atleast 1 damage activateable component get the damage values
                            Mech mech = defender;
                            Log.LogWrite($"Damage >>> D: {damageAmount:F3} DS: {directStructureDamage:F3} H: {0}\n");
                            Log.LogWrite($"{new string('-', 46)}\n");
                            Log.LogWrite($"{"Location",-20} | {"Armor Damage",12} | {"Structure Damage",12}\n");
                            Log.LogWrite($"{new string('-', 46)}\n");
                            Head_s = MaxStructureForLocation(mech, (int)ChassisLocations.Head) - defender.HeadStructure;
                            Head_a = MaxArmorForLocation(mech, (int)ChassisLocations.Head) - defender.HeadArmor;
                            Log.LogWrite($"{ChassisLocations.Head.ToString(),-20} | {Head_a,12:F3} | {Head_s,12:F3}\n");
                            CenterTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.CenterTorso) - defender.CenterTorsoStructure;
                            CenterTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.CenterTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.CenterTorsoRear) - defender.CenterTorsoFrontArmor - defender.CenterTorsoRearArmor;
                            Log.LogWrite($"{ChassisLocations.CenterTorso.ToString(),-20} |  {CenterTorso_a,12:F3} | {CenterTorso_s,12:F3}\n");
                            LeftTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftTorso) - defender.LeftTorsoStructure;
                            LeftTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.LeftTorsoRear) - defender.LeftTorsoFrontArmor - defender.LeftTorsoRearArmor;
                            Log.LogWrite($"{ChassisLocations.LeftTorso.ToString(),-20} |  {LeftTorso_a,12:F3} | {LeftTorso_s,12:F3}\n");
                            RightTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightTorso) - defender.RightTorsoStructure;
                            RightTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.RightTorsoRear) - defender.RightTorsoFrontArmor - defender.RightTorsoRearArmor;
                            Log.LogWrite($"{ChassisLocations.RightTorso.ToString(),-20} |  {RightTorso_a,12:F3} | {RightTorso_s,12:F3}\n");
                            LeftLeg_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftLeg) - defender.LeftLegStructure;
                            LeftLeg_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftLeg) - defender.LeftLegArmor;
                            Log.LogWrite($"{ChassisLocations.LeftLeg.ToString(),-20} |  {LeftLeg_a,12:F3} | {LeftLeg_s,12:F3}\n");
                            RightLeg_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightLeg) - defender.RightLegStructure;
                            RightLeg_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightLeg) - defender.RightLegArmor;
                            Log.LogWrite($"{ChassisLocations.RightLeg.ToString(),-20} |  {RightLeg_a,12:F3} | {RightLeg_s,12:F3}\n");
                            LeftArm_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftArm) - defender.LeftArmStructure;
                            LeftArm_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftArm) - defender.LeftArmArmor;
                            Log.LogWrite($"{ChassisLocations.LeftArm.ToString(),-20} |  {LeftArm_a,12:F3} | {LeftArm_s,12:F3}\n");
                            RightArm_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightArm) - defender.RightArmStructure;
                            RightArm_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightArm) - defender.RightArmArmor;
                            Log.LogWrite($"{ChassisLocations.RightArm.ToString(),-20} |  {RightArm_a,12:F3} | {RightArm_s,12:F3}\n");

                            Log.LogWrite($"{ChassisLocations.Torso.ToString(),-20} |  {CenterTorso_a + LeftTorso_a + RightTorso_a,12:F3} | {CenterTorso_s + LeftTorso_s + RightTorso_s,12:F3}\n");
                            Log.LogWrite($"{ChassisLocations.Legs.ToString(),-20} |  {LeftLeg_a + RightLeg_a,12:F3} | { LeftLeg_s + RightLeg_s,12:F3}\n");
                            Log.LogWrite($"{ChassisLocations.Arms.ToString(),-20} |  {LeftArm_a + RightArm_a,12:F3} | { LeftArm_s + RightArm_s,12:F3}\n");
                            Log.LogWrite($"{ChassisLocations.All.ToString(),-20} |  {CenterTorso_a + LeftTorso_a + RightTorso_a + LeftLeg_a + RightLeg_a + LeftArm_a + RightArm_a,12:F3} | {CenterTorso_s + LeftTorso_s + RightTorso_s + LeftLeg_s + RightLeg_s + LeftArm_s + RightArm_s,12:F3}\n");
                            gotdamagevalues = true;
                    }
                    // we stop trying to activate the component if any of these return true i.e activated;
                    //ignore the damage from this hit and use the current damage levels.
                    //Not handling ChassisLocation MainBody as i dont know what locations it covers.
                    if (
                      ActivatableComponent.ActivateOnDamage(component, Head_a, Head_s, ChassisLocations.Head) ||
                      ActivatableComponent.ActivateOnDamage(component, CenterTorso_a, CenterTorso_s, ChassisLocations.CenterTorso) ||
                      ActivatableComponent.ActivateOnDamage(component, LeftTorso_a, LeftTorso_s, ChassisLocations.LeftTorso) ||
                      ActivatableComponent.ActivateOnDamage(component, RightTorso_a, RightTorso_s, ChassisLocations.RightTorso) ||
                      ActivatableComponent.ActivateOnDamage(component, LeftLeg_a, LeftLeg_s, ChassisLocations.LeftLeg) ||
                      ActivatableComponent.ActivateOnDamage(component, RightLeg_a, RightLeg_s, ChassisLocations.RightLeg) ||
                      ActivatableComponent.ActivateOnDamage(component, LeftArm_a, LeftArm_s, ChassisLocations.LeftArm) ||
                      ActivatableComponent.ActivateOnDamage(component, RightArm_a, RightArm_s, ChassisLocations.RightArm) ||
                      ActivatableComponent.ActivateOnDamage(component, CenterTorso_a + LeftTorso_a + RightTorso_a, CenterTorso_s + LeftTorso_s + RightTorso_s, ChassisLocations.Torso) ||
                      ActivatableComponent.ActivateOnDamage(component, LeftLeg_a + RightLeg_a, LeftLeg_s + RightLeg_s, ChassisLocations.Legs) ||
                      ActivatableComponent.ActivateOnDamage(component, LeftArm_a + RightArm_a, LeftArm_s + RightArm_s, ChassisLocations.Arms) ||
                      ActivatableComponent.ActivateOnDamage(component, CenterTorso_a + LeftTorso_a + RightTorso_a + LeftLeg_a + RightLeg_a + LeftArm_a + RightArm_a, CenterTorso_s + LeftTorso_s + RightTorso_s + LeftLeg_s + RightLeg_s + LeftArm_s + RightArm_s, ChassisLocations.All)
                      )
                    {
                        continue;
                    }


                }

            }
        }
    }