using HarmonyLib;
using UnityEngine.UI;
using NO_Tactitools.Core;
using UnityEngine;

namespace NO_Tactitools.UI.HMD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class RotorBankIndicatorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[RBI] Rotor Bank Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(RotorBankIndicatorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(RotorBankIndicatorComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[RBI] Rotor Bank Indicator plugin successfully started !");
        }
    }
}

public class RotorBankIndicatorComponent {
    // LOGIC ENGINE, INTERNAL STATE, DISPLAY ENGINE
    static class LogicEngine {
        static public void Init() {
            InternalState.arc = null;
            InternalState.needle = null;
            InternalState.currentBankAngle = 0f;
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null)
                return;
            Transform aircraftTransform = GameBindings.Player.Aircraft.GetAircraft().transform;
            float bank = aircraftTransform.localEulerAngles.z;
            if (bank > 180f) bank -= 360f;
            InternalState.currentBankAngle = bank;
        }
    }

    public static class InternalState {
        public static Image arc;
        public static Image needle;
        public static float currentBankAngle = 0f;
    }

    static class DisplayEngine {
        static public void Init() {
            FuelGauge fg = GameObject.FindFirstObjectByType<FuelGauge>();
            InternalState.arc = GameObject.Instantiate(
                new TraverseCache<FuelGauge, Image>("fuelArc").GetValue(fg).GetComponent<Image>(), 
                UIBindings.Game.GetFlightHUDCenterTransform());
            InternalState.arc.name = "i_RBI_arc";
            // move center of arc to top of the screen and rotate it 90 degrees right
            InternalState.arc.rectTransform.anchoredPosition = new Vector2(0, 200);
            InternalState.arc.rectTransform.rotation = Quaternion.Euler(0, 0, -90);
            InternalState.needle = GameObject.Instantiate(
                UIBindings.Game.GetFlightHUDCenterTransform().Find("compass/compassPoint").GetComponent<Image>(),
                UIBindings.Game.GetFlightHUDCenterTransform());
            InternalState.needle.name = "i_RBI_needle";
            // make it 2/3 the size of the compass needle and move it to the same position as the arc
            InternalState.needle.rectTransform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null)
                return;
            float clampedBankAngle = Mathf.Clamp(InternalState.currentBankAngle, -15f, 15f);

            // needle position using trig to stay on arc
            // calculated radius = 724, center Y = -534 (relative to flight HUD center)
            // at 15 deg bank, needle is at -76 X and 186 Y (from 190 Y top)
            float yCenter = -212f;
            float radius = 406f;
            // indicator at max clamped angle is at 8.8 degrees on the arc, so we can use that to calculate the angle on the arc for any given bank angle
            float arcAngle = (-clampedBankAngle / 15f) * 10.9f;
            float x = radius * Mathf.Sin(arcAngle * Mathf.Deg2Rad);
            float y = yCenter + radius * Mathf.Cos(arcAngle * Mathf.Deg2Rad);
            InternalState.needle.rectTransform.anchoredPosition = new Vector2(x, y);
            InternalState.needle.rectTransform.rotation = Quaternion.Euler(0, 0, clampedBankAngle/15f*14f);
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
