using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools {
    [BepInPlugin("NO_Tactitools", "Nuclear Option Tactical Tools!", "1.0.0")]
    public class Plugin : BaseUnityPlugin {
        public static ConfigEntry<string> targetRecallControllerName;
        public static ConfigEntry<int> targetRecallButtonNumber;
        public static ConfigEntry<string> interceptionVectorControllerName;
        public static ConfigEntry<int> interceptionVectorButtonNumber;
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
        public static CombatHUD combatHUD;
        public static AudioClip selectAudio;
        public static FuelGauge fuelGauge;
        public static CameraStateManager cameraStateManager;
        public static InputCatcherPlugin inputCatcherPlugin = new InputCatcherPlugin();
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
            interceptionVectorControllerName = Config.Bind("Interception Vector",      // The section under which the option is shown
                "Interception Vector Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral"); // Description of the option to show in the config file
            interceptionVectorButtonNumber = Config.Bind("Interception Vector",      // The section under which the option is shown
                "Interception Vector Button Number",  // The key of the configuration option in the configuration file
                38, // The default value
                "Number of the button");
            onScreenVectorEnabled = Config.Bind("Interception Vector",      // The section under which the option is shown
                "On-Screen Vector Enabled",  // The key of the configuration option in the configuration file
                true, // The default value
                "Enable or disable the on-screen vector display");
            countermeasureControlsFlareControllerName = Config.Bind("Countermeasure - Flare",      // The section under which the option is shown
                "Countermeasure Controls Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral"); // Description of the option to show in the config file
            countermeasureControlsFlareButtonNumber = Config.Bind("Countermeasure - Flare",      // The section under which the option is shown
                "Countermeasure Controls - Flares - Button Number",  // The key of the configuration option in the configuration file
                39, // The default value
                "Number of the button");
            countermeasureControlsJammerControllerName = Config.Bind("Countermeasure - Jammer",      // The section under which the option is shown
                "Countermeasure Controls Controller Name",  // The key of the configuration option in the configuration file
                "", // The default value
                "Name of the peripheral"); // Description of the option to show in the config file
            countermeasureControlsJammerButtonNumber = Config.Bind("Countermeasure - Jammer",      // The section under which the option is shown
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
            harmony.PatchAll();
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "Awake")]
    class CombatHUDRegisterPatch {
        static void Postfix(CombatHUD __instance) {
            Plugin.Logger.LogInfo("[TR] CombatHUD Registered !");
            Plugin.combatHUD = __instance;
            Plugin.selectAudio = (AudioClip)Traverse.Create(Plugin.combatHUD).Field("selectSound").GetValue();
        }
    }

    [HarmonyPatch(typeof(CameraStateManager), "Start")]
    class CameraStateManagerRegisterPatch {
        static void Postfix(CameraStateManager __instance) {
            Plugin.Logger.LogInfo("[TR] CameraStateManager Registered !");
            Plugin.cameraStateManager = __instance;
        }
    }
}
