using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Rewired.Utils.Classes.Data;
using Unity.Audio;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CountermeasureControlsPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Logger.LogInfo($"[CC] Countermeasure Controls plugin starting !");
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.countermeasureControlsFlareControllerName.Value,
                new ControllerButton(
                (int)Plugin.countermeasureControlsFlareButtonNumber.Value,
                0.2f,
                onHold: HandleOnHoldFlare
                ));
            initialized = true;
            Plugin.Logger.LogInfo("[CC] Countermeasure Controls plugin succesfully started !");
        }
    }
    private static void HandleOnHoldFlare() {
        Plugin.Logger.LogInfo($"[CC] HandleOnHoldFlare");
    }
}