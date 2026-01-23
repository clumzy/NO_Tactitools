using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using NOAutopilot; // Reference from .csproj
using Plugin = NO_Tactitools.Core.Plugin;
using NO_Tactitools.Core;
using System;
using System.Collections.Generic;

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
                    Plugin.autopilotOpenMenuControllerName.Value,
                    Plugin.autopilotOpenMenuButtonIndex.Value,
                    0.2f,
                    ToggleMenu
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotEnterControllerName.Value,
                    Plugin.autopilotEnterButtonIndex.Value,
                    999f, // High threshold so OnHold keeps running indefinitely
                    SelectActionShort,
                    SelectActionHold
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotUpControllerName.Value,
                    Plugin.autopilotUpButtonIndex.Value,
                    999f, // High threshold so OnHold keeps running
                    NavigateUpShort,
                    NavigateUpHold
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotDownControllerName.Value,
                    Plugin.autopilotDownButtonIndex.Value,
                    999f, // High threshold so OnHold keeps running
                    NavigateDownShort,
                    NavigateDownHold
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotLeftControllerName.Value,
                    Plugin.autopilotLeftButtonIndex.Value,
                    999f,
                    NavigateLeftShort,
                    NavigateLeftHold
                );
                InputCatcher.RegisterNewInput(
                    Plugin.autopilotRightControllerName.Value,
                    Plugin.autopilotRightButtonIndex.Value,
                    999f,
                    NavigateRightShort,
                    NavigateRightHold
                );
            }
            else {
                Plugin.Log("[AP] 'no-autopilot-mod' not found. Autopilot controls disabled.");
            }

            initialized = true;
        }
    }

    public static void ToggleMenu() {
        bool wasMenuOpen = NOAutopilotComponent.InternalState.showMenu;
        NOAutopilotComponent.InternalState.showMenu = !wasMenuOpen;

        if (!wasMenuOpen && NOAutopilotComponent.InternalState.showMenu) {
            // Populate staged values and reset position when menu opens
            NOAutopilotComponent.LogicEngine.ResetStagedValues();
            if (NOAutopilotComponent.InternalState.autopilotMenu != null) {
                NOAutopilotComponent.InternalState.autopilotMenu.selectedRow = 0;
                NOAutopilotComponent.InternalState.autopilotMenu.selectedCol = 0;
            }
        }

        UIBindings.Sound.PlaySound("beep_scroll");
        Plugin.Log($"[AP] Toggle Menu: {NOAutopilotComponent.InternalState.showMenu.ToString()}");
    }

    // Helper to check if menu is active and get menu reference
    private static NOAutopilotComponent.NOAutoPilotMenu GetActiveMenu() {
        if (!NOAutopilotComponent.InternalState.showMenu || NOAutopilotComponent.InternalState.autopilotMenu == null)
            return null;
        return NOAutopilotComponent.InternalState.autopilotMenu;
    }

    // Helper for row navigation with snapping logic
    private static void NavigateRow(int direction) {
        var menu = GetActiveMenu();
        if (menu == null || menu.selectedCol is 0 or 5) return;

        int oldRow = menu.selectedRow;

        // Save column before going to row 5
        if (direction > 0 && oldRow != 5) {
            NOAutopilotComponent.InternalState.lastGridCol = menu.selectedCol;
        }

        menu.selectedRow = Mathf.Clamp(menu.selectedRow + direction, 0, 5);

        // Restore column when leaving row 5
        if (direction < 0 && oldRow == 5 && menu.selectedRow != 5) {
            menu.selectedCol = NOAutopilotComponent.InternalState.lastGridCol;
        }

        // Snap to valid columns when entering row 5
        if (menu.selectedRow == 5 && oldRow != 5) {
            menu.selectedCol = menu.selectedCol == 1 ? 1 : 2;
        }

        UIBindings.Sound.PlaySound("beep_scroll");
    }

    public static void SelectActionHold() {
        var menu = GetActiveMenu();
        if (menu == null) return;

        bool isRepeatable = menu.selectedCol is 3 or 4;
        bool isBearingValue = menu.selectedRow == 4 && menu.selectedCol == 1;
        bool isSpeedValue = menu.selectedRow == 3 && menu.selectedCol == 1;
        bool isToggleable = isBearingValue || isSpeedValue;
        bool isCButton = menu.selectedCol == 2 && menu.selectedRow < 5;

        float time = Time.time;

        // First frame of hold - initialize timer
        if (NOAutopilotComponent.InternalState.lastRepeatTime == 0) {
            NOAutopilotComponent.InternalState.lastRepeatTime = time;
            NOAutopilotComponent.InternalState.selectToggleHandled = false;
            NOAutopilotComponent.InternalState.selectActionHandled = false;
            if (isRepeatable) {
                menu.OnSelect();
                NOAutopilotComponent.InternalState.selectActionHandled = true;
            }
            return;
        }

        float holdDuration = time - NOAutopilotComponent.InternalState.lastRepeatTime;

        // Handle C button long press - clear all and stop
        if (isCButton && holdDuration >= 0.8f && !NOAutopilotComponent.InternalState.selectActionHandled) {
            NOAutopilotComponent.InternalState.selectActionHandled = true;
            menu.ResetAllAndStop();
            UIBindings.Sound.PlaySound("beep_scroll");
            return;
        }

        // Handle toggleable values (bearing/speed) - trigger once after 0.4s
        if (isToggleable && holdDuration >= 0.4f && !NOAutopilotComponent.InternalState.selectToggleHandled) {
            NOAutopilotComponent.InternalState.selectToggleHandled = true;
            if (isBearingValue) {
                APData.NavEnabled = !APData.NavEnabled;
                UIBindings.Game.DisplayToast(APData.NavEnabled ? "Autopilot : Nav mode <b>ON</b>" : "Autopilot : Nav mode <b>OFF</b>");
            }
            else {
                APData.AllowExtremeThrottle = !APData.AllowExtremeThrottle;
                UIBindings.Game.DisplayToast(APData.AllowExtremeThrottle ? "Autopilot : Extreme throttle <b>ON</b>" : "Autopilot : Extreme throttle <b>OFF</b>");
            }
            NOAutopilot.Plugin.SyncMenuValues();
            UIBindings.Sound.PlaySound("beep_scroll");
            return;
        }

        // Handle repeatable buttons (+/-) - fast repeat after initial delay
        if (isRepeatable && holdDuration >= 0.15f) {
            if (NOAutopilotComponent.InternalState.selectRepeatTime == 0) {
                NOAutopilotComponent.InternalState.selectRepeatTime = time;
            }
            else if (time >= NOAutopilotComponent.InternalState.selectRepeatTime) {
                menu.OnSelect();
                NOAutopilotComponent.InternalState.selectRepeatTime = time + 0.08f;
            }
        }
    }

    public static void SelectActionShort() {
        NOAutopilotComponent.InternalState.lastRepeatTime = 0f;
        NOAutopilotComponent.InternalState.selectRepeatTime = 0f;

        if (NOAutopilotComponent.InternalState.selectToggleHandled) {
            NOAutopilotComponent.InternalState.selectToggleHandled = false;
            return;
        }

        if (NOAutopilotComponent.InternalState.selectActionHandled) {
            NOAutopilotComponent.InternalState.selectActionHandled = false;
            return;
        }

        GetActiveMenu()?.OnSelect();
    }

    public static void NavigateUpShort() {
        NOAutopilotComponent.InternalState.lastUpRepeatTime = 0f;
        NOAutopilotComponent.InternalState.upHoldStartTime = 0f;
        NavigateRow(-1);
    }

    public static void NavigateUpHold() {
        var menu = GetActiveMenu();
        if (menu == null || menu.selectedCol is 0 or 5) return;

        float time = Time.time;
        ref float startTime = ref NOAutopilotComponent.InternalState.upHoldStartTime;
        ref float repeatTime = ref NOAutopilotComponent.InternalState.lastUpRepeatTime;

        if (startTime == 0) { startTime = time; return; }
        if (time - startTime < 0.3f) return;

        if (repeatTime == 0 || time >= repeatTime) {
            NavigateRow(-1);
            repeatTime = time + 0.15f;
        }
    }

    public static void NavigateDownShort() {
        NOAutopilotComponent.InternalState.lastDownRepeatTime = 0f;
        NOAutopilotComponent.InternalState.downHoldStartTime = 0f;
        NavigateRow(1);
    }

    public static void NavigateDownHold() {
        var menu = GetActiveMenu();
        if (menu == null || menu.selectedCol is 0 or 5) return;

        float time = Time.time;
        ref float startTime = ref NOAutopilotComponent.InternalState.downHoldStartTime;
        ref float repeatTime = ref NOAutopilotComponent.InternalState.lastDownRepeatTime;

        if (startTime == 0) { startTime = time; return; }
        if (time - startTime < 0.3f) return;

        if (repeatTime == 0 || time >= repeatTime) {
            NavigateRow(1);
            repeatTime = time + 0.15f;
        }
    }

    public static void NavigateLeftShort() {
        NOAutopilotComponent.InternalState.lastLeftRepeatTime = 0f;
        NOAutopilotComponent.InternalState.leftHoldStartTime = 0f;
        NavigateCol(-1);
    }

    public static void NavigateLeftHold() {
        var menu = GetActiveMenu();
        if (menu == null) return;

        float time = Time.time;
        ref float startTime = ref NOAutopilotComponent.InternalState.leftHoldStartTime;
        ref float repeatTime = ref NOAutopilotComponent.InternalState.lastLeftRepeatTime;

        if (startTime == 0) { startTime = time; return; }
        if (time - startTime < 0.3f) return;

        if (repeatTime == 0 || time >= repeatTime) {
            NavigateCol(-1);
            repeatTime = time + 0.15f;
        }
    }

    public static void NavigateRightShort() {
        NOAutopilotComponent.InternalState.lastRightRepeatTime = 0f;
        NOAutopilotComponent.InternalState.rightHoldStartTime = 0f;
        NavigateCol(1);
    }

    public static void NavigateRightHold() {
        var menu = GetActiveMenu();
        if (menu == null) return;

        float time = Time.time;
        ref float startTime = ref NOAutopilotComponent.InternalState.rightHoldStartTime;
        ref float repeatTime = ref NOAutopilotComponent.InternalState.lastRightRepeatTime;

        if (startTime == 0) { startTime = time; return; }
        if (time - startTime < 0.3f) return;

        if (repeatTime == 0 || time >= repeatTime) {
            NavigateCol(1);
            repeatTime = time + 0.15f;
        }
    }

    private static void NavigateCol(int direction) {
        var menu = GetActiveMenu();
        if (menu == null) return;

        if (menu.selectedRow == 5) {
            int[] row5Cols = [0, 1, 2, 5];
            int idx = System.Array.IndexOf(row5Cols, menu.selectedCol);
            int newIdx = Mathf.Clamp(idx + direction, 0, row5Cols.Length - 1);
            menu.selectedCol = row5Cols[newIdx];
            if (menu.selectedCol is 1 or 2) NOAutopilotComponent.InternalState.lastGridCol = menu.selectedCol;
        }
        else {
            menu.selectedCol = Mathf.Clamp(menu.selectedCol + direction, 0, 5);
        }
        UIBindings.Sound.PlaySound("beep_scroll");
    }
}

public class NOAutopilotComponent {
    public static class LogicEngine {
        public static void Init() {
            InternalState.showMenu = false;
        }

        public static void ResetStagedValues() {
            InternalState.stagedAlt = APData.TargetAlt;
            InternalState.stagedMaxClimbRate = APData.CurrentMaxClimbRate;
            InternalState.stagedRoll = APData.TargetRoll;
            InternalState.stagedSpeed = APData.TargetSpeed * 3.6f; // APData is m/s, staged is km/h
            InternalState.stagedCourse = APData.TargetCourse;
        }

        public static void Update() {
            if (InternalState.lastApState && !APData.Enabled) {
                APData.TargetSpeed = -1f;
                ResetStagedValues();
                NOAutopilot.Plugin.SyncMenuValues();
            }
            InternalState.lastApState = APData.Enabled;

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
            InternalState.gcasActive = APData.GCASActive;
            InternalState.gcasWarning = APData.GCASWarning;
            InternalState.navEnabled = APData.NavEnabled;
            InternalState.extremeThrottleEnabled = APData.AllowExtremeThrottle;
        }
    }

    public static class InternalState {
        public static NOAutoPilotMenu autopilotMenu;
        public static bool showMenu = false;
        public static bool lastApState = false;
        public static Color mainColor = Color.green;
        public static Color textColor = Color.green;
        public static int lastGridCol = 1;

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
        public static bool gcasActive;
        public static bool gcasWarning;

        // Staged Values (for editing)
        public static float stagedAlt;
        public static float stagedMaxClimbRate;
        public static float stagedRoll;
        public static float stagedSpeed;
        public static float stagedCourse;

        // Increments
        public static float altIncrement = 100f;
        public static float climbIncrement = 5f;
        public static float rollIncrement = 5f;
        public static float speedIncrement = 50f;
        public static float courseIncrement = 1f;

        // Repeat Logic
        public static float lastRepeatTime;
        public static float selectRepeatTime;
        public static bool selectToggleHandled;
        public static bool selectActionHandled;
        public static float lastUpRepeatTime;
        public static float upHoldStartTime;
        public static float lastDownRepeatTime;
        public static float downHoldStartTime;
        public static float lastLeftRepeatTime;
        public static float leftHoldStartTime;
        public static float lastRightRepeatTime;
        public static float rightHoldStartTime;

        // Nav Mode
        public static bool navEnabled;
        public static bool extremeThrottleEnabled;
    }

    private static class DisplayEngine {
        public static void Init() {
            // Initialization logic
            InternalState.autopilotMenu?.Destroy();
            InternalState.autopilotMenu = null;
            InternalState.autopilotMenu = new NOAutoPilotMenu();
        }

        public static void Update() {
            if (InternalState.autopilotMenu == null) {
                return;
            }

            // Hide menu during boot sequence
            bool isBooting = !UI.MFD.BootScreenComponent.InternalState.hasBooted;
            if (isBooting) {
                InternalState.autopilotMenu.containerObject.SetActive(false);
                return;
            }

            InternalState.autopilotMenu.SetVisible();
            if (InternalState.showMenu) {
                InternalState.autopilotMenu.UpdateColors(InternalState.textColor);
                InternalState.autopilotMenu.DisplayCurrentTargetValues();
            }
        }
    }

    public class NOAutoPilotMenu {
        public GameObject containerObject;
        public Transform containerTransform;

        public UIBindings.Draw.UIAdvancedRectangleLabeled engagedBar;
        public UIBindings.Draw.UIAdvancedRectangleLabeled setBar;
        public UIBindings.Draw.UIAdvancedRectangleLabeled ajButton;
        public UIBindings.Draw.UIAdvancedRectangleLabeled gcasButton;

        public List<UIBindings.Draw.UIAdvancedRectangleLabeled> valueRects = [];
        public List<UIBindings.Draw.UIAdvancedRectangleLabeled> cRects = [];
        public List<UIBindings.Draw.UIAdvancedRectangleLabeled> minusRects = [];
        public List<UIBindings.Draw.UIAdvancedRectangleLabeled> plusRects = [];

        public int selectedRow = 0;
        public int selectedCol = 0;
        public int fontSize = 34;
        public float padding = 0;

        public NOAutoPilotMenu() {
            Transform parentTransform = UIBindings.Game.GetTacScreenTransform();
            string platformName = GameBindings.Player.Aircraft.GetPlatformName();

            containerObject = new GameObject("i_ap_NOAutopilotMenu");
            _ = containerObject.AddComponent<RectTransform>();
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
                default:
                    break;
            }

            // Apply global offset to the container
            containerTransform.localPosition += new Vector3(xOffset, yOffset, 0);


            float unit = fontSize + 6;
            float gap = unit / 4f;
            padding = gap;

            // Element Sizes
            Vector2 valueBoxSize = new(4 * unit, unit);
            Vector2 buttonSize = new(unit, unit);
            float bottomButtonHeight = unit * 1.25f;

            // Grid Dimensions
            float gridRowWidth = (7 * unit) + (3 * gap);
            Vector2 ajSize = new(4 * unit, bottomButtonHeight);
            Vector2 gcasSize = new((3 * unit) + (2 * gap), bottomButtonHeight);

            // Total Block Content
            float engagedBarWidth = unit;
            float setBarWidth = unit;

            // Grid Height
            float gridHeight = (5 * unit) + (4 * gap);

            // Total Content Height (Engaged Bar matches this total span + bottom buttons)
            float totalContentHeight = gridHeight + gap + bottomButtonHeight;

            // Total Content Width
            float totalContentWidth = engagedBarWidth + gap + gridRowWidth + gap + setBarWidth;

            // Center Reference (0,0) is center of valid content area
            Vector2 contentTopLeft = new(-totalContentWidth / 2f, totalContentHeight / 2f);

            // Dimensions and Layout
            Vector2 bgSize = new(totalContentWidth + (2 * padding), totalContentHeight + (2 * padding));
            Vector2 bgCenter = Vector2.zero;
            _ = new UIBindings.Draw.UIAdvancedRectangle(
                "i_ap_Background",
                bgCenter - (bgSize / 2f),
                bgCenter + (bgSize / 2f),
                InternalState.mainColor, 2, containerTransform, Color.black
            );
            Vector2 engagedSize = new(totalContentHeight, engagedBarWidth);
            Vector2 engagedCenter = new(contentTopLeft.x + (engagedBarWidth / 2f), 0);

            engagedBar = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                "i_ap_EngagedBar",
                engagedCenter - (engagedSize / 2f),
                engagedCenter + (engagedSize / 2f),
                Color.green, 2, containerTransform,
                Color.clear, // Transparent fill
                FontStyle.Bold,
                Color.green, // Text matches border
                fontSize - 10 // Slightly smaller for vertical bar text? Or keep font size.
            );
            engagedBar.SetText("ENGAGED");
            engagedBar.GetLabel().SetFontSize(fontSize - 4); // Adjustment for fitting
            engagedBar.GetRectTransform().localRotation = Quaternion.Euler(0, 0, 90f);

            string[] defaultValues = ["- m", "- m/s", "-° bnk", "- km/h", "-° hdg"];

            // Grid Top Left Reference
            float gridLeftX = contentTopLeft.x + engagedBarWidth + gap;
            float gridTopY = contentTopLeft.y; // Start at top

            for (int i = 0; i < 5; i++) {
                float y = gridTopY - (i * (unit + gap)) - (unit / 2f);

                // Value Box
                Vector2 valCenter = new(gridLeftX + (valueBoxSize.x / 2f), y);
                UIBindings.Draw.UIAdvancedRectangleLabeled vRect = new(
                    $"i_ap_ValRect_{i.ToString()}",
                    valCenter - (valueBoxSize / 2f), valCenter + (valueBoxSize / 2f),
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
                Vector2 cCenter = new(currentX + (buttonSize.x / 2f), y);
                UIBindings.Draw.UIAdvancedRectangleLabeled cRect = new(
                    $"i_ap_CRect_{i.ToString()}",
                    cCenter - (buttonSize / 2f), cCenter + (buttonSize / 2f),
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
                Vector2 mCenter = new(currentX + (buttonSize.x / 2f), y);
                UIBindings.Draw.UIAdvancedRectangleLabeled mRect = new(
                    $"i_ap_MinusRect_{i.ToString()}",
                    mCenter - (buttonSize / 2f), mCenter + (buttonSize / 2f),
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
                Vector2 pCenter = new(currentX + (buttonSize.x / 2f), y);
                UIBindings.Draw.UIAdvancedRectangleLabeled pRect = new(
                    $"i_ap_PlusRect_{i.ToString()}",
                    pCenter - (buttonSize / 2f), pCenter + (buttonSize / 2f),
                    InternalState.mainColor, 2, containerTransform,
                    Color.clear,
                    FontStyle.Normal,
                    Color.white,
                    fontSize
                );
                pRect.SetText("+");
                plusRects.Add(pRect);
            }

            Vector2 setSizeVisual = new(totalContentHeight, setBarWidth); // Rotated creation
                                                                          // Center X = gridLeftX + gridRowWidth + gap + setBarWidth/2f
            float setCenterX = gridLeftX + gridRowWidth + gap + (setBarWidth / 2f);
            Vector2 setCenter = new(setCenterX, 0);

            setBar = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                "i_ap_SetBar",
                setCenter - (setSizeVisual / 2f),
                setCenter + (setSizeVisual / 2f),
                InternalState.mainColor, 2, containerTransform,
                Color.clear,
                FontStyle.Normal,
                Color.white,
                fontSize - 4
            );
            setBar.SetText("SET");
            setBar.GetRectTransform().localRotation = Quaternion.Euler(0, 0, 90f);

            float bottomY = contentTopLeft.y - gridHeight - gap - (bottomButtonHeight / 2f);

            Vector2 ajCenter = new(gridLeftX + (ajSize.x / 2f), bottomY);

            ajButton = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                "i_ap_AJButton",
                ajCenter - (ajSize / 2f), ajCenter + (ajSize / 2f),
                Color.red, 2, containerTransform,
                Color.clear,
                FontStyle.Bold,
                Color.red,
                fontSize
            );
            ajButton.SetText("AJ");

            float gcasStartX = gridLeftX + ajSize.x + gap;
            Vector2 gcasCenter = new(gcasStartX + (gcasSize.x / 2f), bottomY);

            gcasButton = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                "i_ap_GCASButton",
                gcasCenter - (gcasSize / 2f), gcasCenter + (gcasSize / 2f),
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

        public void DisplayCurrentTargetValues() {
            if (valueRects.Count < 5) {
                return;
            }

            // Row 1: Altitude
            valueRects[0].SetText((InternalState.stagedAlt < 0 ? "OFF" : InternalState.stagedAlt.ToString("0")) + " m");

            // Row 2: Vertical Speed
            valueRects[1].SetText(InternalState.stagedMaxClimbRate.ToString("0") + " m/s");

            // Row 3: Roll
            valueRects[2].SetText((InternalState.stagedRoll <= -900f ? "OFF" : InternalState.stagedRoll.ToString("0")) + "° bnk");

            // Row 4: Speed
            valueRects[3].SetText((InternalState.stagedSpeed < 0 ? "OFF" : InternalState.stagedSpeed.ToString("0")) + " km/h");

            // Row 5: Course
            valueRects[4].SetText((InternalState.stagedCourse < 0 ? "OFF" : InternalState.stagedCourse.ToString("0")) + "° hdg");

            // Status Text
            engagedBar.SetText(InternalState.apEnabled ? "ENGAGED" : "DISENGAGED");
        }

        public void OnSelect() {
            UIBindings.Sound.PlaySound("beep_scroll");
            if (selectedRow < 5) {
                // Engage Bar
                if (selectedCol == 0) {
                    APData.Enabled = !APData.Enabled;
                    NOAutopilot.Plugin.SyncMenuValues();
                    if (APData.Enabled)
                        UIBindings.Sound.PlaySound("beep_autopilot");
                    return;
                }

                // Set Bar
                if (selectedCol == 5) {
                    ApplyStagedValues();
                    return;
                }

                // Grid Actions
                switch (selectedCol) {
                    case 1: // Set to Current
                        SetStagedToCurrent(selectedRow);
                        break;
                    case 2: // Clear (C)
                        ClearStagedValue(selectedRow);
                        break;
                    case 3: // Minus (-)
                        AdjustStagedValue(selectedRow, -1);
                        break;
                    case 4: // Plus (+)
                        AdjustStagedValue(selectedRow, 1);
                        break;
                    default:
                        break;
                }
            }
            else if (selectedRow == 5) {
                // Bottom Buttons
                if (selectedCol == 0) {
                    APData.Enabled = !APData.Enabled;
                    if (APData.Enabled)
                        UIBindings.Sound.PlaySound("beep_autopilot");
                    NOAutopilot.Plugin.SyncMenuValues();
                }
                else if (selectedCol == 1) { // AJ
                    APData.AutoJammerActive = !APData.AutoJammerActive;
                    NOAutopilot.Plugin.SyncMenuValues();
                }
                else if (selectedCol == 2) { // GCAS
                    APData.GCASEnabled = !APData.GCASEnabled;
                    NOAutopilot.Plugin.SyncMenuValues();
                }
                else if (selectedCol == 5) {
                    ApplyStagedValues();
                }
            }
        }

        private void SetStagedToCurrent(int row) {
            switch (row) {
                case 0: InternalState.stagedAlt = Mathf.Round(InternalState.currentAlt / InternalState.altIncrement) * InternalState.altIncrement; break;
                case 1: InternalState.stagedMaxClimbRate = Mathf.Max(InternalState.climbIncrement, Mathf.Round(InternalState.currentVS / InternalState.climbIncrement) * InternalState.climbIncrement); break;
                case 2: InternalState.stagedRoll = Mathf.Round(InternalState.currentRoll / InternalState.rollIncrement) * InternalState.rollIncrement; break;
                case 3: InternalState.stagedSpeed = Mathf.Round(InternalState.currentTAS * 3.6f / InternalState.speedIncrement) * InternalState.speedIncrement; break;
                case 4: InternalState.stagedCourse = Mathf.Round(InternalState.currentCourse / InternalState.courseIncrement) * InternalState.courseIncrement; break;
                default:
                    break;
            }
        }

        private void ClearStagedValue(int row) {
            switch (row) {
                case 0: InternalState.stagedAlt = -1f; break;
                case 1: InternalState.stagedMaxClimbRate = InternalState.climbIncrement * 2f; break; // Defaulting to 10
                case 2: InternalState.stagedRoll = -999f; break;
                case 3: InternalState.stagedSpeed = -1f; break;
                case 4: InternalState.stagedCourse = -1f; break;
                default:
                    break;
            }
        }

        public void ResetAllAndStop() {
            for (int i = 0; i < 5; i++) {
                ClearStagedValue(i);
            }

            ApplyStagedValues(autoEngage: false);
            APData.Enabled = false;
            NOAutopilot.Plugin.SyncMenuValues();
            UIBindings.Game.DisplayToast("Autopilot: <b>RESET & STOPPED</b>");
        }

        private void AdjustStagedValue(int row, int direction) {
            switch (row) {
                case 0: // Alt
                    if (InternalState.stagedAlt < 0) {
                        InternalState.stagedAlt = InternalState.currentAlt;
                    }

                    InternalState.stagedAlt = Mathf.Max(0, Mathf.Round((InternalState.stagedAlt + (direction * InternalState.altIncrement)) / InternalState.altIncrement) * InternalState.altIncrement);
                    break;
                case 1: // Climb
                    InternalState.stagedMaxClimbRate = Mathf.Max(1, Mathf.Round((InternalState.stagedMaxClimbRate + (direction * InternalState.climbIncrement)) / InternalState.climbIncrement) * InternalState.climbIncrement);
                    break;
                case 2: // Roll
                    if (InternalState.stagedRoll <= -900f) {
                        InternalState.stagedRoll = 0f;
                    }

                    InternalState.stagedRoll = Mathf.Clamp(Mathf.Round((InternalState.stagedRoll + (direction * InternalState.rollIncrement)) / InternalState.rollIncrement) * InternalState.rollIncrement, -60f, 60f);
                    break;
                case 3: // Speed
                    if (InternalState.stagedSpeed < 0) {
                        InternalState.stagedSpeed = InternalState.currentTAS * 3.6f;
                    }

                    InternalState.stagedSpeed = Mathf.Max(0, Mathf.Round((InternalState.stagedSpeed + (direction * InternalState.speedIncrement)) / InternalState.speedIncrement) * InternalState.speedIncrement);
                    break;
                case 4: // Course
                    if (InternalState.stagedCourse < 0) {
                        InternalState.stagedCourse = InternalState.currentCourse;
                    }

                    float targetCourse = Mathf.Round((InternalState.stagedCourse + (direction * InternalState.courseIncrement)) / InternalState.courseIncrement) * InternalState.courseIncrement;
                    InternalState.stagedCourse = (targetCourse + 360f) % 360f;
                    break;
                default:
                    break;
            }
        }

        private void ApplyStagedValues(bool autoEngage = true) {
            APData.TargetAlt = InternalState.stagedAlt;
            APData.CurrentMaxClimbRate = InternalState.stagedMaxClimbRate;
            APData.TargetRoll = InternalState.stagedRoll;
            APData.TargetSpeed = InternalState.stagedSpeed < 0 ? -1f : InternalState.stagedSpeed / 3.6f;

            APData.TargetCourse = InternalState.stagedCourse;
            if (autoEngage && !APData.Enabled) {
                APData.Enabled = true;
                UIBindings.Sound.PlaySound("beep_autopilot");
            }
            NOAutopilot.Plugin.SyncMenuValues();
            Plugin.Log("[AP] Values Applied.");
        }

        public void UpdateColors(Color textColor) {
            bool isSelected;
            bool row5 = selectedRow == 5;

            // Engaged Bars (Col 0)
            isSelected = selectedCol == 0;
            Color apColor = InternalState.apEnabled ? Color.green : Color.red;
            ApplyStyle(engagedBar, isSelected ? apColor : Color.clear, apColor, isSelected ? Color.black : apColor);

            // Grid (Rows 0-4, Cols 1-4)
            for (int i = 0; i < 5; i++) {
                // Value (Col 1)
                isSelected = selectedRow == i && selectedCol == 1;
                // Bearing value (row 4) is green in nav/waypoint mode
                // Speed value (row 3) is green when extreme throttle is enabled
                Color valueColor = textColor;
                if (i == 4 && InternalState.navEnabled) valueColor = Color.green;
                if (i == 3 && InternalState.extremeThrottleEnabled) valueColor = Color.green;
                ApplyStyle(valueRects[i], isSelected ? valueColor : Color.clear, valueColor, isSelected ? Color.black : valueColor);

                // C (Col 2)
                isSelected = selectedRow == i && selectedCol == 2;
                ApplyStyle(cRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

                // Minus (Col 3)
                isSelected = selectedRow == i && selectedCol == 3;
                ApplyStyle(minusRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

                // Plus (Col 4)
                isSelected = selectedRow == i && selectedCol == 4;
                ApplyStyle(plusRects[i], isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);
            }

            // Set Bar (Col 5)
            isSelected = selectedCol == 5;
            ApplyStyle(setBar, isSelected ? textColor : Color.clear, textColor, isSelected ? Color.black : textColor);

            // Bottom Buttons (Row 5)
            isSelected = row5 && (selectedCol == 1);
            Color ajColor = InternalState.ajActive ? Color.green : Color.gray;
            ApplyStyle(ajButton, isSelected ? ajColor : Color.clear, ajColor, isSelected ? Color.black : ajColor);

            isSelected = row5 && (selectedCol == 2);
            Color gcasColor = Color.green;
            if (InternalState.gcasWarning) {
                gcasColor = Color.yellow;
            }

            if (InternalState.gcasActive) {
                gcasColor = Color.red;
            }

            if (!InternalState.gcasEnabled) {
                gcasColor = Color.gray;
            }

            ApplyStyle(gcasButton, isSelected ? gcasColor : Color.clear, gcasColor, isSelected ? Color.black : gcasColor);
        }

        private void ApplyStyle(UIBindings.Draw.UIAdvancedRectangleLabeled rect, Color bgColor, Color borderColor, Color textColor) {
            if (rect == null) {
                return;
            }

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

    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart {
        private static void Postfix() {
            LogicEngine.Init();
            DisplayEngine.Init();
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Update")]
    public static class OnPlatformUpdate {
        private static void Postfix() {
            LogicEngine.Update();
            DisplayEngine.Update();
        }
    }
}


