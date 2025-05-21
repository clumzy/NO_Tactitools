using System;
using HarmonyLib;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponSwitcherPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Logger.LogInfo($"[WS] Weapon Switcher plugin starting !");
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                (int)Plugin.weaponSwitcherButton0.Value,
                0.2f,
                onShortPress: HandleClick0
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                (int)Plugin.weaponSwitcherButton1.Value,
                0.2f,
                onShortPress: HandleClick1
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                (int)Plugin.weaponSwitcherButton2.Value,
                0.2f,
                onShortPress: HandleClick2
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                (int)Plugin.weaponSwitcherButton3.Value,
                0.2f,
                onShortPress: HandleClick3
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                (int)Plugin.weaponSwitcherButton4.Value,
                0.2f,
                onShortPress: HandleClick4
                ));
            initialized = true;
            Plugin.Logger.LogInfo("[WS] Weapon Switcher plugin succesfully started !");
        }
    }
    private static void HandleClick0() {
        if (Plugin.combatHUD != null && Plugin.combatHUD.aircraft != null && Plugin.combatHUD.aircraft.weaponManager != null && Plugin.combatHUD.aircraft.weaponStations != null && Plugin.combatHUD.aircraft.weaponStations.Count > 0) {
            try {
                Plugin.combatHUD.aircraft.weaponManager.SetActiveStation((byte)0);
                Plugin.combatHUD.ShowWeaponStation(Plugin.combatHUD.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick1() {
        if (Plugin.combatHUD != null && Plugin.combatHUD.aircraft != null && Plugin.combatHUD.aircraft.weaponManager != null && Plugin.combatHUD.aircraft.weaponStations != null && Plugin.combatHUD.aircraft.weaponStations.Count > 1) {
            try {
                Plugin.combatHUD.aircraft.weaponManager.SetActiveStation((byte)1);
                Plugin.combatHUD.ShowWeaponStation(Plugin.combatHUD.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick2() {
        if (Plugin.combatHUD != null && Plugin.combatHUD.aircraft != null && Plugin.combatHUD.aircraft.weaponManager != null && Plugin.combatHUD.aircraft.weaponStations != null && Plugin.combatHUD.aircraft.weaponStations.Count > 2) {
            try {
                Plugin.combatHUD.aircraft.weaponManager.SetActiveStation((byte)2);
                Plugin.combatHUD.ShowWeaponStation(Plugin.combatHUD.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick3() {
        if (Plugin.combatHUD != null && Plugin.combatHUD.aircraft != null && Plugin.combatHUD.aircraft.weaponManager != null && Plugin.combatHUD.aircraft.weaponStations != null && Plugin.combatHUD.aircraft.weaponStations.Count > 3) {
            try {
                Plugin.combatHUD.aircraft.weaponManager.SetActiveStation((byte)3);
                Plugin.combatHUD.ShowWeaponStation(Plugin.combatHUD.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick4() {
        if (Plugin.combatHUD != null && Plugin.combatHUD.aircraft != null && Plugin.combatHUD.aircraft.weaponManager != null && Plugin.combatHUD.aircraft.weaponStations != null && Plugin.combatHUD.aircraft.weaponStations.Count > 4) {
            try {
                Plugin.combatHUD.aircraft.weaponManager.SetActiveStation((byte)4);
                Plugin.combatHUD.ShowWeaponStation(Plugin.combatHUD.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }
}