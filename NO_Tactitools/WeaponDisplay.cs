using HarmonyLib;
using UnityEngine;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponDisplayPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[WD] Weapon Display plugin starting !");
            initialized = true;
            Plugin.Log("[WD] Weapon Display plugin succesfully started !");
        }
    }
}