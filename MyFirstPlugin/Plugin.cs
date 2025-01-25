using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using NuclearOption.MissionEditorScripts;
using System.Collections.Generic;

namespace MyFirstPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<string> configControllerName;
    public static ConfigEntry<int> configButtonNumber;
    public static Controller matchedController;
    public static CombatHUD combatHUD;
    
    public static List<Unit> units;
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
//

[HarmonyPatch(typeof(Rewired.Controller), "Connected")]
class RegisterPatch
{
    static void Postfix(Controller __instance)
    {
        string cleanedName = __instance.name.Trim();
        Plugin.Logger.LogInfo($"CONTROLLER CONNECTED: {cleanedName}");

        if (string.Equals(cleanedName, Plugin.configControllerName.Value))
        {
            Plugin.matchedController = __instance;
            Plugin.Logger.LogInfo($"CONTROLLER MATCH: {cleanedName}");
        }
    }
}

[HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
class ButtonPatch
{
    private static bool previousButtonState = false;
    private static float buttonPressTime = 0f;
    private const float LONG_PRESS_THRESHOLD = 0.2f; // 500ms

    static bool Prefix(Controller __instance)
    {
        if (__instance == Plugin.matchedController)
        {
            int buttonNumber = Plugin.configButtonNumber.Value;
            bool currentButtonState = __instance.Buttons[buttonNumber].value;

            if (!previousButtonState && currentButtonState)
            {
                // Button just pressed
                buttonPressTime = Time.time;
            }
            else if (previousButtonState && !currentButtonState)
            {
                // Button just released
                HandleButtonRelease();
            }

            previousButtonState = currentButtonState;
        }
        return true;
    }

    private static void HandleButtonRelease()
    {
        float pressDuration = Time.time - buttonPressTime;

        if (pressDuration >= LONG_PRESS_THRESHOLD)
        {
            Plugin.Logger.LogInfo("LONG PRESS !");
            HandleRemember();
        }
        else
        {
            Plugin.Logger.LogInfo("CLICK !");
            HandleRecall();
        }
    }

    private static void HandleRemember(){
        if (Plugin.combatHUD != null){
            Plugin.Logger.LogInfo($"ZOB");
            Plugin.units = [.. (List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()];
        }
    }

    private static void HandleRecall(){
        if (Plugin.combatHUD != null && Plugin.units != null){
            if (Plugin.units.Count > 0){
                Plugin.Logger.LogInfo(Plugin.units.Count);
                Plugin.combatHUD.DeselectAll(true);
                foreach (Unit t_unit in Plugin.units){
                    Plugin.combatHUD.SelectUnit(t_unit);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(CombatHUD), "Awake")]
class TargetRegisterPatch
{
    static void Postfix(CombatHUD __instance)
    {
        Plugin.Logger.LogInfo("COMBAT HUD REGISTERED !");
        Plugin.combatHUD = __instance;
    }
}