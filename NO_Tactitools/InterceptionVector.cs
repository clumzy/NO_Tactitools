using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NO_Tactitools;

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
        //RESET FSM STATE
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
    static UIUtils.UILabel bearingLabel;
    static UIUtils.UILabel timerLabel;
    static UIUtils.UILabel indicatorScreenLabel;
    static UIUtils.UILabel indicatorTargetLabel;
    static UIUtils.UILine indicatorTargetLine;
    static FactionHQ playerFactionHQ;
    static Unit targetUnit;
    static float solutionTime;
    static Vector3 playerPosition;
    static Vector3 playerVelocity;
    static Vector3 targetPosition;
    static Vector3 targetVelocity;
    static Vector3 interceptPosition;
    static Vector3 interceptVector;
    static Vector3 interceptVectorXZ;
    static int interceptBearing;
    static int interceptionTimeInSeconds;
    static Vector3 interceptScreen;
    static bool previousInterceptScreenVisible = false;

    static void Postfix() {
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

    static void HandleInitState() {
        Plugin.Log("[IV] Init state");
        playerFactionHQ = SceneSingleton<CombatHUD>.i.aircraft.NetworkHQ;
        // Create or find the labels
        indicatorScreenLabel = new UIUtils.UILabel(
            "indicatorScreenLabel",
            new Vector2(0, 0),
            UIUtils.HMD,
            false,
            FontStyle.Bold,
            Color.green,
            18,
            0f
        );
        bearingLabel = new UIUtils.UILabel(
            "bearingLabel",
            new Vector2(0, -70),
            UIUtils.HMD,
            true,
            FontStyle.Normal,
            Color.green,
            20
        );
        timerLabel = new UIUtils.UILabel(
            "timerLabel",
            new Vector2(0, -100),
            UIUtils.HMD,
            true,
            FontStyle.Normal,
            Color.green,
            20
        );
        indicatorTargetLabel = new UIUtils.UILabel(
            "indicatorTargetLabel",
            new Vector2(0, 0),
            UIUtils.HMD,
            true,
            FontStyle.Normal,
            Color.magenta,
            36,
            0f
        );
        indicatorTargetLine = new UIUtils.UILine(
            "indicatorTargetLine",
            new Vector2(0, 0),
            new Vector2(0, 0),
            UIUtils.HMD,
            true,
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
        indicatorScreenLabel.SetText("");
        indicatorTargetLabel.SetText("");
        indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(0, 0));
        targetUnit = null;
        solutionTime = 0f;
        previousInterceptScreenVisible = false;
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

    static void HandleTracked() {
        bearingLabel.SetColor(Color.green);
        timerLabel.SetColor(Color.green);
    }

    static void HandleUntracked() {
        bearingLabel.SetColor(Color.red);
        timerLabel.SetColor(Color.red);
    }

    static void HandleTargetReachable() {
        interceptVector = interceptPosition - playerPosition;
        interceptVectorXZ = Vector3.Scale(interceptVector, new Vector3(1f, 0f, 1f)).normalized;
        interceptBearing = (int)(Vector3.SignedAngle(Vector3.forward, interceptVectorXZ, Vector3.up) + 360) % 360;
        interceptionTimeInSeconds = (int)(interceptVector.magnitude / playerVelocity.magnitude);
        interceptScreen = UIUtils.cameraStateManager.mainCamera.WorldToScreenPoint(interceptPosition);
        interceptScreen.x -= Screen.width / 2;
        interceptScreen.y -= Screen.height / 2;
        int relativeHeight = (int)-(
            Vector3.SignedAngle(
                Vector3.ProjectOnPlane(interceptVector, SceneSingleton<CombatHUD>.i.aircraft.rb.transform.up),
                interceptVector,
                SceneSingleton<CombatHUD>.i.aircraft.rb.transform.right));
        int relativeBearing = (int)(
            Vector3.SignedAngle(
                Vector3.ProjectOnPlane(interceptVector, SceneSingleton<CombatHUD>.i.aircraft.rb.transform.right),
                interceptVector,
                SceneSingleton<CombatHUD>.i.aircraft.rb.transform.up));
        Vector3 interceptTarget = new(
            (int)Mathf.Clamp(relativeBearing / 60f * 170f, -170, 170), //180 = width of the canvas
            (int)Mathf.Clamp(relativeHeight / 60f * 110f, -110, 110), //115 = height of the canvas
            0
        );
        bearingLabel.SetText($"({interceptBearing.ToString()}°)");
        timerLabel.SetText($"ETA : {interceptionTimeInSeconds.ToString()}s");
        bool currentInterceptScreenVisible = interceptScreen.z > 0;
        if (currentInterceptScreenVisible != previousInterceptScreenVisible) {
            if (currentInterceptScreenVisible) {
                if(Plugin.onScreenVectorEnabled.Value) indicatorScreenLabel.SetText("+");
                indicatorTargetLabel.SetText("+");
                indicatorTargetLine.ResetThickness();
            }
            else {
                indicatorScreenLabel.SetText("");
                indicatorTargetLabel.SetText("↶");
                indicatorTargetLabel.SetPosition(new Vector2(0, -40));
                indicatorTargetLine.SetThickness(0f);
            }
            previousInterceptScreenVisible = currentInterceptScreenVisible;
        }
        indicatorScreenLabel.SetPosition(new Vector2(interceptScreen.x, interceptScreen.y));
        if (currentInterceptScreenVisible) {
            indicatorTargetLabel.SetPosition(new Vector2(interceptTarget.x, interceptTarget.y));
        }
        indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(interceptTarget.x, interceptTarget.y));
        
    }
    static void HandleTargetUnreachable() {
        bearingLabel.SetText("");
        timerLabel.SetText("");
        indicatorScreenLabel.SetText("");
        indicatorTargetLabel.SetText("");
        indicatorTargetLine.SetCoordinates(new Vector2(0, 0), new Vector2(0, 0));
    }

    static void UpdateInterceptionPosition() {
        interceptPosition = targetPosition + targetVelocity * solutionTime;
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

    public static void ResetState() {
        currentState = State.Init;
    }

    static float FindSolutionTime(Unit targetUnit) {
        //Get target vectors
        targetPosition = targetUnit.rb.transform.position;
        targetVelocity = targetUnit.rb.velocity;
        //Create vector from player to target
        Vector3 calcLine = targetPosition - playerPosition;
        //angle between the two vectors
        float angle = Vector3.Angle(calcLine.normalized, targetVelocity.normalized);
        float solution1;
        float solution2;
        float bestSolution = 0f;
        if (playerVelocity.magnitude == targetVelocity.magnitude) {
            //solution is dist / 2 / sqrt((v_T cos(α_{TX}))²)
            solution1 = calcLine.magnitude / 2 * Mathf.Sqrt(Mathf.Pow(targetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad), 2));
            solution2 = solution1;
        }
        else {
            // this geogebra equation applied here : (dist (v_T cos(α_{TX}) + sqrt(-v_T² sin²(α_{TX}) + v_P²))) / (v_P² - v_T²)
            // where v_T is the target velocity, v_P is the player velocity
            // α_{TX} is the angle between the two vectors and dist is the distance between the player and the target
            solution1 =
            (calcLine.magnitude *
                (targetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad) +
                Mathf.Sqrt(
                    -Mathf.Pow(targetVelocity.magnitude, 2) * Mathf.Pow(Mathf.Sin(angle * Mathf.Deg2Rad), 2) +
                    Mathf.Pow(playerVelocity.magnitude, 2)))) /
            (Mathf.Pow(playerVelocity.magnitude, 2) - Mathf.Pow(targetVelocity.magnitude, 2));
            solution2 =
            (calcLine.magnitude *
                (targetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad) -
                Mathf.Sqrt(
                    -Mathf.Pow(targetVelocity.magnitude, 2) * Mathf.Pow(Mathf.Sin(angle * Mathf.Deg2Rad), 2) +
                    Mathf.Pow(playerVelocity.magnitude, 2)))) /
            (Mathf.Pow(playerVelocity.magnitude, 2) - Mathf.Pow(targetVelocity.magnitude, 2));
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
        return bestSolution;
    }

}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetInterceptionVectorOnRespawnPatch {
    static void Postfix() {
        // Reset the FSM state when the aircraft is destroyed
        InterceptionVectorTask.ResetState();
        // Temporary fix, to check if resetting the Canvas on AircraftReset fixes the bug where the UI
        //that is supposed to appear on the canvas appears on the HUD
        MFD_TargetOnDestroyPatch.ClearMFD_Target_Labels();
    }
}
