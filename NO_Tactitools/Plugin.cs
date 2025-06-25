using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools {
    [BepInPlugin("NO_Tactitools", "NOTT", "0.4.20")]
    public class Plugin : BaseUnityPlugin {
        public static Harmony harmony;
        public static ConfigEntry<bool> targetRecallEnabled;
        public static ConfigEntry<string> targetRecallControllerName;
        public static ConfigEntry<int> targetRecallButtonNumber;
        public static ConfigEntry<bool> interceptionVectorEnabled;
        public static ConfigEntry<bool> onScreenVectorEnabled;
        public static ConfigEntry<bool> countermeasureControlsEnabled;
        public static ConfigEntry<string> countermeasureControlsFlareControllerName;
        public static ConfigEntry<int> countermeasureControlsFlareButtonNumber;
        public static ConfigEntry<string> countermeasureControlsJammerControllerName;
        public static ConfigEntry<int> countermeasureControlsJammerButtonNumber;
        public static ConfigEntry<bool> weaponSwitcherEnabled;
        public static ConfigEntry<string> weaponSwitcherControllerName;
        public static ConfigEntry<int> weaponSwitcherButton0;
        public static ConfigEntry<int> weaponSwitcherButton1;
        public static ConfigEntry<int> weaponSwitcherButton2;
        public static ConfigEntry<int> weaponSwitcherButton3;
        public static ConfigEntry<int> weaponSwitcherButton4;
        public static ConfigEntry<bool> weaponDisplayEnabled;
        public static ConfigEntry<string> weaponDisplayControllerName;
        public static ConfigEntry<int> weaponDisplayButtonNumber;
        public static ConfigEntry<bool> debugModeEnabled;
        internal static new ManualLogSource Logger;

        private void Awake() {
            // Target Recall settings
            targetRecallEnabled = Config.Bind("Target Recall", //Category
                "Target Recall Enabled", // Setting name
                true, // Default value
                "Enable or disable the Target Recall feature"); // Description of the setting
            targetRecallControllerName = Config.Bind("Target Recall",
                "Target Recall Controller Name",
                "",
                "Name of the peripheral");
            targetRecallButtonNumber = Config.Bind("Target Recall",
                "Target Recall Button Number",
                37,
                "Number of the button");
            // Interception Vector settings
            interceptionVectorEnabled = Config.Bind("Interception Vector",
                "Interception Vector Enabled",
                true,
                "Enable or disable the Interception Vector feature");
            // Countermeasure Controls settings
            countermeasureControlsEnabled = Config.Bind("Countermeasures",
                "Countermeasure Controls Enabled",
                true,
                "Enable or disable the Countermeasure Controls feature");
            countermeasureControlsFlareControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls Controller Name",
                "",
                "Name of the peripheral");
            countermeasureControlsFlareButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Button Number",
                39,
                "Number of the button");
            countermeasureControlsJammerControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls Controller Name",
                "",
                "Name of the peripheral");
            countermeasureControlsJammerButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Button Number",
                40,
                "Number of the button");
            // Weapon Switcher settings
            weaponSwitcherEnabled = Config.Bind("Weapon Switcher",
                "Weapon Switcher Enabled",
                true,
                "Enable or disable the Weapon Switcher feature");
            weaponSwitcherControllerName = Config.Bind("Weapon Switcher",
                "Weapon Switcher Controller Name",
                "",
                "Name of the peripheral for weapon switching");
            weaponSwitcherButton0 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 0",
                41,
                "Button number for weapon slot 0");
            weaponSwitcherButton1 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 1",
                42,
                "Button number for weapon slot 1");
            weaponSwitcherButton2 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 2",
                43,
                "Button number for weapon slot 2");
            weaponSwitcherButton3 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 3",
                44,
                "Button number for weapon slot 3");
            weaponSwitcherButton4 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 4",
                45,
                "Button number for weapon slot 4");
            // Weapon Display settings
            weaponDisplayEnabled = Config.Bind("Weapon Display",
                "Weapon Display Enabled",
                true,
                "Enable or disable the Weapon Display feature");
            weaponDisplayControllerName = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display Controller Name",
                "",
                "Name of the peripheral for weapon display");
            weaponDisplayButtonNumber = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display Button Number",
                46,
                "Button number for swapping between weapon display modes (modded or vanilla content)");
            // Debug Mode settings
            debugModeEnabled = Config.Bind("Debug Mode",
                "Debug Mode Enabled",
                true,
                "Enable or disable the debug mode for logging");
            // Plugin startup logic
            harmony = new Harmony("george.no_tactitools");
            Logger = base.Logger;
            // Patch Input Catcher - We do the patches manually here instead of in the "master class" like the other plugins because Rewired is a bit special as my mother would say
            harmony.PatchAll(typeof(InputInterceptionPatch));
            harmony.PatchAll(typeof(RegisterControllerPatch));
            // Patch UI Utils
            harmony.PatchAll(typeof(UIUtilsPlugin));
            // Patch Interception Vector
            if (interceptionVectorEnabled.Value) {
                Logger.LogInfo($"Interception Vector is enabled, patching...");
                harmony.PatchAll(typeof(InterceptionVectorPlugin));
            }
            // Patch Target Recall
            if (targetRecallEnabled.Value) {
                Logger.LogInfo($"Target Recall is enabled, patching...");
                harmony.PatchAll(typeof(TargetRecallPlugin));
            }
            // Patch Countermeasure Controls
            if (countermeasureControlsEnabled.Value) {
                Logger.LogInfo($"Countermeasure Controls is enabled, patching...");
                harmony.PatchAll(typeof(CountermeasureControlsPlugin));
            }
            // Patch Weapon Switcher
            if (weaponSwitcherEnabled.Value) {
                Logger.LogInfo($"Weapon Switcher is enabled, patching...");
                harmony.PatchAll(typeof(WeaponSwitcherPlugin));
            }
            // Patch Weapon Display
            if (weaponDisplayEnabled.Value) {
                Logger.LogInfo($"Weapon Display is enabled, patching...");
                harmony.PatchAll(typeof(WeaponDisplayPlugin));
            }

            Logger.LogInfo($"Plugin NO_Tactitools is loaded!");
        }

        public static void Log(string message) {
            if (debugModeEnabled.Value)
                Logger.LogInfo(message);
        }
    }
}
