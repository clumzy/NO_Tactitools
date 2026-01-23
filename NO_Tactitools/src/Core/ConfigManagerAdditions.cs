using System;
using BepInEx.Configuration;
using UnityEngine;
using Rewired;
using System.Collections.Generic;
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

internal sealed class RewiredConfigManager {
    private static bool _isListeningForInput = false;
    private static ConfigEntryBase _targetEntry = null;
    private static ConfigEntryBase _targetControllerEntry = null;
    private static ConfigEntryBase _targetIndexEntry = null;

    public static void Update() {
        if (_isListeningForInput) {
            if (ReInput.controllers == null) return;
            foreach (var controller in InputCatcher.controllerInputs.Keys) {
                if (controller.GetAnyButtonDown()) {
                    for (int i = 0; i < controller.buttonCount; i++) {
                        if (controller.GetButtonDown(i)) {
                            IList elements = Traverse.Create(controller).Field("KHksquAJKcDEUkNfJQjMANjDEBFB").GetValue<IList>();
                            string controllerName = controller.name.Trim();
                            string buttonName = Traverse.Create(elements[i]).Property("elementIdentifier").GetValue<ControllerElementIdentifier>().name;
                            
                            // 1. Maintain the full composite string as it was
                            _targetEntry.BoxedValue = controllerName + "|" + buttonName + "|" + i.ToString();

                            // 2. Simple assignment to facultative linked configs
                            if (_targetControllerEntry != null) _targetControllerEntry.BoxedValue = controllerName;
                            if (_targetIndexEntry != null) _targetIndexEntry.BoxedValue = i;
                            
                            _isListeningForInput = false;
                            _targetEntry = null;
                            _targetControllerEntry = null;
                            _targetIndexEntry = null;
                            return;
                        }
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) {
                _isListeningForInput = false;
                _targetEntry = null;
                _targetControllerEntry = null;
                _targetIndexEntry = null;
            }
        }
    }

    public static void RewiredButtonDrawer(ConfigEntryBase entry) {
        if (_isListeningForInput && _targetEntry == entry) {
            GUIUtility.keyboardControl = 0;
            if (GUILayout.Button("Listening... (Press button or ESC)", GUILayout.ExpandWidth(true))) {
                _isListeningForInput = false;
                _targetEntry = null;
                _targetControllerEntry = null;
                _targetIndexEntry = null;
            }
        }
        else {
            string val = (string)entry.BoxedValue;
            if (string.IsNullOrEmpty(val)) val = "None - Click to bind";
            if (GUILayout.Button(val, GUILayout.ExpandWidth(true))) {
                _isListeningForInput = true;
                _targetEntry = entry;
                
                // Abstracted lookup of the linked facultative entries
                var attr = entry.Description.Tags?.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
                _targetControllerEntry = attr?.ControllerName as ConfigEntryBase;
                _targetIndexEntry = attr?.ButtonIndex as ConfigEntryBase;
            }
        }
    }
}
