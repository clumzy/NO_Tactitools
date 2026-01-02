using System;
using HarmonyLib;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponSwitcherPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[WS] Weapon Switcher plugin starting !");
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton0.Value,
                0.5f,
                onRelease: HandleClick0,
                onLongPress: HandleToggleAutoControl // Long press to switch to the first weapon station
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton1.Value,
                0.2f,
                onRelease: HandleClick1
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton2.Value,
                0.2f,
                onRelease: HandleClick2
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton3.Value,
                0.2f,
                onRelease: HandleClick3
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton4.Value,
                0.2f,
                onRelease: HandleClick4
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton5.Value,
                0.2f,
                onRelease: HandleClick5
                );
            initialized = true;
            Plugin.Log("[WS] Weapon Switcher plugin succesfully started !");
        }
    }

    private static void HandleClick0() {
        Bindings.Player.Aircraft.Weapons.SetActiveStation(0);
    }

    private static void HandleToggleAutoControl() {
        Bindings.Player.Aircraft.ToggleAutoControl();
    }

    private static void HandleClick1() {
        Bindings.Player.Aircraft.Weapons.SetActiveStation(1);
    }

    private static void HandleClick2() {
        Bindings.Player.Aircraft.Weapons.SetActiveStation(2);
    }

    private static void HandleClick3() {
        Bindings.Player.Aircraft.Weapons.SetActiveStation(3);
    }

    private static void HandleClick4() {
        Bindings.Player.Aircraft.Weapons.SetActiveStation(4);
    }

    private static void HandleClick5() {
        Bindings.Player.Aircraft.Weapons.SetActiveStation(5);
    }
}