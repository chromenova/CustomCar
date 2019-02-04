﻿using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CustomCar
{
    //[HarmonyPatch(typeof(Profile), "GetColorsForIndex")]
    //internal class ProfileGetColorsForIndex
    //{
    //    static bool Prefix(Profile __instance, ref int index)
    //    {
    //        try
    //        {
    //            var carColorsList = (CarColors[])__instance.GetType().GetField("carColorsList_", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

    //            if (index >= carColorsList.Length)
    //                index = 0;
    //        }
    //        catch (Exception e)
    //        {
    //            Console.Out.WriteLine(e.ToString());
    //        }

    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(Profile), "Awake")]
    internal class ProfileAwake
    {
        static void Postfix(Profile __instance)
        {
            var carColors = new CarColors[G.Sys.ProfileManager_.carInfos_.Length];
            for (int i = 0; i < carColors.Length; i++)
                carColors[i] = G.Sys.ProfileManager_.carInfos_[i].colors_;

            var field = __instance.GetType().GetField("carColorsList_", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(__instance, carColors);
        }
    }

    [HarmonyPatch(typeof(Profile), "SetColorsForAllCars")]
    internal class ProfileSetColorsForAllCars
    {
        static bool Prefix(Profile __instance, CarColors cc)
        {
            var carColors = new CarColors[G.Sys.ProfileManager_.carInfos_.Length];
            for (int i = 0; i < carColors.Length; i++)
                carColors[i] = cc;

            var field = __instance.GetType().GetField("carColorsList_", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(__instance, carColors);

            field = __instance.GetType().GetField("dataModified_", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(__instance, true);

            return false;
        }
    }

    //trying to fix colors loading - it look like that does nothing
    //[HarmonyPatch(typeof(Profile), "Visit")]
    //[HarmonyPatch("CheckForErrors")]
    //internal class ProfileVisit
    //{
    //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        var codes = new List<CodeInstruction>(instructions);

    //        for(int i = 0; i < codes.Count; i++)
    //        {
    //            if(codes[i].opcode == OpCodes.Blt) //on the for
    //            {
    //                int index = i - 1;
    //                codes.RemoveAt(index);
    //                codes.Insert(index, new CodeInstruction(OpCodes.Ldarg_0));
    //                codes.Insert(index + 1, new CodeInstruction(OpCodes.Ldfld, typeof(Profile).GetField("carColorsList_", BindingFlags.Instance | BindingFlags.NonPublic)));
    //                codes.Insert(index + 2, new CodeInstruction(OpCodes.Ldlen));
    //                codes.Insert(index + 3, new CodeInstruction(OpCodes.Conv_I4));
    //                break;
    //            }
    //        }

    //        return codes.AsEnumerable();
    //    }
    //}

    //change additive to blend animation blendMode
    [HarmonyPatch(typeof(GadgetWithAnimation), "SetAnimationStateValues")]
    internal class GadgetWithAnimationSetAnimationStateValues
    {
        static bool Prefix(GadgetWithAnimation __instance)
        {
            var comp = __instance.GetComponentInChildren<Animation>();
            if(comp)
            {
                if (!ChangeBlendModeToBlend(comp.transform, __instance.animationName_))
                    return true;

                var state = comp[__instance.animationName_];
                if(state)
                {
                    state.layer = 3;
                    state.blendMode = AnimationBlendMode.Blend;
                    state.wrapMode = WrapMode.ClampForever;
                    state.enabled = true;
                    state.weight = 1f;
                    state.speed = 0f;
                }
            }

            return false;
        }

        static bool ChangeBlendModeToBlend(Transform obj, string animationName)
        {
            for(int i = 0; i < obj.childCount; i++)
            {
                var n = obj.GetChild(i).gameObject.name.ToLower();
                if (!n.StartsWith("#"))
                    continue;

                n = n.Remove(0, 1);
                var parts = n.Split(';');

                if(parts.Length == 1)
                {
                    if (parts[0] == "additive")
                        return false;
                    if (parts[0] == "blend")
                        return true;
                }
                if(parts[1] == animationName.ToLower())
                {
                    if (parts[0] == "additive")
                        return false;
                    if (parts[0] == "blend")
                        return true;
                }
            }
            return false;
        }
    }
}

//Inject code to trigger custom cooldown animation
[HarmonyPatch(typeof(CarLogic), "CoolDown")]
internal class CoolDownMod
{
    static void Postfix(CarLogic __instance, float amount, bool playSound = true)
    {
        Animation carAnimation = (Animation) Harmony.AccessTools.Field(typeof(CarLogic), "animation_").GetValue(__instance);
        if(carAnimation["Cooldown"])
        {
            carAnimation.Play("Cooldown");
        }
    }
}