using HarmonyLib;
using Rewired;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NO_Tactitools.Core;

public class InputRegistration {
    public RewiredInputConfig config;
    public float longPressThreshold;
    public System.Action onShortPress;
    public System.Action onHold;
    public System.Action onLongPress;
}

public class InputCatcher {
    public static List<InputRegistration> allRegistrations = [];
    // Dictionary mapping each controller to its list of buttons
    public static Dictionary<Rewired.Controller, List<ControllerInput>> controllerInputs = [];
    // Dictionary mapping controller names to pending buttons
    public static Dictionary<string, List<PendingInput>> pendingControllerInputs = [];

    public static void RegisterNewInput(
        RewiredInputConfig config,
        float longPressThreshold = 0.2f,
        System.Action onRelease = null,
        System.Action onHold = null,
        System.Action onLongPress = null
        ) {
        
        InputRegistration reg = new() {
            config = config,
            longPressThreshold = longPressThreshold,
            onShortPress = onRelease,
            onHold = onHold,
            onLongPress = onLongPress
        };
        allRegistrations.Add(reg);

        string controllerName = config.ControllerName.Value.Trim();
        int buttonIndex = config.ButtonIndex.Value;
        if (controllerName == "") {
            Plugin.Log("[IC] No controller name provided for button registration. Skipping.");
            return;
        }
        else if (buttonIndex < 0) {
            Plugin.Log("[IC] No input code string provided for button registration. Skipping.");
            return;
        }

        TryRegisterOrQueue(reg, controllerName, buttonIndex);
    }

    public static IEnumerator RegisterPendingInputsRoutine(Controller controller, List<PendingInput> pendingInputs) {
        yield return null;
        foreach (PendingInput pending in pendingInputs) {
            RegisterInputNow(
                pending.registration,
                controller,
                pending.inputIndex);
        }
    }

    public static void RegisterInputNow(
        InputRegistration registration,
        Controller controller,
        int inputIndex) {
        string controllerName = controller.name.Trim();
        Plugin.Log("[IC] Registering button " + inputIndex + " on controller " + controllerName);

        ControllerInput newInput = new(
                    registration,
                    controller,
                    inputIndex
                    );

        controllerInputs[controller].Add(newInput);
        Plugin.Log("[IC] Registered input " + inputIndex + " on controller " + controllerName + ".");
    }

    public static void RegisterNewBinding(RewiredInputConfig config) {
        string controllerName = config.ControllerName.Value.Trim();
        int buttonIndex = config.ButtonIndex.Value;
        if (controllerName == "" || buttonIndex < 0) return;

        foreach (var reg in allRegistrations.Where(r => r.config == config)) {
            TryRegisterOrQueue(reg, controllerName, buttonIndex);
        }
    }
    
    public static void ModifyInputAfterNewConfig(RewiredInputConfig config) {
        UnregisterInput(config);
        RegisterNewBinding(config);
    }

    public static void UnregisterInput(RewiredInputConfig config, bool clearLinkedEntries = true) {
        foreach (Controller controller in controllerInputs.Keys) {
            int removed = controllerInputs[controller].RemoveAll(input => input.registration.config == config);
            if (removed > 0) {
                Plugin.Log("[IC] Unregistered " + removed + " input(s) for config " + config.Input.Definition.Key);
            }
        }

        // Also remove from pending inputs
        foreach (string controllerName in pendingControllerInputs.Keys.ToList()) {
            int removed = pendingControllerInputs[controllerName].RemoveAll(p => p.registration.config == config);
            if (removed > 0) {
                Plugin.Log("[IC] Removed " + removed + " pending input(s) for config " + config.Input.Definition.Key);
            }
        }

        if (clearLinkedEntries) {
            // also clear the linked entries
            config.ControllerName.BoxedValue = "";
            config.ButtonIndex.BoxedValue = -1;
        }
    }

    private static void TryRegisterOrQueue(
        InputRegistration registration,
        string controllerName,
        int buttonIndex
        ) {
        foreach (Controller controller in controllerInputs.Keys) {
            if (controller.name.Trim() != controllerName) continue;

            RegisterInputNow(registration, controller, buttonIndex);
            return;
        }

        // controller not connected yet: queue as pending
        if (!pendingControllerInputs.ContainsKey(controllerName))
            pendingControllerInputs[controllerName] = [];

        pendingControllerInputs[controllerName].Add(new PendingInput(registration, buttonIndex));
        Plugin.Log("[IC] Controller not connected, input " + buttonIndex + " added to pending list for " + controllerName);
    }
}

public class ControllerInput {
    public InputRegistration registration;
    public int buttonNumber;
    public bool currentButtonState;
    public bool previousButtonState;
    public float buttonPressTime;
    public bool longPressHandled;
    public bool holdLongHandled;

    public ControllerInput(
        InputRegistration registration,
        Controller controller,
        int buttonNumber
        ) {
        this.registration = registration;
        this.buttonNumber = buttonNumber;
        this.currentButtonState = controller.Buttons[buttonNumber].value;
        this.previousButtonState = this.currentButtonState;
        this.buttonPressTime = Time.time;
        this.longPressHandled = true; // Assume it's already handled if they're holding it down on registration
        this.holdLongHandled = true;
        if (registration.onShortPress == null && registration.onLongPress == null && registration.onHold == null) {
            Plugin.Logger.LogError("[IC] No actions provided for button " + buttonNumber);
        }
        else {
            Plugin.Log($"[IC] Creating input {buttonNumber.ToString()} with actions");
        }
    }
}

public class PendingInput(InputRegistration registration, int inputIndex) {
    public InputRegistration registration = registration;
    public int inputIndex = inputIndex;
}

[HarmonyPatch(typeof(Rewired.Controller), "pBrAJYWOGkILyqjLrMpmCdajATI")]
class ControllerInputInterceptionPatch {
    static bool Prefix(Controller __instance) {
        if (GameBindings.Player.Aircraft.GetAircraft(silent: true) == null) {
            return true; // Skip the original method
        }
        foreach (Controller controller in InputCatcher.controllerInputs.Keys) {
            if (__instance == controller) {
                foreach (ControllerInput button in InputCatcher.controllerInputs[controller].ToList()) {
                    try {
                        button.currentButtonState = __instance.Buttons[button.buttonNumber].value;
                        if (!button.previousButtonState && button.currentButtonState) {
                            // Button just pressed
                            button.buttonPressTime = Time.time;
                            button.longPressHandled = false;
                            button.holdLongHandled = false;
                        }
                        else if (button.previousButtonState && button.currentButtonState) {
                            // Button is being held down
                            float holdDuration = Time.time - button.buttonPressTime;
                            if (holdDuration >= button.registration.longPressThreshold && !button.longPressHandled && button.registration.onLongPress != null) {
                                Plugin.Log($"[IC] Long press detected on button {button.buttonNumber.ToString()}");
                                button.registration.onLongPress?.Invoke();
                                button.longPressHandled = true;
                            }
                            else if (holdDuration < button.registration.longPressThreshold && button.registration.onHold != null) {
                                if (!button.holdLongHandled) {
                                    Plugin.Log($"[IC] Hold detected on button {button.buttonNumber.ToString()}");
                                    button.holdLongHandled = true;
                                }
                                button.registration.onHold?.Invoke();
                            }
                        }
                        else if (button.previousButtonState && !button.currentButtonState && button.registration.onShortPress != null) {
                            // Button just released
                            if (!button.longPressHandled) {
                                Plugin.Log($"[IC] Short press detected on button {button.buttonNumber.ToString()}");
                                button.registration.onShortPress?.Invoke();
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
