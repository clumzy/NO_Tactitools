using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class InterceptionVectorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[IV] Interception Vector plugin starting !");
            // APPLY SUB PATCHES
            Plugin.harmony.PatchAll(typeof(InterceptionVectorTask));
            Plugin.harmony.PatchAll(typeof(ResetInterceptionVectorOnRespawnPatch));
            initialized = true;
            Plugin.Log("[IV] Interception Vector plugin succesfully started !");
        }
    }

    public static void Reset() {
        InterceptionVectorTask.ResetState();
    }
}

[HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
class InterceptionVectorTask {
    public enum State {
        Init,
        Reset,
        Idle,
        TargetInitiallyUntracked,
        Intercepting
    }
    static State currentState = State.Init;
    static Bindings.UI.Draw.UILabel bearingLabel;
    static Bindings.UI.Draw.UILabel timerLabel;
    static Bindings.UI.Draw.UILabel indicatorTargetLabel;
    static Bindings.UI.Draw.UILine indicatorTargetLine;
    static FactionHQ playerFactionHQ;
    static Unit targetUnit;
    static float solutionTime;
    static Vector3 playerPosition;
    static Vector3 playerVelocity;
    static Vector3 targetPosition;
    static Vector3 targetVelocity;
    static Vector3 interceptPosition;
    static List<Vector3> interceptArray = [];
    const int interceptArraySize = 180; // Number of entries to keep in the intercept array

    static void Postfix() {
        CheckForUIElements();
        switch (currentState) {
            case State.Init:
                HandleInitState();
                break;
            case State.Reset:
                HandleResetState();
                break;
            case State.Idle:
                HandleIdleState();
                break;
            case State.TargetInitiallyUntracked:
                HandleTargetInitiallyUntracked();
                break;
            case State.Intercepting:
                HandleInterception();
                break;
        }
    }

    static void CheckForUIElements() {
        if (bearingLabel == null || timerLabel == null || indicatorTargetLabel == null || indicatorTargetLine == null)
            // UI elements not initialized yet or destroyed
            currentState = State.Init;
    }

    static void HandleInitState() {
        if (
            Bindings.Player.TargetList.GetTargets().Count != 1 ||
            Bindings.UI.Game.GetTacScreen() == null ||
            Bindings.UI.Game.GetCombatHUD() == null) return; // Do not init if no target is selected
        Plugin.Log("[IV] Init state");
        playerFactionHQ = SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;
        bearingLabel = new Bindings.UI.Draw.UILabel(
            "bearingLabel",
            new Vector2(0, -70),
            Bindings.UI.Game.GetTargetScreen(),
            FontStyle.Normal,
            Color.green,
            20
        );
        timerLabel = new Bindings.UI.Draw.UILabel(
            "timerLabel",
            new Vector2(0, -100),
            Bindings.UI.Game.GetTargetScreen(),
            FontStyle.Normal,
            Color.green,
            20
        );
        indicatorTargetLabel = new Bindings.UI.Draw.UILabel(
            "indicatorTargetLabel",
            new Vector2(0, 0),
            Bindings.UI.Game.GetTargetScreen(),
            FontStyle.Normal,
            Color.magenta,
            36,
            0f
        );
        indicatorTargetLine = new Bindings.UI.Draw.UILine(
            "indicatorTargetLine",
            new Vector2(0, 0),
            new Vector2(0, 0),
            Bindings.UI.Game.GetTargetScreen(),
            Color.magenta,
            2f
        );

        currentState = State.Reset;
        Plugin.Log("[IV] Transitioning to Reset state");
        return;
    }

    static void HandleResetState() {
        bearingLabel.SetText("");
        timerLabel.SetText("");
        indicatorTargetLabel.SetText("");
        indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(0, 0));
        targetUnit = null;
        solutionTime = 0f;
        interceptArray.Clear();
        currentState = State.Idle;
        Plugin.Log("[IV] Transitioning to Idle state");
        return;
    }

    static void HandleIdleState() {
        if (((List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()).Count == 1) {
            targetUnit = ((List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue())[0];
            if (playerFactionHQ.IsTargetBeingTracked(targetUnit)) {
                currentState = State.Intercepting;
                Plugin.Log("[IV] Target is being tracked");
                return;
            }
            else {
                currentState = State.TargetInitiallyUntracked;
                Plugin.Log("[IV] Target is initially untracked");
                return;
            }
        }
        return;
    }

    static void HandleTargetInitiallyUntracked() {
        if (((List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()).Count != 1
            || ((List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue())[0] != targetUnit) {
            currentState = State.Reset;
            Plugin.Log("[IV] Switched target, returning to Reset state");
            return;
        }
        else if (playerFactionHQ.IsTargetBeingTracked(targetUnit)) {
            currentState = State.Intercepting;
            Plugin.Log("[IV] Target is being tracked, going to TargetTracked state");
            return;
        }
    }

    static void HandleInterception() {
        if (((List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()).Count != 1
            || ((List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue())[0] != targetUnit) {
            currentState = State.Reset;
            Plugin.Log("[IV] Switched target, returning to Reset state");
            return;
        }
        playerPosition = SceneSingleton<CombatHUD>.i.aircraft.rb.transform.position;
        playerVelocity = SceneSingleton<CombatHUD>.i.aircraft.rb.velocity;
        if (playerFactionHQ.IsTargetBeingTracked(targetUnit)) {
            HandleTracked();
            solutionTime = FindSolutionTime(targetUnit);
            if (solutionTime > 0) {
                UpdateInterceptionPosition();
            }
        }
        else {
            HandleUntracked();
        }
        if (solutionTime > 0) {
            HandleTargetReachable();
        }
        else {
            HandleTargetUnreachable();
        }
    }

    static void HandleTracked() {
        bearingLabel.SetColor(Color.green);
        timerLabel.SetColor(Color.green);
    }

    static void HandleUntracked() {
        bearingLabel.SetColor(Color.red);
        timerLabel.SetColor(Color.red);
    }

    static void HandleTargetReachable() {
        if (Bindings.Player.Aircraft.GetAircraft() == null) {
            currentState = State.Reset;
            Plugin.Log("[IV] Player aircraft was null, returning to Reset state");
            return;
        }
        Vector3 interceptVector = interceptPosition;
        Vector3 interceptVectorXZ = Vector3.Scale(interceptVector, new Vector3(1f, 0f, 1f)).normalized;
        int interceptBearing = (int)(Vector3.SignedAngle(Vector3.forward, interceptVectorXZ, Vector3.up) + 360) % 360;
        int interceptionTimeInSeconds = (int)Mathf.Clamp(interceptVector.magnitude / playerVelocity.magnitude - interceptArraySize / 60, 0, 999);
        Vector3 interceptScreen = Bindings.UI.Game.GetCameraStateManager().mainCamera.WorldToScreenPoint(interceptPosition);
        Vector3 velocityUpNormal = Vector3.Cross(SceneSingleton<CombatHUD>.i.aircraft.rb.velocity, SceneSingleton<CombatHUD>.i.aircraft.rb.transform.right).normalized;
        Vector3 velocityRightNormal = Vector3.Cross(SceneSingleton<CombatHUD>.i.aircraft.rb.velocity, velocityUpNormal).normalized;
        int relativeHeight = (int)(
            Vector3.SignedAngle(
                Vector3.ProjectOnPlane(interceptVector, velocityUpNormal),
                interceptVector,
                velocityRightNormal));
        int relativeBearing = (int)(
            Vector3.SignedAngle(
                Vector3.ProjectOnPlane(interceptVector, velocityRightNormal),
                interceptVector,
                velocityUpNormal));
        Vector3 interceptTarget = new(
            (int)Mathf.Clamp(relativeBearing / 60f * 170f, -170, 170), //180 = width of the canvas
            (int)Mathf.Clamp(relativeHeight / 60f * 170f, -110, 110), //115 = height of the canvas
            0
        );
        bool currentInterceptScreenVisible = interceptScreen.z > 0;
        if (currentInterceptScreenVisible && interceptArray.Count == interceptArraySize) {
            bearingLabel.SetText($"({interceptBearing.ToString()}°)");
            timerLabel.SetText($"ETA : {interceptionTimeInSeconds.ToString()}s");
            if (targetUnit is not Building) { // only display the vector if the target is a unit
                indicatorTargetLabel.SetText("+");
                indicatorTargetLabel.SetPosition(new Vector2(interceptTarget.x, interceptTarget.y));
                indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(interceptTarget.x, interceptTarget.y));
                indicatorTargetLine.ResetThickness();
            }
            else {
                indicatorTargetLabel.SetText("");
                indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(0, 0));
            }
        }
        else {
            if (interceptArray.Count == interceptArraySize) indicatorTargetLabel.SetText("↶");
            else {
                indicatorTargetLabel.SetText("." + new string('.', (int)(interceptArray.Count / 60)));
            }
            indicatorTargetLabel.SetPosition(new Vector2(0, -40));
            indicatorTargetLine.SetThickness(0f);
        }

    }

    static void HandleTargetUnreachable() {
        bearingLabel.SetText("");
        timerLabel.SetText("");
        indicatorTargetLabel.SetText("");
        indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(0, 0));
    }

    static void UpdateInterceptionPosition() {
        Vector3 currentPosition = targetPosition + targetVelocity * solutionTime;
        if (interceptArray.Count < interceptArraySize) {
            interceptArray.Add(currentPosition);
        }
        else {
            interceptArray.RemoveAt(0);
            interceptArray.Add(currentPosition);
        }
        // Calculate the average position of the interceptArraySize 160 entries
        Vector3 averagePosition = Vector3.zero;
        foreach (Vector3 position in interceptArray) {
            averagePosition += position;
        }
        averagePosition /= interceptArray.Count;
        interceptPosition = averagePosition;
    }

    public static void ResetState() {
        currentState = State.Init;
    }

    static float FindSolutionTime(Unit targetUnit) {
        //Get target vectors
        Vector3 localTargetPosition = targetUnit.transform.position;
        Vector3 localTargetVelocity;
        if (targetUnit is Building)
            localTargetVelocity = Vector3.zero;
        else
            localTargetVelocity = targetUnit.rb.velocity;
        //Create vector from player to target
        Vector3 calcLine = localTargetPosition - playerPosition;
        //angle between the two vectors
        float angle = Vector3.Angle(calcLine.normalized, localTargetVelocity.normalized);
        float solution1;
        float solution2;
        float bestSolution = 0f;
        if (playerVelocity.magnitude == localTargetVelocity.magnitude) {
            //solution is dist / 2 / sqrt((v_T cos(α_{TX}))²)
            solution1 = calcLine.magnitude / 2 * Mathf.Sqrt(Mathf.Pow(localTargetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad), 2));
            solution2 = solution1;
        }
        else {
            // this geogebra equation applied here : (dist (v_T cos(α_{TX}) + sqrt(-v_T² sin²(α_{TX}) + v_P²))) / (v_P² - v_T²)
            // where v_T is the target velocity, v_P is the player velocity
            // α_{TX} is the angle between the two vectors and dist is the distance between the player and the target
            solution1 =
            (calcLine.magnitude *
                (localTargetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad) +
                Mathf.Sqrt(
                    -Mathf.Pow(localTargetVelocity.magnitude, 2) * Mathf.Pow(Mathf.Sin(angle * Mathf.Deg2Rad), 2) +
                    Mathf.Pow(playerVelocity.magnitude, 2)))) /
            (Mathf.Pow(playerVelocity.magnitude, 2) - Mathf.Pow(localTargetVelocity.magnitude, 2));
            solution2 =
            (calcLine.magnitude *
                (localTargetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad) -
                Mathf.Sqrt(
                    -Mathf.Pow(localTargetVelocity.magnitude, 2) * Mathf.Pow(Mathf.Sin(angle * Mathf.Deg2Rad), 2) +
                    Mathf.Pow(playerVelocity.magnitude, 2)))) /
            (Mathf.Pow(playerVelocity.magnitude, 2) - Mathf.Pow(localTargetVelocity.magnitude, 2));
        }
        //best solution needs to be positive also
        if (solution1 > 0 && solution2 > 0) {
            bestSolution = Mathf.Min(solution1, solution2);
        }
        else if (solution1 > 0) {
            bestSolution = solution1;
        }
        else if (solution2 > 0) {
            bestSolution = solution2;
        }

        // Update the static variables for use in UpdateInterceptionPosition
        targetPosition = localTargetPosition;
        targetVelocity = localTargetVelocity;

        return bestSolution;
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetInterceptionVectorOnRespawnPatch {
    static void Postfix() {
        // Reset the FSM state when the aircraft is destroyed
        InterceptionVectorPlugin.Reset();
    }
}
