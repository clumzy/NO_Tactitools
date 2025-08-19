using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class MFDColorPlugin {
    private static bool initialized = false;
    public static float mainHue;
    public static float mainSaturation;
    public static float mainBrightness;
    public static bool MFDAlternativeAttitudeEnabled;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[MFD] MFD Color plugin starting !");
            Plugin.harmony.PatchAll(typeof(MFDColorResetPatch));
            Plugin.harmony.PatchAll(typeof(ResetMFDColorOnRespawnPatch));
            Reset();
            initialized = true;
            Plugin.Log("[MFD] MFD Color plugin successfully started !");
        }
    }

    public static void Reset() {
        Color.RGBToHSV(Plugin.MFDColor.Value, out mainHue, out mainSaturation, out mainBrightness);
        MFDAlternativeAttitudeEnabled = Plugin.MFDAlternativeAttitudeEnabled.Value;
        WeaponDisplay.SetMainColor(Color.HSVToRGB(
            mainHue,
            mainSaturation,
            1.0f));
    }
}

[HarmonyPatch(typeof(TacScreen), "Initialize")]
class MFDColorResetPatch {
    static void Postfix() {
        Plugin.Log("[MFD] Resetting MFD colors");
        Transform tacScreenTransform = UIUtils.tacticalScreen;
        foreach(Text text in tacScreenTransform.GetComponentsInChildren<Text>()){
            text.color = Color.HSVToRGB(
                MFDColorPlugin.mainHue,
                MFDColorPlugin.mainSaturation,
                MFDColorPlugin.mainBrightness);
        }
        foreach(Image image in tacScreenTransform.GetComponentsInChildren<Image>()){
            if (image.transform.name == "Ground"){
                if (MFDColorPlugin.MFDAlternativeAttitudeEnabled) {
                    image.color = new Color(
                        45/255f,
                        91/255f,
                        128/255f);
                } // Skip the ground image if alternative attitude is not enabled
            }
            else if (image.transform.name == "Sky"){
                if (MFDColorPlugin.MFDAlternativeAttitudeEnabled) {
                    image.color = new Color(
                        175/255f,
                        225/255f,
                        245/255f);
                } // Skip the horizon image if alternative attitude is not enabled
            }
            else{
                Color.RGBToHSV(image.color, out float hue, out float saturation, out float brightness);
                image.color = Color.HSVToRGB(
                    MFDColorPlugin.mainHue, 
                    saturation, // we keep the original saturation
                    brightness);} // we keep the original brightness
        }
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetMFDColorOnRespawnPatch {
    static void Postfix() {
        // Reset the unitStates when the aircraft is destroyed
        MFDColorPlugin.Reset();
    }
}