using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class InterceptionVectorPlugin {
    private static bool initialized = false;
    public static bool activated = true;
    static void Postfix() {
        if (!initialized) {
            Plugin.Logger.LogInfo($"[IV] Interception Vector plugin starting !");
            Plugin.inputCatcherPlugin.RegisterControllerButton(
                Plugin.interceptionVectorControllerName.Value,
                new ControllerButton(
                (int)Plugin.interceptionVectorButtonNumber.Value,
                0.2f,
                onShortPress: HandleClick
                ));
            initialized = true;
            Plugin.Logger.LogInfo("[IV] Interception Vector plugin succesfully started !");
        }
        //RESET FSM STATE
        InterceptionVectorTask.ResetState();
    }
    private static void HandleClick() {
        Plugin.Logger.LogInfo($"[IV] HandleClick");
        SoundManager.PlayInterfaceOneShot(Plugin.selectAudio);
        activated = !activated;
        string report;
        if (activated) {
            report = "Interception Vector <b>Enabled</b>";
        }
        else {
            report = "Interception Vector <b>Disabled</b>";
        }
        Plugin.Logger.LogInfo($"[IV] {report}");
        SceneSingleton<AircraftActionsReport>.i.ReportText(report, 2f);
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
    static GameObject bearingLabel;
    static GameObject timerLabel;
    static GameObject indicatorScreenLabel;
    static GameObject indicatorTargetLabel;
    static GameObject indicatorTargetLine;
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
        Plugin.Logger.LogInfo("[IV] Init state");
        playerFactionHQ = Plugin.combatHUD.aircraft.NetworkHQ;
        // Create or find the labels
        indicatorScreenLabel = UIUtils.FindOrCreateLabel(
            "indicatorScreenLabel",
            new Vector2(0, 0),
            Plugin.combatHUD.transform,
            false,
            FontStyle.Bold,
            Color.green,
            fontSize: 18,
            backgroundOpacity:0f
        );
        bearingLabel = UIUtils.FindOrCreateLabel(
            "bearingLabel",
            new Vector2(0, 100),
            Plugin.fuelGauge.transform,
            true,
            FontStyle.Normal,
            Color.green,
            fontSize: 20
        );
        timerLabel = UIUtils.FindOrCreateLabel(
            "timerLabel",
            new Vector2(0, -100),
            Plugin.fuelGauge.transform,
            true,
            FontStyle.Normal,
            Color.green,
            fontSize: 20
        );
        indicatorTargetLabel = UIUtils.FindOrCreateLabel(
            "indicatorTargetLabel",
            new Vector2(0, 0),
            Plugin.combatHUD.transform,
            true,
            FontStyle.Normal,
            Color.magenta,
            fontSize: 36,
            backgroundOpacity:0f
        );
        indicatorTargetLine = UIUtils.FindOrCreateLine(
            "indicatorTargetLine",
            new Vector2(0, 0),
            new Vector2(0, 0),
            Plugin.combatHUD.transform,
            true,
            Color.magenta,
            2f
        );

        currentState = State.Reset;
        Plugin.Logger.LogInfo("[IV] Transitioning to Reset state");
        return;
    }
    static void HandleResetState() {
        UIUtils.SetLabelText(bearingLabel,"");
        UIUtils.SetLabelText(timerLabel,"");
        UIUtils.SetLabelText(indicatorScreenLabel,"");
        UIUtils.SetLabelText(indicatorTargetLabel,"");
        UIUtils.SetLineCoordinates(
                indicatorTargetLine,
                new Vector2(0, 0),
                new Vector2(0, 0)
            );
        targetUnit = null;
        solutionTime = 0f;
        currentState = State.Idle;
        Plugin.Logger.LogInfo("[IV] Transitioning to Idle state");
        return;
    }

    static void HandleIdleState() {
        if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count == 1
            && InterceptionVectorPlugin.activated) {
            targetUnit = ((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue())[0];
            if (playerFactionHQ.IsTargetBeingTracked(targetUnit)) {
                currentState = State.Intercepting;
                Plugin.Logger.LogInfo("[IV] Target is being tracked");
                return;
            }
            else {
                currentState = State.TargetInitiallyUntracked;
                Plugin.Logger.LogInfo("[IV] Target is initially untracked");
                return;
            }
        }
        return;
    }

    static void HandleTargetInitiallyUntracked() {
        if (!InterceptionVectorPlugin.activated) {
            currentState = State.Reset;
            Plugin.Logger.LogInfo("[IV] Interception Vector deactivated, returning to Reset state");
            return;
        }
        else if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count != 1
            || ((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue())[0] != targetUnit) {
            currentState = State.Reset;
            Plugin.Logger.LogInfo("[IV] Switched target, returning to Reset state");
            return;
        }
        else if (playerFactionHQ.IsTargetBeingTracked(targetUnit)) {
            currentState = State.Intercepting;
            Plugin.Logger.LogInfo("[IV] Target is being tracked, going to TargetTracked state");
            return;
        }
    }

    static void HandleTracked() {
        UIUtils.SetLabelColor(bearingLabel, Color.green);
        UIUtils.SetLabelColor(timerLabel, Color.green);
    }

    static void HandleUntracked() {
        UIUtils.SetLabelColor(bearingLabel, Color.red);
        UIUtils.SetLabelColor(timerLabel, Color.red);
    }

    static void HandleTargetReachable() {
        interceptVector = interceptPosition - playerPosition;
        interceptVectorXZ = Vector3.Scale(interceptVector, new Vector3(1f, 0f, 1f)).normalized;
        interceptBearing = (int)(Vector3.SignedAngle(Vector3.forward, interceptVectorXZ, Vector3.up) + 360) % 360;
        interceptionTimeInSeconds = (int)(interceptVector.magnitude / playerVelocity.magnitude);
        interceptScreen = Plugin.cameraStateManager.mainCamera.WorldToScreenPoint(interceptPosition);
        interceptScreen.x -= Screen.width / 2;
        interceptScreen.y -= Screen.height / 2;
        int relativeHeight = (int)-(
            Vector3.SignedAngle(
                Vector3.ProjectOnPlane(interceptVector, Plugin.combatHUD.aircraft.rb.transform.up),
                interceptVector,
                Plugin.combatHUD.aircraft.rb.transform.right) + 0);
        int relativeBearing = (int)(
            Vector3.SignedAngle(
                Vector3.ProjectOnPlane(interceptVector, Plugin.combatHUD.aircraft.rb.transform.right),
                interceptVector,
                Plugin.combatHUD.aircraft.rb.transform.up) + 0);
        Vector3 interceptTarget = new(
            (int)Mathf.Clamp(relativeBearing / 60f * 170f, -170, 170), //180 = width of the canvas
            (int)Mathf.Clamp(relativeHeight / 60f * 110f, -110, 110), //115 = height of the canvas
            0
        );
        UIUtils.SetLabelText(bearingLabel, $"({interceptBearing.ToString()}°)");
        UIUtils.SetLabelText(timerLabel, $"ETA : {interceptionTimeInSeconds.ToString()}s");
        indicatorScreenLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            interceptScreen.x,
            interceptScreen.y
        );
        indicatorTargetLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            interceptTarget.x,
            interceptTarget.y
        );
        UIUtils.SetLabelText(indicatorTargetLabel, "+");
        UIUtils.SetLineCoordinates(
            indicatorTargetLine,
            new Vector2(0, 0),
            new Vector2(interceptTarget.x, interceptTarget.y)
        );
        // if z < 0, the target is behind the player
        if (interceptScreen.z > 0 && Plugin.onScreenVectorEnabled.Value) {
            UIUtils.SetLabelText(indicatorScreenLabel, "+");

        }
        else {
            UIUtils.SetLabelText(indicatorScreenLabel, "");
        }

    }
    static void HandleTargetUnreachable() {
        UIUtils.SetLabelText(bearingLabel,"");
        UIUtils.SetLabelText(timerLabel,"");
        UIUtils.SetLabelText(indicatorScreenLabel,"");
        UIUtils.SetLabelText(indicatorTargetLabel,"");
        UIUtils.SetLineCoordinates(
            indicatorTargetLine,
            new Vector2(0, 0),
            new Vector2(0, 0)
        );
    }

    static void UpdateInterceptionPosition() {
        interceptPosition = targetPosition + targetVelocity * solutionTime;
    }
    static void HandleInterception() {
        if (!InterceptionVectorPlugin.activated) {
            currentState = State.Reset;
            Plugin.Logger.LogInfo("[IV] Interception Vector deactivated, returning to Reset state");
            return;
        }
        else if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count != 1
            || ((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue())[0] != targetUnit) {
            currentState = State.Reset;
            Plugin.Logger.LogInfo("[IV] Switched target, returning to Reset state");
            return;
        }
        playerPosition = Plugin.combatHUD.aircraft.rb.transform.position;
        playerVelocity = Plugin.combatHUD.aircraft.rb.velocity;
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
        // TODO: Implement logic for when the target is being tracked
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
class OnHUDResetPatch {
    static void Postfix() {
        // Reset the FSM state when the aircraft is destroyed
        InterceptionVectorTask.ResetState();
        // Temporary fix, to check if resetting the Canvas on AircraftReset fixes the bug where the UI
        //that is supposed to appear on the canvas appears on the HUD
        TargetScreenUIOnDestroyPatch.ClearCanvasLabels();
    }
}
