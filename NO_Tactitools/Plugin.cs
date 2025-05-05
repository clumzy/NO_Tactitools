using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;
using NuclearOption.SavedMission.ObjectiveV2;

namespace NO_Tactitools
{
    [BepInPlugin("NO_Tactitools", "Nuclear Option Tactical Tools", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<string> configControllerName;
        public static ConfigEntry<int> configButtonNumber;
        public static Controller matchedController;
        public static CombatHUD combatHUD;
        public static AudioClip selectAudio;
        public static List<Unit> units;
        public static FuelGauge fuelGauge;
        public static CameraStateManager cameraStateManager;
        public static FlightHud flightHud;
        public static float pan;
        internal static new ManualLogSource Logger;
            
        private void Awake()
        {
            configControllerName = Config.Bind("General",      // The section under which the option is shown
                "configControllerName",  // The key of the configuration option in the configuration file
                "S-TECS MODERN THROTTLE MINI PLUS", // The default value
                "Name of the perihperal"); // Description of the option to show in the config file
            configButtonNumber = Config.Bind("General",      // The section under which the option is shown
                "configButtonNumber",  // The key of the configuration option in the configuration file
                37, // The default value
                "Number of the button");
            // Plugin startup logic
            var harmony = new Harmony("george.no_time");
            harmony.PatchAll();
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! 420 blaze it");
        }

    }

    [HarmonyPatch(typeof(CombatHUD), "Awake")]
    class CombatHUDRegisterPatch
    {
        static void Postfix(CombatHUD __instance)
        {
            Plugin.Logger.LogInfo("COMBAT HUD REGISTERED !");
            Plugin.combatHUD = __instance;
            Plugin.selectAudio = (AudioClip)Traverse.Create(Plugin.combatHUD).Field("selectSound").GetValue();
        }
    }

    [HarmonyPatch(typeof(FuelGauge), "Initialize")]
    class FuelGaugeRegisterPatch
    {
        static void Postfix(FuelGauge __instance)
        {
            Plugin.Logger.LogInfo("FUEL GAUGE REGISTERED !");
            Plugin.fuelGauge = __instance;
        }
    }
    [HarmonyPatch(typeof(CameraStateManager), "Start")]
    class CameraStateManagerRegisterPatch
    {
        static void Postfix(CameraStateManager __instance)
        {
            Plugin.Logger.LogInfo("CAMERA STATE MANAGER REGISTERED !");
            Plugin.cameraStateManager = __instance;
        }
    }
    [HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
    class FlightHudUpdatePatch
    {
        static void Postfix(FlightHud __instance)
        {
            Plugin.Logger.LogInfo("FLIGHT HUD REGISTERED !");
            Plugin.flightHud = __instance;
        }
    }
}
