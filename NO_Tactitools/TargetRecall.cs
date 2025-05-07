using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class TargetRecallPlugin
{
    private static bool initialized = false;
    public static List<Unit> units; 
    static void Postfix()
        {
            if (!initialized)
            {
                Plugin.Logger.LogInfo($"[TR] Target Recall plugin starting !");
                Plugin.inputCatcherPlugin.RegisterControllerButton(
                    Plugin.configControllerName1.Value, 
                    new ControllerButton(
                    (int)Plugin.configButtonNumber1.Value, 
                    0.2f,
                    HandleClick,
                    HandleLongPress
                    ));
                Plugin.Logger.LogInfo("[TR] Target Recall plugin succesfully started !");
            }
            initialized = true;
        }    
    private static void HandleLongPress()
    {
        Plugin.Logger.LogInfo($"[TR] HandleLongPress");
        if (Plugin.combatHUD != null)
        {
            units = [.. (List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()];
            SoundManager.PlayInterfaceOneShot(Plugin.selectAudio);
        }
        }
    private static void HandleClick()
    {
        Plugin.Logger.LogInfo($"[TR] HandleClick");
        if (Plugin.combatHUD != null && units != null)
        {
            if (units.Count > 0)
            {
                Plugin.Logger.LogInfo(units.Count);
                Plugin.combatHUD.DeselectAll(false);
                foreach (Unit t_unit in units)
                {
                    Plugin.combatHUD.SelectUnit(t_unit);
                }
                units = [.. (List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()];
            }
        }
    }
}

