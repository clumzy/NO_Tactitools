using HarmonyLib;
using UnityEngine.UI;
using NO_Tactitools.Core;
using UnityEngine;

namespace NO_Tactitools.UI.HUD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class SlipIndicatorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[SI] Slip Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(SlipIndicatorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(SlipIndicatorComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[SI] Slip Indicator plugin successfully started !");
        }
    }
}

public class SlipIndicatorComponent {
    // LOGIC ENGINE: Handles calculations and aircraft state
    static class LogicEngine {
        private static Vector3 lastVelocity = Vector3.zero;
        private static float smoothingFactor = 5f; // Adjust for more/less smoothing

        static public void Init() {
            InternalState.slipBallOffset = 0f;
            lastVelocity = Vector3.zero;
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null)
                return;
            float dt = Time.fixedDeltaTime; 
            if (dt <= 0) return;

            Vector3 currentVelocity = GameBindings.Player.Aircraft.GetAircraft().rb.velocity;
            Vector3 accel = (currentVelocity - lastVelocity) / dt;
            lastVelocity = currentVelocity;
            Vector3 force = accel - Physics.gravity;
            float lateralForce = Vector3.Dot(
                force, 
                GameBindings.Player.Aircraft.GetAircraft().transform.right);
            float verticalForce = Vector3.Dot(
                force, 
                GameBindings.Player.Aircraft.GetAircraft().transform.up);
            
            if (Mathf.Abs(verticalForce) > 0.1f) {
                float targetOffset = -lateralForce / verticalForce;
                // slowly moving the ball
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, targetOffset, dt * smoothingFactor);
            } else {
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, 0f, dt * smoothingFactor);
            }
        }
    }

    public static class InternalState {
        public static UIBindings.Draw.UILine leftBar;
        public static UIBindings.Draw.UILine rightBar;
        public static UIBindings.Draw.UILine leftOuterBar;
        public static UIBindings.Draw.UILine rightOuterBar;
        public static UIBindings.Draw.UILabel ballLabel;
        public static float slipBallOffset = 0f;
        public static float sensitivity = 150f;
        public static float maxOffset = 50f;
        public static float padding = 10f;
        public static Vector2 basePosition = new Vector2(0, 180);
    }

    // DISPLAY ENGINE: Handles UI creation and updates
    static class DisplayEngine {
        static public void Init() {
            InternalState.leftBar = new UIBindings.Draw.UILine(
                name: "i_SI_leftBar",
                start: InternalState.basePosition + new Vector2(-10, -7),
                end: InternalState.basePosition + new Vector2(-10, 7),
                UIParent: UIBindings.Game.GetFlightHUDCenterTransform(),
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 1f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            InternalState.rightBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightBar",
                start: InternalState.basePosition + new Vector2(10, -7),
                end: InternalState.basePosition + new Vector2(10, 7),
                UIParent: UIBindings.Game.GetFlightHUDCenterTransform(),
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 1f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            InternalState.leftOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_leftOuterBar",
                start: InternalState.basePosition + new Vector2(-InternalState.maxOffset - InternalState.padding, -10),
                end: InternalState.basePosition + new Vector2(-InternalState.maxOffset - InternalState.padding, 10),
                UIParent: UIBindings.Game.GetFlightHUDCenterTransform(),
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 1.5f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            InternalState.rightOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightOuterBar",
                start: InternalState.basePosition + new Vector2(InternalState.maxOffset + InternalState.padding, -10),
                end: InternalState.basePosition + new Vector2(InternalState.maxOffset + InternalState.padding, 10),
                UIParent: UIBindings.Game.GetFlightHUDCenterTransform(),
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 1.5f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            InternalState.ballLabel = new UIBindings.Draw.UILabel(
                name: "i_SI_ballLabel",
                position: InternalState.basePosition,
                UIParent: UIBindings.Game.GetFlightHUDCenterTransform(),
                color: new Color(0f, 1f, 0f, 0.8f),
                fontSize: 25,
                backgroundOpacity: 0f, // No background for the ball
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            InternalState.ballLabel.SetText("●");
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null)
                return;
            float xOffset = Mathf.Clamp(InternalState.slipBallOffset * InternalState.sensitivity, -InternalState.maxOffset, InternalState.maxOffset);
            InternalState.ballLabel.SetPosition(InternalState.basePosition + new Vector2(xOffset, 2));
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart {
        static void Postfix() {
            LogicEngine.Init();
            DisplayEngine.Init();
        }
    }

    // we use fixed hud to ensure proper timesteps
    [HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
    public static class OnPlatformUpdate {
        static void Postfix() {
            LogicEngine.Update();
            DisplayEngine.Update();
        }
    }
}
