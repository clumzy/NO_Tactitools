using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class TargetRecallPlugin {
    private static bool initialized = false;
    private static List<Unit> units;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[TR] Target Recall plugin starting !");
            InputCatcher.RegisterNewInput(
                Plugin.targetRecallControllerName.Value,
                Plugin.targetRecallInput.Value,
                0.2f,
                onShortPress: HandleClick,
                onLongPress: HandleLongPress);
            initialized = true;
            Plugin.Log("[TR] Target Recall plugin succesfully started !");
        }
    }

    private static void HandleLongPress() {
        Plugin.Log($"[TR] HandleLongPress");
        if (Bindings.UI.Game.GetCombatHUD() != null) {
            units = Bindings.Player.TargetList.GetTargets();
            if (units.Count == 0) {
                return;
            }
            string report = $"Saved <b>{units.Count.ToString()}</b> targets";
            Bindings.UI.Game.DisplayToast(report, 3f);
            Bindings.UI.Sound.PlaySound("beep_target.mp3");
        }
    }

    private static void HandleClick() {
        Plugin.Log($"[TR] HandleClick");
        if (units != null) {
            if (units.Count > 0) {
                Bindings.Player.TargetList.DeselectAll();
                Bindings.Player.TargetList.AddTargets(units);
                units = Bindings.Player.TargetList.GetTargets();
                string report = $"Recalled <b>{units.Count.ToString()}</b> targets";
                Bindings.UI.Game.DisplayToast(report, 3f);
            }
        }
    }
}
