using System;
using BepInEx.Configuration;
using UnityEngine;
using Rewired;
using HarmonyLib;
using System.Collections;
using System.Linq;

namespace NO_Tactitools.Core;

internal sealed class ConfigurationManagerAttributes
/// <summary>
/// Class that can be used to customize how a setting is displayed in the configuration manager window.
/// </summary>
{
    public bool? IsAdvanced;
    public bool? Browsable;
    public string Category;
    public Action<ConfigEntryBase> CustomDrawer;
    public string DispName;
    public int? Order;
    public bool? ReadOnly;
    public bool? HideDefaultButton;
    public bool? HideSettingName;
    public object ControllerName;
    public object ButtonIndex;
}

public class RewiredInputConfig {
    public static System.Collections.Generic.List<RewiredInputConfig> AllConfigs = new();
    public ConfigEntry<string> Input { get; private set; }
    public ConfigEntry<string> ControllerName { get; private set; }
    public ConfigEntry<int> ButtonIndex { get; private set; }

    public RewiredInputConfig(ConfigFile config, string category, string featureName, string description, int order) {
        ControllerName = config.Bind(category, $"{featureName} - Controller Name", "", new ConfigDescription("Name of the peripheral", null, new ConfigurationManagerAttributes { Browsable = false }));
        ButtonIndex = config.Bind(category, $"{featureName} - Button Index", -1, new ConfigDescription("Index of the button", null, new ConfigurationManagerAttributes { Browsable = false }));
        Input = config.Bind(category, $"{featureName} - Input", "", new ConfigDescription(description, null, new ConfigurationManagerAttributes {
            Order = order,
            CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
            ControllerName = ControllerName,
            ButtonIndex = ButtonIndex
        }));

        if (!AllConfigs.Contains(this))
            AllConfigs.Add(this);
    }
}

internal sealed class RewiredConfigManager {
    private static bool _isListeningForInput = false;
    private static ConfigEntryBase _targetEntry = null;
    private static ConfigEntryBase _targetControllerEntry = null;
    private static ConfigEntryBase _targetIndexEntry = null;
    private static string _errorMessage = null;
    private static float _errorTimer = 0f;

    public static void Update() {
        if (_errorTimer > 0) {
            _errorTimer -= Time.unscaledDeltaTime;
            if (_errorTimer <= 0) _errorMessage = null;
        }

        if (_isListeningForInput) {
            if (ReInput.controllers == null) return;
            
            foreach (var controller in InputCatcher.controllerInputs.Keys) {
                if (controller.GetAnyButtonDown()) {
                    for (int i = 0; i < controller.buttonCount; i++) {
                        if (controller.GetButtonDown(i)) {
                            IList elements = Traverse.Create(controller).Field("KHksquAJKcDEUkNfJQjMANjDEBFB").GetValue<IList>();
                            string controllerName = controller.name.Trim();
                            string buttonName = Traverse.Create(elements[i]).Property("elementIdentifier").GetValue<ControllerElementIdentifier>().name;

                            // Handle special management keys for the config drawer
                            if (controller.type == ControllerType.Keyboard) {
                                string lowerName = buttonName.ToLower();
                                if (lowerName == "escape" || lowerName == "esc") {
                                    _isListeningForInput = false;
                                    _targetEntry = null;
                                    _targetControllerEntry = null;
                                    _targetIndexEntry = null;
                                    _errorMessage = null;
                                    return;
                                }
                                if (lowerName == "delete" || lowerName == "backspace" || lowerName == "suppr" || lowerName == "del") {
                                    _targetEntry.BoxedValue = "";
                                    if (_targetControllerEntry != null) _targetControllerEntry.BoxedValue = "";
                                    if (_targetIndexEntry != null) _targetIndexEntry.BoxedValue = -1;

                                    _isListeningForInput = false;
                                    _targetEntry = null;
                                    _targetControllerEntry = null;
                                    _targetIndexEntry = null;
                                    _errorMessage = null;
                                    return;
                                }
                            }

                            // Conflict check
                            foreach (var config in RewiredInputConfig.AllConfigs) {
                                if (config.Input == _targetEntry) continue;
                                if (config.ControllerName.Value == controllerName && config.ButtonIndex.Value == i) {
                                    string conflictName = config.Input.Definition.Key;
                                    if (conflictName.EndsWith(" - Input")) conflictName = conflictName.Substring(0, conflictName.Length - 8);
                                    _errorMessage = $"Conflict: {conflictName}";
                                    _errorTimer = 3f;
                                    return;
                                }
                            }

                            _targetEntry.BoxedValue = controllerName + " | " + buttonName + " | " + i.ToString();
                            if (_targetControllerEntry != null) _targetControllerEntry.BoxedValue = controllerName;
                            if (_targetIndexEntry != null) _targetIndexEntry.BoxedValue = i;
                            
                            _isListeningForInput = false;
                            _targetEntry = null;
                            _targetControllerEntry = null;
                            _targetIndexEntry = null;
                            _errorMessage = null;
                            return;
                        }
                    }
                }
            }
        }
    }

    public static void RewiredButtonDrawer(ConfigEntryBase entry) {
        if (_isListeningForInput && _targetEntry == entry) {
            GUIUtility.keyboardControl = 0;
            string label = string.IsNullOrEmpty(_errorMessage) ? "Listening... (Press button or ESC)" : _errorMessage;
            if (GUILayout.Button(label, GUILayout.ExpandWidth(true))) {
                _isListeningForInput = false;
                _targetEntry = null;
                _targetControllerEntry = null;
                _targetIndexEntry = null;
                _errorMessage = null;
            }
        }
        else {
            string val = (string)entry.BoxedValue;
            if (string.IsNullOrEmpty(val)) val = "None - Click to bind";
            if (GUILayout.Button(val, GUILayout.ExpandWidth(true))) {
                _isListeningForInput = true;
                _targetEntry = entry;
                _errorMessage = null;
                _errorTimer = 0f;
                
                // lookup of the linked facultative entries
                ConfigurationManagerAttributes attr = entry.Description.Tags?.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
                _targetControllerEntry = attr?.ControllerName as ConfigEntryBase;
                _targetIndexEntry = attr?.ButtonIndex as ConfigEntryBase;
            }
        }
    }
}
