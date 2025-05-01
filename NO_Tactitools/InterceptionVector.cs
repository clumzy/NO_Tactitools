using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Rewired;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Rewired.Utils.Classes.Data;

namespace NO_Tactitools;


[HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
class VelocityUpdate
{
    static Vector3 playerPosition;
    static Vector3 playerVelocity;
    static Vector3 targetPosition;
    static Vector3 targetVelocity;
    static Vector3 calcLine;
    static float angle;
    static float solution1;
    static float solution2;
    static float bestSolution;
    static bool trackInterception = false;
    static Vector3 interceptPosition;
    static Vector3 interceptVector;
    static Vector3 interceptVectorXZ;
    static int interceptBearing;
    static int interceptHeight;
    static int interceptionTimeInSeconds;
    static GameObject bearingLabel;
    static GameObject heightLabel;
    static GameObject indicatorLabel;
    static GameObject interceptionTimeLabel;
    static void Postfix()
    {
        if (((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue()).Count == 1)
        {
            Plugin.Logger.LogInfo("TARGET LOCKED !");
            Plugin.playerFactionHQ = Plugin.combatHUD.aircraft.NetworkHQ;
            Plugin.targetUnit = ((List<Unit>)Traverse.Create(Plugin.combatHUD).Field("targetList").GetValue())[0];
        }
        else
        {
            Plugin.Logger.LogInfo("NO TARGET LOCKED !");
            Plugin.targetUnit = null;
        }
        if (Plugin.targetUnit != null)
        {
            //Check if target is tracked
            bool tracked = Plugin.playerFactionHQ.IsTargetBeingTracked(Plugin.targetUnit);
            if (tracked)
            {
                //Get copy of player vectors
                playerPosition = Plugin.combatHUD.aircraft.rb.transform.position;
                playerVelocity = Plugin.combatHUD.aircraft.rb.velocity;
                //Get target vectors
                targetPosition = Plugin.targetUnit.rb.transform.position;
                targetVelocity = Plugin.targetUnit.rb.velocity;
                //Create vector from player to target
                calcLine = targetPosition - playerPosition;
                //angle between the two vectors
                angle = Vector3.Angle(calcLine.normalized, targetVelocity.normalized);
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
                    trackInterception = true;
                }
                else if (solution1 > 0)
                {
                    bestSolution = solution1;
                    trackInterception = true;
                }
                else if (solution2 > 0)
                {
                    bestSolution = solution2;
                    trackInterception = true;
                }
                else
                {
                    trackInterception = false;
                }
            }
            if (trackInterception)
            {
                interceptPosition = targetPosition + targetVelocity * bestSolution;
                interceptVector = interceptPosition - playerPosition;
                //keep only the x and z components and normalize the vector
                interceptVectorXZ = new Vector3(interceptVector.x, 0, interceptVector.z).normalized;
                //signed angle between the XZ intercept vector and the north axis, cast to int
                interceptBearing = (int)(Vector3.SignedAngle(Vector3.forward, interceptVectorXZ, Vector3.up) + 360) % 360;
                interceptHeight = (int)(Vector3.SignedAngle(interceptVectorXZ, interceptVector.normalized, Vector3.right));
                interceptionTimeInSeconds = (int)(interceptVector.magnitude / playerVelocity.magnitude);
                Plugin.Logger.LogInfo($"INTERCEPT BEARING:{interceptBearing.ToString()}");
            }
            else
            {
                Plugin.Logger.LogInfo($"NO INTERCEPT BEARING !");
            }
            // See if we already have a child called bearing
            GameObject bearingObj = null;
            foreach (Transform child in Plugin.fuelGauge.transform)
            {
                if (child.name == "bearing")
                {
                    bearingObj = child.gameObject;
                    break;
                }
            }

            // If we don't have it, clone fuelLabel
            if (bearingObj == null)
            {
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
                    Plugin.Logger.LogError("Could not find fuelLabel");
                    return;
                }

                bearingObj = GameObject.Instantiate(fuelLabel, Plugin.fuelGauge.transform);

                bearingObj.name = "bearing";
                // Move it down a bit
                var currentPos = bearingObj.GetComponent<RectTransform>().anchoredPosition;
                bearingObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    0,
                    //a 6th from the top of the screen
                    Screen.height / 2 * 7 / 12
                );
            }
            bearingLabel = bearingObj;
            bearingLabel.GetComponent<Text>().text = "(...)";

            // See if we already have a child called bearing
            GameObject heightObj = null;

            foreach (Transform child in Plugin.fuelGauge.transform)
            {
                if (child.name == "height")
                {
                    heightObj = child.gameObject;
                    break;
                }
            }

            // If we don't have it, clone fuelLabel
            if (heightObj == null)
            {
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
                    Plugin.Logger.LogError("Could not find fuelLabel");
                    return;
                }

                heightObj = GameObject.Instantiate(fuelLabel, Plugin.fuelGauge.transform);

                heightObj.name = "height";
                // Move it down a bit
                var currentPos = heightObj.GetComponent<RectTransform>().anchoredPosition;
                heightObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    0,
                    //a 6th from the top of the screen
                    -80
                );
            }
            heightLabel = heightObj;
            heightLabel.GetComponent<Text>().text = "(...)";
            // See if we already have a child called indicator
            GameObject indicatorObj = null;

            foreach (Transform child in Plugin.fuelGauge.transform)
            {
                if (child.name == "indicator")
                {
                    indicatorObj = child.gameObject;
                    break;
                }
            }

            // If we don't have it, clone fuelLabel
            if (indicatorObj == null)
            {
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
                    Plugin.Logger.LogError("Could not find fuelLabel");
                    return;
                }

                indicatorObj = GameObject.Instantiate(fuelLabel, Plugin.fuelGauge.transform);

                indicatorObj.name = "indicator";
                // Move it down a bit
                var currentPos = indicatorObj.GetComponent<RectTransform>().anchoredPosition;
                indicatorObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    0,
                    0
                );
                //make the text bold
                indicatorObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
            }
            indicatorLabel = indicatorObj;
            indicatorLabel.GetComponent<Text>().text = "X";

            // See if we already have a child called interceptionTime
            GameObject interceptionTimeObj = null;

            foreach (Transform child in Plugin.fuelGauge.transform)
            {
                if (child.name == "interceptionTime")
                {
                    interceptionTimeObj = child.gameObject;
                    break;
                }
            }

            // If we don't have it, clone fuelLabel
            if (interceptionTimeObj == null)
            {
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
                    Plugin.Logger.LogError("Could not find fuelLabel");
                    return;
                }

                interceptionTimeObj = GameObject.Instantiate(fuelLabel, Plugin.fuelGauge.transform);

                interceptionTimeObj.name = "interceptionTime";
                // Move it down a bit
                var currentPos = interceptionTimeObj.GetComponent<RectTransform>().anchoredPosition;
                interceptionTimeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    0,
                    -100
                );
            }
            interceptionTimeLabel = interceptionTimeObj;
            interceptionTimeLabel.GetComponent<Text>().text = "";

            interceptVector = interceptPosition - Plugin.combatHUD.aircraft.rb.transform.position;
            interceptionTimeInSeconds = (int)(interceptVector.magnitude / playerVelocity.magnitude);
            Vector3 interceptScreen = Camera.main.WorldToScreenPoint(interceptPosition);
            //log notchScreen
            Plugin.Logger.LogInfo($"NOTCH SCREEN: {interceptScreen.ToString()}");
            if (tracked)
            {
                // set the color of the labels to green
                bearingLabel.GetComponent<Text>().color = Color.green;
                heightLabel.GetComponent<Text>().color = Color.green;
                indicatorLabel.GetComponent<Text>().color = Color.green;
                interceptionTimeLabel.GetComponent<Text>().color = Color.green;
            }
            else
            {
                // set the color of the labels to red
                bearingLabel.GetComponent<Text>().color = Color.red;
                heightLabel.GetComponent<Text>().color = Color.red;
                indicatorLabel.GetComponent<Text>().color = Color.green;
                interceptionTimeLabel.GetComponent<Text>().color = Color.red;
            }
            if (trackInterception)
            {
                bearingLabel.GetComponent<Text>().text = $"({interceptBearing.ToString()}°)";
                heightLabel.GetComponent<Text>().text = $"({interceptHeight.ToString()}°)";
                indicatorObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    interceptScreen.x - Screen.width / 2,
                    interceptScreen.y - Screen.height / 2
                );
                interceptionTimeLabel.GetComponent<Text>().text = $"({interceptionTimeInSeconds.ToString()}s)";
            }
            else
            {
                bearingLabel.GetComponent<Text>().text = "";
                heightLabel.GetComponent<Text>().text = "";
                indicatorLabel.GetComponent<Text>().text = "";
                interceptionTimeLabel.GetComponent<Text>().text = "";
            }
        }
        else
        {
            bearingLabel.GetComponent<Text>().text = "";
            heightLabel.GetComponent<Text>().text = "";
            indicatorLabel.GetComponent<Text>().text = "";
            interceptionTimeLabel.GetComponent<Text>().text = "";
        }
    }
}
