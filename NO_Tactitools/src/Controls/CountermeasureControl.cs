using System;
using HarmonyLib;
using Rewired;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;
    private static bool isFiring = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[CC] Countermeasure Controls plugin starting !");
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsFlare,
                0.01f,
                onRelease: HandleSelectFlare,
                onLongPress: HandleLongPressFlare,
                onAnyRelease: HandleAnyRelease
            );
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsJammer,
                0.01f,
                onRelease: HandleSelectJammer,
                onLongPress: HandleLongPressJammer,
                onAnyRelease: HandleAnyRelease
            );
            Plugin.harmony.PatchAll(typeof(SimulateCountermeasurePress));
            initialized = true;
            Plugin.Log("[CC] Countermeasure Controls plugin succesfully started !");
        }
    }

    private static void HandleSelectFlare() {
        GameBindings.Player.Aircraft.Countermeasures.SetIRFlare();
    }

    private static void HandleSelectJammer() {
        if (GameBindings.Player.Aircraft.Countermeasures.HasJammer())
            GameBindings.Player.Aircraft.Countermeasures.SetJammer();
    }

    private static void HandleLongPressFlare() {
        GameBindings.Player.Aircraft.Countermeasures.SetIRFlare();
        if (Plugin.countermeasureControlsFireOnHold.Value)
            isFiring = true;
    }

    private static void HandleLongPressJammer() {
        if (GameBindings.Player.Aircraft.Countermeasures.HasJammer()) {
            GameBindings.Player.Aircraft.Countermeasures.SetJammer();
            if (Plugin.countermeasureControlsFireOnHold.Value)
                isFiring = true;
        }
    }

    private static void HandleAnyRelease() {
        isFiring = false;
    }

    // Makes the game's own countermeasure input logic believe its native button
    // is held, so the full vanilla firing/jamming path runs unmodified.
    [HarmonyPatch(typeof(Rewired.Player), "GetButton", typeof(string))]
    static class SimulateCountermeasurePress {
        static void Postfix(string actionName, ref bool __result) {
            if (isFiring && actionName == "Countermeasures")
                __result = true;
        }
    }
}
