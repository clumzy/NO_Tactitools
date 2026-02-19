using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class ILSScreenPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[ILS] ILS Screen plugin starting !");
            Plugin.harmony.PatchAll(typeof(ILSScreenComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(ILSScreenComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[ILS] ILS Screen plugin succesfully started !");
        }
    }
}

class ILSScreenComponent {
    static public class LogicEngine {
        public static void Init() {
            InternalState.FLOLSWidget?.Destroy();
            InternalState.FLOLSWidget = null;
            InternalState.isGlideslope = false;
            InternalState.isAuthorized = false;
            InternalState.currentGlideslopeError = 0f;
            if (InternalState.airbaseOverlayCached == null) {
                InternalState.airbaseOverlayCached = GameObject.FindFirstObjectByType<AirbaseOverlay>();
                if (InternalState.airbaseOverlayCached != null) {
                    Plugin.Log("[ILS] Cached AirbaseOverlay instance successfully.");
                }
                else {
                    Plugin.Log("[ILS] Failed to cache AirbaseOverlay instance.");
                }
            }
        }

        public static void Update() {
            if (
                GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || InternalState.airbaseOverlayCached == null)
                return;
            InternalState.isGlideslope = InternalState.glideslopeCache.GetValue(InternalState.airbaseOverlayCached).enabled;
            InternalState.isAuthorized = InternalState.runwayBordersCache.GetValue(InternalState.airbaseOverlayCached)[0].enabled;
            InternalState.runwayUsage = InternalState.runwayUsageCache.GetValue(InternalState.airbaseOverlayCached);
            if (InternalState.isGlideslope) {
                InternalState.currentGlideslopeError = LogicEngine.GetGlideslopeAngleError(
                    GameBindings.Player.Aircraft.GetAircraft(),
                    InternalState.runwayUsage.Runway,
                    InternalState.runwayUsage.Reverse
                );
            }
        }

        public static float GetGlideslopeAngleError(
            Aircraft aircraft,
            Airbase.Runway runway,
            bool reverse
            ) {
            Vector3 touchdownPos = reverse ? runway.End.position : runway.Start.position;
            Vector3 offset = aircraft.transform.position - touchdownPos;
            Vector3 horizontalOffset = new(offset.x, 0, offset.z);
            float horizontalDistance = horizontalOffset.magnitude;
            if (horizontalDistance < 1f) return 0f;
            float actualHeight = aircraft.transform.position.y - (touchdownPos.y + aircraft.definition.spawnOffset.y);
            float actualAngle = Mathf.Atan2(actualHeight, horizontalDistance) * Mathf.Rad2Deg;
            float targetAngle = Mathf.Atan(0.06f) * Mathf.Rad2Deg;
            return actualAngle - targetAngle;
        }
    }
    static public class InternalState {
        public static bool isGlideslope = false;
        public static bool isAuthorized = false;
        public static Airbase.Runway.RunwayUsage runwayUsage;
        public static float currentGlideslopeError = 0f;
        public static AirbaseOverlay airbaseOverlayCached = null;
        public static TraverseCache<AirbaseOverlay, Image> glideslopeCache = new("glideslope");
        public static TraverseCache<AirbaseOverlay, Image[]> runwayBordersCache = new("runwayBorders");
        public static TraverseCache<AirbaseOverlay, Airbase.Runway.RunwayUsage> runwayUsageCache = new("runwayUsage");
        public static FLOLS FLOLSWidget = null;
    }

    static public class DisplayEngine {
        public static void Init() {
            if (InternalState.FLOLSWidget == null) {
                InternalState.FLOLSWidget = new FLOLS(UIBindings.Game.GetFlightHUDTransform());
                Plugin.Log("[ILS] FLOLS Widget initialized and added to MFD.");
            }
            InternalState.FLOLSWidget.SetActive(false);
        }

        public static void Update() {
            if (
                GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || InternalState.airbaseOverlayCached == null) {
                return;
            }
            if (InternalState.isGlideslope){
                InternalState.FLOLSWidget.SetActive(true);
                InternalState.FLOLSWidget.SetBallPosition(-InternalState.currentGlideslopeError);
                if (InternalState.isAuthorized) {
                    InternalState.FLOLSWidget.SetSideColor(Color.green);
                }
                else {
                    InternalState.FLOLSWidget.SetSideColor(Color.red);
                }
            }
            else {
                InternalState.FLOLSWidget.SetActive(false);
            }
        }
    }

    // ILS WIDGET PATCH
    public class FLOLS {
        public GameObject containerObject;
        public Transform containerTransform;
        public UIBindings.Draw.UIRectangle background;
        public UIBindings.Draw.UIRectangle ball;
        public UIBindings.Draw.UIRectangle[] sideBars = new UIBindings.Draw.UIRectangle[4];

        public FLOLS(Transform parent) {
            containerObject = new GameObject("i_ils_FLOLSContainer");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = new Vector3(0, -180, 0);

            background = new UIBindings.Draw.UIRectangle(
                "FLOLS_Background",
                new Vector2(-15, -60),
                new Vector2(15, 60),
                containerTransform,
                new Color(0.1f, 0.2f, 0.4f, 0.7f)
            );

            ball = new UIBindings.Draw.UIRectangle(
                "FLOLS_Ball",
                new Vector2(-12, -12),
                new Vector2(12, 12),
                containerTransform,
                Color.yellow
            );

            sideBars[0] = new UIBindings.Draw.UIRectangle("SideBar_L1", new Vector2(-70, -8), new Vector2(-45, 8), containerTransform, Color.green);
            sideBars[1] = new UIBindings.Draw.UIRectangle("SideBar_L2", new Vector2(-40, -8), new Vector2(-20, 8), containerTransform, Color.green);
            sideBars[2] = new UIBindings.Draw.UIRectangle("SideBar_R1", new Vector2(20, -8), new Vector2(40, 8), containerTransform, Color.green);
            sideBars[3] = new UIBindings.Draw.UIRectangle("SideBar_R2", new Vector2(45, -8), new Vector2(70, 8), containerTransform, Color.green);
        }

        public void SetActive(bool active) => containerObject?.SetActive(active);

        public void SetBallPosition(float error) {
            float yPos = Mathf.Clamp(error * -50f, -50f, 50f);
            ball.SetCenter(new Vector2(0, yPos));
        }

        public void SetSideColor(Color color) {
            foreach (var bar in sideBars) bar.SetFillColor(color);
        }

        public void Destroy() {
            if (containerObject != null) {
                Object.Destroy(containerObject);
                containerObject = null;
            }
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