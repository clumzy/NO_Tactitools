using NO_Tactitools.Core;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Events;
using NO_Tactitools.Core.Inputs;
using UnityEngine;
using UnityEngine.UI;

namespace NO_Tactitools.Modules.UI;

internal class UIRecolorModule : Module {
    public UIRecolorModule(Plugin pluginInstance) : base(
        pluginInstance: pluginInstance,
        moduleName: "UI Recolor",
        initType: ModuleInitType.TacScreen,
        updateType: ModuleUpdateType.TacScreen) {
        // Register events
        
    }
}