using HarmonyLib;
using UnityEngine.UI;
using NO_Tactitools.Core;
using UnityEngine;
using System.Collections.Generic;

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
    static class LogicEngine {
        static public void Init() {
            InternalState.authorizedPlatforms = FileUtilities.GetListFromConfigFile("SlipIndicator_AuthorizedPlatforms.txt");
            InternalState.isAuthorized = InternalState.authorizedPlatforms.Contains(GameBindings.Player.Aircraft.GetPlatformName());
            if (!InternalState.isAuthorized) return;

            InternalState.slipBallOffset = 0f;
            InternalState.lastVelocity = Vector3.zero;
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized)
                return;
            float dt = Time.fixedDeltaTime; 
            if (dt <= 0) return;
            if (dt>1/30f) dt = 1/30f; // to avoid issues with pausing and resuming the game, or very low framerates. This also ensures consistent behavior across different framerates.

            Vector3 currentVelocity = GameBindings.Player.Aircraft.GetAircraft().rb.velocity;
            Vector3 accel = (currentVelocity - InternalState.lastVelocity) / dt;
            InternalState.lastVelocity = currentVelocity;
            Vector3 force = accel - Physics.gravity;
            float lateralForce = Vector3.Dot(
                force, 
                GameBindings.Player.Aircraft.GetAircraft().transform.right);
            float verticalForce = Vector3.Dot(
                force, 
                GameBindings.Player.Aircraft.GetAircraft().transform.up);
            
            if (Mathf.Abs(verticalForce) > 0.5f) { // to avoid division by zero and noise at very low vertical forces
                float targetOffset = -lateralForce / Mathf.Abs(verticalForce);
                // slowly moving the ball
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, targetOffset, dt * InternalState.smoothingFactor);
            } else {
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, 0f, dt * InternalState.smoothingFactor);
            }
        }
    }

    public static class InternalState {
        public static UIBindings.Draw.UILine leftBar;
        public static UIBindings.Draw.UILine rightBar;
        public static UIBindings.Draw.UILine leftOuterBar;
        public static UIBindings.Draw.UILine rightOuterBar;
        public static UIBindings.Draw.UILabel ballLabel;
        public static Vector3 lastVelocity = Vector3.zero;
        public static float smoothingFactor = 5f; // to be adjusted
        public static float slipBallOffset = 0f;
        public static float sensitivity = 100f; // full deflection at 0.5G lateral acceleration ratio
        public static float maxOffset = 50f;
        public static float padding = 10f;
        public static Vector2 basePosition = new(0, 180);
        public static bool isAuthorized = false;
        public static List<string> authorizedPlatforms = [];
    }

    static class DisplayEngine {
        static public void Init() {
            if (!InternalState.isAuthorized) return;

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
                thickness: 2f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            InternalState.rightOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightOuterBar",
                start: InternalState.basePosition + new Vector2(InternalState.maxOffset + InternalState.padding, -10),
                end: InternalState.basePosition + new Vector2(InternalState.maxOffset + InternalState.padding, 10),
                UIParent: UIBindings.Game.GetFlightHUDCenterTransform(),
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 2f,
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
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized)
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
