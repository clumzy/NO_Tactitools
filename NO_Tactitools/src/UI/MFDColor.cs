using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class MFDColorPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[MFD] MFD Color plugin starting !");
            Plugin.harmony.PatchAll(typeof(MFDColorResetPatch));
            initialized = true;
            Plugin.Log("[MFD] MFD Color plugin successfully started !");
        }
    }
}

[HarmonyPatch(typeof(TacScreen), "Initialize")]
class MFDColorResetPatch {

    static float mainHue;
    static float mainSaturation;
    static float mainBrightness;
    static bool MFDAlternativeAttitudeEnabled;
    static void Postfix() {
        Plugin.Log("[MFD] Resetting MFD colors");
        Color.RGBToHSV(Plugin.MFDColor.Value, out mainHue, out mainSaturation, out mainBrightness);
        MFDAlternativeAttitudeEnabled = Plugin.MFDAlternativeAttitudeEnabled.Value;
        WeaponDisplayComponent.WeaponDisplay.mainColor = Color.HSVToRGB(
            mainHue,
            mainSaturation,
            1.0f);
        Transform tacScreenTransform = Bindings.UI.Game.GetTacScreen();
        foreach (Text text in tacScreenTransform.GetComponentsInChildren<Text>()) {
            text.color = Color.HSVToRGB(
                mainHue,
                mainSaturation,
                mainBrightness);
        }
        foreach (Image image in tacScreenTransform.GetComponentsInChildren<Image>()) {
            if (image.transform.name == "Ground") {
                if (MFDAlternativeAttitudeEnabled) {
                    image.color = new Color(
                        45 / 255f,
                        91 / 255f,
                        128 / 255f);
                } // Skip the ground image if alternative attitude is not enabled
            }
            else if (image.transform.name == "Sky") {
                if (MFDAlternativeAttitudeEnabled) {
                    image.color = new Color(
                        175 / 255f,
                        225 / 255f,
                        245 / 255f);
                } // Skip the horizon image if alternative attitude is not enabled
            }
            else {
                Color.RGBToHSV(image.color, out _, out float saturation, out float brightness);
                image.color = Color.HSVToRGB(
                    mainHue,
                    saturation, // we keep the original saturation
                    brightness);
            } // we keep the original brightness
        }
    }
}