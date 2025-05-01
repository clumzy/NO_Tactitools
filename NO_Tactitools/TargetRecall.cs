using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools;


    [HarmonyPatch(typeof(Rewired.Controller), "Connected")]
    class RegisterControllerPatch
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
        private static bool longPressHandled = false;
        private const float LONG_PRESS_THRESHOLD = 0.2f; // 200ms

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
                    longPressHandled = false;
                }
                else if (previousButtonState && currentButtonState)
                {
                    // Button is being held down
                    float holdDuration = Time.time - buttonPressTime;
                    if (holdDuration >= LONG_PRESS_THRESHOLD && !longPressHandled)
                    {
                        Plugin.Logger.LogInfo("LONG PRESS !");
                        HandleLongPress();
                        longPressHandled = true;
                    }
                }
                else if (previousButtonState && !currentButtonState)
                {
                    // Button just released
                    if (!longPressHandled)
                    {
                        Plugin.Logger.LogInfo("CLICK !");
                        HandleClick();
                    }
                    longPressHandled = false;
                }
                previousButtonState = currentButtonState;
            }
            return true;
        }

        private static void HandleLongPress()
        {
            if (Plugin.combatHUD != null)
            {
                Plugin.units = [.. (List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()];
                SoundManager.PlayInterfaceOneShot(Plugin.selectAudio);
            }
            }
        private static void HandleClick()
        {
            if (Plugin.combatHUD != null && Plugin.units != null)
            {
                if (Plugin.units.Count > 0)
                {
                    Plugin.Logger.LogInfo(Plugin.units.Count);
                    Plugin.combatHUD.DeselectAll(false);
                    foreach (Unit t_unit in Plugin.units)
                    {
                        Plugin.combatHUD.SelectUnit(t_unit);
                    }
                }
            }
        }
    }

