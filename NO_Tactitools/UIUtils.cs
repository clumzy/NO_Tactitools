using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NO_Tactitools;

public class UIUtils {
    static List<GameObject> canvasLabels = [];
    static Canvas TargetScreenCanvas;
    public static GameObject FindOrCreateLabel(
        string name,
        Vector2 position,
        Transform UIParent = null,
        bool targetScreenCanvas = false,
        FontStyle fontStyle = FontStyle.Normal,
        Color? color = null,
        int fontSize = 24) {
        // Check if the label already exists
        foreach (Transform child in UIParent) {
            if (child.name == name) {
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
        if (targetScreenCanvas) {
            canvasLabels.Add(newLabel);
        }
        return newLabel;
    }
    public static GameObject FindOrCreateLine(
        string name,
        Vector2 start,
        Vector2 end,
        Transform UIParent = null,
        bool targetScreenCanvas = false,
        Color? color = null,
        float thickness = 2f) {
        // Check if the line already exists
        foreach (Transform child in UIParent) {
            if (child.name == name) {
                return child.gameObject;
            }
        }

        // Create a new GameObject for the line
        GameObject lineObj = new(name);
        lineObj.transform.SetParent(UIParent, false);

        var image = lineObj.AddComponent<UnityEngine.UI.Image>();
        image.color = color ?? Color.white;

        var rectTransform = lineObj.GetComponent<RectTransform>();
        Vector2 direction = end - start;
        float length = direction.magnitude;
        rectTransform.sizeDelta = new Vector2(length, thickness);
        rectTransform.anchoredPosition = start + direction / 2f;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        if (targetScreenCanvas) {
            canvasLabels.Add(lineObj);
        }
        return lineObj;
    }

    public static void SetLineCoordinates(GameObject lineObj, Vector2 start, Vector2 end) {
        var rectTransform = lineObj.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        Vector2 direction = end - start;
        float length = direction.magnitude;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(length, rectTransform.sizeDelta.y);
        rectTransform.anchoredPosition = start + direction / 2f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
    }
    public static Canvas GetTargetScreenCanvas() {
        return TargetScreenCanvas;
    }

    public static void SetTargetScreenCanvas(Canvas canvas) {
        TargetScreenCanvas = canvas;
    }

    public static List<GameObject> GetCanvasLabels() {
        return canvasLabels;
    }

    public static bool RemoveCanvasLabel(GameObject label) {
        if (canvasLabels.Contains(label)) {
            canvasLabels.Remove(label);
            return true;
        }
        return false;
    }

}

[HarmonyPatch(typeof(TargetScreenUI), "LateUpdate")]
class TargetScreenUIPatch {
    static void Postfix(TargetScreenUI __instance) {
        // Check if the target screen canvas is null
        if (UIUtils.GetTargetScreenCanvas() == null) {
            // Assign the target screen canvas from the instance
            UIUtils.SetTargetScreenCanvas((Canvas)Traverse.Create(__instance).Field("displayCanvas").GetValue());
            foreach (GameObject label in UIUtils.GetCanvasLabels()) {
                // Set the parent of the label to the target screen canvas
                label.transform.SetParent(UIUtils.GetTargetScreenCanvas().transform, false);
            }
            Plugin.Logger.LogInfo("[IV] TargetScreenCanvas registered");
        }
    }
}

[HarmonyPatch(typeof(TargetScreenUI), "OnDestroy")]
class TargetScreenUIOnDestroyPatch {
    static void Postfix() {
        ClearCanvasLabels();
    }

    public static void ClearCanvasLabels() {
        UIUtils.SetTargetScreenCanvas(null);
        UIUtils.GetCanvasLabels().Clear();
        Plugin.Logger.LogInfo("[IV] TargetScreenCanvas cleared");
    }
}