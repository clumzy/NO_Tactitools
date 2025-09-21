using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UnitIconRecolorPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[UIR] Unit Icon Recolor plugin starting !");
            Plugin.harmony.PatchAll(typeof(UnitIconRecolorPatch));
            initialized = true;
            Plugin.Log("[UIR] Unit Icon Recolor plugin successfully started !");
        }
    }
}

[HarmonyPatch(typeof(UnitMapIcon), "UpdateIcon")]
class UnitIconRecolorPatch {
    static List<string> targetUnitNames = [
        "LCV25 AA",
        "AFV6 AA",
        "Linebreaker SAM",
        "AFV8 Mobile Air Defense",
        "AeroSentry SPAAG",
        "FGA-57 Anvil",
        "T9K41 Boltstrike",
        "StratoLance R9 Launcher"];
    static void Postfix(UnitMapIcon __instance) {
        if (__instance.unit.NetworkHQ != SceneSingleton<DynamicMap>.i.HQ &&
            targetUnitNames.Contains(__instance.unit.unitName))
                __instance.iconImage.color = new Color(0.8f, 0.2f, 1f); // yellow color for enemy AA units
    }
}