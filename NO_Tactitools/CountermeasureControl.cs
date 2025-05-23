using System;
using HarmonyLib;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Logger.LogInfo($"[CC] Countermeasure Controls plugin starting !");
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.countermeasureControlsFlareControllerName.Value,
                new ControllerButton(
                (int)Plugin.countermeasureControlsFlareButtonNumber.Value,
                1000f,
                onHold: HandleOnHoldFlare
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.countermeasureControlsJammerControllerName.Value,
                new ControllerButton(
                (int)Plugin.countermeasureControlsJammerButtonNumber.Value,
                1000f,
                onHold: HandleOnHoldJammer
                ));
            initialized = true;
            Plugin.Logger.LogInfo("[CC] Countermeasure Controls plugin succesfully started !");
        }
    }
    private static void HandleOnHoldFlare() {
        Plugin.combatHUD.aircraft.countermeasureManager.activeIndex = 0;
        //Plugin.combatHUD.aircraft.countermeasureManager.DeployCountermeasure(Plugin.combatHUD.aircraft);
    }

    private static void HandleOnHoldJammer() {
        try {
            Plugin.combatHUD.aircraft.countermeasureManager.activeIndex = 1;
            //Plugin.combatHUD.aircraft.countermeasureManager.DeployCountermeasure(Plugin.combatHUD.aircraft);
        } catch (IndexOutOfRangeException) {} // This is to prevent the logger from going insane² if the player has no jammers
    }
}