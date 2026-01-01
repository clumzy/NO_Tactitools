using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class TargetListControllerPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[TR] Target List Controller plugin starting !");
            Plugin.harmony.PatchAll(typeof(TargetListControllerComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(TargetListControllerComponent.OnPlatformUpdate));
            InputCatcher.RegisterNewInput(
                Plugin.targetRecallControllerName.Value,
                Plugin.targetRecallInput.Value,
                0.2f,
                onShortPress: TargetListControllerComponent.RecallTargets,
                onLongPress: TargetListControllerComponent.RememberTargets);
            initialized = true;
            Plugin.Log("[TR] Target List Controller plugin succesfully started !");
        }
    }
}

public static class TargetListControllerComponent {
    static class LogicEngine {
        public static void Init() {
            InternalState.targetIndex = 0;
        }

        public static void Update() {
        }
    }

    public static class InternalState {
        public static List<Unit> unitList;
        public static int targetIndex = 0;
    }
    static class DisplayEngine {
        public static void Init() {
        }
        public static void Update() {
        }

    }

    public static void RememberTargets() {
        Plugin.Log($"[TR] HandleLongPress");
        if (Bindings.UI.Game.GetCombatHUD() != null) {
            InternalState.unitList = Bindings.Player.TargetList.GetTargets();
            if (InternalState.unitList.Count == 0) {
                return;
            }
            string report = $"Saved <b>{InternalState.unitList.Count.ToString()}</b> targets";
            Bindings.UI.Game.DisplayToast(report, 3f);
            Bindings.UI.Sound.PlaySound("beep_target.mp3");
        }
    }

    public static void RecallTargets() {
        Plugin.Log($"[TR] HandleClick");
        if (InternalState.unitList != null) {
            if (InternalState.unitList.Count > 0) {
                Bindings.Player.TargetList.DeselectAll();
                Bindings.Player.TargetList.AddTargets(InternalState.unitList);
                InternalState.unitList = Bindings.Player.TargetList.GetTargets();
                string report = $"Recalled <b>{InternalState.unitList.Count.ToString()}</b> targets";
                Bindings.UI.Game.DisplayToast(report, 3f);
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
