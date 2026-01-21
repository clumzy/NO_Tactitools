using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using NOAutopilot; // Reference from .csproj
using Plugin = NO_Tactitools.Core.Plugin;
using NO_Tactitools.Core;
using System;
using System.Collections.Generic;
using static NO_Tactitools.Core.Bindings.UI.Draw; // Explicit alias to resolve ambiguity

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
public static class NOAutopilotControlPlugin {
    private static bool initialized = false;
    private const string AutopilotModGUID = "com.qwerty1423.NOAutopilot";

    public static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[AP] NOAutopilotControl checking dependencies...");

            if (Chainloader.PluginInfos.ContainsKey(AutopilotModGUID)) {
                Plugin.Log("[AP] Found 'no-autopilot-mod'. Enabling Autopilot controls.");
                Plugin.harmony.PatchAll(typeof(NOAutopilotComponent.OnPlatformStart));
                Plugin.harmony.PatchAll(typeof(NOAutopilotComponent.OnPlatformUpdate));
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotOpenMenuInput.Value,
                    0.2f,
                    ToggleMenu
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotEnterInput.Value
                    // TODO: Add Select Action
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotUpInput.Value,
                    0.2f,
                    NavigateUp
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotDownInput.Value,
                    0.2f,
                    NavigateDown
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotLeftInput.Value,
                    0.2f,
                    NavigateLeft
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotControllerName.Value,
                    Plugin.autopilotRightInput.Value,
                    0.2f,
                    NavigateRight
                );
            }
            else {
                Plugin.Log("[AP] 'no-autopilot-mod' not found. Autopilot controls disabled.");
            }

            initialized = true;
        }
    }

    public static void ToggleMenu() {
        NOAutopilotComponent.InternalState.showMenu = !NOAutopilotComponent.InternalState.showMenu;
        Plugin.Log($"[AP] Toggle Menu: {NOAutopilotComponent.InternalState.showMenu.ToString()}");
    }

    public static void NavigateUp() {
        if (!NOAutopilotComponent.InternalState.showMenu || NOAutopilotComponent.InternalState.autopilotMenu == null) return;
        var menu = NOAutopilotComponent.InternalState.autopilotMenu;
        
        // Prevent row change on side buttons
        if (menu.selectedCol == 0 || menu.selectedCol == 5) return;

        int oldRow = menu.selectedRow;
        menu.selectedRow = Mathf.Max(0, menu.selectedRow - 1);

        // Snapping logic when leaving Row 5
        if (oldRow == 5 && menu.selectedRow != 5) {
            menu.selectedCol = NOAutopilotComponent.InternalState.lastGridCol;
        }
    }

    public static void NavigateDown() {
        if (!NOAutopilotComponent.InternalState.showMenu || NOAutopilotComponent.InternalState.autopilotMenu == null) return;
        var menu = NOAutopilotComponent.InternalState.autopilotMenu;

        // Prevent row change on side buttons
        if (menu.selectedCol == 0 || menu.selectedCol == 5) return;

        int oldRow = menu.selectedRow;
        
        // Save column before going to row 5
        if (oldRow != 5) {
            NOAutopilotComponent.InternalState.lastGridCol = menu.selectedCol;
        }

        menu.selectedRow = Mathf.Min(5, menu.selectedRow + 1);
        
        // Snapping logic for Row 5
        if (menu.selectedRow == 5 && oldRow != 5) {
            if (menu.selectedCol == 1) menu.selectedCol = 1; // Value -> AJ
            else if (menu.selectedCol >= 2 && menu.selectedCol <= 4) menu.selectedCol = 2; // Grid buttons -> GCAS
        }
    }

    public static void NavigateLeft() {
        if (!NOAutopilotComponent.InternalState.showMenu || NOAutopilotComponent.InternalState.autopilotMenu == null) return;
        var menu = NOAutopilotComponent.InternalState.autopilotMenu;
        if (menu.selectedRow == 5) {
            if (menu.selectedCol == 5) menu.selectedCol = 2; // Set -> GCAS
            else if (menu.selectedCol == 2) menu.selectedCol = 1; // GCAS -> AJ
            else if (menu.selectedCol == 1) menu.selectedCol = 0; // AJ -> Engage
            else menu.selectedCol = 0;

            // Sync grid return column
            if (menu.selectedCol == 1) NOAutopilotComponent.InternalState.lastGridCol = 1;
            else if (menu.selectedCol == 2) NOAutopilotComponent.InternalState.lastGridCol = 2;
        } else {
            menu.selectedCol = Mathf.Max(0, menu.selectedCol - 1);
        }
    }

    public static void NavigateRight() {
        if (!NOAutopilotComponent.InternalState.showMenu || NOAutopilotComponent.InternalState.autopilotMenu == null) return;
        var menu = NOAutopilotComponent.InternalState.autopilotMenu;
        if (menu.selectedRow == 5) {
            if (menu.selectedCol == 0) menu.selectedCol = 1; // Engage -> AJ
            else if (menu.selectedCol == 1) menu.selectedCol = 2; // AJ -> GCAS
            else if (menu.selectedCol == 2) menu.selectedCol = 5; // GCAS -> Set
            else menu.selectedCol = 5;

            // Sync grid return column
            if (menu.selectedCol == 1) NOAutopilotComponent.InternalState.lastGridCol = 1;
            else if (menu.selectedCol == 2) NOAutopilotComponent.InternalState.lastGridCol = 2;
        } else {
            menu.selectedCol = Mathf.Min(5, menu.selectedCol + 1);
        }
    }
}

public class NOAutopilotComponent {
    static class LogicEngine {
        static public void Init() {
            // Initialization logic if needed
        }

        static public void Update() {
            try {
                // Row 1: Altitude
                InternalState.currentAlt = APData.CurrentAlt;
                InternalState.targetAlt = APData.TargetAlt;

                // Row 2: Vertical Speed
                InternalState.currentVS = APData.PlayerRB?.velocity.y ?? 0f;
                InternalState.maxClimbRate = APData.CurrentMaxClimbRate;

                // Row 3: Roll
                InternalState.currentRoll = APData.CurrentRoll;
                InternalState.targetRoll = APData.TargetRoll;

                // Row 4: Speed
                InternalState.currentTAS = APData.LocalAircraft?.speed ?? 0f;
                InternalState.targetSpeed = APData.TargetSpeed;

                // Row 5: Course
                InternalState.currentCourse = 0f;
                if (APData.PlayerRB != null && APData.PlayerRB.velocity.sqrMagnitude > 1f) {
                    Vector3 flatVel = Vector3.ProjectOnPlane(APData.PlayerRB.velocity, Vector3.up);
                    InternalState.currentCourse = Quaternion.LookRotation(flatVel).eulerAngles.y;
                }
                InternalState.targetCourse = APData.TargetCourse;

                // System States
                InternalState.apEnabled = APData.Enabled;
                InternalState.ajActive = APData.AutoJammerActive;
                InternalState.gcasEnabled = APData.GCASEnabled;
            }
            catch (Exception) {
                // Silent for now
            }
        }
    }

    public static class InternalState {
        static public NOAutoPilotMenu autopilotMenu;
        static public bool showMenu = false;
        static public Color mainColor = Color.green;
        static public Color textColor = Color.green;
        static public int lastGridCol = 1;

        // Autopilot Data
        public static float currentAlt;
        public static float targetAlt;
        public static float currentVS;
        public static float maxClimbRate;
        public static float currentRoll;
        public static float targetRoll;
        public static float currentTAS;
        public static float targetSpeed;
        public static float currentCourse;
        public static float targetCourse;
        public static bool apEnabled;
        public static bool ajActive;
        public static bool gcasEnabled;
    }

    static class DisplayEngine {
        static public void Init() {
            // Initialization logic
            InternalState.autopilotMenu?.Destroy();
            InternalState.autopilotMenu = null;
            InternalState.autopilotMenu = new NOAutoPilotMenu();
        }

        static public void Update() {
            if (InternalState.autopilotMenu == null) return;
            InternalState.autopilotMenu.SetVisible();
            if (InternalState.showMenu) {
                InternalState.autopilotMenu.UpdateColors(InternalState.mainColor, InternalState.textColor);
                InternalState.autopilotMenu.UpdateValues();
            }
        }
    }

    public class NOAutoPilotMenu {
        public GameObject containerObject;
        public Transform containerTransform;

        public UIAdvancedRectangleLabeled engagedBar;
        public UIAdvancedRectangleLabeled setBar;
        public UIAdvancedRectangleLabeled ajButton;
        public UIAdvancedRectangleLabeled gcasButton;

        public List<UIAdvancedRectangleLabeled> valueRects = new();
        public List<UIAdvancedRectangleLabeled> cRects = new();
        public List<UIAdvancedRectangleLabeled> minusRects = new();
        public List<UIAdvancedRectangleLabeled> plusRects = new();

        public int selectedRow = 0;
        public int selectedCol = 0;
        public int fontSize = 34;
        public float padding = 0;

        public NOAutoPilotMenu() {
            Transform parentTransform = Bindings.UI.Game.GetTacScreenTransform();
            string platformName = Bindings.Player.Aircraft.GetPlatformName();

            containerObject = new GameObject("i_ap_NOAutopilotMenu");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parentTransform, false);

            float xOffset = 0;
            float yOffset = 0;

            // Positioning offsets - adjusted to keep similar positions to loadout preview or platform specific needs
            switch (platformName) {
                case "CI-22 Cricket":
                    xOffset = -105;
                    yOffset = 0;
                    fontSize = 44;
                    break;
                case "SAH-46 Chicane":
                    xOffset = -130;
                    yOffset = 65;
                    break;
                case "T/A-30 Compass":
                    xOffset = 0;
                    yOffset = 80;
                    break;
                case "FS-3 Ternion":
                case "FS-12 Revoker":
                    xOffset = 0;
                    yOffset = 75;
                    break;
                case "FS-20 Vortex":
                    xOffset = 0;
                    yOffset = 75;
                    break;
                case "KR-67 Ifrit":
                    xOffset = -130;
                    yOffset = 65;
                    break;
                case "VL-49 Tarantula":
                    xOffset = -255;
                    yOffset = 60;
                    fontSize = 28;
                    break;
                case "EW-1 Medusa":
                    xOffset = -225;
                    yOffset = 65;
                    break;
                case "SFB-81":
                    xOffset = -180;
                    yOffset = 60;
                    break;
                case "UH-80 Ibis":
                    xOffset = -245;
                    yOffset = 65;
                    break;
                case "A-19 Brawler":
                    yOffset = 70;
                    break;
                case "FQ-106 Kestrel":
                    yOffset = 75;
                    break;
            }

            // Apply global offset to the container
            containerTransform.localPosition += new Vector3(xOffset, yOffset, 0);


            float unit = fontSize + 6;
            float gap = unit / 4f;
            padding = gap;

            // Element Sizes
            Vector2 valueBoxSize = new Vector2(4 * unit, unit);
            Vector2 buttonSize = new Vector2(unit, unit);
            float bottomButtonHeight = unit * 1.25f;

            // Grid Dimensions
            float gridRowWidth = 7 * unit + 3 * gap;
            Vector2 ajSize = new Vector2(4 * unit, bottomButtonHeight);
            Vector2 gcasSize = new Vector2(3 * unit + 2 * gap, bottomButtonHeight);

            // Total Block Content
            float engagedBarWidth = unit;
            float setBarWidth = unit;

            // Grid Height
            float gridHeight = 5 * unit + 4 * gap;

            // Total Content Height (Engaged Bar matches this total span + bottom buttons)
            float totalContentHeight = gridHeight + gap + bottomButtonHeight;

            // Total Content Width
            float totalContentWidth = engagedBarWidth + gap + gridRowWidth + gap + setBarWidth;

            // Center Reference (0,0) is center of valid content area
            Vector2 contentTopLeft = new Vector2(-totalContentWidth / 2f, totalContentHeight / 2f);

            // Dimensions and Layout
            Vector2 bgSize = new Vector2(totalContentWidth + 2 * padding, totalContentHeight + 2 * padding);
            Vector2 bgCenter = Vector2.zero;
            new UIAdvancedRectangle(
                "i_ap_Background",
                bgCenter - bgSize / 2f,
                bgCenter + bgSize / 2f,
                InternalState.mainColor, 2, containerTransform, Color.black
            );
            Vector2 engagedSize = new Vector2(totalContentHeight, engagedBarWidth);
            Vector2 engagedCenter = new Vector2(contentTopLeft.x + engagedBarWidth / 2f, 0);

            engagedBar = new UIAdvancedRectangleLabeled(
                "i_ap_EngagedBar",
                engagedCenter - engagedSize / 2f,
                engagedCenter + engagedSize / 2f,
                Color.green, 2, containerTransform,
                Color.clear, // Transparent fill
                FontStyle.Bold,
                Color.green, // Text matches border
                fontSize - 10 // Slightly smaller for vertical bar text? Or keep font size.
            );
            engagedBar.SetText("ENGAGED");
            engagedBar.GetLabel().SetFontSize(fontSize - 4); // Adjustment for fitting
            engagedBar.GetRectTransform().localRotation = Quaternion.Euler(0, 0, 90f);

            string[] defaultValues = new string[] { "- m", "- m/s", "-° bank", "- km/h", "-° head" };

            // Grid Top Left Reference
            float gridLeftX = contentTopLeft.x + engagedBarWidth + gap;
            float gridTopY = contentTopLeft.y; // Start at top

            for (int i = 0; i < 5; i++) {
                float y = gridTopY - i * (unit + gap) - unit / 2f;

                // Value Box
                Vector2 valCenter = new Vector2(gridLeftX + valueBoxSize.x / 2f, y);
                var vRect = new UIAdvancedRectangleLabeled(
                    $"i_ap_ValRect_{i.ToString()}",
                    valCenter - valueBoxSize / 2f, valCenter + valueBoxSize / 2f,
                    InternalState.mainColor, 2, containerTransform,
                    Color.clear,
                    FontStyle.Normal,
                    Color.white,
                    fontSize - 4 // Slightly compressed for values
                );
                vRect.SetText(defaultValues[i]);
                valueRects.Add(vRect);

                float currentX = gridLeftX + valueBoxSize.x + gap;

                // C Button
                Vector2 cCenter = new Vector2(currentX + buttonSize.x / 2f, y);
                var cRect = new UIAdvancedRectangleLabeled(
                    $"i_ap_CRect_{i.ToString()}",
                    cCenter - buttonSize / 2f, cCenter + buttonSize / 2f,
                    InternalState.mainColor, 2, containerTransform,
                    Color.clear,
                    FontStyle.Normal,
                    Color.white,
                    fontSize
                );
                cRect.SetText("C");
                cRects.Add(cRect);
                currentX += buttonSize.x + gap;

                // Minus Button
                Vector2 mCenter = new Vector2(currentX + buttonSize.x / 2f, y);
                var mRect = new UIAdvancedRectangleLabeled(
                    $"i_ap_MinusRect_{i.ToString()}",
                    mCenter - buttonSize / 2f, mCenter + buttonSize / 2f,
                    InternalState.mainColor, 2, containerTransform,
                    Color.clear,
                    FontStyle.Normal,
                    Color.white,
                    fontSize
                );
                mRect.SetText("-");
                minusRects.Add(mRect);
                currentX += buttonSize.x + gap;

                // Plus Button
                Vector2 pCenter = new Vector2(currentX + buttonSize.x / 2f, y);
                var pRect = new UIAdvancedRectangleLabeled(
                    $"i_ap_PlusRect_{i.ToString()}",
                    pCenter - buttonSize / 2f, pCenter + buttonSize / 2f,
                    InternalState.mainColor, 2, containerTransform,
                    Color.clear,
                    FontStyle.Normal,
                    Color.white,
                    fontSize
                );
                pRect.SetText("+");
                plusRects.Add(pRect);
            }

            Vector2 setSizeVisual = new Vector2(totalContentHeight, setBarWidth); // Rotated creation
            // Center X = gridLeftX + gridRowWidth + gap + setBarWidth/2f
            float setCenterX = gridLeftX + gridRowWidth + gap + setBarWidth / 2f;
            Vector2 setCenter = new Vector2(setCenterX, 0);

            setBar = new UIAdvancedRectangleLabeled(
                "i_ap_SetBar",
                setCenter - setSizeVisual / 2f,
                setCenter + setSizeVisual / 2f,
                InternalState.mainColor, 2, containerTransform,
                Color.clear,
                FontStyle.Normal,
                Color.white,
                fontSize - 4
            );
            setBar.SetText("SET");
            setBar.GetRectTransform().localRotation = Quaternion.Euler(0, 0, 90f);

            float bottomY = contentTopLeft.y - gridHeight - gap - bottomButtonHeight / 2f;

            Vector2 ajCenter = new Vector2(gridLeftX + ajSize.x / 2f, bottomY);

            ajButton = new UIAdvancedRectangleLabeled(
                "i_ap_AJButton",
                ajCenter - ajSize / 2f, ajCenter + ajSize / 2f,
                Color.red, 2, containerTransform,
                Color.clear,
                FontStyle.Bold,
                Color.red,
                fontSize
            );
            ajButton.SetText("AJ");

            float gcasStartX = gridLeftX + ajSize.x + gap;
            Vector2 gcasCenter = new Vector2(gcasStartX + gcasSize.x / 2f, bottomY);

            gcasButton = new UIAdvancedRectangleLabeled(
                "i_ap_GCASButton",
                gcasCenter - gcasSize / 2f, gcasCenter + gcasSize / 2f,
                Color.green, 2, containerTransform,
                Color.clear,
                FontStyle.Bold,
                Color.green,
                fontSize
            );
            gcasButton.SetText("GCAS");
            containerObject.SetActive(false);
        }

        public void Destroy() {
            if (containerObject != null) {
                UnityEngine.Object.Destroy(containerObject);
                containerObject = null;
            }
        }

        public void UpdateValues() {
            if (valueRects.Count < 5) return;

            // Row 1: Altitude
            valueRects[0].SetText(InternalState.targetAlt.ToString("0") + " m");
            valueRects[1].SetText(InternalState.maxClimbRate.ToString("0") + " m/s");
            valueRects[2].SetText(InternalState.targetRoll.ToString("0") + "° bank");
            valueRects[3].SetText(InternalState.targetSpeed.ToString("0") + " km/h");
            valueRects[4].SetText(InternalState.targetCourse.ToString("0") + "° head");

            // Status Text
            engagedBar.SetText(InternalState.apEnabled ? "ENGAGED" : "DISENGAGED");
        }

        public void UpdateColors(Color mainColor, Color textColor) {
            bool isSelected;
            bool row5 = (selectedRow == 5);

            // Engaged Bars (Col 0)
            isSelected = (selectedCol == 0);
            Color apColor = InternalState.apEnabled ? Color.green : Color.red;
            ApplyStyle(engagedBar, isSelected ? apColor : Color.clear, apColor, isSelected ? Color.black : apColor);

            // Grid (Rows 0-4, Cols 1-4)
            for (int i = 0; i < 5; i++) {
                // Value (Col 1)
                isSelected = (selectedRow == i && selectedCol == 1);
                ApplyStyle(valueRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

                // C (Col 2)
                isSelected = (selectedRow == i && selectedCol == 2);
                ApplyStyle(cRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

                // Minus (Col 3)
                isSelected = (selectedRow == i && selectedCol == 3);
                ApplyStyle(minusRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

                // Plus (Col 4)
                isSelected = (selectedRow == i && selectedCol == 4);
                ApplyStyle(plusRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);
            }

            // Set Bar (Col 5)
            isSelected = (selectedCol == 5);
            ApplyStyle(setBar, isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

            // Bottom Buttons (Row 5)
            isSelected = row5 && (selectedCol == 1);
            Color ajColor = InternalState.ajActive ? Color.green : Color.red;
            ApplyStyle(ajButton, isSelected ? ajColor : Color.clear, ajColor, isSelected ? Color.black : ajColor);

            isSelected = row5 && (selectedCol == 2);
            Color gcasColor = InternalState.gcasEnabled ? Color.green : Color.red;
            ApplyStyle(gcasButton, isSelected ? gcasColor : Color.clear, gcasColor, isSelected ? Color.black : gcasColor);
        }

        private void ApplyStyle(UIAdvancedRectangleLabeled rect, Color bgColor, Color borderColor, Color textColor) {
            if (rect == null) return;
            rect.SetBorderColor(borderColor);
            rect.SetFillColor(bgColor);
            rect.GetLabel().SetColor(textColor);
        }

        public void SetVisible() {
            if (containerObject.activeSelf != InternalState.showMenu) {
                containerObject.SetActive(InternalState.showMenu);
            }
        }
    }

    static class Bridge {
        static public float GetTargetAlt() {
            try {
                return APData.TargetAlt;
            }
            catch (NullReferenceException) {
                return 0f;
            }
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart {
        static void Postfix() {
            LogicEngine.Init();
            DisplayEngine.Init();
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Update")]
    public static class OnPlatformUpdate {
        static void Postfix() {
            LogicEngine.Update();
            DisplayEngine.Update();
        }
    }
}