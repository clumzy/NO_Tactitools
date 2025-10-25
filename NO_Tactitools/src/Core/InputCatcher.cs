using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NO_Tactitools.Core;

public class InputCatcher {
    // Dictionary mapping each controller to its list of buttons
    public static Dictionary<Rewired.Controller, List<ControllerButton>> controllerStructure =
        [];
    // Dictionary mapping controller names to pending buttons
    public static Dictionary<string, List<ControllerButton>> pendingButtons = [];

    public static void RegisterControllerButton(string controllerName, ControllerButton button) {
        Plugin.Log($"[IC] Registering button {button.buttonNumber.ToString()} on controller {controllerName.ToString()}");
        bool found = false;
        foreach (Controller controller in controllerStructure.Keys) {
            if (controller.name.Trim() == controllerName) {
                controllerStructure[controller].Add(button);
                Plugin.Log($"[IC] Registered button {button.buttonNumber.ToString()} on controller {controllerName.ToString()}");
                found = true;
                break;
            }
        }
        if (!found) {
            if (!pendingButtons.ContainsKey(controllerName))
                pendingButtons[controllerName] = [];
            pendingButtons[controllerName].Add(button);
            Plugin.Log($"[IC] Controller not connected, button {button.buttonNumber.ToString()} added to pending list for {controllerName}");
        }
    }
}

public class ControllerButton {
    public int buttonNumber;
    public System.Action OnShortPress;
    public System.Action OnHold;
    public System.Action OnLongPress;
    public bool currentButtonState;
    public bool previousButtonState;
    public float buttonPressTime;
    public float longPressThreshold;
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
        }
        else {
            Plugin.Log($"[IC] Creating button {buttonNumber.ToString()} with actions");
            this.OnShortPress = onShortPress;
            this.OnHold = onHold;
            this.OnLongPress = onLongPress;
        }
    }
}

[HarmonyPatch(typeof(Rewired.Joystick), "qEOMcUOdQiTnCAuDVnEAygszhlYP")]
class InputInterceptionPatch {
    static bool Prefix(Joystick __instance) {
        foreach (Controller controller in InputCatcher.controllerStructure.Keys) {
            if (__instance == controller) {
                foreach (ControllerButton button in InputCatcher.controllerStructure[controller]) {
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
                            Plugin.Log($"[IC] Long press detected on button {button.buttonNumber.ToString()}");
                            button.OnLongPress?.Invoke();
                            button.longPressHandled = true;
                        }
                        else if (holdDuration < button.longPressThreshold && button.OnHold != null) {
                            Plugin.Log($"[IC] Hold detected on button {button.buttonNumber.ToString()}");
                            button.OnHold?.Invoke();
                        }
                    }
                    else if (button.previousButtonState && !button.currentButtonState && button.OnShortPress != null) {
                        // Button just released
                        if (!button.longPressHandled) {
                            Plugin.Log($"[IC] Short press detected on button {button.buttonNumber.ToString()}");
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
        Plugin.Log($"[IC] Controller connected: {cleanedName}");

        if (!InputCatcher.controllerStructure.ContainsKey(__instance)) {
            InputCatcher.controllerStructure[__instance] = [];
            Plugin.Log($"[IC] Controller structure initialized for: {cleanedName}");
        }

        if (InputCatcher.pendingButtons.ContainsKey(cleanedName)) {
            foreach (var btn in InputCatcher.pendingButtons[cleanedName]) {
                InputCatcher.controllerStructure[__instance].Add(btn);
                Plugin.Log($"[IC] Registered pending button {btn.buttonNumber.ToString()} on controller {cleanedName}");
            }
            InputCatcher.pendingButtons.Remove(cleanedName);
        }
    }
}


/// TEST ZONE

[HarmonyPatch(typeof(Rewired.Joystick), "qEOMcUOdQiTnCAuDVnEAygszhlYP")]
class HatInputInterceptionPatch {

    static int FindFirstDifferenceIndex(IList<bool> list1, IList<bool> list2) {
        for (int i = 0; i < list1.Count && i < list2.Count; i++) {
            if (list1[i] != list2[i]) {
                return i;
            }
        }

        return Math.Min(list1.Count, list2.Count);
    }
    static double time = -1;
    static IList<bool> previousHatStates = [];
    static void Postfix(Joystick __instance) {
        if (previousHatStates == null) {
            for (int i = 0; i < __instance.Buttons.Count; i++) {
                previousHatStates.Add(__instance.Buttons[i].value);
            }
        }
        double newTime = __instance.GetLastTimeAnyButtonPressed();
        if (newTime != time) {
            time = newTime;
            int diffIndex = FindFirstDifferenceIndex(previousHatStates, [.. __instance.Buttons.Select(b => b.value)]);
            previousHatStates.Clear();
            for (int i = 0; i < __instance.Buttons.Count; i++) {
                previousHatStates.Add(__instance.Buttons[i].value);
            }
            Plugin.Log($"[IC] Hat input detected on controller {__instance.name.Trim()} at time {time.ToString()}");
            // MES TROUVAILLES ICI
            // LES HAT DEMARRENT A L'INDEX 128.
            // L'ALGO C'EST INDEX = 128 * HAT NUMBER + DIRECTION (0-7)
            Plugin.Log(diffIndex.ToString());
        }
    }
}