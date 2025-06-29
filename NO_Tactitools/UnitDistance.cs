using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UnitDistancePlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[UD] Unit Marker Distance Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(UnitDistanceTask));
            initialized = true;
            Plugin.Log("[UD] Unit Marker Distance Indicator plugin succesfully started !");
        }
    }
}

[HarmonyPatch(typeof(HUDUnitMarker), "UpdatePosition")]
class UnitDistanceTask {
    static void Postfix(HUDUnitMarker __instance) {
        if (__instance.unit is not Aircraft || __instance.unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ) return; // Only apply to enemy aircraft units
        Transform markerTransform = (Transform)Traverse.Create(__instance).Field("_transform").GetValue();
        int distanceToPlayer = Mathf.RoundToInt(Vector3.Distance(__instance.unit.rb.transform.position, SceneSingleton<CombatHUD>.i.aircraft.rb.transform.position));
        if (distanceToPlayer < 10000){
            // rotate the marker image 180 degrees
            markerTransform.rotation = Quaternion.Euler(0, 0, 180);
        }
        else{
            // rotate the marker image 0 degrees
            markerTransform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }
}