using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.IO.Compression;

namespace NO_Tactitools.UI;

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
            InternalState.loadoutPreview.Reset();
            for (int i = 0; i < Bindings.Player.Weapons.GetStationCount(); i++) {
                InternalState.WeaponStationInfo stationInfo = new() {
                    stationName = Bindings.Player.Weapons.GetStationNameByIndex(i),
                    ammo = Bindings.Player.Weapons.GetStationAmmoByIndex(i),
                    maxAmmo = Bindings.Player.Weapons.GetStationMaxAmmoByIndex(i)
                };
                InternalState.weaponStations.Add(stationInfo);
            }
            InternalState.currentWeaponStation = Bindings.Player.Weapons.GetActiveStationName();
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() || Bindings.Player.Aircraft.GetAircraft() == null)
                return;
            if (InternalState.currentWeaponStation != Bindings.Player.Weapons.GetActiveStationName() && BootScreenComponent.InternalState.hasBooted) {
                InternalState.lastUpdateTime = Time.time;
                InternalState.currentWeaponStation = Bindings.Player.Weapons.GetActiveStationName();
            }
            if (Time.time - InternalState.lastUpdateTime < 0.8f)
                InternalState.needsUpdate = true;
            else
                InternalState.needsUpdate = false;
            if (InternalState.needsUpdate) {
                for (int i = 0; i < Bindings.Player.Weapons.GetStationCount(); i++) {
                    InternalState.weaponStations[i].stationName = Bindings.Player.Weapons.GetStationNameByIndex(i);
                    InternalState.weaponStations[i].ammo = Bindings.Player.Weapons.GetStationAmmoByIndex(i);
                    InternalState.weaponStations[i].maxAmmo = Bindings.Player.Weapons.GetStationMaxAmmoByIndex(i);
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
        public static List<WeaponStationInfo> weaponStations = [];
        public static LoadoutPreview loadoutPreview;
        public static Color mainColor = Color.green;
    }

    static class DisplayEngine {
        static public void Init() {
            InternalState.loadoutPreview = new LoadoutPreview();
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() || Bindings.Player.Aircraft.GetAircraft() == null)
                return;
            if (!InternalState.needsUpdate) {
                InternalState.loadoutPreview.SetActive(false);
                return;
            }
            InternalState.loadoutPreview.SetActive(true);
            InternalState.loadoutPreview.backgroundRect.SetColor(Color.black);
            for (int i = 0; i < InternalState.weaponStations.Count; i++) {
                InternalState.loadoutPreview.stationLabels[i].SetColor(
                    (InternalState.weaponStations[i].ammo == 0) ? Color.red : InternalState.mainColor);
            }
            for (int i = 0; i < InternalState.weaponStations.Count; i++) {
                var ws = InternalState.weaponStations[i];
                InternalState.loadoutPreview.stationLabels[i].SetText(
                    "[" + i.ToString() + "]" +
                    ws.stationName + ": " +
                    ws.ammo + "/" +
                    ws.maxAmmo);
                // keep color/size adjustments minimal here; DisplayEngine handles color each frame
                InternalState.loadoutPreview.stationLabels[i].SetFontSize(
                    (Bindings.Player.Weapons.GetActiveStationName() == ws.stationName) ? (InternalState.loadoutPreview.fontSize + 6) : InternalState.loadoutPreview.fontSize);
                InternalState.loadoutPreview.stationLabels[i].SetFontStyle(
                    (Bindings.Player.Weapons.GetActiveStationName() == ws.stationName) ? FontStyle.Bold : FontStyle.Normal);
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
        public Bindings.UI.Draw.UIRectangle borderRect;
        public Bindings.UI.Draw.UIRectangle backgroundRect;
        public int maxLabelWidth = 0;
        public int verticalOffset = 0;
        public int horizontalOffset = 0;
        public int fontSize = 34;
        public LoadoutPreview() {
            List<InternalState.WeaponStationInfo> weaponStations = InternalState.weaponStations;
            loadoutPreview_transform = Bindings.UI.Game.GetTacScreen();
            string platformName = Bindings.Player.Aircraft.GetPlatformName();
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
                default:
                    break;
            }
            // Create background rectangle
            borderRect = new Bindings.UI.Draw.UIRectangle(
                "i_LoadoutPreviewBorder",
                new Vector2(-1, -1),
                new Vector2(1, 1),
                loadoutPreview_transform,
                InternalState.mainColor
            );
            backgroundRect = new Bindings.UI.Draw.UIRectangle(
                "i_LoadoutPreviewBackground",
                new Vector2(-1, -1),
                new Vector2(1, 1),
                loadoutPreview_transform,
                Color.black
            );
            // Create labels
            for (int i = 0; i < weaponStations.Count; i++) {
                Bindings.UI.Draw.UILabel stationLabel = new(
                    "i_Slot " + i,
                    new Vector2(0, 0),
                    loadoutPreview_transform,
                    fontStyle: FontStyle.Bold, // Default to bold; will be updated in DisplayEngine
                    color: InternalState.mainColor,
                    fontSize: fontSize+6, // Default to 40; will be updated in DisplayEngine
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
            float rectHalfHeight = weaponStations.Count / 2f * (fontSize+6) + padding;
            int border = 2;
            // Center labels based on max width
            for (int i = 0; i < stationLabels.Count; i++) {
                Vector2 textSize = stationLabels[i].GetTextSize();
                stationLabels[i].SetPosition(
                    new Vector2(
                        -(maxLabelWidth - textSize.x) / 2f + horizontalOffset,
                        (stationLabels.Count - 1) * padding*2f - i * (fontSize+6) + verticalOffset));
            }
            // Set background size
            borderRect.SetCorners(
                new Vector2(-rectHalfWidth - border + horizontalOffset, -rectHalfHeight - border + verticalOffset),
                new Vector2(rectHalfWidth + border + horizontalOffset, rectHalfHeight + border + verticalOffset)
            );
            backgroundRect.SetCorners(
                new Vector2(-rectHalfWidth + horizontalOffset, -rectHalfHeight + verticalOffset),
                new Vector2(rectHalfWidth + horizontalOffset, rectHalfHeight + verticalOffset)
            );
        }

        public void SetActive(bool active) {
            borderRect.GetGameObject().SetActive(active);
            backgroundRect.GetGameObject().SetActive(active);
            foreach (var label in stationLabels) {
                label.GetGameObject().SetActive(active);
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
