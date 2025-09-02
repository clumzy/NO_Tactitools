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
            InputCatcher.RegisterControllerButton(
                Plugin.targetRecallControllerName.Value,
                new ControllerButton(
                Plugin.targetRecallButtonNumber.Value,
                0.2f,
                onShortPress: HandleClick,
                onLongPress: HandleLongPress
                ));
            initialized = true;
            Plugin.Log("[TR] Target Recall plugin succesfully started !");
        }
    }

    private static void HandleLongPress() {
        Plugin.Log($"[TR] HandleLongPress");
        if (SceneSingleton<CombatHUD>.i != null) {
            units = [.. (List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()];
            if (units.Count == 0) {
                return;
            }
            string report = $"Saved <b>{units.Count.ToString()}</b> targets";
            UIUtils.DisplayToast(report, 3f);
            UIUtils.PlaySound("beep_target.mp3");
        }
    }

    private static void HandleClick() {
        Plugin.Log($"[TR] HandleClick");
        if (SceneSingleton<CombatHUD>.i != null && units != null) {
            if (units.Count > 0) {
                SceneSingleton<CombatHUD>.i.DeselectAll(false);
                foreach (Unit t_unit in units) {
                    SceneSingleton<CombatHUD>.i.SelectUnit(t_unit);
                }
                units = [.. (List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()];
                string report = $"Recalled <b>{units.Count.ToString()}</b> targets";
                UIUtils.DisplayToast(report, 3f);
            }
        }
    }
}
