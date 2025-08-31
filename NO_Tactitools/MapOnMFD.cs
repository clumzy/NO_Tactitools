using UnityEngine;
using HarmonyLib;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class MapOnMFDPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[MFD] Map on MFD plugin starting !");
            Plugin.harmony.PatchAll(typeof(MapOnMFDPatch));
            initialized = true;
            Plugin.Log("[MFD] Map on MFD plugin successfully started !");
        }
    }
}

[HarmonyPatch(typeof(TacScreen), "Initialize")]
class MapOnMFDPatch {
    static void Postfix() {
        if (true) {
            Plugin.Log("[MFD] Enabling map on MFD");
        }
    }
}