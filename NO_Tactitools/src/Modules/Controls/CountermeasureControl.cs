using HarmonyLib;
using NO_Tactitools.Core;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Inputs;

namespace NO_Tactitools.Modules.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[CC] Countermeasure Controls plugin starting !");
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsFlare,
                0.0001f, 
                onLongPress: HandleOnHoldFlare
                );
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsJammer,
                0.0001f,
                onLongPress: HandleOnHoldJammer
                );
            initialized = true;
            Plugin.Log("[CC] Countermeasure Controls plugin succesfully started !");
        }
    }

    private static void HandleOnHoldFlare() {
        GameBindings.Player.Aircraft.Countermeasures.SetIRFlare();
    }

    private static void HandleOnHoldJammer() {
        if (GameBindings.Player.Aircraft.Countermeasures.HasJammer())
            GameBindings.Player.Aircraft.Countermeasures.SetJammer();
    }
}

