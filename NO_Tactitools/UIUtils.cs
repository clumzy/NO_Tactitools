using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NO_Tactitools;

public class UIUtils {
    static List<GameObject> canvasLabels = [];
    static Canvas targetScreenCanvas;
    public static CombatHUD combatHUD;
    public static AudioClip selectAudio;
    public static FuelGauge mainHUD;
    public static CameraStateManager cameraStateManager;

    public class UILabel {
        private GameObject labelObject;
        private RectTransform rectTransform;
        private Text textComponent;

        public UILabel(
            string name,
            Vector2 position,
            Transform UIParent = null,
            bool targetScreenCanvas = false,
            FontStyle fontStyle = FontStyle.Normal,
            Color? color = null,
            int fontSize = 24,
            float backgroundOpacity = 0.8f)
        {
            // Check if the label already exists
            if (UIParent != null) {
                foreach (Transform child in UIParent) {
                    if (child.name == name) {
                        labelObject = child.gameObject;
                        break;
                    }
                }
            }
            bool isScreenCanvas = UIUtils.GetTargetScreenCanvas() != null;
            if (isScreenCanvas) {
                UIParent = UIUtils.GetTargetScreenCanvas().transform;
            }
            // Create a new GameObject for the label
            labelObject = new GameObject(name);
            labelObject.transform.SetParent(UIParent, false);
            var rect = labelObject.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 40);
            var imageComponent = labelObject.AddComponent<Image>();
            imageComponent.color = new Color(0, 0, 0, backgroundOpacity);
            GameObject textObj = new("LabelText");
            textObj.transform.SetParent(labelObject.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComp = textObj.AddComponent<Text>();
            textComp.font = SceneSingleton<CombatHUD>.i.weaponName.font;
            textComp.fontSize = fontSize;
            textComp.fontStyle = fontStyle;
            textComp.color = color ?? Color.white;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.text = "Yo";
            rect.sizeDelta = new Vector2(textComp.preferredWidth, textComp.preferredHeight);
            if (targetScreenCanvas && !isScreenCanvas) {
                canvasLabels.Add(labelObject);
                labelObject.SetActive(false);
            }
            rectTransform = labelObject.GetComponent<RectTransform>();
            var textTransform = labelObject.transform.Find("LabelText");
            if (textTransform != null && !isScreenCanvas)
                textComponent = textTransform.GetComponent<Text>();
        }

        public void SetText(string text) {
            if (textComponent != null) {
                textComponent.text = text;
                if (rectTransform != null) {
                    rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
                }
            }
        }

        public void SetColor(Color color) {
            if (textComponent != null) {
                textComponent.color = color;
            }
        }

        public void SetPosition(Vector2 position) {
            if (rectTransform != null) {
                rectTransform.anchoredPosition = position;
            }
        }
    }

    public class UILine {
        private GameObject lineObject;
        private RectTransform rectTransform;
        private Image imageComponent;
        public float thickness;

        public UILine(
            string name,
            Vector2 start,
            Vector2 end,
            Transform UIParent = null,
            bool targetScreenCanvas = false,
            Color? color = null,
            float thickness = 2f)
        {
            this.thickness = thickness;
            // Check if the line already exists
            if (UIParent != null) {
                foreach (Transform child in UIParent) {
                    if (child.name == name) {
                        lineObject = child.gameObject;
                        break;
                    }
                }
            }
            bool isScreenCanvas = UIUtils.GetTargetScreenCanvas() != null;
            if (isScreenCanvas) {
                UIParent = UIUtils.GetTargetScreenCanvas().transform;
            }
            // Create a new GameObject for the line
            lineObject = new GameObject(name);
            lineObject.transform.SetParent(UIParent, false);
            var image = lineObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color ?? Color.white;
            var rect = lineObject.GetComponent<RectTransform>();
            Vector2 direction = end - start;
            float length = direction.magnitude;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = start + direction / 2f;
            rect.pivot = new Vector2(0.5f, 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rect.rotation = Quaternion.Euler(0, 0, angle);
            if (targetScreenCanvas && !isScreenCanvas) {
                canvasLabels.Add(lineObject);
                lineObject.SetActive(false);
            }
            rectTransform = lineObject.GetComponent<RectTransform>();
            imageComponent = lineObject.GetComponent<Image>();
        }

        public void SetCoordinates(Vector2 start, Vector2 end) {
            if (rectTransform == null) return;
            Vector2 direction = end - start;
            float length = direction.magnitude;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(length, rectTransform.sizeDelta.y);
            rectTransform.anchoredPosition = start + direction / 2f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public void SetColor(Color color) {
            if (imageComponent != null) {
                imageComponent.color = color;
            }
        }

        public void SetThickness(float thickness) {
            if (rectTransform != null) {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
            }
        }

        public void ResetThickness() {
            if (rectTransform != null) {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
            }
        }

    }

    public class UIRectangle {
        private GameObject rectObject;
        private RectTransform rectTransform;
        private Image imageComponent;

        private Vector2 cornerA;
        private Vector2 cornerB;
        private Color fillColor;

        public UIRectangle(
            string name,
            Vector2 cornerA,
            Vector2 cornerB,
            Transform UIParent = null,
            bool targetScreenCanvas = false,
            Color? fillColor = null)
        {
            if (UIParent != null) {
                foreach (Transform child in UIParent) {
                    if (child.name == name) {
                        rectObject = child.gameObject;
                        break;
                    }
                }
            }
            bool isScreenCanvas = UIUtils.GetTargetScreenCanvas() != null;
            if (isScreenCanvas) {
                UIParent = UIUtils.GetTargetScreenCanvas().transform;
            }
            this.cornerA = cornerA;
            this.cornerB = cornerB;
            this.fillColor = fillColor ?? new Color(1, 1, 1, 0.1f);
            rectObject = new GameObject(name);
            rectObject.transform.SetParent(UIParent, false);
            rectTransform = rectObject.AddComponent<RectTransform>();
            imageComponent = rectObject.AddComponent<Image>();
            imageComponent.color = this.fillColor;

            UpdateRect();

            if (targetScreenCanvas && !isScreenCanvas) {
                canvasLabels.Add(rectObject);
                rectObject.SetActive(false);
            }
        }

        private void UpdateRect() {
            Vector2 min = Vector2.Min(cornerA, cornerB);
            Vector2 max = Vector2.Max(cornerA, cornerB);
            Vector2 size = max - min;
            Vector2 center = (min + max) / 2f;

            rectTransform.anchoredPosition = center;
            rectTransform.sizeDelta = size;
        }

        public void SetCorners(Vector2 a, Vector2 b) {
            cornerA = a;
            cornerB = b;
            UpdateRect();
        }

        public void SetFillColor(Color color) {
            fillColor = color;
            imageComponent.color = fillColor;
        }

        public void SetCenter(Vector2 center) {
            Vector2 size = rectTransform.sizeDelta;
            Vector2 half = size / 2f;
            cornerA = center - half;
            cornerB = center + half;
            UpdateRect();
        }

        public void MoveCenter(Vector2 delta) {
            SetCenter(rectTransform.anchoredPosition + delta);
        }

        public Vector2 GetCornerA() => cornerA;
        public Vector2 GetCornerB() => cornerB;
        public Vector2 GetCenter() => rectTransform.anchoredPosition;
        public Color GetFillColor() => fillColor;
        public GameObject GetRectObject() => rectObject;
    }

    public static Canvas GetTargetScreenCanvas() {
        return targetScreenCanvas;
    }

    public static void SetTargetScreenCanvas(Canvas canvas) {
        targetScreenCanvas = canvas;
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

[HarmonyPatch(typeof(CombatHUD), "Awake")]
class CombatHUDRegisterPatch {
    static void Postfix(CombatHUD __instance) {
        Plugin.Logger.LogInfo("[TR] CombatHUD Registered !");
        UIUtils.combatHUD = __instance;
        UIUtils.selectAudio = (AudioClip)Traverse.Create(UIUtils.combatHUD).Field("selectSound").GetValue();
    }
}

[HarmonyPatch(typeof(FuelGauge), "Initialize")]
class MainHUDRegisterPatch {
    static void Postfix(FuelGauge __instance) {
        Plugin.Logger.LogInfo("[TR] MainHUD Registered !");
        UIUtils.mainHUD = __instance;
    }
}

[HarmonyPatch(typeof(CameraStateManager), "Start")]
class CameraStateManagerRegisterPatch {
    static void Postfix(CameraStateManager __instance) {
        Plugin.Logger.LogInfo("[TR] CameraStateManager Registered !");
        UIUtils.cameraStateManager = __instance;
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
                label.SetActive(true);
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