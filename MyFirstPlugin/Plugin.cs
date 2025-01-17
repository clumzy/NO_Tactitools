using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MyFirstPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private ConfigEntry<string> configGreeting;
    private ConfigEntry<bool> configDisplayGreeting;

    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        configGreeting = Config.Bind("General",      // The section under which the option is shown
            "GreetingText",  // The key of the configuration option in the configuration file
            "Hello, world!", // The default value
            "A greeting text to show when the game is launched"); // Description of the option to show in the config file

        configDisplayGreeting = Config.Bind("General.Toggles", 
            "DisplayGreeting",
            true,
            "Whether or not to show the greeting text");
        // Plugin startup logic
        var harmony = new Harmony("george.no_time");
        harmony.PatchAll();
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! 420 blaze it");
        if(configDisplayGreeting.Value)
            Logger.LogInfo(configGreeting.Value);
    }
}

[HarmonyPatch(typeof(LevelInfo), "Update")]
class Patch
{
    static bool Prefix(LevelInfo __instance)
    {   
        __instance.NetworktimeOfDay = __instance.timeOfDay + Time.deltaTime * 0.00027777778f*59;
        if (__instance.timeOfDay > 24f)
		{
			__instance.NetworktimeOfDay = __instance.timeOfDay - 24f;
		}
        float time_of = __instance.timeOfDay;
        string a = "Current time" + time_of.ToString();
        Plugin.Logger.LogInfo(a);
        return true;
    }
}