using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Collections.Generic;

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
            InternalState.loadoutPreview?.Reset();
            InternalState.weaponStations.Clear();
            InternalState.displayDuration = Plugin.loadoutPreviewDuration.Value;
            InternalState.onlyShowOnBoot = Plugin.loadoutPreviewOnlyShowOnBoot.Value;
            InternalState.sendToHMD = Plugin.loadoutPreviewSendToHMD.Value;
            InternalState.manualPlacement = Plugin.loadoutPreviewManualPlacement.Value;
            InternalState.horizontalOffset = Plugin.loadoutPreviewPositionX.Value;
            InternalState.verticalOffset = Plugin.loadoutPreviewPositionY.Value;
            InternalState.transparency = Plugin.loadoutPreviewTransparency.Value;
            InternalState.neverShown = true;
            InternalState.hasStations = Bindings.Player.Aircraft.Weapons.GetStationCount() > 0;
            for (int i = 0; i < Bindings.Player.Aircraft.Weapons.GetStationCount(); i++) {
                InternalState.WeaponStationInfo stationInfo = new() {
                    stationName = Bindings.Player.Aircraft.Weapons.GetStationNameByIndex(i),
                    ammo = Bindings.Player.Aircraft.Weapons.GetStationAmmoByIndex(i),
                    maxAmmo = Bindings.Player.Aircraft.Weapons.GetStationMaxAmmoByIndex(i)
                };
                InternalState.weaponStations.Add(stationInfo);
            }
            if (InternalState.hasStations) {
                InternalState.currentWeaponStation = Bindings.Player.Aircraft.Weapons.GetActiveStationName();
            }
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() || Bindings.Player.Aircraft.GetAircraft() == null || !InternalState.hasStations)
                return;
            if (InternalState.onlyShowOnBoot && InternalState.neverShown && BootScreenComponent.InternalState.hasBooted) {
                InternalState.lastUpdateTime = Time.time;
                InternalState.currentWeaponStation = Bindings.Player.Aircraft.Weapons.GetActiveStationName();
                InternalState.neverShown = false;
            }
            else if (
                InternalState.currentWeaponStation != Bindings.Player.Aircraft.Weapons.GetActiveStationName() &&
                BootScreenComponent.InternalState.hasBooted &&
                !InternalState.onlyShowOnBoot) {
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
        public static bool manualPlacement = false;
        public static int horizontalOffset = 0;
        public static int verticalOffset = 0;
        public static float transparency = 0.6f;
        public static bool hasStations = true;
        public static float displayDuration = 1f;
        public static Color mainColor = Color.green;
        public static Color textColor = Color.green;
    }

    static class DisplayEngine {
        static public void Init() {
            if (InternalState.hasStations)
                InternalState.loadoutPreview = new LoadoutPreview(sendToHMD: InternalState.sendToHMD);
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
                    (InternalState.weaponStations[i].ammo == 0) ? Color.red : InternalState.textColor);
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
            for (int i = 0; i < InternalState.loadoutPreview.stationLabels.Count; i++) {
                Vector2 labelPos = InternalState.loadoutPreview.stationLabels[i].GetPosition();
                Vector2 textSize = InternalState.loadoutPreview.stationLabels[i].GetTextSize();
                InternalState.loadoutPreview.stationLabels[i].SetPosition(
                    new Vector2(
                        -(InternalState.loadoutPreview.maxLabelWidth - textSize.x) / 2f + InternalState.loadoutPreview.horizontalOffset,
                        labelPos.y));
            }
        }
    }

    public class LoadoutPreview {
        public Transform loadoutPreview_transform;
        public List<Bindings.UI.Draw.UILabel> stationLabels = [];
        public Bindings.UI.Draw.UIAdvancedRectangle borderRect;
        public int maxLabelWidth = 0;
        public int verticalOffset = 0;
        public int horizontalOffset = 0;
        public int fontSize = 34;
        public LoadoutPreview(bool sendToHMD = false) {
            List<InternalState.WeaponStationInfo> weaponStations = InternalState.weaponStations;
            string platformName;
            if (!sendToHMD) {
                loadoutPreview_transform = Bindings.UI.Game.GetTacScreenTransform();
                platformName = Bindings.Player.Aircraft.GetPlatformName();
            }
            else {
                loadoutPreview_transform = Bindings.UI.Game.GetCombatHUDTransform();
                platformName = "HMD";
            }
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
                InternalState.mainColor = new(0f, 1f, 0f, 0.9f);
                InternalState.textColor = new(0f, 1f, 0f, 0.9f);
                backgroundColor = new(0f, 0f, 0f, InternalState.transparency);
            }
            // Create background rectangle
            borderRect = new(
                "i_LoadoutPreviewBorder",
                new Vector2(-1, -1),
                new Vector2(1, 1),
                InternalState.mainColor,
                border,
                loadoutPreview_transform,
                backgroundColor
            );
            // Create labels
            for (int i = 0; i < weaponStations.Count; i++) {
                Bindings.UI.Draw.UILabel stationLabel = new(
                    "i_Slot " + i,
                    new Vector2(0, 0),
                    loadoutPreview_transform,
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
                stationLabels.Add(stationLabel);
            }
            // Adjust sizes and positions
            // Find max label width
            foreach (var label in stationLabels) {
                Vector2 textSize = label.GetTextSize();
                if (textSize.x > maxLabelWidth) {
                    maxLabelWidth = (int)textSize.x;
                }
            }
            int padding = (fontSize + 6) / 4;
            float rectHalfWidth = maxLabelWidth / 2f + padding;
            float rectHalfHeight = weaponStations.Count / 2f * (fontSize + 6) + padding;
            if (sendToHMD) {
                if (InternalState.manualPlacement) {
                    horizontalOffset += InternalState.horizontalOffset;
                    verticalOffset += InternalState.verticalOffset;
                }
                else {
                    horizontalOffset += ((int)1920 / 2) - (int)rectHalfWidth - border - padding;
                    verticalOffset += ((int)1080 / 2) - (int)rectHalfHeight - border - padding;
                    if (WeaponDisplayComponent.InternalState.vanillaUIEnabled) {
                        verticalOffset -= 100;
                        if (Bindings.Player.Aircraft.Countermeasures.HasJammer()) {
                            verticalOffset -= 45;
                        }
                    }
                }
            }
            // Center labels based on max width
            for (int i = 0; i < stationLabels.Count; i++) {
                Vector2 textSize = stationLabels[i].GetTextSize();
                stationLabels[i].SetPosition(
                    new Vector2(
                        -(maxLabelWidth - textSize.x) / 2f + horizontalOffset,
                        (stationLabels.Count - 1) * padding * 2f - i * (fontSize + 6) + verticalOffset));
            }
            // Set background size
            borderRect.SetCorners(
                new Vector2(-rectHalfWidth - border + horizontalOffset, -rectHalfHeight - border + verticalOffset),
                new Vector2(rectHalfWidth + border + horizontalOffset, rectHalfHeight + border + verticalOffset)
            );
        }

        public void SetActive(bool active) {
            borderRect.GetGameObject()?.SetActive(active);
            foreach (Bindings.UI.Draw.UILabel label in stationLabels) {
                label.GetGameObject()?.SetActive(active);
            }
        }
        public void Reset() {
            foreach (var label in stationLabels) {
                label.Destroy();
            }
            stationLabels.Clear();
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
