using System;
using HarmonyLib;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[CC] Countermeasure Controls plugin starting !");
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsControllerName.Value,
                Plugin.countermeasureControlsFlareButtonNumber.Value,
                1000f,
                onShortPress: HandleOnHoldFlare
                );
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsControllerName.Value,
                Plugin.countermeasureControlsJammerButtonNumber.Value,
                1000f,
                onShortPress: HandleOnHoldJammer
                );
            initialized = true;
            Plugin.Log("[CC] Countermeasure Controls plugin succesfully started !");
        }
    }

    private static void HandleOnHoldFlare() {
        Bindings.Player.Aircraft.Countermeasures.SetIRFlare();
    }

    private static void HandleOnHoldJammer() {
        if (Bindings.Player.Aircraft.Countermeasures.HasJammer())
            Bindings.Player.Aircraft.Countermeasures.SetJammer();
    }
}