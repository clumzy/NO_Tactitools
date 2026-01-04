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
            Plugin.harmony.PatchAll(typeof(MFDColorComponent.OnSystemStatusRefresh));
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
        static public Color otherComponentMainColor;
        static public Color otherComponentTextColor;
        static public bool needsUpdate = false;
    }

    static class DisplayEngine {
        public static void Init() {
            Plugin.Log("[MFD] Resetting MFD colors");
            // Now onto the original elements
            Transform tacScreenTransform = Bindings.UI.Game.GetTacScreenTransform();
            foreach (Text text in tacScreenTransform.GetComponentsInChildren<Text>(true)) { // TEXT HANDLING
                Color originalTextColor = text.color;
                Color newTextColor = Color.HSVToRGB(
                    InternalState.textHue,
                    InternalState.textSaturation,
                    InternalState.textBrightness);
                newTextColor.a = originalTextColor.a;
                text.color = newTextColor;
            }
            foreach (Image image in tacScreenTransform.GetComponentsInChildren<Image>(true)) { // GROUND AND SKY HANDLING
                float originalAlpha = image.color.a;
                if (image.transform.name == "Ground") {
                    if (InternalState.MFDAlternativeAttitudeEnabled) {
                        Color groundColor = Color.HSVToRGB(
                            InternalState.mainHue,
                            1f,
                            0.5f);
                        groundColor.a = originalAlpha;
                        image.color = groundColor;
                    } // Skip the ground image if alternative attitude is not enabled
                }
                else if (image.transform.name == "Sky") {
                    if (InternalState.MFDAlternativeAttitudeEnabled) {
                        Color skyColor = Color.HSVToRGB(
                            InternalState.mainHue,
                            0.7f,
                            1f);
                        skyColor.a = originalAlpha;
                        image.color = skyColor;
                    } // Skip the horizon image if alternative attitude is not enabled
                }
                else { // IMAGES OTHER THAN GROUND AND SKY
                    Color.RGBToHSV(image.color, out _, out float saturation, out float brightness);
                    Color newImageColor = Color.HSVToRGB(
                        InternalState.mainHue,
                        saturation, // we keep the original saturation
                        brightness);
                    newImageColor.a = originalAlpha;
                    image.color = newImageColor;
                } // we keep the original brightness
            }
            // Apply the main color to other components (other plugins + System Status through InternalState colors)
            InternalState.otherComponentMainColor = Color.HSVToRGB(
                InternalState.mainHue,
                1.0f,
                1.0f);
            InternalState.otherComponentMainColor.a = 1;
            InternalState.otherComponentTextColor = Color.HSVToRGB(
                InternalState.textHue,
                InternalState.textSaturation,
                InternalState.textBrightness); // We keep the brightness for the text
            InternalState.otherComponentTextColor.a = 1;
            WeaponDisplayComponent.InternalState.mainColor = InternalState.otherComponentMainColor;
            WeaponDisplayComponent.InternalState.textColor = InternalState.otherComponentTextColor;
            // Apply the main color to loadout preview
            if (!LoadoutPreviewComponent.InternalState.sendToHMD) {
                LoadoutPreviewComponent.InternalState.mainColor = InternalState.otherComponentMainColor;
                LoadoutPreviewComponent.InternalState.textColor = InternalState.otherComponentTextColor;
            }
            // apply the color to target list and interception vector task
            // normally in logic engine but we want to compute newMainColor first
            TargetListControllerComponent.InternalState.mainColor = InternalState.otherComponentMainColor;
            InterceptionVectorTask.mainColor = InternalState.otherComponentMainColor;
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

    // SPECIFIC SYSTEM STATUS PATCH
    [HarmonyPatch(typeof(SystemStatusDisplay), "Refresh")]
    public static class OnSystemStatusRefresh {
        static void Postfix(SystemStatusDisplay __instance) {
            // Reapply the main color to system status texts and images
            foreach (Text text in __instance.GetComponentsInChildren<Text>(true)) {
                if (text.color == Color.green)
                    text.color = InternalState.otherComponentTextColor;
            }
            foreach (Image image in __instance.GetComponentsInChildren<Image>(true)) {
                if (image.transform.name != "Background" && image.color == Color.green) { // skip background image
                    image.color = InternalState.otherComponentMainColor;
                }
            }
        }
    }
}