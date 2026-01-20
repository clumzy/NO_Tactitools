using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using NOAutopilot; // Reference from .csproj
using Plugin = NO_Tactitools.Core.Plugin; // Explicit alias to resolve ambiguity

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
public static class NOAutopilotControlPlugin {
    private static bool initialized = false;
    private const string AutopilotModGUID = "com.qwerty1423.NOAutopilot";

    public static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[AP] NOAutopilotControl checking dependencies...");

            if (Chainloader.PluginInfos.ContainsKey(AutopilotModGUID)) {
                Plugin.Log("[AP] Found 'no-autopilot-mod'. Enabling Autopilot controls.");
                Plugin.harmony.PatchAll(typeof(NOAutopilotComponent.OnPlatformStart));
                Plugin.harmony.PatchAll(typeof(NOAutopilotComponent.OnPlatformUpdate));
            }
            else {
                Plugin.Log("[AP] 'no-autopilot-mod' not found. Autopilot controls disabled.");
            }

            initialized = true;
        }
    }
}

public class NOAutopilotComponent {
    static class LogicEngine {
        static public void Init() {
            // Initialization logic if needed
        }

        static public void Update() {
            // Update logic if needed
        }
    }

    public static class InternalState {

    }

    static class DisplayEngine {
        static public void Init() {
            // Initialization logic if needed
        }

        static public void Update() {
            // Update logic if needed
        }
    }

    static class NOAutoPilotMenu {

    }

    static class Bridge {
        
    }

    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart {
        static void Postfix() {
            LogicEngine.Init();
            DisplayEngine.Init();
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Update")]
    public static class OnPlatformUpdate {
        static void Postfix() {
            LogicEngine.Update();
            DisplayEngine.Update();
        }
    }
}