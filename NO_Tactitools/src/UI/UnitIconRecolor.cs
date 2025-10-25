using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;
using JetBrains.Annotations;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UnitIconRecolorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[UIR] Unit Icon Recolor plugin starting !");
            Plugin.harmony.PatchAll(typeof(UnitIconRecolorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(UnitIconRecolorComponent.OnPlatformUpdate));
            // we do it here instead of Init because we want the values to be read
            // before TacScreen.Initialize is called
            UnitIconRecolorComponent.InternalState.targetUnitNames = FileUtilities.GetListFromConfigFile("UnitIconRecolor_TargetUnits.txt");
            UnitIconRecolorComponent.InternalState.unitIconRecolorEnemyColor = Plugin.unitIconRecolorEnemyColor.Value;
            initialized = true;
            Plugin.Log("[UIR] Unit Icon Recolor plugin successfully started !");
        }
    }
}

public static class UnitIconRecolorComponent {
    static class LogicEngine {
        public static void Init() {
            InternalState.unitIconRecolorEnemyColor = Plugin.unitIconRecolorEnemyColor.Value;
        }

        public static void Update(UnitMapIcon __instance) {
            InternalState.needsUpdate = __instance.unit.NetworkHQ != SceneSingleton<DynamicMap>.i.HQ &&
            InternalState.targetUnitNames.Contains(__instance.unit.unitName) &&
            __instance.iconImage.color != InternalState.unitIconRecolorEnemyColor;
        }
    }

    public static class InternalState {
        static public Color unitIconRecolorEnemyColor;
        static public List<string> targetUnitNames;
        static public bool needsUpdate = false;
    }
    public static class DisplayEngine {
        public static void Init() {
        }

        public static void Update(UnitMapIcon __instance) {
            if (InternalState.needsUpdate)
                __instance.iconImage.color = InternalState.unitIconRecolorEnemyColor;
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart {
        static void Postfix() {
            LogicEngine.Init();
            DisplayEngine.Init();
        }
    }
    [HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
    public static class OnPlatformUpdate {
        static void Postfix(UnitMapIcon __instance) {
            LogicEngine.Update(__instance);
            DisplayEngine.Update(__instance);
        }
    }
}


