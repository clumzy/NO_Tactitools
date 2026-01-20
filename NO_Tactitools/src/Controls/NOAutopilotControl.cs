using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using NOAutopilot; // Reference from .csproj
using Plugin = NO_Tactitools.Core.Plugin;
using NO_Tactitools.Core;
using System;
using static NO_Tactitools.Core.Bindings.UI.Draw; // Explicit alias to resolve ambiguity

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
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotOpenMenuInput.Value
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotEnterInput.Value
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotUpInput.Value
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotDownInput.Value
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotLeftInput.Value
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotRightInput.Value
                );
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
        static public NOAutoPilotMenu autopilotMenu;

    }

    static class DisplayEngine {
        static public void Init() {
            // Initialization logic if needed
            InternalState.autopilotMenu = new NOAutoPilotMenu();
        }

        static public void Update() {
        }
    }

    public class NOAutoPilotMenu {
        public GameObject containerObject;
        public Transform containerTransform;

        public UIAdvancedRectangleLabeled altitudeBox;

        public NOAutoPilotMenu() {
            Transform parentTransform = Bindings.UI.Game.GetTacScreenTransform();
            string platformName = Bindings.Player.Aircraft.GetPlatformName();
            containerObject = new GameObject("i_ap_NOAutopilotMenu");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parentTransform, false);
            switch (platformName) {
                case "CI-22 Cricket":
                    break;
                case "SAH-46 Chicane":
                    break;
                case "T/A-30 Compass":
                    break;
                case "FS-3 Ternion":
                case "FS-12 Revoker":
                    break;
                case "FS-20 Vortex":
                    break;
                case "KR-67 Ifrit":
                    break;
                case "VL-49 Tarantula":
                    break;
                case "EW-1 Medusa":
                    break;
                case "SFB-81":
                    break;
                case "UH-80 Ibis":
                    break;
                case "A-19 Brawler":
                    break;
                case "FQ-106 Kestrel":
                    break;
                default:
                    break;
            }

        }

    }

    static class Bridge {
        static public float GetTargetAlt() {
            try {
                return APData.TargetAlt;
            }
            catch (NullReferenceException) {
                return 0f;
            }
        }
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