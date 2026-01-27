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
                Plugin.weaponSwitcherControllerName0.Value,
                Plugin.weaponSwitcherButtonIndex0.Value,
                0.0001f, 
                onLongPress: HandleClick0
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName1.Value,
                Plugin.weaponSwitcherButtonIndex1.Value,
                0.0001f, 
                onLongPress: HandleClick1
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName2.Value,
                Plugin.weaponSwitcherButtonIndex2.Value,
                0.0001f, 
                onLongPress: HandleClick2
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName3.Value,
                Plugin.weaponSwitcherButtonIndex3.Value,
                0.0001f, 
                onLongPress: HandleClick3
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName4.Value,
                Plugin.weaponSwitcherButtonIndex4.Value,
                0.0001f, 
                onLongPress: HandleClick4
                );
            InputCatcher.RegisterNewInput(
                Plugin.weaponSwitcherControllerName5.Value,
                Plugin.weaponSwitcherButtonIndex5.Value,
                0.0001f, 
                onLongPress: HandleClick5
                );
            initialized = true;
            Plugin.Log("[WS] Weapon Switcher plugin succesfully started !");
        }
    }

    private static void HandleClick0() {
        GameBindings.Player.Aircraft.Weapons.SetActiveStation(0);
    }

    private static void HandleClick1() {
        GameBindings.Player.Aircraft.Weapons.SetActiveStation(1);
    }

    private static void HandleClick2() {
        GameBindings.Player.Aircraft.Weapons.SetActiveStation(2);
    }

    private static void HandleClick3() {
        GameBindings.Player.Aircraft.Weapons.SetActiveStation(3);
    }

    private static void HandleClick4() {
        GameBindings.Player.Aircraft.Weapons.SetActiveStation(4);
    }

    private static void HandleClick5() {
        GameBindings.Player.Aircraft.Weapons.SetActiveStation(5);
    }
}
