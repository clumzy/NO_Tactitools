using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;
using UnityEngine.UI;
using UnityEngine;
using NuclearOption.SceneLoading;

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
                onRelease: TargetListControllerComponent.RecallTargets,
                onLongPress: TargetListControllerComponent.RememberTargets);
            initialized = true;
            Plugin.Log("[TR] Target List Controller plugin succesfully started !");
        }
    }
}

public static class TargetListControllerComponent {
    static class LogicEngine {
        public static void Init() {
            InternalState.previousTargetList = [];
            InternalState.targetIndex = 0;
        }

        public static void Update() {
            if (InternalState.previousTargetList != Bindings.Player.TargetList.GetTargets()) {
                InternalState.previousTargetList = Bindings.Player.TargetList.GetTargets();
                InternalState.updateDisplay = true;
                InternalState.targetIndex = InternalState.updateDisplay ? InternalState.previousTargetList.Count - 1 : 0;
            }
            else {
                InternalState.updateDisplay = false;
            }
        }
    }

    public static class InternalState {
        public static List<Unit> unitRecallList;
        public static List <Unit> previousTargetList;
        public static bool updateDisplay = false;
        public static int targetIndex = 0;
    }
    static class DisplayEngine {
        public static void Init() {
        }
        public static void Update() {
            if (Bindings.UI.Game.GetCombatHUDTransform() != null && 
                Bindings.Player.TargetList.GetTargets().Count > 0 &&
                Bindings.UI.Game.GetTargetScreenTransform(nullIsOkay: true) != null &&
                InternalState.updateDisplay
                ) {
                TargetScreenUI targetScreen = Bindings.UI.Game.GetTargetScreenUIComponent();
                List<Image> targetIcons = Traverse.Create(targetScreen).Field("targetBoxes").GetValue<List<Image>>();
                for (int i = 0; i < targetIcons.Count; i++) {
                    // Get or Add Outline component
                    Outline outline = targetIcons[i].GetComponent<Outline>() ?? targetIcons[i].gameObject.AddComponent<Outline>();
                    if (i == InternalState.targetIndex) {
                        targetIcons[i].color = Color.green;
                        targetIcons[i].transform.localScale = new Vector3(.6f, .6f, 1f);
                        
                        // Configure Outline for selected item
                        outline.enabled = true;
                        outline.effectColor = Color.green;
                        outline.effectDistance = new Vector2(1f, -1f); // Adjust this value for thickness
                    } else {
                        targetIcons[i].color = Color.white;
                        targetIcons[i].transform.localScale = new Vector3(.5f, .5f, 1f);
                        
                        // Disable Outline for others
                        outline.enabled = false;
                    }
                }
            }
        }

    }

    public static void RememberTargets() {
        Plugin.Log($"[TR] HandleLongPress");
        if (Bindings.UI.Game.GetCombatHUDTransform() != null) {
            InternalState.unitRecallList = Bindings.Player.TargetList.GetTargets();
            if (InternalState.unitRecallList.Count == 0) {
                return;
            }
            string report = $"Saved <b>{InternalState.unitRecallList.Count.ToString()}</b> targets";
            Bindings.UI.Game.DisplayToast(report, 3f);
            Bindings.UI.Sound.PlaySound("beep_target.mp3");
        }
    }

    public static void RecallTargets() {
        Plugin.Log($"[TR] HandleClick");
        if (InternalState.unitRecallList != null) {
            if (InternalState.unitRecallList.Count > 0) {
                Bindings.Player.TargetList.DeselectAll();
                Bindings.Player.TargetList.AddTargets(InternalState.unitRecallList);
                InternalState.unitRecallList = Bindings.Player.TargetList.GetTargets();
                string report = $"Recalled <b>{InternalState.unitRecallList.Count.ToString()}</b> targets";
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
