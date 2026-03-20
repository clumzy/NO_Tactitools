using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Inputs;
using BepInEx.Configuration;

namespace NO_Tactitools.Modules.UI.MFD;

class WeaponDisplayModule : Module {
    public WeaponDisplayModule(Plugin pluginInstance) : base(
        pluginInstance,
        "Weapon Display",
        ModuleInitType.TacScreen,
        ModuleUpdateType.TacScreen) {
        if (!this.Enabled) return; // exit if module is disabled
        // Register new configs
        // Add new inputs
        InputCatcher.RegisterNewInput(
            AddNewInputConfig(
                "Toggle Screens",
                "Press to toggle."
            ),
            0.2f,
            onLongPress: HandleDisplayToggle
        );
    }

    private static void HandleDisplayToggle() {
    }
}