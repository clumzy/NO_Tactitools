using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools;

public class InputCatcherPlugin
{
    // Dictionary mapping each controller to its list of buttons
    public Dictionary<Rewired.Controller, List<ControllerButton>> controllerStructure =
        new Dictionary<Rewired.Controller, List<ControllerButton>>();
    public InputCatcherPlugin()
    {
    }

    public void RegisterControllerButton(string controllerName, ControllerButton button)
    {
        Plugin.Logger.LogInfo($"[IC] Registering button {button.buttonNumber.ToString()} on controller {controllerName.ToString()}");
        foreach(Controller controller in controllerStructure.Keys)
        {
            if (controller.name.Trim() == controllerName)
            {
                controllerStructure[controller].Add(button);
                Plugin.Logger.LogInfo($"[IC] Registered button {button.buttonNumber.ToString()} on controller {controllerName.ToString()}");
            }
        }
    }
}

public class ControllerButton
{
    public int buttonNumber;
    public System.Action OnShortPress;
    public System.Action OnLongPress;
    public float longPressThreshold;
    public bool currentButtonState;
    public bool previousButtonState;
    public float buttonPressTime;
    public float holdDuration;
    public bool longPressHandled;

    public ControllerButton(
        int buttonNumber, 
        float longPressThreshold = 0.2f, // 200ms,
        System.Action onShortPress = null,
        System.Action onLongPress = null
        )
    {
        this.buttonNumber = buttonNumber;
        this.longPressThreshold = longPressThreshold;
        if (onShortPress == null && onLongPress == null)
        {
            Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber);
            this.OnShortPress = () => {Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber.ToString()); };
            this.OnLongPress = () => {Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber.ToString()); };
        }
        else
        {
            Plugin.Logger.LogInfo($"[IC] Creating button {buttonNumber.ToString()} with actions");
            this.OnShortPress = onShortPress;
            this.OnLongPress = onLongPress;
        }
    }
}

[HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
    class InputInterceptionPatch
    {
        static bool Prefix(Controller __instance)
        {
            foreach(Controller controller in Plugin.inputCatcherPlugin.controllerStructure.Keys)
            {
                if(__instance == controller)
                {
                    foreach(ControllerButton button in Plugin.inputCatcherPlugin.controllerStructure[controller])
                    {
                        button.currentButtonState = __instance.Buttons[button.buttonNumber].value;
                        if (!button.previousButtonState && button.currentButtonState)
                        {
                            // Button just pressed
                            button.buttonPressTime = Time.time;
                            button.longPressHandled = false;
                        }
                        else if (button.previousButtonState && button.currentButtonState)
                        {
                            // Button is being held down
                            float holdDuration = Time.time - button.buttonPressTime;
                            if (holdDuration >= button.longPressThreshold && !button.longPressHandled)
                            {
                                Plugin.Logger.LogInfo($"[IC] Long press detected on button {button.buttonNumber.ToString()}");
                                button.OnLongPress?.Invoke();
                                button.longPressHandled = true;
                            }
                        }
                        else if (button.previousButtonState && !button.currentButtonState)
                        {
                            // Button just released
                            if (!button.longPressHandled)
                            {
                                Plugin.Logger.LogInfo($"[IC] Short press detected on button {button.buttonNumber.ToString()}");
                                button.OnShortPress?.Invoke();
                            }
                            button.longPressHandled = false;
                        }
                        button.previousButtonState = button.currentButtonState;
                    }
                }
            }
            return true;
        }
    }

[HarmonyPatch(typeof(Rewired.Controller), "Connected")]
class RegisterControllerPatch
{
    static void Postfix(Controller __instance)
    {
        string cleanedName = __instance.name.Trim();
        Plugin.Logger.LogInfo($"[IC] Controller connected: {cleanedName}");

        if (string.Equals(cleanedName, Plugin.configControllerName1.Value) || string.Equals(cleanedName, Plugin.configControllerName2.Value))
        {
            // Register the controller and initialize its button list
            Plugin.inputCatcherPlugin.controllerStructure[__instance] = new List<ControllerButton>();
            Plugin.Logger.LogInfo($"[IC] Controller match: {cleanedName}");
        }
        else if (Plugin.inputCatcherPlugin.controllerStructure.ContainsKey(__instance))
        {
            Plugin.inputCatcherPlugin.controllerStructure[__instance] = new List<ControllerButton>();
            Plugin.Logger.LogInfo($"[IC] Controller match: {cleanedName}");
        }
    }
}