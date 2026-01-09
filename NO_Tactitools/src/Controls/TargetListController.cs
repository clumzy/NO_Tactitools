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
                0.5f,
                onRelease: NextTarget,
                onLongPress: SortTargetsByDistance);
            InputCatcher.RegisterNewInput(
                Plugin.targetPreviousControllerName.Value,
                Plugin.targetPreviousInput.Value,
                0.5f,
                onRelease: PreviousTarget,
                onLongPress: SortTargetsByName);
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
            Bindings.UI.Sound.PlaySound("beep_scroll");
        }
    }

    private static void PreviousTarget() {
        Plugin.Log($"[TC] PreviousTarget");
        int targetCount = Bindings.Player.TargetList.GetTargets().Count;
        if (targetCount > 1) {
            TargetListControllerComponent.InternalState.targetIndex = (TargetListControllerComponent.InternalState.targetIndex + 1) % targetCount;
            TargetListControllerComponent.InternalState.updateDisplay = true;
            Bindings.UI.Sound.PlaySound("beep_scroll");
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
        if (currentTargets.Count > 0 && TargetListControllerComponent.InternalState.targetIndex < currentTargets.Count) {
            Unit targetToKeep = currentTargets[TargetListControllerComponent.InternalState.targetIndex];
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets([targetToKeep]);
            TargetListControllerComponent.InternalState.updateDisplay = true;
        }
    }

    private static void KeepOnlyDataLinkedTargets() {
        Plugin.Log($"[TC] KeepOnlyDataLinkedTargets");
        if (Bindings.Player.TargetList.GetTargets().Count == 0) {
            return;
        }
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        List<Unit> dataLinkedTargets = [];
        foreach (Unit target in currentTargets) {
            if (TargetListControllerComponent.InternalState.playerFactionHQ.IsTargetPositionAccurate(target, 20f)) {
                dataLinkedTargets.Add(target);
            }
        }
        if (dataLinkedTargets.Count >= 0) {
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets(dataLinkedTargets);
            TargetListControllerComponent.InternalState.resetIndex = true;
            TargetListControllerComponent.InternalState.updateDisplay = true;
            Bindings.UI.Game.DisplayToast($"Kept <b>{dataLinkedTargets.Count.ToString()}</b> data linked target" + (dataLinkedTargets.Count > 1 ? "s" : ""), 3f);
        }
    }

    private static void KeepClosestTargetsBasedOnAmmo() {
        Plugin.Log($"[TC] KeepClosestTargetsBasedOnAmmo");
        if (Bindings.Player.TargetList.GetTargets().Count == 0) {
            return;
        }
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
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
        int activeStationAmmo = Bindings.Player.Aircraft.Weapons.GetActiveStationAmmo();
        List<Unit> closestTargets = sortedTargets.GetRange(0, Mathf.Min(activeStationAmmo, sortedTargets.Count));
        List<Unit> targetsToKeep = [.. currentTargets.Where(closestTargets.Contains)];

        if (targetsToKeep.Count >= 0) {
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
            if (Bindings.Player.TargetList.GetTargets().Count == 0) {
                return;
            }
            TargetListControllerComponent.InternalState.unitRecallList = Bindings.Player.TargetList.GetTargets();
            string report = $"Saved <b>{TargetListControllerComponent.InternalState.unitRecallList.Count.ToString()}</b> targets";
            Bindings.UI.Game.DisplayToast(report, 3f);
            Bindings.UI.Sound.PlaySound("beep_target");
        }
    }

    private static void RecallTargets() {
        Plugin.Log($"[TC] HandleClick");
        if (TargetListControllerComponent.InternalState.unitRecallList != null) {
            if (TargetListControllerComponent.InternalState.unitRecallList.Count > 0) {
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

    private static void SortTargetsByDistance() {
        Plugin.Log($"[TC] SortTargetsByDistance");
        if (Bindings.Player.TargetList.GetTargets().Count < 2) {
            return;
        }
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        List<Unit> sortedTargets = [.. currentTargets];
        sortedTargets.Sort((b, a) => { // reverse order for LIFO

            float distanceA = Vector3.Distance(
                Bindings.Player.Aircraft.GetAircraft().transform.position.ToGlobalPosition().AsVector3(),
                (Vector3)(Bindings.Player.Aircraft.GetAircraft().NetworkHQ.GetKnownPosition(a)?.AsVector3()));
            float distanceB = Vector3.Distance(
                Bindings.Player.Aircraft.GetAircraft().transform.position.ToGlobalPosition().AsVector3(),
                (Vector3)(Bindings.Player.Aircraft.GetAircraft().NetworkHQ.GetKnownPosition(b)?.AsVector3()));
            return distanceA.CompareTo(distanceB);
        });
        Bindings.Player.TargetList.DeselectAll();
        Bindings.Player.TargetList.AddTargets(sortedTargets, muteSound: true);
        string report = $"Sorted <b>{sortedTargets.Count.ToString()}</b> targets by distance";
        TargetListControllerComponent.InternalState.resetIndex = true;
        TargetListControllerComponent.InternalState.updateDisplay = true;
        Bindings.UI.Game.DisplayToast(report, 3f);
        Bindings.UI.Sound.PlaySound("beep_sort");
    }

    private static void SortTargetsByName() {
        Plugin.Log($"[TC] SortTargetsByName");
        if (Bindings.Player.TargetList.GetTargets().Count < 2) {
            return;
        }
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        List<Unit> sortedTargets = [.. currentTargets];
        sortedTargets.Sort((b, a) => { // reverse order for LIFO
            return a.unitName.CompareTo(b.unitName);
        });
        Bindings.Player.TargetList.DeselectAll();
        Bindings.Player.TargetList.AddTargets(sortedTargets, muteSound: true);
        string report = $"Sorted <b>{sortedTargets.Count.ToString()}</b> targets by name";
        TargetListControllerComponent.InternalState.resetIndex = true;
        TargetListControllerComponent.InternalState.updateDisplay = true;
        Bindings.UI.Game.DisplayToast(report, 3f);
        Bindings.UI.Sound.PlaySound("beep_sort");
    }
}

public static class TargetListControllerComponent {
    static class LogicEngine {
        public static void Init() {
            InternalState.previousTargetList = [];
            InternalState.targetIndex = 0;
            InternalState.playerFactionHQ = SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;
        }

        public static void Update() {
            int currentCount = Bindings.Player.TargetList.GetTargets().Count;
            if (InternalState.previousTargetList.Count != currentCount || InternalState.resetIndex) {
                if (InternalState.previousTargetList.Count != currentCount) {
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
                }
                InternalState.targetIndex = Mathf.Clamp(InternalState.targetIndex, 0, Mathf.Max(0, currentCount - 1));
                InternalState.previousTargetList = Bindings.Player.TargetList.GetTargets();
                if (InternalState.resetIndex) { // don't forget that the list is in reverse order (LIFO), this is why we set to count - 1
                    InternalState.targetIndex = currentCount - 1;
                    InternalState.resetIndex = false;
                }
                InternalState.updateDisplay = true;
            }
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
    }
    static class DisplayEngine {
        private static readonly TraverseCache<TargetScreenUI, FactionHQ> _hqCache = new("hq");
        private static readonly TraverseCache<TargetScreenUI, Text> _typeTextCache = new("typeText");
        private static readonly TraverseCache<TargetScreenUI, Text> _headingCache = new("heading");
        private static readonly TraverseCache<TargetScreenUI, Text> _altitudeCache = new("altitude");
        private static readonly TraverseCache<TargetScreenUI, Text> _relAltitudeCache = new("rel_altitude");
        private static readonly TraverseCache<TargetScreenUI, Text> _speedCache = new("speed");
        private static readonly TraverseCache<TargetScreenUI, Text> _relSpeedCache = new("rel_speed");
        private static readonly TraverseCache<TargetScreenUI, Text> _pilotTextCache = new("pilotText");
        private static readonly TraverseCache<TargetScreenUI, Text> _distanceCache = new("distance");
        private static readonly TraverseCache<TargetScreenUI, List<Image>> _targetBoxesCache = new("targetBoxes");
        
        public static void Init() {
        }
        public static void Update() {
            static void UpdateTargetTexts() {
                TargetScreenUI targetScreen = Bindings.UI.Game.GetTargetScreenUIComponent();
                if (targetScreen == null) return;

                List<Unit> targets = Bindings.Player.TargetList.GetTargets();
                int index = InternalState.targetIndex;
                if (index >= targets.Count) return;

                Unit unit = targets[index];
                FactionHQ hq = _hqCache.GetValue(targetScreen);

                Text typeText = _typeTextCache.GetValue(targetScreen);
                Text heading = _headingCache.GetValue(targetScreen);
                Text altitude = _altitudeCache.GetValue(targetScreen);
                Text rel_altitude = _relAltitudeCache.GetValue(targetScreen);
                Text speed = _speedCache.GetValue(targetScreen);
                Text rel_speed = _relSpeedCache.GetValue(targetScreen);
                Text pilotText = _pilotTextCache.GetValue(targetScreen);

                Text distance = _distanceCache.GetValue(targetScreen);
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
                Bindings.UI.Game.GetTargetScreenTransform(silent: true) == null) {
                return;
            }

            List<Unit> targets = Bindings.Player.TargetList.GetTargets();
            if (targets.Count == 0) return;

            if (InternalState.updateDisplay) {
                TargetScreenUI targetScreen = Bindings.UI.Game.GetTargetScreenUIComponent();
                List<Image> targetIcons = _targetBoxesCache.GetValue(targetScreen);
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
