using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools
{
    [BepInPlugin("NO_Tactitools", "Nuclear Option Tactical Tools!", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<string> targetRecallControllerName;
        public static ConfigEntry<int> targetRecallButtonNumber;
        public static ConfigEntry<string> interceptionVectorControllerName;
        public static ConfigEntry<int> interceptionVectorButtonNumber;
        public static ConfigEntry<bool> onScreenVectorEnabled;
        public static ConfigEntry<string> countermeasureControlsFlareControllerName;
        public static ConfigEntry<int> countermeasureControlsFlareButtonNumber;
        public static ConfigEntry<string> countermeasureControlsJammerControllerName;
        public static ConfigEntry<int> countermeasureControlsJammerButtonNumber;
        public static CombatHUD combatHUD;
        public static AudioClip selectAudio;
        public static FuelGauge fuelGauge;
        public static CameraStateManager cameraStateManager;
        public static InputCatcherPlugin inputCatcherPlugin = new InputCatcherPlugin();
        internal static new ManualLogSource Logger;

        private void Awake()
        {
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
            // Plugin startup logic
            var harmony = new Harmony("george.no_tactitools");
            harmony.PatchAll();
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "Awake")]
    class CombatHUDRegisterPatch
    {
        static void Postfix(CombatHUD __instance)
        {
            Plugin.Logger.LogInfo("[TR] CombatHUD Registered !");
            Plugin.combatHUD = __instance;
            Plugin.selectAudio = (AudioClip)Traverse.Create(Plugin.combatHUD).Field("selectSound").GetValue();
        }
    }

    [HarmonyPatch(typeof(CameraStateManager), "Start")]
    class CameraStateManagerRegisterPatch
    {
        static void Postfix(CameraStateManager __instance)
        {
            Plugin.Logger.LogInfo("[TR] CameraStateManager Registered !");
            Plugin.cameraStateManager = __instance;
        }
    }
}
