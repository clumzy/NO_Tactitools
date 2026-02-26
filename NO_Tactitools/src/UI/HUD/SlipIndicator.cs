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
            InternalState.slipIndicator?.Destroy();
            InternalState.slipIndicator = null;
            InternalState.authorizedPlatforms = FileUtilities.GetListFromConfigFile("SlipIndicator_AuthorizedPlatforms.txt");
            InternalState.isAuthorized = InternalState.authorizedPlatforms.Contains(GameBindings.Player.Aircraft.GetPlatformName());
            if (!InternalState.isAuthorized) return;

            InternalState.slipBallOffset = 0f;
            InternalState.lastVelocity = Vector3.zero;
            InternalState.isFirstFrame = true;
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null) {
                InternalState.isFirstFrame = true;
                return;
            }

            if (!InternalState.isAuthorized)
                return;
            
            float dt = Time.fixedDeltaTime; 
            if (dt <= 0) return;

            Vector3 currentVelocity = GameBindings.Player.Aircraft.GetAircraft().rb.velocity;
            
            if (InternalState.isFirstFrame) {
                InternalState.lastVelocity = currentVelocity;
                InternalState.isFirstFrame = false;
                InternalState.filteredForce = -Physics.gravity;
                return;
            }

            Vector3 accel = (currentVelocity - InternalState.lastVelocity) / dt;
            InternalState.lastVelocity = currentVelocity;
            Vector3 rawForce = accel - Physics.gravity;

            // a sort of low pass filter, idk tbh
            InternalState.filteredForce = Vector3.Lerp(InternalState.filteredForce, rawForce, dt * InternalState.smoothingFactor);
            
            float lateralForce = Vector3.Dot(
                InternalState.filteredForce, 
                GameBindings.Player.Aircraft.GetAircraft().transform.right);
            float verticalForce = Vector3.Dot(
                InternalState.filteredForce, 
                GameBindings.Player.Aircraft.GetAircraft().transform.up);
            
            if (Mathf.Abs(verticalForce) > 0.2f) {
                float targetOffset = -lateralForce / Mathf.Abs(verticalForce);
                // slowly moving the ball
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, targetOffset, dt * InternalState.smoothingFactor);
            } else {
                InternalState.slipBallOffset = Mathf.Lerp(InternalState.slipBallOffset, 0f, dt * InternalState.smoothingFactor);
            }
        }
    }

    public static class InternalState {
        public static Vector3 lastVelocity = Vector3.zero;
        public static Vector3 filteredForce = Vector3.zero;
        public static bool isFirstFrame = true;
        public static float smoothingFactor = 5f; // to be adjusted
        public static float slipBallOffset = 0f;
        public static float sensitivity = 333.33f; // full deflection at 0.15G lateral acceleration
        public static float maxOffset = 50f;
        public static float padding = 10f;
        public static Vector2 basePosition = new(0, 180);
        public static bool isAuthorized = false;
        public static List<string> authorizedPlatforms = [];
        public static SlipIndicatorWidget slipIndicator = null;
    }

    static class DisplayEngine {
        static public void Init() {
            if (!InternalState.isAuthorized) return;

            if (InternalState.slipIndicator == null) {
                InternalState.slipIndicator = new SlipIndicatorWidget(UIBindings.Game.GetFlightHUDCenterTransform());
                Plugin.Log("[SI] Slip Indicator Widget initialized and added to HUD.");
            }
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized)
                return;
            float xOffset = Mathf.Clamp(InternalState.slipBallOffset * InternalState.sensitivity, -InternalState.maxOffset, InternalState.maxOffset);
            InternalState.slipIndicator.SetBallOffset(xOffset);
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

        public SlipIndicatorWidget(Transform parent) {
            containerObject = new GameObject("i_SI_Container");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = new Vector3(InternalState.basePosition.x, InternalState.basePosition.y, 0);

            leftBar = new UIBindings.Draw.UILine(
                name: "i_SI_leftBar",
                start: new Vector2(-10, -7),
                end: new Vector2(-10, 7),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 0.75f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            rightBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightBar",
                start: new Vector2(10, -7),
                end: new Vector2(10, 7),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 0.75f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            leftOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_leftOuterBar",
                start: new Vector2(-InternalState.maxOffset - InternalState.padding, -10),
                end: new Vector2(-InternalState.maxOffset - InternalState.padding, 10),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 1.5f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            rightOuterBar = new UIBindings.Draw.UILine(
                name: "i_SI_rightOuterBar",
                start: new Vector2(InternalState.maxOffset + InternalState.padding, -10),
                end: new Vector2(InternalState.maxOffset + InternalState.padding, 10),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, 0.8f),
                thickness: 1.5f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            ballLabel = new UIBindings.Draw.UILabel(
                name: "i_SI_ballLabel",
                position: Vector2.zero,
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, 0.8f),
                fontSize: 25,
                backgroundOpacity: 0f, // No background for the ball
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            ballLabel.SetText("●");
        }

        public void SetActive(bool active) => containerObject?.SetActive(active);

        public void SetBallOffset(float offset) {
            ballLabel.SetPosition(new Vector2(offset, 2));
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
