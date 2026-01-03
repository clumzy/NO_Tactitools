using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;
using NO_Tactitools.Controls;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class MFDColorPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[MFD] MFD Color plugin starting !");
            Plugin.harmony.PatchAll(typeof(MFDColorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(MFDColorComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[MFD] MFD Color plugin successfully started !");
        }
    }
}

public static class MFDColorComponent {
    static class LogicEngine {
        public static void Init() {
            Color.RGBToHSV(Plugin.MFDColor.Value, out InternalState.mainHue, out InternalState.mainSaturation, out InternalState.mainBrightness);
            Color.RGBToHSV(Plugin.MFDTextColor.Value, out InternalState.textHue, out InternalState.textSaturation, out InternalState.textBrightness);
            InternalState.MFDAlternativeAttitudeEnabled = Plugin.MFDAlternativeAttitudeEnabled.Value;
            InternalState.currentColor = Plugin.MFDColor.Value;
            InternalState.currentTextColor = Plugin.MFDTextColor.Value;
        }

        public static void Update() {
            InternalState.needsUpdate = (
                Plugin.MFDColor.Value != InternalState.currentColor ||
                Plugin.MFDTextColor.Value != InternalState.currentTextColor ||
                Plugin.MFDAlternativeAttitudeEnabled.Value != InternalState.MFDAlternativeAttitudeEnabled);
            if (InternalState.needsUpdate) {
                Color.RGBToHSV(Plugin.MFDColor.Value, out InternalState.mainHue, out InternalState.mainSaturation, out InternalState.mainBrightness);
                Color.RGBToHSV(Plugin.MFDTextColor.Value, out InternalState.textHue, out InternalState.textSaturation, out InternalState.textBrightness);
                InternalState.currentColor = Plugin.MFDColor.Value;
                InternalState.currentTextColor = Plugin.MFDTextColor.Value;
            }
        }
    }

    public static class InternalState {
        static public float mainHue;
        static public float mainSaturation;
        static public float mainBrightness;
        static public float textHue;
        static public float textSaturation;
        static public float textBrightness;
        static public bool MFDAlternativeAttitudeEnabled;
        static public Color currentColor;
        static public Color currentTextColor;
        static public bool needsUpdate = false;
    }

    static class DisplayEngine {
        public static void Init() {
            Plugin.Log("[MFD] Resetting MFD colors");
            // Now onto the original elements
            Transform tacScreenTransform = Bindings.UI.Game.GetTacScreenTransform();
            foreach (Text text in tacScreenTransform.GetComponentsInChildren<Text>(true)) {
                Color originalTextColor = text.color;
                Color newTextColor = Color.HSVToRGB(
                    InternalState.textHue,
                    InternalState.textSaturation,
                    InternalState.textBrightness);
                newTextColor.a = originalTextColor.a;
                text.color = newTextColor;
            }
            foreach (Image image in tacScreenTransform.GetComponentsInChildren<Image>(true)) {
                float originalAlpha = image.color.a;
                if (image.transform.name == "Ground") {
                    if (InternalState.MFDAlternativeAttitudeEnabled) {
                        Color groundColor = Color.HSVToRGB(
                            InternalState.mainHue,
                            InternalState.mainSaturation,
                            InternalState.mainBrightness * 0.5f);
                        groundColor.a = originalAlpha;
                        image.color = groundColor;
                    } // Skip the ground image if alternative attitude is not enabled
                }
                else if (image.transform.name == "Sky") {
                    if (InternalState.MFDAlternativeAttitudeEnabled) {
                        Color skyColor = Color.HSVToRGB(
                            InternalState.mainHue,
                            InternalState.mainSaturation * 0.7f,
                            InternalState.mainBrightness);
                        skyColor.a = originalAlpha;
                        image.color = skyColor;
                    } // Skip the horizon image if alternative attitude is not enabled
                }
                else {
                    Color.RGBToHSV(image.color, out _, out float saturation, out float brightness);
                    Color newImageColor = Color.HSVToRGB(
                        InternalState.mainHue,
                        saturation, // we keep the original saturation
                        brightness);
                    newImageColor.a = originalAlpha;
                    image.color = newImageColor;
                } // we keep the original brightness
            }
            // Apply the main color to weapon display and MFD texts and images
            Color newMainColor = Color.HSVToRGB(
                InternalState.mainHue,
                InternalState.mainSaturation,
                1.0f);
            newMainColor.a = 1;
            Color newComponentTextColor = Color.HSVToRGB(
                InternalState.textHue,
                InternalState.textSaturation,
                InternalState.textBrightness); // We keep the brightness for the text
            newComponentTextColor.a = 1;
            WeaponDisplayComponent.InternalState.mainColor = newMainColor;
            WeaponDisplayComponent.InternalState.textColor = newComponentTextColor;
            // Apply the main color to loadout preview
            if (!LoadoutPreviewComponent.InternalState.sendToHMD) {
                Color newLoadoutColor = Color.HSVToRGB(
                    InternalState.mainHue,
                    InternalState.mainSaturation,
                    1.0f);
                newLoadoutColor.a = 1.0f;
                LoadoutPreviewComponent.InternalState.mainColor = newLoadoutColor;
                LoadoutPreviewComponent.InternalState.textColor = newComponentTextColor;
            }
            TargetListControllerComponent.InternalState.mainColor = newMainColor;
            InterceptionVectorTask.mainColor = newMainColor;
        }

        public static void Update() {
            if (InternalState.needsUpdate) {
                Init(); // reapply the colors if needed
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