using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

using NO_Tactitools.Core.Events;

namespace NO_Tactitools.Core;

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
    protected readonly ConfigEntry<bool> EnabledConfig;
    protected readonly string ModuleName;
    protected readonly ModuleInitType InitType;
    protected readonly ModuleUpdateType UpdateType;
    protected readonly Harmony Instance;

    protected Module(
        string moduleName, 
        ModuleInitType initType, 
        ModuleUpdateType updateType, 
        ConfigEntry<bool> enabledConfig, 
        Harmony instance) {
        // Assign the properties
        ModuleName = moduleName;
        InitType = initType;
        UpdateType = updateType;
        EnabledConfig = enabledConfig;
        Instance = instance;
        
        // Subscribe to the events based on the init type
    }

    public void Initialize() {
        if (EnabledConfig.Value) {
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
        }
    }
    public void OnInit(object sender, ModEventArgs e) {
    }

    public void OnUpdate(object sender, ModEventArgs e) {
    }
}