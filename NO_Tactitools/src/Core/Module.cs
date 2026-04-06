#nullable enable
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
        // Check if a module of the same type already exists in the list
        if (Modules.Any(m => m.GetType() == module.GetType())) {
            Plugin.Log($"Module {module.ModuleName.ToString()} is already registered.");
            return;
        }

        if (!module.Enabled) {
            Plugin.Log($"Module {module.ModuleName.ToString()} is disabled.");
            return;
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
    private readonly ConfigEntry<bool> enabledConfig;
    public bool Enabled => enabledConfig.Value;
    public bool hasInitialized = false;
    private readonly Dictionary<string, ConfigEntryBase> configEntries;
    public readonly string ModuleName;
    private readonly ModuleInitType initType;
    private readonly ModuleUpdateType updateType;
    private readonly bool hasDrawableElement;
    private readonly List<RewiredInputConfig> inputConfigs = []; // List of input configs for this module
    protected DrawableElement? DrawableElementInstance;

    // MODULE
    protected Module(
        Plugin pluginInstance,
        string moduleName,
        ModuleInitType initType,
        ModuleUpdateType updateType,
        bool hasDrawableElement = false) {
        // Assign the properties
        this.pluginInstance = pluginInstance;
        ModuleName = moduleName;
        this.initType = initType;
        this.updateType = updateType;
        this.hasDrawableElement = hasDrawableElement;
        this.configEntries = [];
        // Bind the config
        enabledConfig = this.pluginInstance.Config.Bind(
            $"{ModuleName}",
            $"{ModuleName} - Enabled",
            true,
            new ConfigDescription(
                $"Enable the {ModuleName.ToString()} module.",
                null,
                new ConfigurationManagerAttributes { Order = 9999 }
            ));
        // Initialize events, always runs last
        InitializeEvents();
        Plugin.Log($"Module {ModuleName.ToString()} events registered.");
        Plugin.Log(enabledConfig.Value
            ? $"Module {ModuleName.ToString()} is enabled."
            : $"Module {moduleName.ToString()} is disabled.");
    }

    // DRAWABLE ELEMENT
    protected abstract class DrawableElement {
        protected readonly Transform parentTransform;
        protected readonly GameObject gameObject;

        protected DrawableElement(
            Transform parentTransform,
            string drawableName) {
            this.parentTransform = parentTransform;
            gameObject = new GameObject("Container_" + drawableName);
            gameObject.AddComponent<RectTransform>();
            gameObject.transform.SetParent(this.parentTransform, false);
        }

        public void Destroy() {
            if (parentTransform != null) return;
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private void InitializeEvents() {
        // Subscribe to the events based on the init type
        switch (initType) {
            case ModuleInitType.TacScreen:
                EventSystem.Events.TacScreen.OnInitialize += OnInit;
                break;
            case ModuleInitType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (updateType) {
            case ModuleUpdateType.TacScreen:
                EventSystem.Events.TacScreen.OnUpdate += OnUpdate;
                break;
            case ModuleUpdateType.CombatHUD:
                EventSystem.Events.CombatHUD.OnFixedUpdate += OnUpdate;
                break;
            case ModuleUpdateType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private bool MustSkipInit() {
        return initType switch {
            ModuleInitType.TacScreen => !Enabled,
            ModuleInitType.None => true,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool MustSkipUpdate() {
        if (hasDrawableElement && DrawableElementInstance == null) 
            return true;
        return updateType switch {
            ModuleUpdateType.TacScreen => !hasInitialized
                                          || GameBindings.GameState.IsGamePaused()
                                          || GameBindings.Player.Aircraft.GetAircraft() == null
                                          || UIBindings.Game.GetCombatHUDTransform() == null,
            ModuleUpdateType.CombatHUD => !Enabled,
            ModuleUpdateType.None => true,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    // functions to be overridden by child classes
    protected virtual void OnInit(object sender, ModEventArgs e) {
        hasInitialized = false;
        // Destroy the drawable
        if (hasDrawableElement) {
            DrawableElementInstance?.Destroy();
            DrawableElementInstance = null;
        }
        // Check for return conditions
        if (MustSkipInit()) return;

        // Proceed with the child class's logic
        OnInitInternal(sender, e);
        hasInitialized = true;
    }

    protected virtual void OnInitInternal(object sender, ModEventArgs e) {
        // This method can be overridden by child classes to provide their logic
    }

    protected virtual void OnUpdate(object sender, ModEventArgs e) {
        // Check for return conditions
        if (MustSkipUpdate()) return;

        // Proceed with the child class's logic
        OnUpdateInternal(sender, e);
    }

    protected virtual void OnUpdateInternal(object sender, ModEventArgs e) {
        // This method can be overridden by child classes to provide their logic
    }

    // Overriding config functions 8)
    public ConfigEntry<T>? AddNewConfigEntry<T>(
        string key,
        T defaultValue,
        string description,
        AcceptableValueBase? acceptableValues = null,
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

    public T? GetConfigValueFromKey<T>(string key) {
        if (configEntries.TryGetValue(key, out ConfigEntryBase? entry)) return ((ConfigEntry<T>)entry).Value;
        Plugin.Log($"Config entry {key.ToString()} doesn't exist!");
        return default(T);
    }

    public RewiredInputConfig? AddNewInputConfig(
        string featureName,
        string description,
        float longPressThreshold = 0.2f,
        Action? onRelease = null,
        Action? onHold = null,
        Action? onLong = null
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
        
        InputCatcher.RegisterNewInput(
            config: inputConfig,
            longPressThreshold: longPressThreshold,
            onRelease : onRelease,
            onHold : onHold,
            onLong: onLong
        );
        return inputConfig;
    }
}