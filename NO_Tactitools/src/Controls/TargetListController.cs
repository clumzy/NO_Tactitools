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
            Plugin.Log($"[TR] Target List Controller plugin starting !");
            Plugin.harmony.PatchAll(typeof(TargetListControllerComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(TargetListControllerComponent.OnPlatformUpdate));
            InputCatcher.RegisterNewInput(
                Plugin.targetRecallControllerName.Value,
                Plugin.targetRecallInput.Value,
                0.2f,
                onRelease: TargetListControllerComponent.RecallTargets,
                onLongPress: TargetListControllerComponent.RememberTargets);
            InputCatcher.RegisterNewInput(
                Plugin.targetNextControllerName.Value,
                Plugin.targetNextInput.Value,
                0.2f,
                onRelease: TargetListControllerComponent.NextTarget);
            InputCatcher.RegisterNewInput(
                Plugin.targetPreviousControllerName.Value,
                Plugin.targetPreviousInput.Value,
                0.2f,
                onRelease: TargetListControllerComponent.PreviousTarget);
            InputCatcher.RegisterNewInput(
                Plugin.targetPopOrKeepControllerName.Value,
                Plugin.targetPopOrKeepInput.Value,
                0.2f,
                onRelease: TargetListControllerComponent.PopCurrentTarget,
                onLongPress: TargetListControllerComponent.KeepOnlyCurrentTarget);
            InputCatcher.RegisterNewInput(
                Plugin.targetSmartControlControllerName.Value,
                Plugin.targetSmartControlInput.Value,
                0.2f,
                onRelease: TargetListControllerComponent.KeepOnlyDataLinkedTargets,
                onLongPress: TargetListControllerComponent.KeepClosestTargetsBasedOnAmmo);
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
            InternalState.playerFactionHQ = SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;
        }

        public static void Update() {
            int currentCount = Bindings.Player.TargetList.GetTargets().Count;
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
                InternalState.targetIndex = Mathf.Clamp(InternalState.targetIndex, 0, Mathf.Max(0, currentCount - 1));
                InternalState.previousTargetList = Bindings.Player.TargetList.GetTargets();
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
        public static void Init() {
        }
        public static void Update() {
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

                if (InternalState.resetIndex) { // don't forget that the list is in reverse order (LIFO), this is why we set to count - 1
                    InternalState.targetIndex = targets.Count - 1;
                    InternalState.resetIndex = false;
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

    public static void UpdateTargetTexts() {
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

    public static void NextTarget() {
        Plugin.Log($"[TR] NextTarget");
        int targetCount = Bindings.Player.TargetList.GetTargets().Count;
        if (targetCount > 0) {
            InternalState.targetIndex = (InternalState.targetIndex - 1 + targetCount) % targetCount;
            InternalState.updateDisplay = true;
            Bindings.UI.Sound.PlaySound("beep_scroll.mp3");
        }
    }

    public static void PreviousTarget() {
        Plugin.Log($"[TR] PreviousTarget");
        int targetCount = Bindings.Player.TargetList.GetTargets().Count;
        if (targetCount > 0) {
            InternalState.targetIndex = (InternalState.targetIndex + 1) % targetCount;
            InternalState.updateDisplay = true;
            Bindings.UI.Sound.PlaySound("beep_scroll.mp3");
        }
    }

    public static void PopCurrentTarget() {
        Plugin.Log($"[TR] DeselectCurrentTarget");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        if (currentTargets.Count > 1 && InternalState.targetIndex < currentTargets.Count) {
            Unit targetToDeselect = currentTargets[InternalState.targetIndex];
            InternalState.targetIndex = Mathf.Clamp(InternalState.targetIndex, 0, Mathf.Max(0, currentTargets.Count - 1));
            Bindings.Player.TargetList.DeselectUnit(targetToDeselect);
            InternalState.updateDisplay = true;
        }
    }

    public static void KeepOnlyCurrentTarget() {
        Plugin.Log($"[TR] KeepOnlyCurrentTarget");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        if (currentTargets.Count > 0 && InternalState.targetIndex < currentTargets.Count) {
            Unit targetToKeep = currentTargets[InternalState.targetIndex];
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets([targetToKeep]);
            InternalState.updateDisplay = true;
        }
    }

    public static void KeepOnlyDataLinkedTargets() {
        Plugin.Log($"[TR] KeepOnlyDataLinkedTargets");
        List<Unit> currentTargets = Bindings.Player.TargetList.GetTargets();
        List<Unit> dataLinkedTargets = [];
        foreach (Unit target in currentTargets) {
            if (InternalState.playerFactionHQ.IsTargetPositionAccurate(target, 20f)) {
                dataLinkedTargets.Add(target);
            }
        }
        if (dataLinkedTargets.Count >= 0) {
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets(dataLinkedTargets);
            InternalState.resetIndex = true;
            InternalState.updateDisplay = true;
            Bindings.UI.Game.DisplayToast($"Kept <b>{dataLinkedTargets.Count.ToString()}</b> data linked target"+(dataLinkedTargets.Count>1 ? "s" : ""), 3f);
        }
    }

    public static void KeepClosestTargetsBasedOnAmmo() {
        int count = Bindings.Player.Aircraft.Weapons.GetActiveStationAmmo();
        if (count == 0) {
            return;
        }
        Plugin.Log($"[TR] KeepClosestTargetsBasedOnAmmo");
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
        List<Unit> closestTargets = sortedTargets.GetRange(0, Mathf.Min(count, sortedTargets.Count));
        List<Unit> targetsToKeep = [.. currentTargets.Where(closestTargets.Contains)];

        if (targetsToKeep.Count >= 0) {
            Bindings.Player.TargetList.DeselectAll();
            Bindings.Player.TargetList.AddTargets(targetsToKeep);
            InternalState.resetIndex = true;
            InternalState.updateDisplay = true;
            Bindings.UI.Game.DisplayToast($"Kept <b>{targetsToKeep.Count.ToString()}</b> closest targets", 3f);
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
                InternalState.resetIndex = true;
                InternalState.updateDisplay = true;
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
