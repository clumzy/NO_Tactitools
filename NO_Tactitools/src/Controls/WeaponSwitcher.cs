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
                onShortPress: HandleClick0,
                onLongPress: HandleToggleAutoControl // Long press to switch to the first weapon station
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton1.Value,
                0.2f,
                onShortPress: HandleClick1
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton2.Value,
                0.2f,
                onShortPress: HandleClick2
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton3.Value,
                0.2f,
                onShortPress: HandleClick3
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton4.Value,
                0.2f,
                onShortPress: HandleClick4
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName.Value,
                Plugin.weaponSwitcherButton5.Value,
                0.2f,
                onShortPress: HandleClick5
                );
            initialized = true;
            Plugin.Log("[WS] Weapon Switcher plugin succesfully started !");
        }
    }

    private static void HandleClick0() {
        Bindings.Player.Weapons.SetActiveStation(0);
    }

    private static void HandleToggleAutoControl() {
        Bindings.Player.Aircraft.ToggleAutoControl();
    }

    private static void HandleClick1() {
        Bindings.Player.Weapons.SetActiveStation(1);
    }

    private static void HandleClick2() {
        Bindings.Player.Weapons.SetActiveStation(2);
    }

    private static void HandleClick3() {
        Bindings.Player.Weapons.SetActiveStation(3);
    }

    private static void HandleClick4() {
        Bindings.Player.Weapons.SetActiveStation(4);
    }

    private static void HandleClick5() {
        Bindings.Player.Weapons.SetActiveStation(5);
    }
}