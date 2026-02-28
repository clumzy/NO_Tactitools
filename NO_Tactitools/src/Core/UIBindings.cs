using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace NO_Tactitools.Core;

public class UIBindings {
        private static Sprite _antialiasedSprite;
        private static Sprite GetAntialiasedSprite() {
            if (_antialiasedSprite != null) return _antialiasedSprite;
            int size = 64; // 64x64 texture with a 1-pixel transparent border for anti-aliasing
            Texture2D tex = new(size, size, TextureFormat.RGBA32, false);
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    bool isEdge = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                    tex.SetPixel(x, y, isEdge ? new Color(1, 1, 1, 0) : Color.white);
                }
            }
            tex.Apply();
            _antialiasedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
            return _antialiasedSprite;
        }

        public class Draw {
            public abstract class UIElement {
                protected GameObject gameObject;
                protected RectTransform rectTransform;
                protected Image imageComponent;
                protected string mfdKey;

                protected UIElement(
                    string name,
                    Transform UIParent = null,
                    string mfdKey = null,
                    Material material = null) {
                    this.mfdKey = mfdKey;
                    if (UIParent != null) {
                        foreach (Transform child in UIParent) {
                            if (child.name == name) {
                                gameObject = child.gameObject;
                                rectTransform = gameObject.GetComponent<RectTransform>();
                                imageComponent = gameObject.GetComponent<Image>();
                                if (material != null) imageComponent.material = material;
                                return;
                            }
                        }
                    }
                    // Create a new GameObject for the element
                    gameObject = new GameObject(name);
                    gameObject.transform.SetParent(UIParent, false);
                    rectTransform = gameObject.AddComponent<RectTransform>();
                    imageComponent = gameObject.AddComponent<Image>();
                    if (material != null) imageComponent.material = material;
                    return;
                }

                public virtual void SetPosition(Vector2 position) {
                    rectTransform.anchoredPosition = position;
                }

                public virtual Vector2 GetPosition() {
                    return rectTransform.anchoredPosition;
                }

                public virtual void SetColor(Color color) {
                    imageComponent.color = color;
                }

                public virtual void SetAntialiased(bool antialiased) {
                    if (antialiased) {
                        if (imageComponent.sprite == null) {
                            imageComponent.sprite = UIBindings.GetAntialiasedSprite();
                            imageComponent.type = Image.Type.Sliced;
                            imageComponent.pixelsPerUnitMultiplier = 1f;
                            rectTransform.sizeDelta += new Vector2(2, 2);
                        }
                    }
                    else {
                        if (imageComponent.sprite != null) {
                            imageComponent.sprite = null;
                            imageComponent.type = Image.Type.Simple;
                            rectTransform.sizeDelta -= new Vector2(2, 2);
                        }
                    }
                }

                public GameObject GetGameObject() => gameObject;
                public RectTransform GetRectTransform() => rectTransform;
                public Image GetImageComponent() => imageComponent;

                public void Destroy() {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            public class UILabel : UIElement {
                private Text textComponent;
                private float backgroundOpacity;
                private float textOpacity;

                public UILabel(
                    string name,
                    Vector2 position,
                    Transform UIParent = null,
                    FontStyle fontStyle = FontStyle.Normal,
                    Color? color = null,
                    int fontSize = 24,
                    float backgroundOpacity = 0.8f,
                    Material material = null) : base(name, UIParent) {
                    this.backgroundOpacity = backgroundOpacity;
                    rectTransform.anchoredPosition = position;
                    rectTransform.sizeDelta = new Vector2(200, 40);
                    imageComponent.color = new Color(0, 0, 0, this.backgroundOpacity);
                    GameObject textObj = new("LabelText");
                    textObj.transform.SetParent(gameObject.transform, false);
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    Text textComp = textObj.AddComponent<Text>();
                    textComp.font = UIBindings.Draw.GetDefaultFont();
                    textComp.fontSize = fontSize;
                    textComp.fontStyle = fontStyle;
                    textComp.color = color ?? Color.white;
                    this.textOpacity = textComp.color.a;
                    textComp.alignment = TextAnchor.MiddleCenter;
                    textComp.text = "";
                    textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textComp.verticalOverflow = VerticalWrapMode.Overflow;
                    rectTransform.sizeDelta = new Vector2(textComp.preferredWidth, textComp.fontSize);
                    Transform textTransform = gameObject.transform.Find("LabelText");
                    textComponent = textTransform.GetComponent<Text>();
                    if (material != null) {
                        textComponent.material = material;
                    }
                    return;
                }

                public void SetText(string text) {
                    textComponent.text = text;
                    rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.fontSize);
                }

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

                public void SetOpacity(float opacity) {
                    opacity = Mathf.Clamp01(opacity);
                    Color textColor = textComponent.color;
                    textComponent.color = new Color(textColor.r, textColor.g, textColor.b, textOpacity * opacity);
                    Color bgColor = imageComponent.color;
                    imageComponent.color = new Color(bgColor.r, bgColor.g, bgColor.b, backgroundOpacity * opacity);
                }
                public string GetText() => textComponent.text;

                public Vector2 GetTextSize() {
                    return new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
                }
            }

            public class UILine : UIElement {
                private float thickness;
                private float baseOpacity;

                public UILine(
                    string name,
                    Vector2 start,
                    Vector2 end,
                    Transform UIParent = null,
                    Color? color = null,
                    float thickness = 2f,
                    Material material = null,
                    bool antialiased = false) : base(name, UIParent, material: material) {
                    this.thickness = thickness;
                    imageComponent.color = color ?? Color.white;
                    this.baseOpacity = imageComponent.color.a;

                    Vector2 direction = end - start;
                    float length = direction.magnitude;
                    rectTransform.sizeDelta = new Vector2(length, thickness);
                    rectTransform.anchoredPosition = start + direction / 2f;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

                    if (antialiased) SetAntialiased(true);
                    return;
                }

                public override void SetAntialiased(bool antialiased) {
                    base.SetAntialiased(antialiased);
                }

                public void SetCoordinates(Vector2 start, Vector2 end) {
                    if (rectTransform == null) return;
                    Vector2 direction = end - start;
                    float length = direction.magnitude;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    
                    // If antialiased, we need to add the 2-pixel padding (1px on each side)
                    float padding = (imageComponent.sprite != null) ? 2f : 0f;
                    rectTransform.sizeDelta = new Vector2(length + padding, rectTransform.sizeDelta.y);
                    
                    rectTransform.anchoredPosition = start + direction / 2f;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                }

                public void SetThickness(float thickness) {
                    float padding = (imageComponent.sprite != null) ? 2f : 0f;
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness + padding);
                }

                public void ResetThickness() {
                    float padding = (imageComponent.sprite != null) ? 2f : 0f;
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness + padding);
                }

                public void SetOpacity(float opacity) {
                    opacity = Mathf.Clamp01(opacity);
                    Color color = imageComponent.color;
                    imageComponent.color = new Color(color.r, color.g, color.b, baseOpacity * opacity);
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
                    Color? fillColor = null,
                    Material material = null) : base(name, UIParent, material: material) {

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
                    float padding = (imageComponent.sprite != null) ? 2f : 0f;
                    rectTransform.sizeDelta = size + new Vector2(padding, padding);
                }

                public virtual void SetCorners(Vector2 a, Vector2 b) {
                    cornerA = a;
                    cornerB = b;
                    UpdateRect();
                }

                public virtual void SetFillColor(Color color) {
                    fillColor = color;
                    imageComponent.color = fillColor;
                }

                public virtual void SetCenter(Vector2 center) {
                    Vector2 size = rectTransform.sizeDelta;
                    Vector2 half = size / 2f;
                    cornerA = center - half;
                    cornerB = center + half;
                    UpdateRect();
                }

                public Vector2 GetSize() => rectTransform.sizeDelta;

                public Vector2 GetCornerA() => cornerA;
                public Vector2 GetCornerB() => cornerB;
                public Vector2 GetCenter() => rectTransform.anchoredPosition;
                public Color GetFillColor() => fillColor;
                public GameObject GetRectObject() => gameObject;
            }

            public class UIAdvancedRectangle : UIRectangle {
                private UILine topBorder;
                private UILine bottomBorder;
                private UILine leftBorder;
                private UILine rightBorder;
                private float borderThickness;
                private Color borderColor;

                public UIAdvancedRectangle(
                    string name,
                    Vector2 cornerA,
                    Vector2 cornerB,
                    Color borderColor,
                    float borderThickness,
                    Transform UIParent = null,
                    Color? fillColor = null,
                    Material material = null) : base(name, cornerA, cornerB, UIParent, fillColor, material: material) {

                    this.borderColor = borderColor;
                    this.borderThickness = borderThickness;

                    topBorder = new UILine(name + "_Top", Vector2.zero, Vector2.zero, gameObject.transform, borderColor, borderThickness, material: material);
                    bottomBorder = new UILine(name + "_Bottom", Vector2.zero, Vector2.zero, gameObject.transform, borderColor, borderThickness, material: material);
                    leftBorder = new UILine(name + "_Left", Vector2.zero, Vector2.zero, gameObject.transform, borderColor, borderThickness, material: material);
                    rightBorder = new UILine(name + "_Right", Vector2.zero, Vector2.zero, gameObject.transform, borderColor, borderThickness, material: material);

                    topBorder = new UILine(
                        name + "_Top",
                        Vector2.zero,
                        Vector2.zero,
                        gameObject.transform,
                        borderColor,
                        borderThickness,
                        material: material,
                        antialiased: true
                    );
                    bottomBorder = new UILine(
                        name + "_Bottom",
                        Vector2.zero,
                        Vector2.zero,
                        gameObject.transform,
                        borderColor,
                        borderThickness,
                        material: material,
                        antialiased: true
                    );
                    leftBorder = new UILine(
                        name + "_Left",
                        Vector2.zero,
                        Vector2.zero,
                        gameObject.transform,
                        borderColor,
                        borderThickness,
                        material: material,
                        antialiased: true
                    );
                    rightBorder = new UILine(
                        name + "_Right",
                        Vector2.zero,
                        Vector2.zero,
                        gameObject.transform,
                        borderColor,
                        borderThickness,
                        material: material,
                        antialiased: true
                    );

                    UpdateBorders();
                }

                private void UpdateBorders() {
                    Vector2 size = GetSize();
                    Vector2 halfSize = size / 2f;
                    float t = borderThickness;

                    // Outward borders matching the existing outward rectangle logic
                    // Top: Left to Right across the top edge (extended by t)
                    topBorder.SetCoordinates(new Vector2(-halfSize.x - t, halfSize.y + t / 2f), new Vector2(halfSize.x + t, halfSize.y + t / 2f));
                    topBorder.SetThickness(t);

                    // Bottom: Left to Right across the bottom edge (extended by t)
                    bottomBorder.SetCoordinates(new Vector2(-halfSize.x - t, -halfSize.y - t / 2f), new Vector2(halfSize.x + t, -halfSize.y - t / 2f));
                    bottomBorder.SetThickness(t);

                    // Left: Top to Bottom between the top/bottom extensions
                    leftBorder.SetCoordinates(new Vector2(-halfSize.x - t / 2f, halfSize.y), new Vector2(-halfSize.x - t / 2f, -halfSize.y));
                    leftBorder.SetThickness(t);

                    // Right: Top to Bottom between the top/bottom extensions
                    rightBorder.SetCoordinates(new Vector2(halfSize.x + t / 2f, halfSize.y), new Vector2(halfSize.x + t / 2f, -halfSize.y));
                    rightBorder.SetThickness(t);

                    topBorder.SetColor(borderColor);
                    bottomBorder.SetColor(borderColor);
                    leftBorder.SetColor(borderColor);
                    rightBorder.SetColor(borderColor);
                }

                public override void SetCorners(Vector2 a, Vector2 b) {
                    base.SetCorners(a, b);
                    UpdateBorders();
                }

                public void SetBorderColor(Color color) {
                    borderColor = color;
                    UpdateBorders();
                }

                public void SetBorderThickness(float thickness) {
                    borderThickness = thickness;
                    UpdateBorders();
                }
            }

            public class UIAdvancedRectangleLabeled : UIAdvancedRectangle {
                private UILabel label;

                public UIAdvancedRectangleLabeled(
                    string name,
                    Vector2 cornerA,
                    Vector2 cornerB,
                    Color borderColor,
                    float borderThickness,
                    Transform UIParent = null,
                    Color? fillColor = null,
                    FontStyle fontStyle = FontStyle.Normal,
                    Color? textColor = null,
                    int fontSize = 24,
                    Material material = null)
                    : base(name, cornerA, cornerB, borderColor, borderThickness, UIParent, fillColor, material: material) {

                    label = new UILabel(name + "_Label", Vector2.zero, gameObject.transform, fontStyle, textColor, fontSize, 0, material: material);
                    label.SetPosition(Vector2.zero);
                }

                public void SetText(string text) {
                    label.SetText(text);
                }

                public void SetTextColor(Color color) {
                    label.SetColor(color);
                }

                public UILabel GetLabel() => label;
            }

            public static Font GetDefaultFont() {
                Text weaponText = UIBindings.Game.GetFlightHUDTransform().GetComponentInChildren<Text>();
                return weaponText.font;
            }
        }

        public class Game {
            private static readonly TraverseCache<CombatHUD, GameObject> _topRightPanelCache = new("topRightPanel");
            private static readonly TraverseCache<TargetCam, TargetScreenUI> _targetScreenUICache = new("targetScreenUI");
            private static readonly TraverseCache<Cockpit, TacScreen> _tacScreenCache = new("tacScreen");
            private static readonly TraverseCache<FlightHud, Transform> _flightHUDCenterCache = new("HUDCenter");
            private static TacScreen _cachedTacScreen = null; // For caching the tacscreen instance since finding it is expensive
            
            public static void DisplayToast(string message, float duration = 2f) {
                SceneSingleton<AircraftActionsReport>.i?.ReportText(message, duration);
            }

            public static Transform GetCombatHUDTransform() { // HMD
                try {
                    return SceneSingleton<CombatHUD>.i.transform;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static CombatHUD GetCombatHUDComponent() {
                try {
                    return SceneSingleton<CombatHUD>.i;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static Transform GetFlightHUDTransform() { // HUD
                try {
                    return SceneSingleton<FlightHud>.i.transform;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static Transform GetFlightHUDCenterTransform() {
                try {
                    Transform hudLockedTransform = _flightHUDCenterCache.GetValue(SceneSingleton<FlightHud>.i);
                    return hudLockedTransform;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            // using find functions, get the first material from a text
            public static Material GetFlightHUDFontMaterial() {
                try {
                    Text textComponent = SceneSingleton<FlightHud>.i.transform.GetComponentInChildren<Text>();
                    return textComponent.material;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static Transform GetTargetScreenTransform(bool silent = false) {
                try {
                    TargetCam currentTargetCam = SceneSingleton<CombatHUD>.i.aircraft.targetCam;
                    TargetScreenUI targetScreenUIObject = _targetScreenUICache.GetValue(currentTargetCam);
                    return targetScreenUIObject.transform;
                }
                catch (NullReferenceException e) {
                    if (!silent)
                        Plugin.Log(e.ToString());
                    return null;
                }
            }

            public static TargetScreenUI GetTargetScreenUIComponent(bool silent = false) {
                try {
                    TargetCam currentTargetCam = SceneSingleton<CombatHUD>.i.aircraft.targetCam;
                    TargetScreenUI targetScreenUI = _targetScreenUICache.GetValue(currentTargetCam);
                    return targetScreenUI;
                }
                catch (NullReferenceException e) {
                    if (!silent)
                        Plugin.Log(e.ToString());
                    return null;
                }
            }

            public static TargetCam GetTargetCamComponent() {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft.targetCam;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static Transform GetTacScreenTransform(bool silent = false) {
                try {
                    TacScreen tacScreenObject = GetTacScreenComponent(silent:silent);
                    if (tacScreenObject != null) {
                        return tacScreenObject.transform.Find("Canvas").transform;
                    }
                    else {
                        if (!silent)
                            Plugin.Log("[UIBindings.Game.GetTacScreen] TacScreen component not found; returning null.");
                        return null;
                    }
                }
                catch (NullReferenceException e) { 
                    if (!silent)
                        Plugin.Log(e.ToString()); 
                    return null; }
            }

            public static TacScreen GetTacScreenComponent(bool silent = false) {
                try {
                    if (_cachedTacScreen != null) {
                        return _cachedTacScreen;
                    }

                    else{
                        TacScreen tacScreenObject = null;
                        tacScreenObject = UnityEngine.Object.FindObjectOfType<TacScreen>();
                        if (tacScreenObject != null) {
                            _cachedTacScreen = tacScreenObject;
                            Plugin.Log("[BD] Found TacScreen via Object.FindObjectOfType.");
                            return tacScreenObject;
                        }
                    }

                    if (!silent)
                        Plugin.Log("[BD] No Cockpit with TacScreen found !");
                    return null;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static WeaponStatus GetWeaponStatus() {
                try {
                    CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                    GameObject topRightPanel = _topRightPanelCache.GetValue(currentCombatHUD);
                    WeaponStatus weaponStatus = topRightPanel.GetComponentInChildren<WeaponStatus>();
                    return weaponStatus;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
                catch (IndexOutOfRangeException) { return null; } // this means the game is paused and there is no weapon status displayed
            }

            public static CameraStateManager GetCameraStateManager() {
                try {
                    return SceneSingleton<CameraStateManager>.i;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
            }

            public static void HideWeaponPanel() {
                CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                GameObject topRightPanel = _topRightPanelCache.GetValue(currentCombatHUD);
                topRightPanel.SetActive(false);
            }

            public static void ShowWeaponPanel() {
                CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                GameObject topRightPanel = _topRightPanelCache.GetValue(currentCombatHUD);
                topRightPanel.SetActive(true);
            }
        }

        public class Generic {
            public static void KillLayout(Transform target) {
                UnityEngine.UI.LayoutGroup layoutGroup = target.GetComponent<UnityEngine.UI.LayoutGroup>();
                if (layoutGroup != null) layoutGroup.enabled = false;
                UnityEngine.UI.ContentSizeFitter contentFitter = target.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (contentFitter != null) contentFitter.enabled = false;
            }

            public static void HideChildren(Transform target) {
                foreach (Transform child in target) {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public class Sound {
            public static Dictionary<string, AudioClip> loadedClips = [];
            public static void PlaySound(string fileName) {
                static IEnumerator PlayAudio(string fileName) {
                    AudioClip clip = loadedClips[fileName];
                    if (clip == null) {
                        Plugin.Log("[UIUtils] Loaded audio clip" + fileName + "is null.");
                        yield break;
                    }
                    SoundManager.PlayInterfaceOneShot(clip);
                }

                SceneSingleton<CombatHUD>.i.StartCoroutine(PlayAudio(fileName));
            }

            public static void LoadAllSounds() {
                string soundsDirectory = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "assets", "sounds");
                if (!Directory.Exists(soundsDirectory)) {
                    Plugin.Log($"[UIUtils] Sounds directory not found at: {soundsDirectory}");
                    return;
                }

                foreach (string filePath in Directory.GetFiles(soundsDirectory)) {
                    string fileName = Path.GetFileName(filePath).Split('.')[0];
                    if (!loadedClips.ContainsKey(fileName)) {
                        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
                        UnityEngine.Networking.UnityWebRequestAsyncOperation operation = www.SendWebRequest();
                        while (!operation.isDone) { } // Wait for completion

                        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
                            Plugin.Log("[UIUtils] Error loading audio: " + www.error);
                        }
                        else {
                            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                            if (clip != null) {
                                loadedClips[fileName] = clip;
                                Plugin.Log("[UIUtils] Loaded audio clip: " + fileName);
                            }
                            else {
                                Plugin.Log("[UIUtils] Loaded audio clip is null for file: " + fileName);
                            }
                        }
                    }
                }
            }
        }
}
