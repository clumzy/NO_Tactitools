using System;
using HarmonyLib;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;
    private static bool isFiringFlare = false;
    private static bool isFiringJammer = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[CC] Countermeasure Controls plugin starting !");
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsFlare,
                0.3f,
                onRelease: HandleSelectFlare,
                onLongPress: HandleLongPressFlare,
                onAnyRelease: () => { isFiringFlare = false; }
            );
            InputCatcher.RegisterNewInput(
                Plugin.countermeasureControlsJammer,
                0.3f,
                onRelease: HandleSelectJammer,
                onLongPress: HandleLongPressJammer,
                onAnyRelease: () => { isFiringJammer = false; }
            );
            Plugin.harmony.PatchAll(typeof(CountermeasureFirePatch));
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
            isFiringFlare = true;
    }

    private static void HandleLongPressJammer() {
        if (GameBindings.Player.Aircraft.Countermeasures.HasJammer()) {
            GameBindings.Player.Aircraft.Countermeasures.SetJammer();
            if (Plugin.countermeasureControlsFireOnHold.Value)
                isFiringJammer = true;
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
    static class CountermeasureFirePatch {
        static void Postfix() {
            if (isFiringFlare || isFiringJammer)
                GameBindings.Player.Aircraft.Countermeasures.FireCountermeasure();
        }
    }
}
