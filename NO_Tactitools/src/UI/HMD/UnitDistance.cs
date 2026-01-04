using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI.HMD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UnitDistancePlugin {
    private static bool initialized = false;
    public static int unitDistanceThreshold;
    public static bool unitDistanceSoundEnabled;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[UD] Unit Marker Distance Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(UnitDistanceTask));
            Plugin.harmony.PatchAll(typeof(ResetUnitDistanceDictOnRespawnPatch));
            Reset();
            initialized = true;
            Plugin.Log("[UD] Unit Marker Distance Indicator plugin succesfully started !");
        }
    }

    public static void Reset() {
        unitDistanceThreshold = Plugin.unitDistanceThreshold.Value * 1000; // Convert threshold from kilometers to Unity units (1 unit = 1 meter)
        unitDistanceSoundEnabled = Plugin.unitDistanceSoundEnabled.Value;
        UnitDistanceTask.unitStates.Clear();
    }
}

[HarmonyPatch(typeof(HUDUnitMarker), "UpdatePosition")]
class UnitDistanceTask {

    public static readonly Dictionary<HUDUnitMarker, string> unitStates = [];

    static void Postfix(HUDUnitMarker __instance) {
        if (__instance.unit is not Aircraft || __instance.unit.NetworkHQ == SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ) return; // Only apply to enemy aircraft units
        Transform markerTransform = (Transform)Traverse.Create(__instance).Field("_transform").GetValue();
        if (SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ.IsTargetPositionAccurate(__instance.unit, 20f)) {
            int distanceToPlayer = Mathf.RoundToInt(Vector3.Distance(__instance.unit.rb.transform.position, SceneSingleton<CombatHUD>.i.aircraft.rb.transform.position));
            int threshold = UnitDistancePlugin.unitDistanceThreshold;
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

            if (currentState == "transition" && previousState == "far" && UnitDistancePlugin.unitDistanceSoundEnabled) {
                Bindings.UI.Sound.PlaySound("beep_alert.mp3");
            }

            if (currentState == previousState && currentState != "transition") {
                return;
            }

            unitStates[__instance] = currentState;
            markerTransform.rotation = Quaternion.Euler(0, 0, rotationValue);
        }
        else {
            // If the unit is not being tracked, reset the marker rotation
            if (unitStates.ContainsKey(__instance)) {
                unitStates.Remove(__instance);
            }
            markerTransform.rotation = Quaternion.Euler(0, 0, 0); // Reset to default rotation    
        }
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetUnitDistanceDictOnRespawnPatch {
    static void Postfix() {
        // Reset the unitStates when the aircraft is destroyed
        UnitDistancePlugin.Reset();
    }
}