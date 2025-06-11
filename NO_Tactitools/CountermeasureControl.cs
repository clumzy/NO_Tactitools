using System;
using HarmonyLib;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[CC] Countermeasure Controls plugin starting !");
            InputCatcher.RegisterControllerButton(
                Plugin.countermeasureControlsFlareControllerName.Value,
                new ControllerButton(
                (int)Plugin.countermeasureControlsFlareButtonNumber.Value,
                1000f,
                onShortPress: HandleOnHoldFlare
                ));
            InputCatcher.RegisterControllerButton(
                Plugin.countermeasureControlsJammerControllerName.Value,
                new ControllerButton(
                (int)Plugin.countermeasureControlsJammerButtonNumber.Value,
                1000f,
                onShortPress: HandleOnHoldJammer
                ));
            initialized = true;
            Plugin.Log("[CC] Countermeasure Controls plugin succesfully started !");
        }
    }
    
    private static void HandleOnHoldFlare() {
        try {
        SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
        } 
        catch (NullReferenceException) {}
    }

    private static void HandleOnHoldJammer() {
        try {
            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 1;
        } 
        catch (IndexOutOfRangeException) {} // This is to prevent the logger from going insane² if the player has no jammers
        catch (NullReferenceException) {}
    }
}