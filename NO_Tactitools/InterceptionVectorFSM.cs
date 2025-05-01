using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Rewired.Utils.Classes.Data;
using Unity.Audio;

namespace NO_Tactitools;

[HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
class InterceptionFSM
{
    public enum State
    {
        Init,
        Idle,
        TargetInitiallyUntracked,
        Intercepting
    }
    public static State currentState = State.Init;
    static GameObject bearingLabel;
    static GameObject timerLabel;
    static GameObject indicatorLabel;

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
    static GameObject FindOrCreateLabel(string name, Vector2 position, FontStyle fontStyle = FontStyle.Normal)
    {
        // Check if the label already exists
        foreach (Transform child in Plugin.fuelGauge.transform)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        // Find the fuelLabel to clone
        GameObject fuelLabel = null;
        foreach (Transform child in Plugin.fuelGauge.transform)
        {
            if (child.name == "fuelLabel")
            {
                fuelLabel = child.gameObject;
                break;
            }
        }

        if (fuelLabel == null)
        {
            Plugin.Logger.LogError($"Could not find fuelLabel to create {name}");
            return null;
        }

        // Clone the fuelLabel and configure it
        GameObject newLabel = GameObject.Instantiate(fuelLabel, Plugin.fuelGauge.transform);
        newLabel.name = name;
        var rectTransform = newLabel.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        // Set font style if specified
        var textComponent = newLabel.GetComponent<Text>();
        if (textComponent != null)
        {
            textComponent.fontStyle = fontStyle;
        }

        return newLabel;
    }
    static void Postfix()
    {
        switch (currentState)
        {
            case State.Init:
                HandleInitState();
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
    static void HandleInitState()
    {
        Plugin.Logger.LogInfo("Init state");
        
        playerFactionHQ = Plugin.combatHUD.aircraft.NetworkHQ;
        // Create or find the labels
        bearingLabel = FindOrCreateLabel("bearing", new Vector2(0, Screen.height / 2 * 7 / 12));
        timerLabel = FindOrCreateLabel("timer", new Vector2(0, -100));
        indicatorLabel = FindOrCreateLabel("indicator", new Vector2(0, 0), FontStyle.Bold);
        bearingLabel.GetComponent<Text>().text = "";
        timerLabel.GetComponent<Text>().text = "";
        indicatorLabel.GetComponent<Text>().text = "";
        targetUnit = null;
        solutionTime = 0f;
        currentState = State.Idle;
        Plugin.Logger.LogInfo("Transitioning to Idle state");
        return;
    }

    static void HandleIdleState()
    {
        if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count == 1)
        {
            targetUnit = ((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue())[0];
            if (playerFactionHQ.IsTargetBeingTracked(targetUnit)){
                currentState = State.Intercepting;
                Plugin.Logger.LogInfo("Target is being tracked");
                return;
            }
            else
            {
                currentState = State.TargetInitiallyUntracked;
                Plugin.Logger.LogInfo("Target is initially untracked");
                return;
            }
        }
    }

    static void HandleTargetInitiallyUntracked()
    {
        if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count != 1)
        {
            currentState = State.Init;
            Plugin.Logger.LogInfo("Selected more than one target, returning to Init state");
            return;
        }
        if (playerFactionHQ.IsTargetBeingTracked(targetUnit))
        {
            currentState = State.Intercepting;
            Plugin.Logger.LogInfo("Target is being tracked, going to TargetTracked state");
            return;
        }
    }

    static void HandleTracked()
    {
        bearingLabel.GetComponent<Text>().color = Color.green;
        timerLabel.GetComponent<Text>().color = Color.green;
    }

    static void HandleUntracked()
    {
        bearingLabel.GetComponent<Text>().color = Color.red;
        timerLabel.GetComponent<Text>().color = Color.red;
    }

    static void HandleTargetReachable()
    {
        interceptVector = interceptPosition - playerPosition;
        interceptVectorXZ = new Vector3(interceptVector.x, 0, interceptVector.z).normalized;
        interceptBearing = (int)(Vector3.SignedAngle(Vector3.forward, interceptVectorXZ, Vector3.up) + 360) % 360;
        interceptionTimeInSeconds = (int)(interceptVector.magnitude / playerVelocity.magnitude);
        interceptScreen = Plugin.cameraStateManager.mainCamera.WorldToScreenPoint(interceptPosition);
        interceptScreen.x -= Screen.width / 2;
        interceptScreen.y -= Screen.height / 2;
        if(interceptScreen.z >0)
        {
            indicatorLabel.GetComponent<Text>().text = "X";
        }
        else
        {
            indicatorLabel.GetComponent<Text>().text = "";
        }
        bearingLabel.GetComponent<Text>().text = $"({interceptBearing.ToString()}°)";
        timerLabel.GetComponent<Text>().text = $"({interceptionTimeInSeconds.ToString()}s)";
        indicatorLabel.GetComponent<Text>().text = $"X";
        indicatorLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            interceptScreen.x,
            interceptScreen.y
        );
        Plugin.Logger.LogInfo($"PanCAM: {Plugin.cameraStateManager.mainCamera.transform.rotation.eulerAngles.ToString()}");
        Plugin.Logger.LogInfo($"PanPLANE: {Plugin.combatHUD.aircraft.transform.rotation.eulerAngles.ToString()}");
    }
    static void HandleTargetUnreachable()
    {
        bearingLabel.GetComponent<Text>().text = "";
        timerLabel.GetComponent<Text>().text = "";
        indicatorLabel.GetComponent<Text>().text = "";
    }

    static void UpdateInterceptionPosition()
    {
        interceptPosition = targetPosition + targetVelocity * solutionTime;
    }
    static void HandleInterception()
    {
        if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count != 1)
        {
            currentState = State.Init;
            Plugin.Logger.LogInfo("Selected more than one target, returning to Init state");
            return;
        }
        playerPosition = Plugin.combatHUD.aircraft.rb.transform.position;
        playerVelocity = Plugin.combatHUD.aircraft.rb.velocity;
        if (playerFactionHQ.IsTargetBeingTracked(targetUnit))
        {
            HandleTracked();
            solutionTime = FindSolutionTime(targetUnit);
            if(solutionTime>0)
            {   
                UpdateInterceptionPosition();
            }
        }
        else
        {
            HandleUntracked();
        }
        if(solutionTime>0)
        {   
            HandleTargetReachable();
        }
        else
        {
            HandleTargetUnreachable();
        }
        // TODO: Implement logic for when the target is being tracked
    }    

    static float FindSolutionTime(Unit targetUnit)
    {
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
        if (playerVelocity.magnitude == targetVelocity.magnitude)
        {
            //solution is dist / 2 / sqrt((v_T cos(α_{TX}))²)
            solution1 = calcLine.magnitude / 2 * Mathf.Sqrt(Mathf.Pow(targetVelocity.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad), 2));
            solution2 = solution1;
        }
        else
        {
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
        if (solution1 > 0 && solution2 > 0)
        {
            bestSolution = Mathf.Min(solution1, solution2);
        }
        else if (solution1 > 0)
        {
            bestSolution = solution1;
        }
        else if (solution2 > 0)
        {
            bestSolution = solution2;
        }
        return bestSolution;
    }

}
[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class OnHUDResetPatch
{
    static void Postfix()
    {
        // Reset the FSM state when the aircraft is destroyed
        InterceptionFSM.currentState = InterceptionFSM.State.Init;
    }
}

[HarmonyPatch(typeof(CameraCockpitState), "EnterState")]
class OnCockpitState
{
    static void Postfix(CameraCockpitState __instance)
    {
        Plugin.Logger.LogInfo("Cockpit ENTER");
        Plugin.pan = (float)Traverse.Create(__instance).Field("panView").GetValue();
    }
}

