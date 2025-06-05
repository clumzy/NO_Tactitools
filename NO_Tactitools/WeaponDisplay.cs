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
    private static UIUtils.UILabel flareLabel;
    private static UIUtils.UILabel radarLabel;
    private static UIUtils.UILine MFD_systemsLine;
    private static UIUtils.UILabel weaponNameLabel;
    private static UIUtils.UILabel weaponAmmoLabel;

    static void Postfix() {
        // The Cricket is a special case, it has no systems screen to display on
        if (
            SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName == "CI-22 Cricket" ||
            SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName == "SAH-46 Chicane" ||
            SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName == "VL-49 Tarantula")
            return;
        if (!initialized) {
            if (flareEjector != null || radarJammer != null) {
                // Remove the existing MFD content, maybe I should add that to a UIUtils method
                foreach (Transform child in UIUtils.MFD_List["systems"].GetMFDTransform()) {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                flareLabel = new(
                    "flareLabel",
                    new Vector2(0, -40),
                    null,
                    "systems",
                    FontStyle.Normal,
                    Color.green,
                    30,
                    0f
                );
                flareLabel.SetText("");
                radarLabel = new(
                    "radarLabel",
                    new Vector2(0, -80),
                    null,
                    "systems",
                    FontStyle.Normal,
                    Color.green,
                    30,
                    0f
                );
                radarLabel.SetText("⇌");
                MFD_systemsLine = new(
                    "MFD_systemsLine",
                    new Vector2(0, -60),
                    new Vector2(0, 60),
                    null,
                    "systems",
                    Color.green,
                    4f
                );
                weaponNameLabel = new(
                    "weaponNameLabel",
                    new Vector2(0, 60),
                    null,
                    "systems",
                    FontStyle.Normal,
                    Color.green,
                    18,
                    0f
                );
                weaponAmmoLabel = new(
                    "weaponAmmoLabel",
                    new Vector2(0, 30),
                    null,
                    "systems",
                    FontStyle.Normal,
                    Color.green,
                    30,
                    0f
                );
                // We deactivate the existing now useless UI elements
                GameObject countermeasureBackground = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("countermeasureBackground").GetValue();
                GameObject weaponBackground = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("weaponBackground").GetValue();
                GameObject topRightPanel = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("topRightPanel").GetValue();
                countermeasureBackground.SetActive(false);
                weaponBackground.SetActive(false);
                CanvasGroup cg = topRightPanel.GetComponent<CanvasGroup>();
                if (cg == null) {
                    cg = topRightPanel.AddComponent<CanvasGroup>();
                }
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                //set weapon image as child to the systems MFD
                Image weaponImage = SceneSingleton<CombatHUD>.i.weaponImage;
                weaponImage.transform.SetParent(UIUtils.MFD_List["systems"].GetMFDTransform(), false);
                // divide the size of the image by 2 to center it
                weaponImage.rectTransform.sizeDelta = new Vector2(weaponImage.rectTransform.sizeDelta.x * 0.6f, weaponImage.rectTransform.sizeDelta.y * 0.6f);
                // move image 80 pixels up
                weaponImage.rectTransform.anchoredPosition = new Vector2(0, 80);

                initialized = true;
            }
            else return;
        }
        // REGULAR OPERATION STARTS HERE
        // Set weapon label texts
        weaponNameLabel.SetText(SceneSingleton<CombatHUD>.i.weaponName.text);
        weaponAmmoLabel.SetText(SceneSingleton<CombatHUD>.i.ammoCount.text);
        weaponAmmoLabel.SetColor(SceneSingleton<CombatHUD>.i.ammoCount.color);
        if (flareEjector == null && radarJammer == null) {
            Plugin.Log("[WD] FlareEjector or RadarJammer is null, aborting update.");
        }
        else {
            if (flareEjector != null) {
                int ammo = flareEjector.GetAmmo();
                // Set the flare label text and color
                flareLabel.SetText("IR:" + ammo.ToString());
                if (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex == 0) {
                    flareLabel.SetFontStyle(FontStyle.Bold);
                    flareLabel.SetFontSize(30);
                }
                else {
                    flareLabel.SetFontStyle(FontStyle.Normal);
                    flareLabel.SetFontSize(25);
                }
                float t = Mathf.Clamp01((float)ammo / flareEjector.GetMaxAmmo());
                Color color = Color.Lerp(Color.red, Color.green, t);
                flareLabel.SetColor(color);
            }
            if (radarJammer != null) {
                // we use Traverse to access private fields
                PowerSupply powerSupply = (PowerSupply)Traverse.Create(radarJammer).Field("powerSupply").GetValue();
                int charge = (int)(powerSupply.GetCharge() * 100f);
                // Set the radar label text and color
                radarLabel.SetText("EW:" + charge.ToString() + "%");
                if (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex == 1) {
                    radarLabel.SetFontStyle(FontStyle.Bold);
                    radarLabel.SetFontSize(30);
                }
                else {
                    radarLabel.SetFontStyle(FontStyle.Normal);
                    radarLabel.SetFontSize(25);
                }
                float t = Mathf.Clamp01(charge / 100f);
                Color color = Color.Lerp(Color.red, Color.green, t);
                radarLabel.SetColor(color);
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
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetWeaponDisplayOnRespawnPatch {
    static void Postfix() {
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