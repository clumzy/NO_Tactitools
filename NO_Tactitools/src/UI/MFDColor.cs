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
            InternalState.MFDAlternativeAttitudeEnabled = Plugin.MFDAlternativeAttitudeEnabled.Value;
        }

        public static void Update() { }
    }

    public static class InternalState {
        static public float mainHue;
        static public float mainSaturation;
        static public float mainBrightness;
        static public bool MFDAlternativeAttitudeEnabled;
    }

    static class DisplayEngine {
        public static void Init() {
            Plugin.Log("[MFD] Resetting MFD colors");
            WeaponDisplayComponent.WeaponDisplay.mainColor = Color.HSVToRGB(
                InternalState.mainHue,
                InternalState.mainSaturation,
                1.0f);
            Transform tacScreenTransform = Bindings.UI.Game.GetTacScreen();
            foreach (Text text in tacScreenTransform.GetComponentsInChildren<Text>()) {
                text.color = Color.HSVToRGB(
                    InternalState.mainHue,
                    InternalState.mainSaturation,
                    InternalState.mainBrightness);
            }
            foreach (Image image in tacScreenTransform.GetComponentsInChildren<Image>()) {
                if (image.transform.name == "Ground") {
                    if (InternalState.MFDAlternativeAttitudeEnabled) {
                        image.color = new Color(
                            45 / 255f,
                            91 / 255f,
                            128 / 255f);
                    } // Skip the ground image if alternative attitude is not enabled
                }
                else if (image.transform.name == "Sky") {
                    if (InternalState.MFDAlternativeAttitudeEnabled) {
                        image.color = new Color(
                            175 / 255f,
                            225 / 255f,
                            245 / 255f);
                    } // Skip the horizon image if alternative attitude is not enabled
                }
                else {
                    Color.RGBToHSV(image.color, out _, out float saturation, out float brightness);
                    image.color = Color.HSVToRGB(
                        InternalState.mainHue,
                        saturation, // we keep the original saturation
                        brightness);
                } // we keep the original brightness
            }
        }

        public static void Update() { }
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