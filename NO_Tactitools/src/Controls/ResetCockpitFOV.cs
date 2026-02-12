using System;
using HarmonyLib;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class ResetCockpitFOV {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[RCFOV] Reset Cockpit FOV plugin starting !");
            
            InputCatcher.RegisterNewInput(
                Plugin.resetCockpitFOV,
                0.2f, 
                onRelease: HandleResetFOV
            );
            
            initialized = true;
            Plugin.Log("[RCFOV] Reset Cockpit FOV plugin successfully started !");
        }
    }

    private static void HandleResetFOV() {
        if (!Plugin.resetCockpitFOVEnabled.Value) return;
        
        Plugin.Log("[RCFOV] Resetting FOV...");
        // TODO: Implement FOV reset logic
    }
}
