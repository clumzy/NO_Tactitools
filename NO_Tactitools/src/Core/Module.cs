using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;

using NO_Tactitools.Core;
using NO_Tactitools.Core.Events;
using NO_Tactitools.Core.Inputs;
using UnityEngine;

namespace NO_Tactitools.Core;

public static class ModuleManager {
    public static List<Module> Modules = [];
    public static bool TryAddModule(Module module) {
        if (Modules.Contains(module)) return false;
        Modules.Add(module);
        return true;
    }
}

public enum ModuleInitType{
    TacScreen,
    None
}

public enum ModuleUpdateType{
    TacScreen,
    CombatHUD,
    None
}


/// <summary>
/// Base class for all NOTT modules with support for different update patterns
/// </summary>
public abstract class Module {
    protected readonly Plugin PluginInstance;
    protected bool Enabled = false;
    protected readonly ConfigEntry<bool> EnabledConfig;
    protected readonly List<ConfigEntryBase> ConfigEntries;
    protected readonly string ModuleName;
    protected readonly ModuleInitType InitType;
    protected readonly ModuleUpdateType UpdateType;
    protected List<RewiredInputConfig> InputConfigs = []; // List of input configs for this module

    protected Module(
        Plugin pluginInstance,
        string moduleName, 
        ModuleInitType initType, 
        ModuleUpdateType updateType) {
        // Assign the properties
        PluginInstance = pluginInstance;
        ModuleName = moduleName;
        InitType = initType;
        UpdateType = updateType;
        // Bind the config
        EnabledConfig = PluginInstance.Config.Bind(
            $"{ModuleName.ToString()}", 
            $"{ModuleName.ToString()} - Enabled", 
            true, 
            new ConfigDescription(
                $"Enable the {ModuleName.ToString()} module.", 
                null,
                new ConfigurationManagerAttributes { Order = -9999 }
            ));
        // Add the module to the module manager if enabled
        if (EnabledConfig.Value) {
            ModuleManager.TryAddModule(this);
        } else {
            Plugin.Logger.LogInfo($"Module {ModuleName.ToString()} is disabled.");
        }
        // Initialize the module if enabled
        if (EnabledConfig.Value) {
            Initialize();
            Plugin.Logger.LogInfo($"Module {ModuleName.ToString()} initialized.");
        }
    }

    private void Initialize() {
        if (EnabledConfig.Value) {
            // Subscribe to the events based on the init type
            switch (InitType) {
                case ModuleInitType.TacScreen:
                    EventSystem.OnTacScreenInit += OnInit;
                    break;
                case ModuleInitType.None:
                    break;
            }
            switch (UpdateType) {
                case ModuleUpdateType.TacScreen:
                    EventSystem.OnTacScreenUpdate += OnUpdate;
                    break;
                case ModuleUpdateType.CombatHUD:
                    EventSystem.OnCombatHUDFixedUpdate += OnUpdate;
                    break;
                case ModuleUpdateType.None:
                    break;
            }
            Enabled = true;
        }
    }
    // functions to be overridden by child classes
    public void OnInit(object sender, ModEventArgs e) {
    }

    public void OnUpdate(object sender, ModEventArgs e) {
    }

    // returns ConfigEntryBase so that we can instantiante module configs as one liners
    public ConfigEntryBase AddNewConfigEntry(ConfigEntryBase configEntry){
        // check if a config with the same name doesn't exist
        if (ConfigEntries.Any(entry => entry.Definition.Key == configEntry.Definition.Key)){
            Plugin.Log($"Config entry {configEntry.Definition.Key.ToString()} already exists!");
            return null;
        }
        PluginInstance.Config.Bind(
            configEntry.Definition, 
            configEntry.BoxedValue, 
            configEntry.Description);
        ConfigEntries.Add(configEntry);
        return configEntry;
    }

    public RewiredInputConfig AddNewInputConfig(
        string featureName,
        string description
    ){
        // check if a config with the same name doesn't exist
        if (InputConfigs.Any(inputConfig => inputConfig.Input.Definition.Key == featureName)){
            Plugin.Log($"Input config {featureName.ToString()} already exists!");
            return null;
        }
        RewiredInputConfig inputConfig = new(
            PluginInstance.Config,
            ModuleName,
            featureName,
            description,
            -1000 - InputConfigs.Count // ensuring they are at the bottom in the instanciation order
        );
        InputConfigs.Add(inputConfig);
        return inputConfig;
    }

}
