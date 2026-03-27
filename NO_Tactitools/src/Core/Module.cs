using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NO_Tactitools.Core.Bindings;
using UnityEngine;
using NO_Tactitools.Core.Events;
using NO_Tactitools.Core.Inputs;

namespace NO_Tactitools.Core;

public static class ModuleManager {
    private static readonly List<Module> Modules = [];

    public static void TryAddModule(Module module) {
        if (Modules.Contains(module)) {
            Plugin.Log($"Module {module.ModuleName.ToString()} is already registered.");
        }

        if (!module.Enabled) {
            Plugin.Log($"Module {module.ModuleName.ToString()} is disabled.");
        }

        Modules.Add(module);
        Plugin.Log($"Module {module.ModuleName.ToString()} registered.");
    }
}

public enum ModuleInitType {
    TacScreen,
    None
}

public enum ModuleUpdateType {
    TacScreen,
    CombatHUD,
    None
}

/// <summary>
/// Base class for all NOTT modules with support for different update patterns
/// </summary>
public abstract class Module {
    private readonly Plugin pluginInstance;
    public readonly bool Enabled;
    private readonly Dictionary<string, ConfigEntryBase> configEntries;
    public readonly string ModuleName;
    private readonly ModuleInitType initType;
    private readonly ModuleUpdateType updateType;
    private readonly List<RewiredInputConfig> inputConfigs = []; // List of input configs for this module

    // MODULE
    protected Module(
        Plugin pluginInstance,
        string moduleName,
        ModuleInitType initType,
        ModuleUpdateType updateType) {
        // Assign the properties
        this.pluginInstance = pluginInstance;
        ModuleName = moduleName;
        this.initType = initType;
        this.updateType = updateType;
        this.configEntries = [];
        // Bind the config
        ConfigEntry<bool> enabledConfig = this.pluginInstance.Config.Bind(
            $"{ModuleName}",
            $"{ModuleName} - Enabled",
            true,
            new ConfigDescription(
                $"Enable the {ModuleName.ToString()} module.",
                null,
                new ConfigurationManagerAttributes { Order = -9999 }
            ));
        // Initialize events, always runs last
        if (enabledConfig.Value) {
            InitializeEvents();
            Plugin.Log($"Module {ModuleName.ToString()} events registered.");
            Enabled = true;
        }
        else {
            Plugin.Log($"Module {ModuleName.ToString()} is disabled.");
            Enabled = false;
            return; // explicit return for child elements to not run
        }

    }
    
    // DRAWABLE ELEMENT
    protected abstract class DrawableElement {
        private readonly Transform parentTransform;
        protected readonly GameObject gameObject;

        protected DrawableElement(
            Transform parentTransform,
            string drawableName) {
            this.parentTransform = parentTransform;
            gameObject = new GameObject(drawableName + "_Container");
            gameObject.AddComponent<RectTransform>();
            gameObject.transform.SetParent(this.parentTransform, false);
        }

        public void Destroy() {
            if (parentTransform != null) return;
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    protected DrawableElement DrawableElementInstance;

    // where the module stores its internal state in our paradigm
    // data is stored raw as static variables in a child class of InternalState
    protected abstract class InternalState;
    
    private void InitializeEvents() {
        // Subscribe to the events based on the init type
        switch (initType) {
            case ModuleInitType.TacScreen:
                EventSystem.OnTacScreenInit += OnInit;
                break;
            case ModuleInitType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (updateType) {
            case ModuleUpdateType.TacScreen:
                EventSystem.OnTacScreenUpdate += OnUpdate;
                break;
            case ModuleUpdateType.CombatHUD:
                EventSystem.OnCombatHUDFixedUpdate += OnUpdate;
                break;
            case ModuleUpdateType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool MustSkipInit() {
        return initType switch {
            ModuleInitType.TacScreen => false,
            ModuleInitType.None => false,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool MustSkipUpdate() {
        return updateType switch {
            ModuleUpdateType.TacScreen => GameBindings.GameState.IsGamePaused()
                                          || GameBindings.Player.Aircraft.GetAircraft() == null
                                          || UIBindings.Game.GetCombatHUDTransform() == null,
            ModuleUpdateType.CombatHUD => false,
            ModuleUpdateType.None => false,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    // functions to be overridden by child classes
    protected virtual void OnInit(object sender, ModEventArgs e) {
        // Destroy the drawable
        DrawableElementInstance?.Destroy();
        DrawableElementInstance = null;
        // Check for return conditions
        if (MustSkipInit()) return;
        {}
    }

    protected virtual void OnUpdate(object sender, ModEventArgs e) {
        // Check for return conditions
        if (MustSkipUpdate()) return;
        {}
    }

    // Overriding config functions 8)
    public ConfigEntry<T> AddNewConfigEntry<T>(
        string key,
        T defaultValue,
        string description,
        AcceptableValueBase acceptableValues = null,
        params object[] args) {
        // check if a config with the same name doesn't exist
        if (configEntries.ContainsKey(key)) {
            Plugin.Log($"Config entry {key.ToString()} already exists!");
            return null;
        }
        ConfigEntry<T> configEntry = pluginInstance.Config.Bind(
            ModuleName,
            ModuleName + " - " + key,
            defaultValue,
            new ConfigDescription(
                description, 
                acceptableValues, 
                args));
        configEntries.Add(
            key,
            configEntry);
        return configEntry;
    }

    public RewiredInputConfig AddNewInputConfig(
        string featureName,
        string description
    ) {
        // check if a config with the same name doesn't exist
        if (inputConfigs.Any(inputConfig => inputConfig.Input.Definition.Key == featureName)) {
            Plugin.Log($"Input config {featureName.ToString()} already exists!");
            return null;
        }

        RewiredInputConfig inputConfig = new(
            pluginInstance.Config,
            ModuleName,
            featureName,
            description,
            -1000 - inputConfigs.Count // ensuring they are at the bottom in the instantiation order
        );
        inputConfigs.Add(inputConfig);
        return inputConfig;
    }
}