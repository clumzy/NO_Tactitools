using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NO_Tactitools;

public class UIUtils
{
    static List<GameObject> canvasLabels = new();
    static Canvas TargetScreenCanvas;
    public static GameObject FindOrCreateLabel(
        string name,
        Vector2 position,
        Transform UIParent = null,
        bool targetScreenCanvas = false,
        FontStyle fontStyle = FontStyle.Normal,
        Color? color = null,
        int fontSize = 24)
    {
        // Check if the label already exists
        foreach (Transform child in UIParent)
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        // Create a new GameObject for the label
        GameObject newLabel = new(name);
        newLabel.transform.SetParent(UIParent, false);

        // Add RectTransform and set position
        var rectTransform = newLabel.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        // Add Text component and configure it
        var textComponent = newLabel.AddComponent<Text>();
        textComponent.font = SceneSingleton<CombatHUD>.i.weaponName.font;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color ?? Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.text = "";

        // Optionally, set size and other properties
        rectTransform.sizeDelta = new Vector2(200, 40);
        if (targetScreenCanvas)
        {
            canvasLabels.Add(newLabel);
        }
        return newLabel;
    }

    public static Canvas GetTargetScreenCanvas()
    {
        return TargetScreenCanvas;
    }

    public static void SetTargetScreenCanvas(Canvas canvas)
    {
        TargetScreenCanvas = canvas;
    }

    public static List<GameObject> GetCanvasLabels()
    {
        return canvasLabels;
    }

    public static bool RemoveCanvasLabel(GameObject label)
    {
        if (canvasLabels.Contains(label))
        {
            canvasLabels.Remove(label);
            return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(TargetScreenUI), "LateUpdate")]
class TargetScreenUIPatch
{
    static void Postfix(TargetScreenUI __instance)
    {
        // Check if the target screen canvas is null
        if (UIUtils.GetTargetScreenCanvas() == null)
        {
            // Assign the target screen canvas from the instance
            UIUtils.SetTargetScreenCanvas((Canvas)Traverse.Create(__instance).Field("displayCanvas").GetValue());
            foreach (GameObject label in UIUtils.GetCanvasLabels())
            {
                // Set the parent of the label to the target screen canvas
                label.transform.SetParent(UIUtils.GetTargetScreenCanvas().transform, false);
            }
            Plugin.Logger.LogInfo("[IV] TargetScreenCanvas registered");
        }
    }
}