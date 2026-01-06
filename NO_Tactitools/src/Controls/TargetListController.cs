using HarmonyLib;
using System.Collections.Generic;
using NO_Tactitools.Core;
using UnityEngine.UI;
using UnityEngine;
using NuclearOption.SceneLoading;
using Unity.Properties;
using System.Linq;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class TargetListControllerPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[TC] Target List Controller plugin starting !");
            Plugin.harmony.PatchAll(typeof(TargetListControllerComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(TargetListControllerComponent.OnPlatformUpdate));
            InputCatcher.RegisterNewInput(
                Plugin.targetRecallControllerName.Value,
                Plugin.targetRecallInput.Value,
                0.2f,
                onRelease: RecallTargets,
                onLongPress: RememberTargets);
            InputCatcher.RegisterNewInput(
                Plugin.targetNextControllerName.Value,
                Plugin.targetNextInput.Value,
                0.2f,
                onRelease: NextTarget,
                onLongPress: Redo);
            InputCatcher.RegisterNewInput(
                Plugin.targetPreviousControllerName.Value,
                Plugin.targetPreviousInput.Value,
                0.2f,
                onRelease: PreviousTarget,
                onLongPress: Undo);
            InputCatcher.RegisterNewInput(
                Plugin.targetPopOrKeepControllerName.Value,
                Plugin.targetPopOrKeepInput.Value,
                0.2f,
                onRelease: PopCurrentTarget,
                onLongPress: KeepOnlyCurrentTarget);
            InputCatcher.RegisterNewInput(
                Plugin.targetSmartControlControllerName.Value,
                Plugin.targetSmartControlInput.Value,
                0.2f,
                onRelease: KeepOnlyDataLinkedTargets,
                onLongPress: KeepClosestTargetsBasedOnAmmo);
            initialized = true;
            Plugin.Log("[TC] Target List Controller plugin succesfully started !");
        }
    }

    private static void NextTarget() {
        Plugin.Log($"[TC] NextTarget");
        int targetCount = Bindings.Player.TargetList.GetTargets().Count;
        if (targetCount > 1) {
            TargetListControllerComponent.InternalState.targetIndex = (TargetListControllerComponent.InternalState.targetIndex - 1 + targetCount) % targetCount;
            TargetListControllerComponent.InternalState.updateDisplay = true;
            Bindings.UI.Sound.PlaySound("beep_scroll.mp3");
        }
    }

    private static void PreviousTarget() {
        Plugin.Log($"[TC] PreviousTarget");
        int targetCount = Bindings.Player.TargetList.GetTargets().Count;
        if (targetCount > 1) {
            TargetListControllerComponent.InternalState.targetIndex = (TargetListControllerComponent.InternalState.targetIndex + 1) % targetCount;
            TargetListControllerComponent.InternalState.updateDisplay = true;
            Bindings.UI.Sound.PlaySound("beep_scroll.mp3");
        }
    }

    private static void PopCurrentTarget() {
        Plugin.Log($"[TC] DeselectCurrentTarget");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        if (currentTargets.Count > 0 && TargetListControllerComponent.InternalState.targetIndex < currentTargets.Count) {
            Unit targetToDeselect = currentTargets[TargetListControllerComponent.InternalState.targetIndex];
            TargetListControllerComponent.InternalState.targetIndex = Mathf.Clamp(TargetListControllerComponent.InternalState.targetIndex, 0, Mathf.Max(0, currentTargets.Count - 1));
            Bindings.Player.TargetList.DeselectUnit(targetToDeselect);
            TargetListControllerComponent.InternalState.updateDisplay = true;
        }
    }

    private static void KeepOnlyCurrentTarget() {
        Plugin.Log($"[TC] KeepOnlyCurrentTarget");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        if (currentTargets.Count > 1 && TargetListControllerComponent.InternalState.targetIndex < currentTargets.Count) {
            Unit targetToKeep = currentTargets[TargetListControllerComponent.InternalState.targetIndex];
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets([targetToKeep]);
            TargetListControllerComponent.InternalState.updateDisplay = true;
        }
    }

    private static void KeepOnlyDataLinkedTargets() {
        Plugin.Log($"[TC] KeepOnlyDataLinkedTargets");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        if (currentTargets.Count <= 1) return;
        List<Unit> dataLinkedTargets = [];
        foreach (Unit target in currentTargets) {
            if (TargetListControllerComponent.InternalState.playerFactionHQ.IsTargetPositionAccurate(target, 20f)) {
                dataLinkedTargets.Add(target);
            }
        }
        if (dataLinkedTargets.Count >= 0) {
            TargetListControllerComponent.InternalState.nextChangeActionType = TargetListControllerComponent.TargetActionType.SmartDataLink;
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets(dataLinkedTargets);
            TargetListControllerComponent.InternalState.resetIndex = true;
            TargetListControllerComponent.InternalState.updateDisplay = true;
            Bindings.UI.Game.DisplayToast($"Kept <b>{dataLinkedTargets.Count.ToString()}</b> data linked target" + (dataLinkedTargets.Count > 1 ? "s" : ""), 3f);
        }
    }

    private static void KeepClosestTargetsBasedOnAmmo() {
        int count = Bindings.Player.Aircraft.Weapons.GetActiveStationAmmo();
        if (count == 0) {
            return;
        }
        Plugin.Log($"[TC] KeepClosestTargetsBasedOnAmmo");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        if (currentTargets.Count <= 1) return;
        List<Unit> sortedTargets = [.. currentTargets];
        sortedTargets.Sort((a, b) => {

            float distanceA = Vector3.Distance(
                Bindings.Player.Aircraft.GetAircraft().transform.position.ToGlobalPosition().AsVector3(),
                (Vector3)(Bindings.Player.Aircraft.GetAircraft().NetworkHQ.GetKnownPosition(a)?.AsVector3()));
            float distanceB = Vector3.Distance(
                Bindings.Player.Aircraft.GetAircraft().transform.position.ToGlobalPosition().AsVector3(),
                (Vector3)(Bindings.Player.Aircraft.GetAircraft().NetworkHQ.GetKnownPosition(b)?.AsVector3()));
            return distanceA.CompareTo(distanceB);
        });
        List<Unit> closestTargets = sortedTargets.GetRange(0, Mathf.Min(count, sortedTargets.Count));
        List<Unit> targetsToKeep = [.. currentTargets.Where(closestTargets.Contains)];

        if (targetsToKeep.Count >= 0) {
            TargetListControllerComponent.InternalState.nextChangeActionType = TargetListControllerComponent.TargetActionType.SmartAmmo;
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets(targetsToKeep);
            TargetListControllerComponent.InternalState.resetIndex = true;
            TargetListControllerComponent.InternalState.updateDisplay = true;
            Bindings.UI.Game.DisplayToast($"Kept <b>{targetsToKeep.Count.ToString()}</b> closest targets", 3f);
        }
    }

    private static void RememberTargets() {
        Plugin.Log($"[TC] HandleLongPress");
        if (Bindings.UI.Game.GetCombatHUDTransform() != null) {
            TargetListControllerComponent.InternalState.unitRecallList = Bindings.Player.TargetList.GetTargets();
            if (TargetListControllerComponent.InternalState.unitRecallList.Count == 0) {
                return;
            }
            string report = $"Saved <b>{TargetListControllerComponent.InternalState.unitRecallList.Count.ToString()}</b> targets";
            Bindings.UI.Game.DisplayToast(report, 3f);
            Bindings.UI.Sound.PlaySound("beep_target.mp3");
        }
    }

    private static void RecallTargets() {
        Plugin.Log($"[TC] HandleClick");
        if (TargetListControllerComponent.InternalState.unitRecallList != null) {
            if (TargetListControllerComponent.InternalState.unitRecallList.Count > 0) {
                TargetListControllerComponent.InternalState.nextChangeActionType = TargetListControllerComponent.TargetActionType.Explicit;
                Bindings.Player.TargetList.DeselectAll();
                Bindings.Player.TargetList.AddTargets(TargetListControllerComponent.InternalState.unitRecallList);
                TargetListControllerComponent.InternalState.unitRecallList = Bindings.Player.TargetList.GetTargets();
                string report = $"Recalled <b>{TargetListControllerComponent.InternalState.unitRecallList.Count.ToString()}</b> targets";
                TargetListControllerComponent.InternalState.resetIndex = true;
                TargetListControllerComponent.InternalState.updateDisplay = true;
                Bindings.UI.Game.DisplayToast(report, 3f);
            }
        }
    }

    private static void Undo() {
        if (TargetListControllerComponent.InternalState.historyIndex > 0) {
            Plugin.Log("[TC] Undo");
            // Check if the previous state is empty
            TargetListControllerComponent.HistoryItem previousState = TargetListControllerComponent.InternalState.history[TargetListControllerComponent.InternalState.historyIndex - 1];
            if (previousState.targets.Count == 0) {
                return;
            }

            TargetListControllerComponent.InternalState.historyIndex--;
            ApplyState(previousState.targets);
            // Restore the action type that created this state, so we can potentially resume it
            TargetListControllerComponent.InternalState.lastActionType = previousState.actionType;
            
            Bindings.UI.Sound.PlaySound("beep_undo_redo.mp3");
        }
    }

    private static void Redo() {
        if (TargetListControllerComponent.InternalState.historyIndex < TargetListControllerComponent.InternalState.history.Count - 1) {
            Plugin.Log("[TC] Redo");
            TargetListControllerComponent.InternalState.historyIndex++;
            TargetListControllerComponent.HistoryItem targetState = TargetListControllerComponent.InternalState.history[TargetListControllerComponent.InternalState.historyIndex];
            ApplyState(targetState.targets);
            TargetListControllerComponent.InternalState.lastActionType = targetState.actionType;

            Bindings.UI.Sound.PlaySound("beep_undo_redo.mp3");
        }
    }

    private static void ApplyState(List<Unit> targets) {
        TargetListControllerComponent.InternalState.isRestoringHistory = true;
        Bindings.Player.TargetList.DeselectAll();
        Bindings.Player.TargetList.AddTargets(targets, muteSound: true);
        TargetListControllerComponent.InternalState.resetIndex = true;
        TargetListControllerComponent.InternalState.updateDisplay = true;
    }
}

public static class TargetListControllerComponent {
    static class LogicEngine {
        public static void Init() {
            InternalState.targetIndex = 0;
            InternalState.playerFactionHQ = SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;

            InternalState.previousTargetList = Bindings.Player.TargetList.GetTargets();
            InternalState.history.Clear();
            InternalState.history.Add(new HistoryItem([.. InternalState.previousTargetList], TargetActionType.None));
            InternalState.historyIndex = 0;
            InternalState.lastActionType = TargetActionType.None;
            InternalState.nextChangeActionType = TargetActionType.None;
            InternalState.isRestoringHistory = false;
        }

        public static void Update() {
            List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
            bool listChanged = false;

            if (InternalState.previousTargetList.Count != currentTargets.Count || !InternalState.previousTargetList.SequenceEqual(currentTargets)) {
                listChanged = true;
            }

            if (listChanged) {
                if (InternalState.isRestoringHistory) {
                    InternalState.isRestoringHistory = false;
                }
                else {
                    bool isPassiveUpdate = false;
                    // Detect if targets were removed because they were destroyed (null)
                    if (InternalState.previousTargetList.Count > currentTargets.Count) {
                        IEnumerable<Unit> missing = InternalState.previousTargetList.Except(currentTargets);
                        if (missing.All(u => u == null)) {
                            isPassiveUpdate = true;
                        }
                    }
                    if (isPassiveUpdate) {
                        // update the current history state to reflect the reality (dead units gone)
                        // do not trigger divergence or new history entry
                        if (InternalState.historyIndex >= 0 && InternalState.historyIndex < InternalState.history.Count) {
                            InternalState.history[InternalState.historyIndex] = new HistoryItem([.. currentTargets], InternalState.history[InternalState.historyIndex].actionType);
                        }
                    }
                    else {
                        TargetActionType currentAction = TargetActionType.None;

                        if (InternalState.nextChangeActionType != TargetActionType.None) {
                            currentAction = InternalState.nextChangeActionType;
                            InternalState.nextChangeActionType = TargetActionType.None;
                        }
                        else if (currentTargets.Count > InternalState.previousTargetList.Count) {
                            currentAction = TargetActionType.Adding;
                        }
                        else if (currentTargets.Count < InternalState.previousTargetList.Count) {
                            currentAction = TargetActionType.Removing;
                        }
                        else {
                            currentAction = TargetActionType.Explicit;
                        }

                        if (InternalState.historyIndex < InternalState.history.Count - 1) {
                            InternalState.history.RemoveRange(InternalState.historyIndex + 1, InternalState.history.Count - (InternalState.historyIndex + 1));
                            // ig we diverge, we keep the lastActionType of the current state, because we might be continuing the same action
                            // Add -> Undo -> Add (should merge)
                        }

                        bool merge = false;
                        if (currentAction == InternalState.lastActionType && currentAction != TargetActionType.Explicit && currentAction != TargetActionType.None) {
                            merge = true;
                        }

                        if (merge && InternalState.historyIndex >= 0) {
                            InternalState.history[InternalState.historyIndex] = new HistoryItem([.. currentTargets], currentAction);
                        }
                        else {
                            InternalState.history.Add(new HistoryItem([.. currentTargets], currentAction));
                            InternalState.historyIndex++;
                            InternalState.lastActionType = currentAction;

                            if (InternalState.history.Count > 10) {
                                InternalState.history.RemoveAt(0);
                                InternalState.historyIndex--;
                            }
                        }
                    }
                }
                // TARGET FOCUSING CODE
                int currentCount = currentTargets.Count;
                if (currentCount <= 1) {
                    InternalState.targetIndex = 0;
                }
                else {
                    if (currentCount > InternalState.previousTargetList.Count) {
                        InternalState.targetIndex++;
                    }
                    else {
                        InternalState.targetIndex--;
                    }
                }
                InternalState.targetIndex = Mathf.Clamp(InternalState.targetIndex, 0, Mathf.Max(0, currentCount - 1));
                InternalState.previousTargetList = [.. currentTargets];
                if (InternalState.resetIndex) { // don't forget that the list is in reverse order (LIFO), this is why we set to count - 1
                    InternalState.targetIndex = currentCount - 1;
                    InternalState.resetIndex = false;
                }
                InternalState.updateDisplay = true;
            }
        }
    }

    public enum TargetActionType {
        None,
        Adding,
        Removing,
        Explicit,
        SmartDataLink,
        SmartAmmo
    }

    public struct HistoryItem {
        public List<Unit> targets;
        public TargetActionType actionType;

        public HistoryItem(List<Unit> t, TargetActionType a) {
            targets = t;
            actionType = a;
        }
    }

    public static class InternalState {
        public static List<Unit> unitRecallList;
        public static FactionHQ playerFactionHQ;
        public static List<Unit> previousTargetList;
        public static Color mainColor = Color.green;
        public static bool updateDisplay = false;
        public static bool resetIndex = false;
        public static int targetIndex = 0;

        public static List<HistoryItem> history = [];
        public static int historyIndex = -1;
        public static TargetActionType lastActionType = TargetActionType.None;
        public static TargetActionType nextChangeActionType = TargetActionType.None;
        public static bool isRestoringHistory = false;
    }
    static class DisplayEngine {
        public static void Init() {
        }
        public static void Update() {
            static void UpdateTargetTexts() {
                TargetScreenUI targetScreen = Bindings.UI.Game.GetTargetScreenUIComponent();
                if (targetScreen == null) return;
                Traverse traverse = Traverse.Create(targetScreen);

                List<Unit> targets = Bindings.Player.TargetList.GetTargets();
                int index = InternalState.targetIndex;
                if (index >= targets.Count) return;

                Unit unit = targets[index];
                FactionHQ hq = traverse.Field("hq").GetValue<FactionHQ>();

                Text typeText = traverse.Field("typeText").GetValue<Text>();
                Text heading = traverse.Field("heading").GetValue<Text>();
                Text altitude = traverse.Field("altitude").GetValue<Text>();
                Text rel_altitude = traverse.Field("rel_altitude").GetValue<Text>();
                Text speed = traverse.Field("speed").GetValue<Text>();
                Text rel_speed = traverse.Field("rel_speed").GetValue<Text>();
                Text pilotText = traverse.Field("pilotText").GetValue<Text>();

                Text distance = traverse.Field("distance").GetValue<Text>();
                /* Text bearingText = traverse.Field("bearingText").GetValue<Text>();
                Image bearingImg = traverse.Field("bearingImg").GetValue<Image>(); */

                bool isAirOrMissile = unit is Aircraft || unit is Missile;

                if (unit.NetworkHQ == null) {
                    typeText.color = Color.white;
                }
                else {
                    typeText.color = (unit.NetworkHQ == hq) ? GameAssets.i.HUDFriendly : GameAssets.i.HUDHostile;
                }

                if (isAirOrMissile) {
                    Aircraft aircraft = unit as Aircraft;
                    if (aircraft != null && aircraft.pilots[0].player != null) {
                        pilotText.gameObject.SetActive(true);
                        pilotText.text = "Pilot : " + aircraft.pilots[0].player.PlayerName;
                        pilotText.color = typeText.color;
                    }
                    else {
                        pilotText.gameObject.SetActive(false);
                    }
                }
                else {
                    pilotText.gameObject.SetActive(false);
                }

                if (hq.IsTargetPositionAccurate(unit, 20f) && isAirOrMissile) {
                    GlobalPosition globalPos = unit.GlobalPosition();
                    Vector3 relPos = globalPos - SceneSingleton<CombatHUD>.i.aircraft.GlobalPosition();

                    heading.text = string.Format("HDG {0:F0}°", unit.transform.eulerAngles.y);
                    altitude.text = "ALT " + UnitConverter.AltitudeReading(globalPos.y);
                    rel_altitude.text = "REL " + UnitConverter.AltitudeReading(relPos.y);
                    speed.text = "SPD " + UnitConverter.SpeedReading(unit.speed);
                    rel_speed.text = "REL " + UnitConverter.SpeedReading(Vector3.Dot(SceneSingleton<CombatHUD>.i.aircraft.rb.velocity, relPos.normalized) - Vector3.Dot(unit.rb.velocity, relPos.normalized));
                }
                else {
                    heading.text = "HDG -";
                    altitude.text = "ALT -";
                    rel_altitude.text = "REL -";
                    speed.text = "SPD -";
                    rel_speed.text = "REL -";
                }
                distance.text = "RNG " + UnitConverter.DistanceReading(Vector3.Distance(SceneSingleton<CombatHUD>.i.aircraft.transform.position, unit.transform.position));
                typeText.text = string.Format("[{0}/{1}] ", targets.Count - index, targets.Count) + ((unit is Aircraft) ? unit.definition.unitName : unit.unitName);
            }
            // PROPER UPDATE START
            if (Bindings.UI.Game.GetCombatHUDTransform() == null ||
                Bindings.UI.Game.GetTargetScreenTransform(nullIsOkay: true) == null) {
                return;
            }

            List<Unit> targets = Bindings.Player.TargetList.GetTargets();
            if (targets.Count == 0) return;

            if (InternalState.updateDisplay) {
                TargetScreenUI targetScreen = Bindings.UI.Game.GetTargetScreenUIComponent();
                List<Image> targetIcons = Traverse.Create(targetScreen).Field("targetBoxes").GetValue<List<Image>>();
                // Wait until the UI has instantiated the boxes for the new targets
                if (targetIcons.Count < targets.Count) {
                    return;
                }
                for (int i = 0; i < targetIcons.Count; i++) {

                    Rect rect = targetIcons[i].rectTransform.rect;
                    Vector2 size = rect.size + new Vector2(10f, 10f);
                    Vector2 halfSize = size / 2f;

                    Bindings.UI.Draw.UIAdvancedRectangle selectionRect = new(
                        "SelectionOutline",
                        -halfSize,
                        halfSize,
                        InternalState.mainColor,
                        4f,
                        targetIcons[i].transform,
                        Color.clear
                    );
                    selectionRect.GetImageComponent().raycastTarget = false;

                    if (i == InternalState.targetIndex) {
                        selectionRect.GetGameObject().SetActive(true);
                    }
                    else {
                        selectionRect.GetGameObject().SetActive(false);
                    }
                }
                InternalState.updateDisplay = false;
            }

            if (targets.Count > 1) {
                UpdateTargetTexts();
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
