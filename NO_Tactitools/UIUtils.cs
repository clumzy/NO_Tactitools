using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class UIUtilsPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[UU] UIUtils plugin starting !");
            // APPLY SUB PATCHES
            Plugin.harmony.PatchAll(typeof(HMDRegisterPatch));
            Plugin.harmony.PatchAll(typeof(HUDRegisterPatch));
            Plugin.harmony.PatchAll(typeof(CameraStateManagerRegisterPatch));
            Plugin.harmony.PatchAll(typeof(MFD_TargetRegisterPatch));
            Plugin.harmony.PatchAll(typeof(MFD_TacticalRegisterPatch));
            initialized = true;
            Plugin.Log("[UU] UIUtils plugin succesfully started !");
        }
    }
}

public class UIUtils {
    public static Transform HMD;
    public static Transform HUD;
    public static Transform targetScreen;
    public static Transform tacticalScreen;
    public static AudioClip selectAudio;
    public static CameraStateManager cameraStateManager;
    private static AudioSource audioSource;

    public static void PlaySound(string soundFileName) {
        if (audioSource == null) {
            GameObject audioGO = new("NOTT_AudioSource");
            audioSource = audioGO.AddComponent<AudioSource>();
            Object.DontDestroyOnLoad(audioGO);
        }

        string soundPath = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "assets", "sounds", soundFileName);
        if (!File.Exists(soundPath)) {
            Plugin.Logger.LogError($"[UIUtils] Sound file not found at: {soundPath}");
            return;
        }

        SceneSingleton<CombatHUD>.i.StartCoroutine(LoadAndPlayAudio(soundPath));
    }

    private static IEnumerator<UnityWebRequestAsyncOperation> LoadAndPlayAudio(string path) {
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
            Plugin.Logger.LogError("[UIUtils] Error loading audio: " + www.error);
        }
        else {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.PlayOneShot(clip);
        }
    }

    public abstract class UIElement {
        protected GameObject gameObject;
        protected RectTransform rectTransform;
        protected Image imageComponent;
        protected string mfdKey;

        protected UIElement(
            string name,
            Transform UIParent = null,
            string mfdKey = null) {
            this.mfdKey = mfdKey;
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
            // Create a new GameObject for the element
            gameObject = new GameObject(name);
            gameObject.transform.SetParent(UIParent, false);
            rectTransform = gameObject.AddComponent<RectTransform>();
            imageComponent = gameObject.AddComponent<Image>();
            return;
        }

        public virtual void SetPosition(Vector2 position) {
            rectTransform.anchoredPosition = position;
        }

        public virtual void SetColor(Color color) {
            imageComponent.color = color;
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
            FontStyle fontStyle = FontStyle.Normal,
            Color? color = null,
            int fontSize = 24,
            float backgroundOpacity = 0.8f) : base(name, UIParent) {

            rectTransform.localPosition = position;
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
            textComp.text = "";
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;
            rectTransform.sizeDelta = new Vector2(textComp.preferredWidth, textComp.preferredHeight);
            var textTransform = gameObject.transform.Find("LabelText");
            textComponent = textTransform.GetComponent<Text>();
            return;
        }

        public void SetText(string text) {
            textComponent.text = text;
            rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
        }

        public string GetText() => textComponent.text;

        public override void SetColor(Color color) {
            textComponent.color = color;
        }

        public void SetFontSize(int size) {
            textComponent.fontSize = size;
            rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
        }

        public void SetFontStyle(FontStyle style) {
            textComponent.fontStyle = style;
        }
    }

    public class UILine : UIElement {
        private float thickness;

        public UILine(
            string name,
            Vector2 start,
            Vector2 end,
            Transform UIParent = null,
            Color? color = null,
            float thickness = 2f) : base(name, UIParent) {
            this.thickness = thickness;
            imageComponent.color = color ?? Color.white;
            Vector2 direction = end - start;
            float length = direction.magnitude;
            rectTransform.sizeDelta = new Vector2(length, thickness);
            rectTransform.anchoredPosition = start + direction / 2f;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            return;
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
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
        }

        public void ResetThickness() {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
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
            Color? fillColor = null) : base(name, UIParent) {

            this.cornerA = cornerA;
            this.cornerB = cornerB;
            this.fillColor = fillColor ?? new Color(1, 1, 1, 0.1f);
            imageComponent.color = this.fillColor;

            UpdateRect();
            return;
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

    public static void HideWeaponPanel() {
        GameObject countermeasureBackground = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("countermeasureBackground").GetValue();
        GameObject weaponBackground = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("weaponBackground").GetValue();
        GameObject topRightPanel = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("topRightPanel").GetValue();
        countermeasureBackground.SetActive(false);
        weaponBackground.SetActive(false);
        CanvasGroup cg = topRightPanel.GetComponent<CanvasGroup>() ?? topRightPanel.AddComponent<CanvasGroup>();
        if (cg != null) {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    public static void RestoreWeaponPanel() {
        GameObject countermeasureBackground = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("countermeasureBackground").GetValue();
        GameObject weaponBackground = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("weaponBackground").GetValue();
        GameObject topRightPanel = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("topRightPanel").GetValue();
        countermeasureBackground.SetActive(true);
        weaponBackground.SetActive(true);
        CanvasGroup cg = topRightPanel.GetComponent<CanvasGroup>() ?? topRightPanel.AddComponent<CanvasGroup>();
        if (cg != null) {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    public static void KillLayout(Transform target) {
        var layoutGroup = target.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;
        var contentFitter = target.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (contentFitter != null) contentFitter.enabled = false;
    }

    public static void HideChildren(Transform target) {
        foreach (Transform child in target) {
            child.gameObject.SetActive(false);
        }
    }

    public static void DestroyChildren(Transform target) {
        foreach (Transform child in target) {
            GameObject.Destroy(child.gameObject);
        }
    }
}

// THIS IS THE HMD
[HarmonyPatch(typeof(CombatHUD), "Awake")]
class HMDRegisterPatch {
    static void Postfix(CombatHUD __instance) {
        UIUtils.HMD = __instance.transform;
        Plugin.Log("[UU] HMD Registered !");
    }
}

// UNUSED FOR NOW, WE'LL KEEP IT FOR LATER SO AS TO BE ABLE TO DISPLAY
// ELEMENTS ON THE MAIN HUD
// THIS IS THE HUD
[HarmonyPatch(typeof(FlightHud), "Awake")]
class HUDRegisterPatch {
    static void Postfix(FlightHud __instance) {
        UIUtils.HUD = Traverse.Create(__instance).Field("HUDCenter").GetValue<Transform>();
        Plugin.Log("[UU] HUD Registered !");
    }
}

//Sends us back the main camera
[HarmonyPatch(typeof(CameraStateManager), "Start")]
class CameraStateManagerRegisterPatch {
    static void Postfix(CameraStateManager __instance) {
        UIUtils.cameraStateManager = __instance;
        Plugin.Log("[UU] CameraStateManager Registered !");
    }
}

//Specifically the target screen with the camera
[HarmonyPatch(typeof(TargetScreenUI), "LateUpdate")]
class MFD_TargetRegisterPatch {
    static void Postfix(TargetScreenUI __instance) {
        if (UIUtils.targetScreen == null) { // we cehck for null to avoid registering multiple times
            UIUtils.targetScreen = __instance.transform;
            Plugin.Log($"[UU] Target Screen registered !");
        }
    }
}

//The entire tactical screen
[HarmonyPatch(typeof(TacScreen), "Initialize")]
class MFD_TacticalRegisterPatch {
    static void Postfix(TacScreen __instance) {
        UIUtils.tacticalScreen = __instance.transform;
        Plugin.Log($"[UU] Tactical Screen registered !");
    }
}