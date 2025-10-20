using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class ArtificialHorizonPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[AH] Artificial Horizon plugin starting !");
            Plugin.harmony.PatchAll(typeof(ArtificialHorizonComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(ArtificialHorizonComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[AH] Artificial Horizon plugin successfully started !");
        }
    }
}

public class ArtificialHorizonComponent {
    // LOGIC ENGINE, INTERNAL STATE, DISPLAY ENGINE
    static class LogicEngine {
        static public void Init() {
            Plugin.Log("[AH] Initializing Artificial Horizon");
            InternalState.destination = Bindings.UI.Game.GetFlightHUD();
            InternalState.canvasRectTransform = InternalState.destination?.GetComponent<RectTransform>();
            InternalState.mainCamera = Bindings.UI.Game.GetCameraStateManager()?.mainCamera;
            Plugin.Log("[AH] Logic Engine initialized");
        }

        static public void Update() {
            if (Bindings.Player.Aircraft.GetAircraft() == null || InternalState.canvasRectTransform == null)
                return; // do not refresh anything if the player aircraft is not available
            
            // Get the camera position
            Vector3 cameraPosition = InternalState.mainCamera.transform.position;
            
            // Get camera forward direction projected onto horizontal plane
            Vector3 cameraForwardOnPlane = Vector3.ProjectOnPlane(InternalState.mainCamera.transform.forward, Vector3.up).normalized;
            
            // Create a far point on the horizontal plane at sea level (y=0)
            // Start from camera position and go far forward, then project down to sea level
            Vector3 centerPoint = cameraPosition + cameraForwardOnPlane * 10000000f;
            centerPoint.y = 0f; // Force to sea level
            
            // Get the perpendicular right vector on the horizontal plane
            Vector3 cameraRightOnPlane = Vector3.Cross(Vector3.up, cameraForwardOnPlane).normalized;
            
            // Create horizon line endpoints in world space at sea level
            Vector3 leftPoint = centerPoint - cameraRightOnPlane * 1000000000f;
            leftPoint.y = 0f; // Ensure at sea level
            Vector3 rightPoint = centerPoint + cameraRightOnPlane * 1000000000f;
            rightPoint.y = 0f; // Ensure at sea level
            
            // Convert world space points to screen space
            Vector3 leftScreen = InternalState.mainCamera.WorldToScreenPoint(leftPoint);
            Vector3 rightScreen = InternalState.mainCamera.WorldToScreenPoint(rightPoint);
            
            // Convert screen space to local canvas coordinates
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                leftScreen,
                null,
                out Vector2 leftLocal
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform, 
                rightScreen, 
                null,
                out Vector2 rightLocal
            );
            
            InternalState.horizonStart = leftLocal;
            InternalState.horizonEnd = rightLocal;
        }
    }

    public static class InternalState {
        static public Transform destination;
        static public RectTransform canvasRectTransform;
        static public ArtificialHorizon artificialHorizon;
        static public Camera mainCamera;
        
        // Screen space coordinates for the horizon line
        static public Vector2 horizonStart;
        static public Vector2 horizonEnd;
    }

    static class DisplayEngine {
        static public void Init() {
            if (InternalState.destination != null) {
                InternalState.artificialHorizon = new ArtificialHorizon(InternalState.destination);
            }
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() ||
                Bindings.Player.Aircraft.GetAircraft() == null)
                return; // do not refresh anything if the game is paused or the player aircraft is not available
            InternalState.artificialHorizon.horizonLine.SetCoordinates(
                InternalState.horizonStart,
                InternalState.horizonEnd
            );
        }
    }

    public class ArtificialHorizon {
        public Transform horizon_transform;
        public Bindings.UI.Draw.UILine horizonLine;
        public Color mainColor = new Color(0f, 1f, 0f, 0.4f); // Green with transparency
        const float lineThickness = 1.5f;

        public ArtificialHorizon(Transform destination) {
            horizon_transform = destination;
            
            // Create the horizon line
            horizonLine = new Bindings.UI.Draw.UILine(
                "horizonLine",
                new Vector2(-1000, 0),
                new Vector2(1000, 0),
                destination,
                mainColor,
                lineThickness
            );
            
            Plugin.Log("[AH] Artificial Horizon display created");
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
