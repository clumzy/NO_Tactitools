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
                Plugin.weaponSwitcherButton0.Value,
                0.2f,
                onShortPress: HandleClick0
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                Plugin.weaponSwitcherButton1.Value,
                0.2f,
                onShortPress: HandleClick1
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                Plugin.weaponSwitcherButton2.Value,
                0.2f,
                onShortPress: HandleClick2
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                Plugin.weaponSwitcherButton3.Value,
                0.2f,
                onShortPress: HandleClick3
                ));
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.weaponSwitcherControllerName.Value,
                new ControllerButton(
                Plugin.weaponSwitcherButton4.Value,
                0.2f,
                onShortPress: HandleClick4
                ));
            initialized = true;
            Plugin.Logger.LogInfo("[WS] Weapon Switcher plugin succesfully started !");
        }
    }
    private static void HandleClick0() {
        if (SceneSingleton<CombatHUD>.i != null && 
            SceneSingleton<CombatHUD>.i.aircraft != null && 
            SceneSingleton<CombatHUD>.i.aircraft.weaponManager != null && 
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations != null && 
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count > 0) {
            try {
                SceneSingleton<CombatHUD>.i.aircraft.weaponManager.SetActiveStation((byte)0);
                SceneSingleton<CombatHUD>.i.ShowWeaponStation(SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }
    private static void HandleClick1() {
        if (SceneSingleton<CombatHUD>.i != null &&
            SceneSingleton<CombatHUD>.i.aircraft != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponManager != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count > 1) {
            try {
                SceneSingleton<CombatHUD>.i.aircraft.weaponManager.SetActiveStation((byte)1);
                SceneSingleton<CombatHUD>.i.ShowWeaponStation(SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick2() {
        if (SceneSingleton<CombatHUD>.i != null &&
            SceneSingleton<CombatHUD>.i.aircraft != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponManager != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count > 2) {
            try {
                SceneSingleton<CombatHUD>.i.aircraft.weaponManager.SetActiveStation((byte)2);
                SceneSingleton<CombatHUD>.i.ShowWeaponStation(SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick3() {
        if (SceneSingleton<CombatHUD>.i != null &&
            SceneSingleton<CombatHUD>.i.aircraft != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponManager != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count > 3) {
            try {
                SceneSingleton<CombatHUD>.i.aircraft.weaponManager.SetActiveStation((byte)3);
                SceneSingleton<CombatHUD>.i.ShowWeaponStation(SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }

    private static void HandleClick4() {
        if (SceneSingleton<CombatHUD>.i != null &&
            SceneSingleton<CombatHUD>.i.aircraft != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponManager != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations != null &&
            SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count > 4) {
            try {
                SceneSingleton<CombatHUD>.i.aircraft.weaponManager.SetActiveStation((byte)4);
                SceneSingleton<CombatHUD>.i.ShowWeaponStation(SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation);
            }
            catch (Exception) { }
        }
    }
}