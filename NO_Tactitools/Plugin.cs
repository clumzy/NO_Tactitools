using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools {
    [BepInPlugin("NO_Tactitools", "NO Tactical Tools!", "0.4.20")]
    public class Plugin : BaseUnityPlugin {
        public static ConfigEntry<string> targetRecallControllerName;
        public static ConfigEntry<int> targetRecallButtonNumber;
        public static ConfigEntry<bool> onScreenVectorEnabled;
        public static ConfigEntry<string> countermeasureControlsFlareControllerName;
        public static ConfigEntry<int> countermeasureControlsFlareButtonNumber;
        public static ConfigEntry<string> countermeasureControlsJammerControllerName;
        public static ConfigEntry<int> countermeasureControlsJammerButtonNumber;
        public static ConfigEntry<string> weaponSwitcherControllerName;
        public static ConfigEntry<int> weaponSwitcherButton0;
        public static ConfigEntry<int> weaponSwitcherButton1;
        public static ConfigEntry<int> weaponSwitcherButton2;
        public static ConfigEntry<int> weaponSwitcherButton3;
        public static ConfigEntry<int> weaponSwitcherButton4;
        public static InputCatcherPlugin inputCatcherPlugin = new();
        internal static new ManualLogSource Logger;

        private void Awake() {

            targetRecallControllerName = Config.Bind("Target Recall",      // The section under which the option is shown
                "Target Recall Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral"); // Description of the option to show in the config file
            targetRecallButtonNumber = Config.Bind("Target Recall",      // The section under which the option is shown
                "Target Recall Button Number",  // The key of the configuration option in the configuration file
                37, // The default value
                "Number of the button");
            onScreenVectorEnabled = Config.Bind("Interception Vector",      // The section under which the option is shown
                "On-Screen Vector Enabled",  // The key of the configuration option in the configuration file
                true, // The default value
                "Enable or disable the on-screen vector display");
            countermeasureControlsFlareControllerName = Config.Bind("Countermeasures",      // The section under which the option is shown
                "Countermeasure Controls Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral"); // Description of the option to show in the config file
            countermeasureControlsFlareButtonNumber = Config.Bind("Countermeasures",      // The section under which the option is shown
                "Countermeasure Controls - Flares - Button Number",  // The key of the configuration option in the configuration file
                39, // The default value
                "Number of the button");
            countermeasureControlsJammerControllerName = Config.Bind("Countermeasures",      // The section under which the option is shown
                "Countermeasure Controls Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral"); // Description of the option to show in the config file
            countermeasureControlsJammerButtonNumber = Config.Bind("Countermeasures",      // The section under which the option is shown
                "Countermeasure Controls - Jammer - Button Number",  // The key of the configuration option in the configuration file
                40, // The default value
                "Number of the button");
            weaponSwitcherControllerName = Config.Bind("Weapon Switcher",      // The section under which the option is shown
                "Weapon Switcher Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral for weapon switching"); // Description of the option to show in the config file
            weaponSwitcherButton0 = Config.Bind("Weapon Switcher",      // The section under which the option is shown
                "Weapon Switcher Button 0",  // The key of the configuration option in the configuration file
                41, // The default value
                "Button number for weapon slot 0");
            weaponSwitcherButton1 = Config.Bind("Weapon Switcher",      // The section under which the option is shown
                "Weapon Switcher Button 1",  // The key of the configuration option in the configuration file
                42, // The default value
                "Button number for weapon slot 1");
            weaponSwitcherButton2 = Config.Bind("Weapon Switcher",      // The section under which the option is shown
                "Weapon Switcher Button 2",  // The key of the configuration option in the configuration file
                43, // The default value
                "Button number for weapon slot 2");
            weaponSwitcherButton3 = Config.Bind("Weapon Switcher",      // The section under which the option is shown
                "Weapon Switcher Button 3",  // The key of the configuration option in the configuration file
                44, // The default value
                "Button number for weapon slot 3");
            weaponSwitcherButton4 = Config.Bind("Weapon Switcher", 
                "Weapon Switcher Button 4", 
                45, 
                "Button number for weapon slot 4");
            // Plugin startup logic
            var harmony = new Harmony("george.no_tactitools");
            Logger = base.Logger;
            // Patch UI Utils
            harmony.PatchAll(typeof(CombatHUDRegisterPatch));
            harmony.PatchAll(typeof(MainHUDRegisterPatch));
            harmony.PatchAll(typeof(CameraStateManagerRegisterPatch));
            harmony.PatchAll(typeof(TargetScreenUIPatch));
            harmony.PatchAll(typeof(TargetScreenUIOnDestroyPatch));
            // Patch Input Catcher
            harmony.PatchAll(typeof(InputInterceptionPatch));
            harmony.PatchAll(typeof(RegisterControllerPatch));
            Logger.LogInfo($"Plugin NO_Tactitools is loaded!");
        }
    }

}
