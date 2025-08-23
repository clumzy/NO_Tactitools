using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UnitIconRecolorPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[UIR] Unit Icon Recolor plugin starting !");
            Plugin.harmony.PatchAll(typeof(UnitIconRecolorPatch));
            Reset();
            initialized = true;
            Plugin.Log("[UIR] Unit Icon Recolor plugin successfully started !");
        }
    }

    public static void Reset() {
    }
}

[HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
class UnitIconRecolorPatch {
    static List<string> targetUnitNames = new List<string> {
        "LCV25 AA",
        "AFV6 AA",
        "Linebreaker SAM",
        "AFV8 Mobile Air Defense",
        "AeroSentry SPAAG",
        "FGA-57 Anvil",
        "T9K41 Boltstrike",
        "StratoLance R9 Launcher"};
    static void Postfix(UnitMapIcon __instance) {
        if (__instance.unit.NetworkHQ != SceneSingleton<DynamicMap>.i.HQ &&
            targetUnitNames.Contains(__instance.unit.unitName))
                __instance.iconImage.color = new Color(1f, 0.87f, 0.13f); // yellow color for enemy AA units
    }
}