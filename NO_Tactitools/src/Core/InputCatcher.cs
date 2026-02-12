using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NO_Tactitools.Core;

public class InputCatcher {
    // Dictionary mapping each controller to its list of buttons
    public static Dictionary<Rewired.Controller, List<ControllerInput>> controllerInputs = [];
    // Dictionary mapping controller names to pending buttons
    public static Dictionary<string, List<PendingInput>> pendingControllerInputs = [];
    // keyboard pointer for easy access
    public static Rewired.Keyboard keyboardController = null;

    public static void RegisterNewInput(
        RewiredInputConfig config,
        float longPressThreshold = 0.2f,
        System.Action onRelease = null,
        System.Action onHold = null,
        System.Action onLongPress = null
    ) {
        RegisterNewInput(config.ControllerName.Value, config.ButtonIndex.Value, longPressThreshold, onRelease, onHold, onLongPress);
    }

    public static void RegisterNewInput(
        string controllerName,
        int buttonIndex,
        float longPressThreshold = 0.2f,
        System.Action onRelease = null,
        System.Action onHold = null,
        System.Action onLongPress = null
        ) {
        if (controllerName == "") {
            Plugin.Log("[IC] No controller name provided for button registration. Skipping.");
            return;
        }
        else if (buttonIndex < 0) {
            Plugin.Log("[IC] No input code string provided for button registration. Skipping.");
            return;
        }

        bool found = false;
        foreach (Controller controller in controllerInputs.Keys) {
            if (controller.name.Trim() == controllerName) {
                RegisterInputNow(controller, buttonIndex, longPressThreshold, onRelease, onHold, onLongPress);
                found = true;
                break;
            }
        }

        if (!found) {
            if (!pendingControllerInputs.ContainsKey(controllerName))
                pendingControllerInputs[controllerName] = [];
            pendingControllerInputs[controllerName].Add(new PendingInput(buttonIndex, longPressThreshold, onRelease, onHold, onLongPress));
            Plugin.Log("[IC] Controller not connected, input " + buttonIndex + " added to pending list for " + controllerName);
        }
    }

    public static IEnumerator RegisterPendingInputsRoutine(Controller controller, List<PendingInput> pendingInputs) {
        yield return null;
        foreach (PendingInput pending in pendingInputs) {
            RegisterInputNow(controller, pending.inputIndex, pending.longPressThreshold, pending.onShortPress, pending.onHold, pending.onLongPress);
        }
    }

    public static void RegisterInputNow(
        Controller controller,
        int inputIndex,
        float longPressThreshold,
        System.Action onRelease,
        System.Action onHold,
        System.Action onLongPress) 
        {
        string controllerName = controller.name.Trim();
        Plugin.Log("[IC] Registering button " + inputIndex + " on controller " + controllerName);

        ControllerInput newInput = new(
                    inputIndex,
                    longPressThreshold,
                    onRelease,
                    onHold,
                    onLongPress
                    );

        controllerInputs[controller].Add(newInput);
        Plugin.Log("[IC] Registered input " + inputIndex + " on controller " + controllerName + ".");
    }

}

public class ControllerInput {
    public int buttonNumber;
    public System.Action OnShortPress;
    public System.Action OnHold;
    public System.Action OnLongPress;
    public bool currentButtonState;
    public bool previousButtonState;
    public float buttonPressTime;
    public float longPressThreshold;
    public bool longPressHandled;
    public bool holdLogHandled;

    public ControllerInput( // CONSTRUCTOR FOR BUTTONS AND HATS
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
            Plugin.Log($"[IC] Creating input {buttonNumber.ToString()} with actions");
            this.OnShortPress = onShortPress;
            this.OnHold = onHold;
            this.OnLongPress = onLongPress;
        }
    }
}

public class PendingInput(int inputIndex, float longPressThreshold, System.Action onShortPress, System.Action onHold, System.Action onLongPress) {
    // Different because we don't have button number yet
    public int inputIndex = inputIndex;
    public float longPressThreshold = longPressThreshold;
    public System.Action onShortPress = onShortPress;
    public System.Action onHold = onHold;
    public System.Action onLongPress = onLongPress;
}


[HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
class ControllerInputInterceptionPatch {
    static bool Prefix(Controller __instance) {
        if (GameBindings.Player.Aircraft.GetAircraft(silent: true) == null) {
            return true; // Skip the original method
        }
        foreach (Controller controller in InputCatcher.controllerInputs.Keys) {
            if (__instance == controller) {
                foreach (ControllerInput button in InputCatcher.controllerInputs[controller]) {
                    try {
                        button.currentButtonState = __instance.Buttons[button.buttonNumber].value;
                        if (!button.previousButtonState && button.currentButtonState) {
                            // Button just pressed
                            button.buttonPressTime = Time.time;
                            button.longPressHandled = false;
                            button.holdLogHandled = false;
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
                                if (!button.holdLogHandled) {
                                    Plugin.Log($"[IC] Hold detected on button {button.buttonNumber.ToString()}");
                                    button.holdLogHandled = true;
                                }
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
                    catch (ArgumentOutOfRangeException) {
                        Plugin.Log("[IC] Error processing button " + button.buttonNumber.ToString() + " on controller" + __instance.name.Trim().ToString() + ". Removing from registered inputs.");
                        InputCatcher.controllerInputs[controller].Remove(button);
                    }
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
        Plugin.Log("[IC] Controller connected: " + cleanedName);
        if (!InputCatcher.controllerInputs.ContainsKey(__instance)) {
            InputCatcher.controllerInputs[__instance] = [];
            Plugin.Log("[IC] Controller structure initialized for: " + cleanedName);
        }

        if (InputCatcher.pendingControllerInputs.ContainsKey(cleanedName)) {
            List<PendingInput> pendingInputs = InputCatcher.pendingControllerInputs[cleanedName];
            Plugin.Instance.StartCoroutine(InputCatcher.RegisterPendingInputsRoutine(__instance, pendingInputs));
            InputCatcher.pendingControllerInputs.Remove(cleanedName);
        }
    }
}

/* [HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
class TestInput {

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
    static void Postfix(Controller __instance) {
        if (__instance.name != "Keyboard") {
            return;
        }
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
            Plugin.Log($"[IC] input detected on controller {__instance.name.Trim()} at time {time.ToString()}");
            // MES TROUVAILLES ICI
            // LES HAT DEMARRENT A L'INDEX 128.
            // L'ALGO C'EST INDEX = 128 * HAT NUMBER + DIRECTION (0-7)
            Plugin.Log(diffIndex.ToString());
        }
    }
} */

