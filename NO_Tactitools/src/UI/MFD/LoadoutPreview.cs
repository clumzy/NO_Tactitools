using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Collections.Generic;
using UnityEngine.UI;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class LoadoutPreviewPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[LP] Loadout Preview plugin starting !");
            Plugin.harmony.PatchAll(typeof(LoadoutPreviewComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(LoadoutPreviewComponent.OnPlatformUpdate));
            // TODO: Register a button if needed for toggling or interaction
            initialized = true;
            Plugin.Log("[LP] Loadout Preview plugin succesfully started !");
        }
    }

    // TODO: Add handler methods for button presses if any
}

public class LoadoutPreviewComponent {
    // LOGIC ENGINE, INTERNAL STATE, DISPLAY ENGINE
    static class LogicEngine {
        static public void Init() {
            //if loadout preview is not null, destroy it properly
            InternalState.loadoutPreview?.Destroy();
            InternalState.loadoutPreview = null;
            InternalState.weaponStations.Clear();
            InternalState.neverShown = true;
            InternalState.needsUpdate = false;
            InternalState.displayDuration = Plugin.loadoutPreviewDuration.Value;
            InternalState.onlyShowOnBoot = Plugin.loadoutPreviewOnlyShowOnBoot.Value;
            InternalState.sendToHMD = Plugin.loadoutPreviewSendToHMD.Value;
            InternalState.vanillaUIEnabled = Plugin.weaponDisplayVanillaUIEnabled.Value; // WE READ THIS SETTING HERE BECAUSE WE NEED IT, WE COULD CALL IT FROM WEAPON DISPLAY BUT THIS LETS US AVOID LOAD ORDER ISSUES
            InternalState.manualPlacement = Plugin.loadoutPreviewManualPlacement.Value;
            InternalState.horizontalOffset = Plugin.loadoutPreviewPositionX.Value;
            InternalState.verticalOffset = Plugin.loadoutPreviewPositionY.Value;
            InternalState.backgroundTransparency = Plugin.loadoutPreviewBackgroundTransparency.Value;
            InternalState.textAndBorderTransparency = Plugin.loadoutPreviewTextAndBorderTransparency.Value;
            InternalState.hasStations = Bindings.Player.Aircraft.Weapons.GetStationCount() > 1;
            if (InternalState.hasStations) {
                InternalState.currentWeaponStation = Bindings.Player.Aircraft.Weapons.GetActiveStationName();
                for (int i = 0; i < Bindings.Player.Aircraft.Weapons.GetStationCount(); i++) {
                InternalState.WeaponStationInfo stationInfo = new() {
                    stationName = Bindings.Player.Aircraft.Weapons.GetStationNameByIndex(i),
                    ammo = Bindings.Player.Aircraft.Weapons.GetStationAmmoByIndex(i),
                    maxAmmo = Bindings.Player.Aircraft.Weapons.GetStationMaxAmmoByIndex(i)
                };
                InternalState.weaponStations.Add(stationInfo);
            }
            }
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() || Bindings.Player.Aircraft.GetAircraft() == null || !InternalState.hasStations)
                return;
            if (InternalState.onlyShowOnBoot 
                && InternalState.neverShown 
                && BootScreenComponent.InternalState.hasBooted) {
                InternalState.lastUpdateTime = Time.time;
                InternalState.currentWeaponStation = Bindings.Player.Aircraft.Weapons.GetActiveStationName();
                InternalState.neverShown = false;
            }
            else if (
                InternalState.currentWeaponStation != Bindings.Player.Aircraft.Weapons.GetActiveStationName() 
                && BootScreenComponent.InternalState.hasBooted 
                && !InternalState.onlyShowOnBoot) {
                InternalState.lastUpdateTime = Time.time;
                InternalState.currentWeaponStation = Bindings.Player.Aircraft.Weapons.GetActiveStationName();
            }
            InternalState.needsUpdate = ((Time.time - InternalState.lastUpdateTime) < InternalState.displayDuration);
            if (InternalState.needsUpdate) {
                for (int i = 0; i < Bindings.Player.Aircraft.Weapons.GetStationCount(); i++) {
                    InternalState.weaponStations[i].stationName = Bindings.Player.Aircraft.Weapons.GetStationNameByIndex(i);
                    InternalState.weaponStations[i].ammo = Bindings.Player.Aircraft.Weapons.GetStationAmmoByIndex(i);
                    InternalState.weaponStations[i].maxAmmo = Bindings.Player.Aircraft.Weapons.GetStationMaxAmmoByIndex(i);
                }
            }
        }
    }


    public static class InternalState {
        public class WeaponStationInfo {
            public string stationName;
            public int ammo;
            public int maxAmmo;
        }
        public static string currentWeaponStation = "";
        public static float lastUpdateTime = 0;
        public static bool needsUpdate = false;
        public static bool onlyShowOnBoot;
        public static bool neverShown = true;
        public static List<WeaponStationInfo> weaponStations = [];
        public static LoadoutPreview loadoutPreview;
        public static bool sendToHMD = false;
        public static bool vanillaUIEnabled = true;
        public static bool manualPlacement = false;
        public static int horizontalOffset = 0;
        public static int verticalOffset = 0;
        public static float backgroundTransparency = 0.6f;
        public static float textAndBorderTransparency = 0.9f;
        public static bool hasStations = true;
        public static float displayDuration = 1f;
        public static Color mainColor = Color.green;
        public static Color textColor = Color.green;
    }

    static class DisplayEngine {
        static public void Init() {
            if (InternalState.hasStations) {
                if (InternalState.sendToHMD) {
                    InternalState.mainColor = Color.green;
                    InternalState.textColor = Color.green;
                }
                else {
                    Plugin.Log("[LP] Initializing Loadout Preview for Tac Screen");
                }
                InternalState.loadoutPreview = new LoadoutPreview(sendToHMD: InternalState.sendToHMD);
                Plugin.Log("[LP] Loadout Preview initialized.");
            }
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() ||
                Bindings.Player.Aircraft.GetAircraft() == null ||
                !InternalState.hasStations)
                return;
            if (!InternalState.needsUpdate) {
                // if loadout preview is inactive, hide it
                InternalState.loadoutPreview.SetActive(false);
                return;
            }
            InternalState.loadoutPreview.SetActive(true);
            for (int i = 0; i < InternalState.weaponStations.Count; i++) {
                InternalState.loadoutPreview.stationLabels[i].SetColor(
                    (InternalState.weaponStations[i].ammo == 0) ? Color.red : InternalState.sendToHMD ? Color.green : InternalState.textColor);
            }
            for (int i = 0; i < InternalState.weaponStations.Count; i++) {
                InternalState.WeaponStationInfo ws = InternalState.weaponStations[i];
                InternalState.loadoutPreview.stationLabels[i].SetText(
                    "[" + i.ToString() + "]" +
                    ws.stationName + ": " +
                    ws.ammo + "/" +
                    ws.maxAmmo);
                // keep color/size adjustments minimal here; DisplayEngine handles color each frame
                InternalState.loadoutPreview.stationLabels[i].SetFontSize(
                    (Bindings.Player.Aircraft.Weapons.GetActiveStationName() == ws.stationName) ? (InternalState.loadoutPreview.fontSize + 6) : InternalState.loadoutPreview.fontSize);
                InternalState.loadoutPreview.stationLabels[i].SetFontStyle(
                    (Bindings.Player.Aircraft.Weapons.GetActiveStationName() == ws.stationName) ? FontStyle.Bold : FontStyle.Normal);
            }
            InternalState.loadoutPreview.UpdateLabelPositions();
        }
    }

    public class LoadoutPreview {
        public GameObject containerObject;
        public Transform containerTransform;
        public List<Bindings.UI.Draw.UILabel> stationLabels = [];
        public Bindings.UI.Draw.UIAdvancedRectangle borderRect;
        public float maxLabelWidth;
        public float verticalOffset = 0;
        public float horizontalOffset = 0;
        public float padding = 0;
        public int fontSize = 34;
        public LoadoutPreview(bool sendToHMD = false) {
            maxLabelWidth = 0;
            List<InternalState.WeaponStationInfo> weaponStations = InternalState.weaponStations;
            string platformName;
            Transform parentTransform;
            if (!sendToHMD) {
                parentTransform = Bindings.UI.Game.GetTacScreenTransform();
                platformName = Bindings.Player.Aircraft.GetPlatformName();
            }
            else {
                parentTransform = Bindings.UI.Game.GetCombatHUDTransform();
                platformName = "HMD";
            }
            
            // Create container GameObject to hold all LoadoutPreview elements
            containerObject = new GameObject("i_lp_LoadoutPreviewContainer");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parentTransform, false);
            switch (platformName) {
                case "CI-22 Cricket":
                    horizontalOffset = -105;
                    verticalOffset = 0;
                    fontSize = 44;
                    break;
                case "SAH-46 Chicane":
                    horizontalOffset = -130;
                    verticalOffset = 65;
                    break;
                case "T/A-30 Compass":
                    horizontalOffset = 0;
                    verticalOffset = 80;
                    break;
                case "FS-12 Revoker":
                    horizontalOffset = 0;
                    verticalOffset = 75;
                    break;
                case "FS-20 Vortex":
                    horizontalOffset = 0;
                    verticalOffset = 75;
                    break;
                case "KR-67 Ifrit":
                    horizontalOffset = -130;
                    verticalOffset = 65;
                    break;
                case "VL-49 Tarantula":
                    horizontalOffset = -255;
                    verticalOffset = 60;
                    fontSize = 28;
                    break;
                case "EW-1 Medusa":
                    horizontalOffset = -225;
                    verticalOffset = 65;
                    break;
                case "SFB-81":
                    horizontalOffset = -180;
                    verticalOffset = 60;
                    break;
                case "UH-80 Ibis":
                    horizontalOffset = -245;
                    verticalOffset = 65;
                    break;
                case "A-19 Brawler":
                    verticalOffset = 70;
                    break;
                case "FQ-106 Kestrel":
                    verticalOffset = 75;
                    break;
                case "HMD":
                    horizontalOffset = 0;
                    verticalOffset = 0;
                    fontSize = 14;
                    break;
                default:
                    break;
            }


            Color backgroundColor = Color.black;
            int border = 2;
            if (sendToHMD) {
                InternalState.mainColor = new(0f, 1f, 0f, InternalState.textAndBorderTransparency);
                InternalState.textColor = new(0f, 1f, 0f, InternalState.textAndBorderTransparency);
                backgroundColor = new(0f, 0f, 0f, InternalState.backgroundTransparency);
            }
            // Create background rectangle
            borderRect = new(
                "i_lp_LoadoutPreviewBorder",
                new Vector2(-1, -1),
                new Vector2(1, 1),
                InternalState.mainColor,
                border,
                containerTransform,
                backgroundColor
            );
            // Create labels
            for (int i = 0; i < weaponStations.Count; i++) {
                Bindings.UI.Draw.UILabel stationLabel = new(
                    "i_lp_Slot " + i,
                    new Vector2(0, 0),
                    containerTransform,
                    fontStyle: FontStyle.Bold, // Default to bold; will be updated in DisplayEngine
                    color: InternalState.textColor,
                    fontSize: fontSize + 6, // Default to 40; will be updated in DisplayEngine
                    backgroundOpacity: 0f
                );
                stationLabel.SetText(
                    "[" + i.ToString() + "]" +
                    weaponStations[i].stationName + ": " +
                    weaponStations[i].ammo + "/" +
                    weaponStations[i].maxAmmo);
                stationLabel.SetFontSize(fontSize+6); // WE FORCE IT, otherwise the max size might not get taken into account
                Vector2 textSize = stationLabel.GetTextSize();
                if (textSize.x > maxLabelWidth) {
                    maxLabelWidth = textSize.x;
                }
                stationLabels.Add(stationLabel);
            }
            // Adjust sizes and positions
            padding = (fontSize + 6) / 4;
            float rectHalfWidth = maxLabelWidth / 2f;
            float rectHalfHeight = weaponStations.Count / 2f * (fontSize + 6);
            if (sendToHMD) {
                if (InternalState.manualPlacement) {
                    horizontalOffset += InternalState.horizontalOffset;
                    verticalOffset += InternalState.verticalOffset;
                }
                else {
                    if (InternalState.vanillaUIEnabled) {
                        // The new way to punctually directly reflect elements
                        GameObject topRightPanel = new TraverseCache<CombatHUD, GameObject>("topRightPanel").GetValue(Bindings.UI.Game.GetCombatHUDComponent());
                        GameObject powerPanel = topRightPanel.transform.Find("PowerPanel").gameObject;
                        horizontalOffset = 
                            topRightPanel.transform.localPosition.x 
                            - rectHalfWidth - border - padding;
                        verticalOffset = 
                            topRightPanel.transform.localPosition.y
                            - rectHalfHeight - border - padding
                            - 170;
                        if (Bindings.Player.Aircraft.Countermeasures.HasJammer() || powerPanel.activeSelf) {
                            Plugin.Log("[LP] Adjusting Loadout Preview position to avoid jammer display overlap.");
                            verticalOffset -= 40;
                        }
                    }
                    else {
                        horizontalOffset = (1920 / 2) - rectHalfWidth - border - padding;
                        verticalOffset = (1080 / 2) - rectHalfHeight - border - padding;
                    }
                }
            }
            // Center labels based on max width
            UpdateLabelPositions();
            // Set background size
            borderRect.SetCorners(
                a: new Vector2(-rectHalfWidth - padding + horizontalOffset, -rectHalfHeight - padding + verticalOffset),
                b: new Vector2(rectHalfWidth + padding + horizontalOffset, rectHalfHeight + padding + verticalOffset)
            );
        }

        public void UpdateLabelPositions() {
            for (int i = 0; i < stationLabels.Count; i++) {
                Vector2 textSize = stationLabels[i].GetTextSize();
                stationLabels[i].SetPosition(
                    new Vector2(
                        x: -(maxLabelWidth - textSize.x) / 2f + horizontalOffset - padding/2,
                        y: (stationLabels.Count - 1) * padding * 2f - i * (fontSize + 6) + verticalOffset));
            }
        }

        public void SetActive(bool active) {
            containerObject?.SetActive(active);
        }

        public void Destroy() {
            if (containerObject != null) {
                Object.Destroy(containerObject);
                containerObject = null;
            }
            stationLabels.Clear();
            borderRect = null;
        }
    }

    // INIT AND REFRESH LOOP
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
