using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NO_Tactitools;

public class UIUtils {
    static List<GameObject> MFD_TargetLabels = [];
    public static Transform MFD_Target;
    public static Transform HMD;
    public static Transform HUD;
    public static AudioClip selectAudio;
    public static CameraStateManager cameraStateManager;

    public abstract class UIElement {
        protected GameObject gameObject;
        protected RectTransform rectTransform;
        protected Image imageComponent;

        protected UIElement(string name, Transform UIParent = null, bool DisplayOnMFD_Target = false) {
            // Check if the element already exists
            if (UIParent != null) {
                foreach (Transform child in UIParent) {
                    if (child.name == name) {
                        gameObject = child.gameObject;
                        rectTransform = gameObject.GetComponent<RectTransform>();
                        imageComponent = gameObject.GetComponent<Image>();
                        return;
                    }
                }
            }

            bool isMFD_Target_Canvas = UIUtils.GetMFD_TargetTransform() != null;
            if (isMFD_Target_Canvas) {
                UIParent = UIUtils.GetMFD_TargetTransform();
            }

            // Create a new GameObject for the element
            gameObject = new GameObject(name);
            gameObject.transform.SetParent(UIParent, false);
            rectTransform = gameObject.AddComponent<RectTransform>();
            imageComponent = gameObject.AddComponent<Image>();

            if (DisplayOnMFD_Target && !isMFD_Target_Canvas) {
                MFD_TargetLabels.Add(gameObject);
                gameObject.SetActive(false);
            }
        }

        public virtual void SetPosition(Vector2 position) {
            if (rectTransform != null) {
                rectTransform.anchoredPosition = position;
            }
        }

        public virtual void SetColor(Color color) {
            if (imageComponent != null) {
                imageComponent.color = color;
            }
        }

        public GameObject GetGameObject() => gameObject;
        public RectTransform GetRectTransform() => rectTransform;
        public Image GetImageComponent() => imageComponent;
    }
    public class UILabel : UIElement {
        private Text textComponent;

        public UILabel(
            string name,
            Vector2 position,
            Transform UIParent = null,
            bool MFD_Target_Canvas = false,
            FontStyle fontStyle = FontStyle.Normal,
            Color? color = null,
            int fontSize = 24,
            float backgroundOpacity = 0.8f) : base(name, UIParent, MFD_Target_Canvas) {

            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(200, 40);
            imageComponent.color = new Color(0, 0, 0, backgroundOpacity);

            GameObject textObj = new("LabelText");
            textObj.transform.SetParent(gameObject.transform, false);
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
            rectTransform.sizeDelta = new Vector2(textComp.preferredWidth, textComp.preferredHeight);

            var textTransform = gameObject.transform.Find("LabelText");
            if (textTransform != null && UIUtils.GetMFD_TargetTransform() == null)
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

        public override void SetColor(Color color) {
            if (textComponent != null) {
                textComponent.color = color;
            }
        }
    }
    public class UILine : UIElement {
        public float thickness;

        public UILine(
            string name,
            Vector2 start,
            Vector2 end,
            Transform UIParent = null,
            bool MFD_Target_Canvas = false,
            Color? color = null,
            float thickness = 2f) : base(name, UIParent, MFD_Target_Canvas) {

            this.thickness = thickness;
            imageComponent.color = color ?? Color.white;

            Vector2 direction = end - start;
            float length = direction.magnitude;
            rectTransform.sizeDelta = new Vector2(length, thickness);
            rectTransform.anchoredPosition = start + direction / 2f;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.Euler(0, 0, angle);
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
    public class UIRectangle : UIElement {
        private Vector2 cornerA;
        private Vector2 cornerB;
        private Color fillColor;

        public UIRectangle(
            string name,
            Vector2 cornerA,
            Vector2 cornerB,
            Transform UIParent = null,
            bool MFD_Target_Canvas = false,
            Color? fillColor = null) : base(name, UIParent, MFD_Target_Canvas) {

            this.cornerA = cornerA;
            this.cornerB = cornerB;
            this.fillColor = fillColor ?? new Color(1, 1, 1, 0.1f);
            imageComponent.color = this.fillColor;

            UpdateRect();
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
        public GameObject GetRectObject() => gameObject;
    }

    public static Transform GetMFD_TargetTransform() {
        return MFD_Target;
    }

    public static void SetMFD_TargetTransform(Transform canvas) {
        MFD_Target = canvas;
    }

    public static List<GameObject> GetMFD_TargetLabels() {
        return MFD_TargetLabels;
    }

    public static bool RemoveMFD_TargetLabels(GameObject label) {
        if (MFD_TargetLabels.Contains(label)) {
            MFD_TargetLabels.Remove(label);
            return true;
        }
        return false;
    }
}


// THIS IS THE HMD
[HarmonyPatch(typeof(CombatHUD), "Awake")]
class HMDRegisterPatch {
    static void Postfix(CombatHUD __instance) {
        Plugin.Log("[UU] HMD Registered !");
        UIUtils.HMD = __instance.transform;
        UIUtils.selectAudio = (AudioClip)Traverse.Create(UIUtils.HMD).Field("selectSound").GetValue();
    }
}


// UNUSED FOR NOW, WE'LL KEEP IT FOR LATER SO AS TO BE ABLE TO DISPLAY
// ELEMENTS ON THE MAIN HUD
// THIS IS THE HUD
[HarmonyPatch(typeof(FlightHud), "Awake")]
class HUDRegisterPatch {
    static void Postfix(FlightHud __instance) {
        Plugin.Log("[UU] HUD Registered !");
        UIUtils.HUD = Traverse.Create(__instance).Field("HUDCenter").GetValue<Transform>();
    }
}

[HarmonyPatch(typeof(CameraStateManager), "Start")]
class CameraStateManagerRegisterPatch {
    static void Postfix(CameraStateManager __instance) {
        Plugin.Log("[UU] CameraStateManager Registered !");
        UIUtils.cameraStateManager = __instance;
    }
}

// THIS IS THE TARGET MFD
[HarmonyPatch(typeof(TargetScreenUI), "LateUpdate")]
class TargetScreenRegisterPatch {
    static void Postfix(TargetScreenUI __instance) {
        // Check if the target screen canvas is null
        if (UIUtils.GetMFD_TargetTransform() == null) {
            // Assign the target screen canvas from the instance
            UIUtils.SetMFD_TargetTransform(Traverse.Create(__instance).Field("displayCanvas").GetValue<Canvas>().transform);
            foreach (GameObject label in UIUtils.GetMFD_TargetLabels()) {
                // Set the parent of the label to the target screen canvas
                label.transform.SetParent(UIUtils.GetMFD_TargetTransform().transform, false);
                label.SetActive(true);
            }
            Plugin.Log("[UU] TargetScreenCanvas registered");
        }
    }
}

[HarmonyPatch(typeof(TargetScreenUI), "OnDestroy")]
class TargetScreenOnDestroyPatch {
    static void Postfix() {
        ClearMFD_Target_Labels();
    }

    public static void ClearMFD_Target_Labels() {
        UIUtils.SetMFD_TargetTransform(null);
        UIUtils.GetMFD_TargetLabels().Clear();
        Plugin.Log("[UU] TargetScreenCanvas cleared");
    }
}