using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class MFDColorPlugin {
    private static bool initialized = false;
    public static float mainColor;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[MFD] MFD Color plugin starting !");
            Plugin.harmony.PatchAll(typeof(MFDColorResetPatch));
            mainColor = Plugin.MFDColorMainColor.Value;
            WeaponDisplay.SetMainColor(Color.HSVToRGB(mainColor, 1.0f, 1.0f));
            initialized = true;
            Plugin.Log("[MFD] MFD Color plugin successfully started !");
        }
    }
}

[HarmonyPatch(typeof(TacScreen), "Initialize")]
class MFDColorResetPatch {
    static void Postfix() {
        Plugin.Log("[MFD] Resetting MFD colors");
        Transform tacScreenTransform = UIUtils.tacticalScreen;
        foreach(Text text in tacScreenTransform.GetComponentsInChildren<Text>()){
            Color.RGBToHSV(text.color, out float hue, out float saturation, out float value);
            text.color = Color.HSVToRGB(MFDColorPlugin.mainColor, saturation, value);
        }
        foreach(Image image in tacScreenTransform.GetComponentsInChildren<Image>()){
            if (image.transform.parent.name == "Horizon") continue; // Skip Horizon images
            if (image.transform.parent.name == "Ground") continue; // Skip Ground images
            Color.RGBToHSV(image.color, out float hue, out float saturation, out float value);
            image.color = Color.HSVToRGB(MFDColorPlugin.mainColor, saturation, value);
        }
    }
}