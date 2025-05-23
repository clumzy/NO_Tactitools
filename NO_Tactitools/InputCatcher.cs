using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools;

public class InputCatcherPlugin {
    // Dictionary mapping each controller to its list of buttons
    public Dictionary<Rewired.Controller, List<ControllerButton>> controllerStructure =
        [];

    // Dictionary mapping controller names to pending buttons
    public Dictionary<string, List<ControllerButton>> pendingButtons = [];

    public InputCatcherPlugin() {
    }

    public void RegisterControllerButton(string controllerName, ControllerButton button) {
        Plugin.Logger.LogInfo($"[IC] Registering button {button.buttonNumber.ToString()} on controller {controllerName.ToString()}");
        bool found = false;
        foreach (Controller controller in controllerStructure.Keys) {
            if (controller.name.Trim() == controllerName) {
                controllerStructure[controller].Add(button);
                Plugin.Logger.LogInfo($"[IC] Registered button {button.buttonNumber.ToString()} on controller {controllerName.ToString()}");
                found = true;
                break;
            }
        }
        if (!found) {
            if (!pendingButtons.ContainsKey(controllerName))
                pendingButtons[controllerName] = [];
            pendingButtons[controllerName].Add(button);
            Plugin.Logger.LogInfo($"[IC] Controller not connected, button {button.buttonNumber.ToString()} added to pending list for {controllerName}");
        }
    }
}

public class ControllerButton {
    public int buttonNumber;
    public System.Action OnShortPress;
    public System.Action OnHold;
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
        System.Action onHold = null,
        System.Action onLongPress = null
        ) {
        this.buttonNumber = buttonNumber;
        this.longPressThreshold = longPressThreshold;
        if (onShortPress == null && onLongPress == null && onHold == null) {
            Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber);
            this.OnShortPress = () => { Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber.ToString()); };
            this.OnHold = () => { Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber.ToString()); };
            this.OnLongPress = () => { Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber.ToString()); };
        }
        else {
            Plugin.Logger.LogInfo($"[IC] Creating button {buttonNumber.ToString()} with actions");
            this.OnShortPress = onShortPress;
            this.OnHold = onHold;
            this.OnLongPress = onLongPress;
        }
    }
}

[HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
class InputInterceptionPatch {
    static bool Prefix(Controller __instance) {
        foreach (Controller controller in Plugin.inputCatcherPlugin.controllerStructure.Keys) {
            if (__instance == controller) {
                foreach (ControllerButton button in Plugin.inputCatcherPlugin.controllerStructure[controller]) {
                    button.currentButtonState = __instance.Buttons[button.buttonNumber].value;
                    if (!button.previousButtonState && button.currentButtonState) {
                        // Button just pressed
                        button.buttonPressTime = Time.time;
                        button.longPressHandled = false;
                    }
                    else if (button.previousButtonState && button.currentButtonState) {
                        // Button is being held down
                        float holdDuration = Time.time - button.buttonPressTime;
                        if (holdDuration >= button.longPressThreshold && !button.longPressHandled && button.OnLongPress != null) {
                            Plugin.Logger.LogInfo($"[IC] Long press detected on button {button.buttonNumber.ToString()}");
                            button.OnLongPress?.Invoke();
                            button.longPressHandled = true;
                        }
                        else if (holdDuration < button.longPressThreshold && button.OnHold != null) {
                            Plugin.Logger.LogInfo($"[IC] Hold detected on button {button.buttonNumber.ToString()}");
                            button.OnHold?.Invoke();
                        }
                    }
                    else if (button.previousButtonState && !button.currentButtonState && button.OnShortPress != null) {
                        // Button just released
                        if (!button.longPressHandled) {
                            Plugin.Logger.LogInfo($"[IC] Short press detected on button {button.buttonNumber.ToString()}");
                            button.OnShortPress?.Invoke();
                        }
                    }
                    button.previousButtonState = button.currentButtonState;
                }
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(Rewired.Controller), "Connected")]
class RegisterControllerPatch {
    static void Postfix(Controller __instance) {
        string cleanedName = __instance.name.Trim();
        Plugin.Logger.LogInfo($"[IC] Controller connected: {cleanedName}");

        if (!Plugin.inputCatcherPlugin.controllerStructure.ContainsKey(__instance)) {
            Plugin.inputCatcherPlugin.controllerStructure[__instance] = [];
            Plugin.Logger.LogInfo($"[IC] Controller structure initialized for: {cleanedName}");
        }

        if (Plugin.inputCatcherPlugin.pendingButtons.ContainsKey(cleanedName)) {
            foreach (var btn in Plugin.inputCatcherPlugin.pendingButtons[cleanedName]) {
                Plugin.inputCatcherPlugin.controllerStructure[__instance].Add(btn);
                Plugin.Logger.LogInfo($"[IC] Registered pending button {btn.buttonNumber.ToString()} on controller {cleanedName}");
            }
            Plugin.inputCatcherPlugin.pendingButtons.Remove(cleanedName);
        }
    }
}