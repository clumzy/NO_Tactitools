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

    public static void RegisterNewInput(
        RewiredInputConfig config,
        float longPressThreshold = 0.2f,
        System.Action onRelease = null,
        System.Action onHold = null,
        System.Action onLongPress = null
        ) {
        config.LongPressThreshold = longPressThreshold;
        if (onRelease != null)   config.OnShortPress = config.OnShortPress == null ? onRelease   : config.OnShortPress + onRelease;
        if (onHold != null)      config.OnHold       = config.OnHold       == null ? onHold      : config.OnHold + onHold;
        if (onLongPress != null) config.OnLongPress  = config.OnLongPress  == null ? onLongPress : config.OnLongPress + onLongPress;

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

        TryRegisterOrQueue(config, controllerName, buttonIndex, longPressThreshold, onRelease, onHold, onLongPress);
    }

    public static IEnumerator RegisterPendingInputsRoutine(Controller controller, List<PendingInput> pendingInputs) {
        yield return null;
        foreach (PendingInput pending in pendingInputs) {
            RegisterInputNow(
                pending.config,
                controller,
                pending.inputIndex,
                pending.longPressThreshold,
                pending.onShortPress,
                pending.onHold,
                pending.onLongPress);
        }
    }

    public static void RegisterInputNow(
        RewiredInputConfig config,
        Controller controller,
        int inputIndex,
        float longPressThreshold,
        System.Action onRelease,
        System.Action onHold,
        System.Action onLongPress) {
        string controllerName = controller.name.Trim();
        Plugin.Log("[IC] Registering button " + inputIndex + " on controller " + controllerName);

        ControllerInput newInput = new(
                    config,
                    inputIndex,
                    longPressThreshold,
                    onRelease,
                    onHold,
                    onLongPress
                    );

        controllerInputs[controller].Add(newInput);
        Plugin.Log("[IC] Registered input " + inputIndex + " on controller " + controllerName + ".");
    }


    public static void RegisterNewBinding(RewiredInputConfig config) {
        string controllerName = config.ControllerName.Value.Trim();
        int buttonIndex = config.ButtonIndex.Value;
        if (controllerName == "" || buttonIndex < 0) return;

        TryRegisterOrQueue(config, controllerName, buttonIndex, config.LongPressThreshold, config.OnShortPress, config.OnHold, config.OnLongPress);
    }
    
    public static void ModifyInputAfterNewConfig(
        RewiredInputConfig config) {
        string controllerName = config.ControllerName.Value.Trim();
        int buttonIndex = config.ButtonIndex.Value;

        // existing controllers
        foreach (Controller controller in controllerInputs.Keys.ToList()) {
            ControllerInput existingInput = controllerInputs[controller].FirstOrDefault(input => input.config == config);
            if (existingInput != null) {
                if (controller.name.Trim() != controllerName) {
                    // Controller has changed, move the input to the right controller
                    controllerInputs[controller].Remove(existingInput);
                    
                    bool targetFound = false;
                    foreach (Controller targetController in controllerInputs.Keys) {
                        if (targetController.name.Trim() == controllerName) {
                            existingInput.buttonNumber = buttonIndex;
                            controllerInputs[targetController].Add(existingInput);
                            Plugin.Log("[IC] Moved active input for config " + config.Input.Definition.Key + " to new controller " + controllerName);
                            targetFound = true;
                            break;
                        }
                    }
                    if (!targetFound) {
                        // move to pending
                        if (!pendingControllerInputs.ContainsKey(controllerName))
                            pendingControllerInputs[controllerName] = [];
                        
                        pendingControllerInputs[controllerName].Add(new PendingInput(config, buttonIndex, existingInput.longPressThreshold, existingInput.OnShortPress, existingInput.OnHold, existingInput.OnLongPress));
                        Plugin.Log("[IC] Moved active input for config " + config.Input.Definition.Key + " to pending for " + controllerName);
                    }
                }
                else if (existingInput.buttonNumber != buttonIndex) {
                    // Button index has changed
                    existingInput.buttonNumber = buttonIndex;
                    Plugin.Log("[IC] Updated active button index for config " + config.Input.Definition.Key + " to " + buttonIndex.ToString());
                }
                return;
            }
        }

        // pending controllers
        foreach (string pendingController in pendingControllerInputs.Keys.ToList()) {
            PendingInput existingPending = pendingControllerInputs[pendingController].FirstOrDefault(p => p.config == config);
            if (existingPending != null) {
                pendingControllerInputs[pendingController].Remove(existingPending);
                TryRegisterOrQueue(config, controllerName, buttonIndex, existingPending.longPressThreshold, existingPending.onShortPress, existingPending.onHold, existingPending.onLongPress);
                return;
            }
        }
    }

    public static void UnregisterInput(RewiredInputConfig config) {
        foreach (Controller controller in controllerInputs.Keys) {
            ControllerInput existingInput = controllerInputs[controller].FirstOrDefault(input => input.config == config);
            if (existingInput != null) {
                controllerInputs[controller].Remove(existingInput);
                Plugin.Log("[IC] Unregistered input for config " + config.Input.Definition.Key);
            }
        }

        // Also remove from pending inputs
        foreach (string controllerName in pendingControllerInputs.Keys.ToList()) {
            int removed = pendingControllerInputs[controllerName].RemoveAll(p => p.config == config);
            if (removed > 0) {
                Plugin.Log("[IC] Removed " + removed + " pending input(s) for config " + config.Input.Definition.Key);
            }
        }
    }

    private static void TryRegisterOrQueue(
        RewiredInputConfig config,
        string controllerName,
        int buttonIndex,
        float longPressThreshold,
        System.Action onRelease,
        System.Action onHold,
        System.Action onLongPress
        ) {
        foreach (Controller controller in controllerInputs.Keys) {
            if (controller.name.Trim() != controllerName) continue;

            ControllerInput existing = controllerInputs[controller].FirstOrDefault(input => input.config == config);
            if (existing != null) {
                // same config already registered: merge new callbacks
                if (onRelease != null)   existing.OnShortPress = existing.OnShortPress == null ? onRelease   : existing.OnShortPress + onRelease;
                if (onHold != null)      existing.OnHold       = existing.OnHold       == null ? onHold      : existing.OnHold + onHold;
                if (onLongPress != null) existing.OnLongPress  = existing.OnLongPress  == null ? onLongPress : existing.OnLongPress + onLongPress;
                Plugin.Log("[IC] Merged callbacks for already-registered config " + config.Input.Definition.Key);
            }
            else {
                RegisterInputNow(config, controller, buttonIndex, longPressThreshold, onRelease, onHold, onLongPress);
            }
            return;
        }

        // controller not connected yet: queue as pending
        if (!pendingControllerInputs.ContainsKey(controllerName))
            pendingControllerInputs[controllerName] = [];

        PendingInput existingPending = pendingControllerInputs[controllerName].FirstOrDefault(p => p.config == config);
        if (existingPending != null) {
            // same config already pending: merge new callbacks
            if (onRelease != null)   existingPending.onShortPress = existingPending.onShortPress == null ? onRelease   : existingPending.onShortPress + onRelease;
            if (onHold != null)      existingPending.onHold       = existingPending.onHold       == null ? onHold      : existingPending.onHold + onHold;
            if (onLongPress != null) existingPending.onLongPress  = existingPending.onLongPress  == null ? onLongPress : existingPending.onLongPress + onLongPress;
            Plugin.Log("[IC] Merged callbacks for already-pending config " + config.Input.Definition.Key);
        }
        else {
            pendingControllerInputs[controllerName].Add(new PendingInput(config, buttonIndex, longPressThreshold, onRelease, onHold, onLongPress));
            Plugin.Log("[IC] Controller not connected, input " + buttonIndex + " added to pending list for " + controllerName);
        }
    }
}

public class ControllerInput {
    public RewiredInputConfig config; // Only used for config entries, not for actual input catching
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

    public ControllerInput(
        RewiredInputConfig config,
        int buttonNumber,
        float longPressThreshold = 0.2f, // 200ms,
        System.Action onShortPress = null,
        System.Action onHold = null,
        System.Action onLongPress = null
        ) {
        this.config = config;
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

public class PendingInput(RewiredInputConfig config, int inputIndex, float longPressThreshold, System.Action onShortPress, System.Action onHold, System.Action onLongPress) {
    public RewiredInputConfig config = config;
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
