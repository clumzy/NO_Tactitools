using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UnitIconRecolorPlugin {
    private static bool initialized = false;
    public static Color unitIconRecolorEnemyColor = Color.red;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[UIR] Unit Icon Recolor plugin starting !");
            Plugin.harmony.PatchAll(typeof(UnitIconRecolorPatch));
            unitIconRecolorEnemyColor = Plugin.unitIconRecolorEnemyColor.Value;
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
        "StratoLance R9 Launcher",
        "Hexhound SAM",
        "CRAM Trailer",
        "Laser CIWS Trailer"];
    static void Postfix(UnitMapIcon __instance) {
        if (__instance.unit.NetworkHQ != SceneSingleton<DynamicMap>.i.HQ &&
            targetUnitNames.Contains(__instance.unit.unitName))
                __instance.iconImage.color = UnitIconRecolorPlugin.unitIconRecolorEnemyColor;
    }
}