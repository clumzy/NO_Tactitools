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
            InternalState.currentX = Plugin.slipIndicatorPositionX.Value;
            InternalState.currentY = Plugin.slipIndicatorPositionY.Value;
            InternalState.smoothingFactor = 21 - Plugin.slipIndicatorSmoothing.Value;
            InternalState.sensitivity = InternalState.maxOffset / Plugin.slipIndicatorSensitivity.Value;
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized)
                return;
            float dt = Time.fixedDeltaTime; 
            if (dt <= 0) return;
            if (dt>1/30f) dt = 1/30f; // to avoid issues with pausing and resuming the game, or very low framerates. This also ensures consistent behavior across different framerates.

            InternalState.smoothingFactor = 21 - Plugin.slipIndicatorSmoothing.Value;
            InternalState.sensitivity = InternalState.maxOffset / Plugin.slipIndicatorSensitivity.Value;

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
            float forwardForce = Vector3.Dot(
                force, 
                GameBindings.Player.Aircraft.GetAircraft().transform.forward);
            float otherForces = Mathf.Abs(verticalForce) + Mathf.Abs(forwardForce);
            if (otherForces > 0.1f) { // to avoid division by zero and noise at very low vertical forces
                float targetOffset = -lateralForce / otherForces;
                // slowly moving the ball
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, targetOffset, dt * InternalState.smoothingFactor);
            } else {
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, 0f, dt * InternalState.smoothingFactor);
            }
            InternalState.needsUpdate = (InternalState.currentX != Plugin.slipIndicatorPositionX.Value || InternalState.currentY != Plugin.slipIndicatorPositionY.Value);
            if (InternalState.needsUpdate) {
                InternalState.currentX = Plugin.slipIndicatorPositionX.Value;
                InternalState.currentY = Plugin.slipIndicatorPositionY.Value;
            }
        }
    }

    public static class InternalState {
        public static Vector3 lastVelocity = Vector3.zero;
        public static float smoothingFactor; 
        public static float slipBallOffset = 0f;
        public static float sensitivity;
        public static float maxOffset = 40f;
        public static int currentX;
        public static int currentY;
        public static bool needsUpdate = false;
        public static bool isAuthorized = false;
        public static List<string> authorizedPlatforms = [];
        public static SlipIndicatorWidget SIWidget = null;
    }

    static class DisplayEngine {
        static public void Init() {
            if (!InternalState.isAuthorized) return;
            InternalState.SIWidget?.Destroy();
            InternalState.SIWidget = new SlipIndicatorWidget(UIBindings.Game.GetFlightHUDCenterTransform());
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized
                || InternalState.SIWidget == null)
                return;

            if (InternalState.needsUpdate) {
                InternalState.SIWidget.SetPosition(new Vector2(InternalState.currentX, InternalState.currentY));
            }

            float xOffset = Mathf.Clamp(InternalState.slipBallOffset * InternalState.sensitivity, -InternalState.maxOffset, InternalState.maxOffset);
            InternalState.SIWidget.UpdateDisplay(xOffset);
        }
    }

    public class SlipIndicatorWidget {
        public GameObject containerObject;
        public Transform containerTransform;
        public UIBindings.Draw.UILine leftBar;
        public UIBindings.Draw.UILine rightBar;
        public UIBindings.Draw.UILine leftOuterBar;
        public UIBindings.Draw.UILine rightOuterBar;
        public UIBindings.Draw.UILabel ballLabel;
        public float padding = 10f;

        public SlipIndicatorWidget(Transform parent) {
            containerObject = new GameObject("i_SI_Container");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = new Vector3(Plugin.slipIndicatorPositionX.Value, Plugin.slipIndicatorPositionY.Value, 0);

            float maxOffset = InternalState.maxOffset;

            leftBar = new UIBindings.Draw.UILine(
                name: "i_SI_leftBar",
                start: new Vector2(-10, -7),
                end: new Vector2(-10, 7),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, Plugin.slipIndicatorTransparency.Value),
                thickness: 1f,
                material: UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            rightBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightBar",
                start: new Vector2(10, -7),
                end: new Vector2(10, 7),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, Plugin.slipIndicatorTransparency.Value),
                thickness: 1f,
                material: UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            leftOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_leftOuterBar",
                start: new Vector2(-maxOffset - padding, -7),
                end: new Vector2(-maxOffset - padding, 7),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, Plugin.slipIndicatorTransparency.Value),
                thickness: 1.5f,
                material: UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            rightOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightOuterBar",
                start: new Vector2(maxOffset + padding, -7),
                end: new Vector2(maxOffset + padding, 7),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, Plugin.slipIndicatorTransparency.Value),
                thickness: 1.5f,
                material: UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            ballLabel = new UIBindings.Draw.UILabel(
                name: "i_SI_ballLabel",
                position: Vector2.zero,
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, Plugin.slipIndicatorTransparency.Value),
                fontSize: 25,
                backgroundOpacity: 0f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            ballLabel.SetText("●");
        }

        public void SetPosition(Vector2 position) {
            if (containerTransform != null) {
                containerTransform.localPosition = new Vector3(position.x, position.y, 0);
            }
        }

        public void UpdateDisplay(float xOffset) {
            ballLabel.SetPosition(new Vector2(xOffset, 2));
        }

        public void Destroy() {
            if (containerObject != null) {
                Object.Destroy(containerObject);
                containerObject = null;
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

    // we use fixed hud to ensure proper timesteps
    [HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
    public static class OnPlatformUpdate {
        static void Postfix() {
            LogicEngine.Update();
            DisplayEngine.Update();
        }
    }
}
