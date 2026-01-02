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
        string controllerName,
        string inputCodeString,
        float longPressThreshold = 0.2f,
        System.Action onRelease = null,
        System.Action onHold = null,
        System.Action onLongPress = null
        ) {
        if (controllerName == "") {
            Plugin.Log("[IC] No controller name provided for button registration. Skipping.");
            return;
        }
        else if (inputCodeString == "") {
            Plugin.Log("[IC] No input code string provided for button registration. Skipping.");
            return;
        }

        bool found = false;
        foreach (Controller controller in controllerInputs.Keys) {
            if (controller.name.Trim() == controllerName) {
                RegisterInputNow(controller, inputCodeString, longPressThreshold, onRelease, onHold, onLongPress);
                found = true;
                break;
            }
        }

        if (!found) {
            if (!pendingControllerInputs.ContainsKey(controllerName))
                pendingControllerInputs[controllerName] = [];
            pendingControllerInputs[controllerName].Add(new PendingInput(inputCodeString, longPressThreshold, onRelease, onHold, onLongPress));
            Plugin.Log("[IC] Controller not connected, input " + inputCodeString + " added to pending list for " + controllerName);
        }
    }

    public static IEnumerator RegisterPendingInputsRoutine(Controller controller, List<PendingInput> pendingInputs) {
        yield return null;
        foreach (PendingInput pending in pendingInputs) {
            RegisterInputNow(controller, pending.inputCodeString, pending.longPressThreshold, pending.onShortPress, pending.onHold, pending.onLongPress);
        }
    }

    public static void RegisterInputNow(
        Controller controller,
        string inputCodeString,
        float longPressThreshold,
        System.Action onRelease,
        System.Action onHold,
        System.Action onLongPress
    ) {
        string controllerName = controller.name.Trim();
        Plugin.Log("[IC] Registering button " + inputCodeString + " on controller " + controllerName);
        ControllerInput newInput;
        string inputType = ParseInputType(inputCodeString, controllerName);

        switch (inputType) {
            case "KeyCode":
                newInput = new ControllerInput(
                    keyboardController.GetButtonIndexByKeyCode((KeyCode)Enum.Parse(typeof(KeyCode), inputCodeString)),
                    longPressThreshold,
                    onRelease,
                    onHold,
                    onLongPress
                    );
                break;
            case "ButtonNumber":
                newInput = new ControllerInput(
                    int.Parse(inputCodeString),
                    longPressThreshold,
                    onRelease,
                    onHold,
                    onLongPress
                    );
                break;
            case "Hat":
                newInput = new ControllerInput(
                    ParseHatInput(inputCodeString),
                    longPressThreshold,
                    onRelease,
                    onHold,
                    onLongPress
                    );
                break;
            default:
                Plugin.Log("[IC] Unknown input type for input code: " + inputCodeString + " on controller: " + controllerName);
                return;
        }
        
        controllerInputs[controller].Add(newInput);
        Plugin.Log("[IC] Registered input " + inputCodeString + " on controller " + controllerName);
    }

    public static string ParseInputType(string inputCodeString, string controllerName) {
        if (controllerName == "Keyboard" && Enum.TryParse<KeyCode>(inputCodeString, out _)) {
            return "KeyCode";
        }
        else if (int.TryParse(inputCodeString, out _)) {
            return "ButtonNumber";
        }
        else if (inputCodeString.StartsWith("h_")) {
            string[] parts = inputCodeString.Split('_');
            if (parts.Length == 3 && parts[0] == "h" && int.TryParse(parts[1], out _)) {
                string direction = parts[2].ToLower();
                if (direction == "left" || direction == "right" || direction == "up" || direction == "down") {
                    return "Hat";
                }
            }
        }
        Plugin.Log("[IC] Unable to parse input code: " + inputCodeString);
        return "Unknown";
    }
    // MES TROUVAILLES ICI
    // LES HAT DEMARRENT A L'INDEX 128.
    // L'ALGO C'EST INDEX = 128 * HAT NUMBER + DIRECTION (0-7)
    public static int ParseHatInput(string inputCodeString) {
        string[] parts = inputCodeString.Split('_');
        int.TryParse(parts[1], out int hatNumber);
        string directionStr = parts[2].ToLower();
        int direction = 0;
        switch (directionStr) {
            case "up":
                direction = 0;
                break;
            case "right":
                direction = 2;
                break;
            case "down":
                direction = 4;
                break;
            case "left":
                direction = 6;
                break;
        }
        int index = 128 + 8 * hatNumber + direction;
        return index;
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

public class PendingInput {
    public string inputCodeString;
    public float longPressThreshold;
    public System.Action onShortPress;
    public System.Action onHold;
    public System.Action onLongPress;

    public PendingInput(string inputCodeString, float longPressThreshold, System.Action onShortPress, System.Action onHold, System.Action onLongPress) {
        this.inputCodeString = inputCodeString;
        this.longPressThreshold = longPressThreshold;
        this.onShortPress = onShortPress;
        this.onHold = onHold;
        this.onLongPress = onLongPress;
    }
}


[HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
class ControllerInputInterceptionPatch {
    static bool Prefix(Controller __instance) {
        if(Bindings.Player.Aircraft.GetAircraft(nullIsOkay:true) == null) {
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

        // Special case for keyboard
        if (cleanedName == "Keyboard") {
            InputCatcher.keyboardController = (Rewired.Keyboard)__instance;
            Plugin.Log("[IC] Keyboard controller pointer set.");
        }
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