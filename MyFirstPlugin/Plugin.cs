using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MyFirstPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<int> configMultiplier;

    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        configMultiplier = Config.Bind("General",      // The section under which the option is shown
            "TimeMultiplier",  // The key of the configuration option in the configuration file
            8, // The default value
            "Time multiplier"); // Description of the option to show in the config file
        // Plugin startup logic
        var harmony = new Harmony("george.no_time");
        harmony.PatchAll();
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! 420 blaze it");
    }

}

[HarmonyPatch(typeof(LevelInfo), "Update")]
class TimePatch
{
    static void Postfix(LevelInfo __instance)
    {   
        float mult = Plugin.configMultiplier.Value;
        __instance.NetworktimeOfDay = __instance.timeOfDay + Time.deltaTime * 0.00027777778f * (mult-1f);
        if (__instance.timeOfDay > 24f)
        {
            __instance.NetworktimeOfDay = __instance.timeOfDay - 24f;
        }
        return;
    }
}
