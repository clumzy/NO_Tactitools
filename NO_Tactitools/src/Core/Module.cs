using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

using NO_Tactitools.Core.Events;
using NO_Tactitools.Core.Inputs;

namespace NO_Tactitools.Core;

public static class ModuleManager {
    private static readonly List<Module> Modules = [];
    public static void TryAddModule(Module module) {
        if (Modules.Contains(module)) {
            Plugin.Log($"Module {module.ModuleName.ToString()} is already registered.");
        }
        if (module.Enabled == false) {
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
    private readonly ConfigEntry<bool> enabledConfig;
    public List<ConfigEntryBase> configEntries;
    public readonly string ModuleName;
    private readonly ModuleInitType initType;
    private readonly ModuleUpdateType updateType;
    private readonly List<RewiredInputConfig> inputConfigs = []; // List of input configs for this module

    // LOGIC ENGINE
    protected abstract class LogicEngine {
        public abstract void Init();
        public abstract void Update();
    }
    protected LogicEngine LogicEngineInstance;

    // DISPLAY ENGINE
    protected abstract class DisplayEngine {
        public abstract void Init();
        public abstract void Update();
    }
    protected DisplayEngine DisplayEngineInstance;

    // where the module stores its internal state in our paradigm
    // data is stored raw as static variables in a child class of InternalState
    protected abstract class InternalState;

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
        enabledConfig = this.pluginInstance.Config.Bind(
            $"{ModuleName}",
            $"{ModuleName} - Enabled",
            true,
            new ConfigDescription(
                $"Enable the {ModuleName} module.",
                null,
                new ConfigurationManagerAttributes { Order = -9999 }
            ));
        // Initialize events, always runs last
        if (enabledConfig.Value) {
            InitializeEvents();
            Plugin.Logger.LogInfo($"Module {ModuleName} events registered.");
        }
        Enabled = true;
    }

    private void InitializeEvents() {
        if (!enabledConfig.Value) return;
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

    // functions to be overridden by child classes
    private void OnInit(object sender, ModEventArgs e) {
        LogicEngineInstance.Init();
        DisplayEngineInstance.Init();
    }

    private void OnUpdate(object sender, ModEventArgs e) {
        LogicEngineInstance.Update();
        DisplayEngineInstance.Update();
    }

    // returns ConfigEntryBase so that we can instantiate module configs as one-liners
    public ConfigEntryBase AddNewConfigEntry(ConfigEntryBase configEntry) {
        // check if a config with the same name doesn't exist
        if (configEntries.Any(entry => entry.Definition.Key == configEntry.Definition.Key)) {
            Plugin.Log($"Config entry {configEntry.Definition.Key.ToString()} already exists!");
            return null;
        }
        pluginInstance.Config.Bind(
            configEntry.Definition,
            configEntry.BoxedValue,
            configEntry.Description);
        configEntries.Add(configEntry);
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