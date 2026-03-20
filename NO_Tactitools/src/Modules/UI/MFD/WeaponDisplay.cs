using NO_Tactitools.Core;
using NO_Tactitools.Core.Inputs;

namespace NO_Tactitools.Modules.UI.MFD;

class WeaponDisplayModule : Module {
    public WeaponDisplayModule(Plugin pluginInstance) : base(
        pluginInstance,
        "Weapon Display",
        ModuleInitType.TacScreen,
        ModuleUpdateType.TacScreen) {
        if (!this.Enabled) return; // exit if module is disabled
        // Register new configs
        // TODO
        // Create Logic and Display Engines
        LogicEngineInstance = new WeaponDisplayLogicEngine();
        DisplayEngineInstance = new WeaponDisplayDisplayEngine();
        // Create Internal State
        InternalStateInstance = new WeaponDisplayInternalState();
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

    // Concrete implementation of LogicEngine
    private class WeaponDisplayLogicEngine : LogicEngine {
        public override void Init() {
            // Initialize logic here
        }

        public override void Update() {
            // Update logic here
        }
    }

    // Concrete implementation of InternalState
    public class WeaponDisplayInternalState : InternalState {
    }

    // Concrete implementation of DisplayEngine
    private class WeaponDisplayDisplayEngine : DisplayEngine {
        public override void Init() {
            // Initialize display here
        }

        public override void Update() {
            // Update display here
        }
    }
}