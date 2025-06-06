using System;
using System.Collections.Generic;
using System.Data;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponDisplayPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[WD] Weapon Display plugin starting !");
            Plugin.harmony.PatchAll(typeof(WeaponDisplayTask));
            Plugin.harmony.PatchAll(typeof(ResetWeaponDisplayOnRespawnPatch));
            // Register the FlareEjector and RadarJammer patches
            Plugin.harmony.PatchAll(typeof(FlareEjectorRegisterPatch));
            Plugin.harmony.PatchAll(typeof(RadarJammerRegisterPatch));
            initialized = true;
            Plugin.Log("[WD] Weapon Display plugin succesfully started !");
        }
    }
}

[HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
class WeaponDisplayTask {
    public static bool initialized = false;
    private static FlareEjector flareEjector;
    private static RadarJammer radarJammer;
    private static WeaponDisplay weaponDisplay;

    static void Postfix() {
        string platformName = SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName;
        Transform destination = GetDestination(platformName);
        if (destination == null) return; // If the platform is not supported or destination canvas is not initialized, do nothing
        if (!initialized) {
            Plugin.Log("[WD] Weapon Display Task starting for airplane " + platformName);
            if (flareEjector != null) {
                // Remove the existing MFD content and kill the layout
                weaponDisplay = new WeaponDisplay(platformName, destination);
                UIUtils.HideWeaponPanel();
                initialized = true;
            }
            else return;
        }
        // REGULAR OPERATION STARTS HERE
        // Refresh Weapons
        weaponDisplay.RefreshWeapon();
        if (flareEjector == null && radarJammer == null) {
            Plugin.Log("[WD] FlareEjector or RadarJammer is null, aborting update.");
        }
        else {
            if (flareEjector != null) {
                if (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex == 0) {
                    weaponDisplay.RefreshFlare(true);
                }
                else {
                    weaponDisplay.RefreshFlare(false);
                }
            }
            if (radarJammer != null) {
                if (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex == 1) {
                    weaponDisplay.RefreshJammer(true);
                }
                else {
                    weaponDisplay.RefreshJammer(false);
                }
            }
        }
        return;
    }

    public static FlareEjector GetFlareEjector() {
        return flareEjector;
    }

    public static void SetFlareEjector(FlareEjector flare) {
        flareEjector = flare;
    }

    public static RadarJammer GetRadarJammer() {
        return radarJammer;
    }

    public static void SetRadarJammer(RadarJammer jammer) {
        radarJammer = jammer;
    }

    public static Transform GetDestination(string platformName) {
        // Get the destination Transform based on the platform name
        return platformName switch {
            "T/A-30 Compass" or "FS-12 Revoker" or "FS-20 Vortex" or "KR-67 Ifrit" => UIUtils.tacticalScreen.Find("Canvas/SystemStatus").transform,
            // For Cricket, return the EngPanel under Canvas under tacticalScreen
            "CI-22 Cricket" => UIUtils.tacticalScreen.Find("Canvas/EngPanel").transform,
            "SAH-46 Chicane" => UIUtils.tacticalScreen.Find("Canvas/TelemetryPanel").transform,
            _ => null, // Return null if the platform is not supported
        };
    }

}
public class WeaponDisplay {
        private static UIUtils.UILabel flareLabel;
        private static UIUtils.UILabel radarLabel;
        private static UIUtils.UILine MFD_systemsLine;
        private static UIUtils.UILabel weaponNameLabel;
        private static UIUtils.UILabel weaponAmmoLabel;
        private static GameObject weaponImageClone;

        // Store original font sizes
        private int originalFlareFont;
        private int originalRadarFont;

        public WeaponDisplay(string platformName, Transform destination) {
            bool rotateWeaponImage = false;
            float imageScaleFactor = 0.6f;
            // Layout settings for each supported platform
            Vector2 flarePos, radarPos, lineStart, lineEnd, weaponNamePos, weaponAmmoPos, weaponImagePos;
            int flareFont, radarFont, weaponNameFont, weaponAmmoFont;
            switch (platformName) {
                case "CI-22 Cricket":
                    flarePos = new Vector2(0, -40);
                    radarPos = new Vector2(0, -80);
                    lineStart = new Vector2(0, -60);
                    lineEnd = new Vector2(0, 60);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 30);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    radarFont = 30;
                    weaponNameFont = 18;
                    weaponAmmoFont = 40;
                    break;
                case "SAH-46 Chicane":
                    flarePos = new Vector2(0, -80);
                    radarPos = new Vector2(0, -160);
                    lineStart = new Vector2(-6000, 0);
                    lineEnd = new Vector2(6000, 0);
                    weaponNamePos = new Vector2(0, 110);
                    weaponAmmoPos = new Vector2(0, 60);
                    weaponImagePos = new Vector2(0, 160);
                    flareFont = 55;
                    radarFont = 55;
                    weaponNameFont = 35;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 1.2f; // Scale the image for SAH-46 Chicane
                    break;
                case "T/A-30 Compass":
                    flarePos = new Vector2(0, -40);
                    radarPos = new Vector2(0, -80);
                    lineStart = new Vector2(0, -60);
                    lineEnd = new Vector2(0, 60);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 30);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    radarFont = 30;
                    weaponNameFont = 18;
                    weaponAmmoFont = 40;
                    break;
                case "FS-12 Revoker":
                    flarePos = new Vector2(0, -40);
                    radarPos = new Vector2(0, -80);
                    lineStart = new Vector2(-80, -10);
                    lineEnd = new Vector2(80, -10);
                    weaponNamePos = new Vector2(0, 50);
                    weaponAmmoPos = new Vector2(0, 20);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    radarFont = 30;
                    weaponNameFont = 25;
                    weaponAmmoFont = 35;
                    imageScaleFactor = 0.8f; // Scale the image for FS-12 Revoker
                    break;
                case "FS-20 Vortex":
                    flarePos = new Vector2(-70, -70);
                    radarPos = new Vector2(70, -70);
                    lineStart = new Vector2(-200, -20);
                    lineEnd = new Vector2(200, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(70, 20);
                    weaponImagePos = new Vector2(-70, 20);
                    flareFont = 35;
                    radarFont = 35;
                    weaponNameFont = 30;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 0.6f; // Scale the image for KR-67 Ifrit
                    break;
                case "KR-67 Ifrit":
                    flarePos = new Vector2(-70, -70);
                    radarPos = new Vector2(70, -70);
                    lineStart = new Vector2(-200, -20);
                    lineEnd = new Vector2(200, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(80, 20);
                    weaponImagePos = new Vector2(-70, 20);
                    flareFont = 45;
                    radarFont = 45;
                    weaponNameFont = 45;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 0.8f; // Scale the image for KR-67 Ifrit
                    break;
                default:
                    flarePos = new Vector2(0, -40);
                    radarPos = new Vector2(0, -80);
                    lineStart = new Vector2(0, -60);
                    lineEnd = new Vector2(0, 60);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 30);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    radarFont = 30;
                    weaponNameFont = 18;
                    weaponAmmoFont = 30;
                    break;
            }
            // Store original font sizes
            originalFlareFont = flareFont;
            originalRadarFont = radarFont;

        // Hide the existing MFD content and kill the layout
            UIUtils.HideChildren(destination);
            UIUtils.KillLayout(destination);
            
            // Create the labels and line for the systems MFD
            flareLabel = new(
                "flareLabel",
                flarePos,
                destination,
                FontStyle.Normal,
                Color.green,
                flareFont,
                0f
            );
            flareLabel.SetText("");
            radarLabel = new(
                "radarLabel",
                radarPos,
                destination,
                FontStyle.Normal,
                Color.green,
                radarFont,
                0f
            );
            radarLabel.SetText("⇌");
            MFD_systemsLine = new(
                "MFD_systemsLine",
                lineStart,
                lineEnd,
                destination,
                Color.green,
                4f
            );
            weaponNameLabel = new(
                "weaponNameLabel",
                weaponNamePos,
                destination,
                FontStyle.Normal,
                Color.green,
                weaponNameFont,
                0f
            );
            weaponAmmoLabel = new(
                "weaponAmmoLabel",
                weaponAmmoPos,
                destination,
                FontStyle.Normal,
                Color.green,
                weaponAmmoFont,
                0f
            );
            // Clone the weapon image and set it as a child of the systems MFD
            weaponImageClone = GameObject.Instantiate(SceneSingleton<CombatHUD>.i.weaponImage.gameObject, destination);
            var cloneImg = weaponImageClone.GetComponent<Image>();
            cloneImg.rectTransform.sizeDelta = new Vector2(
                cloneImg.rectTransform.sizeDelta.x * imageScaleFactor, 
                cloneImg.rectTransform.sizeDelta.y * imageScaleFactor);
            cloneImg.rectTransform.anchoredPosition = weaponImagePos;
            //rotate the image 90 degrees clockwise
            if (rotateWeaponImage) cloneImg.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
        }

        public void RefreshWeapon() {
            weaponNameLabel.SetText(SceneSingleton<CombatHUD>.i.weaponName.text);
            weaponAmmoLabel.SetText(SceneSingleton<CombatHUD>.i.ammoCount.text);
            weaponAmmoLabel.SetColor(SceneSingleton<CombatHUD>.i.ammoCount.color);

            var image = weaponImageClone.GetComponent<Image>();
            var srcImg = SceneSingleton<CombatHUD>.i.weaponImage;
            image.sprite = srcImg.sprite;
        }

        public void RefreshFlare(bool highlight) {
            var flareEjector = WeaponDisplayTask.GetFlareEjector();
            int ammo = flareEjector.GetAmmo();
            flareLabel.SetText("IR:" + ammo.ToString());

            int font = highlight ? originalFlareFont + 10 : originalFlareFont;
            flareLabel.SetFontStyle(highlight ? FontStyle.Bold : FontStyle.Normal);
            flareLabel.SetFontSize(Mathf.Max(1, font));

            float t = Mathf.Clamp01((float)ammo / flareEjector.GetMaxAmmo());
            Color color = Color.Lerp(Color.red, Color.green, t);
            flareLabel.SetColor(color);
        }

        public void RefreshJammer(bool highlight) {
            var radarJammer = WeaponDisplayTask.GetRadarJammer();
            PowerSupply powerSupply = (PowerSupply)Traverse.Create(radarJammer).Field("powerSupply").GetValue();
            int charge = (int)(powerSupply.GetCharge() * 100f);
            radarLabel.SetText("EW:" + charge.ToString() + "%");

            int font = highlight ? originalRadarFont + 10 : originalRadarFont;
            radarLabel.SetFontStyle(highlight ? FontStyle.Bold : FontStyle.Normal);
            radarLabel.SetFontSize(Mathf.Max(1, font));

            float t = Mathf.Clamp01(charge / 100f);
            Color color = Color.Lerp(Color.red, Color.green, t);
            radarLabel.SetColor(color);
        }
    }

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetWeaponDisplayOnRespawnPatch {
    static void Postfix() {
        UIUtils.RestoreWeaponPanel();
        WeaponDisplayTask.initialized = false;
    }
}

[HarmonyPatch(typeof(FlareEjector), "UpdateHUD")]
class FlareEjectorRegisterPatch {
    static void Postfix(FlareEjector __instance) {
        if (WeaponDisplayTask.GetFlareEjector() == null && __instance.aircraft == SceneSingleton<CombatHUD>.i.aircraft) {
            WeaponDisplayTask.SetFlareEjector(__instance);
            Plugin.Log($"[WD] FlareEjector registered !");
        }
    }
}

[HarmonyPatch(typeof(RadarJammer), "UpdateHUD")]
class RadarJammerRegisterPatch {
    static void Postfix(RadarJammer __instance) {
        if (WeaponDisplayTask.GetRadarJammer() == null && __instance.aircraft == SceneSingleton<CombatHUD>.i.aircraft) {
            WeaponDisplayTask.SetRadarJammer(__instance);
            Plugin.Log($"[WD] RadarJammer registered !");
        }
    }
}