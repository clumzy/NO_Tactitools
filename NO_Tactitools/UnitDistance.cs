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

    public static readonly Dictionary<HUDUnitMarker, string> unitStates = [];

    static void Postfix(HUDUnitMarker __instance) {
        if (__instance.unit is not Aircraft || __instance.unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ) return; // Only apply to enemy aircraft units
        Transform markerTransform = (Transform)Traverse.Create(__instance).Field("_transform").GetValue();
        int distanceToPlayer = Mathf.RoundToInt(Vector3.Distance(__instance.unit.rb.transform.position, SceneSingleton<CombatHUD>.i.aircraft.rb.transform.position));
        int threshold = Plugin.unitDistanceThreshold.Value * 1000;
        int rotationThreshold = threshold + 250; // Convert threshold from kilometers to Unity units (1 unit = 1 meter)

        string currentState;
        float rotationValue;

        if (distanceToPlayer >= rotationThreshold) {
            currentState = "far";
            rotationValue = 0f;
        }
        else if (distanceToPlayer <= threshold) {
            currentState = "near";
            rotationValue = 180f;
        }
        else {
            currentState = "transition";
            float t = (distanceToPlayer - threshold) / 250f;
            rotationValue = Mathf.Lerp(180f, 0f, t);
        }

        unitStates.TryGetValue(__instance, out string previousState);

        if (currentState == "near" && previousState != "near" && Plugin.unitDistanceSoundEnabled.Value) {
            UIUtils.PlaySound("beep_alert.mp3");
        }

        if (currentState == previousState && currentState != "transition") {
            return;
        }

        unitStates[__instance] = currentState;
        markerTransform.rotation = Quaternion.Euler(0, 0, rotationValue);
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetUnitDistanceDictOnRespawnPatch {
    static void Postfix() {
        // Reset the unitStates when the aircraft is destroyed
        UnitDistanceTask.unitStates.Clear();
    }
}