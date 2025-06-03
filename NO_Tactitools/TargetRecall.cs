using HarmonyLib;
using System.Collections.Generic;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class TargetRecallPlugin {
    private static bool initialized = false;
    public static List<Unit> units;
    static void Postfix() {
        if (!initialized) {
            Plugin.Logger.LogInfo($"[TR] Target Recall plugin starting !");
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.targetRecallControllerName.Value,
                new ControllerButton(
                Plugin.targetRecallButtonNumber.Value,
                0.2f,
                onShortPress: HandleClick,
                onLongPress: HandleLongPress
                ));
            initialized = true;
            Plugin.Logger.LogInfo("[TR] Target Recall plugin succesfully started !");
        }
    }
    private static void HandleLongPress() {
        Plugin.Logger.LogInfo($"[TR] HandleLongPress");
        if (Plugin.combatHUD != null) {
            units = [.. (List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()];
            SoundManager.PlayInterfaceOneShot(Plugin.selectAudio);
            string report = $"Saved <b>{units.Count.ToString()}</b> targets";
            SceneSingleton<AircraftActionsReport>.i.ReportText(report, 3f);
        }
    }
    private static void HandleClick() {
        Plugin.Logger.LogInfo($"[TR] HandleClick");
        Plugin.Logger.LogInfo($"{Plugin.combatHUD.aircraft.countermeasureManager.activeIndex.ToString()}");
        if (Plugin.combatHUD != null && units != null) {
            if (units.Count > 0) {
                Plugin.combatHUD.DeselectAll(false);
                foreach (Unit t_unit in units) {
                    Plugin.combatHUD.SelectUnit(t_unit);
                }
                units = [.. (List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()];
                string report = $"Recalled <b>{units.Count.ToString()}</b> targets";
                SceneSingleton<AircraftActionsReport>.i.ReportText(report, 2f);
            }
        }
    }
}
