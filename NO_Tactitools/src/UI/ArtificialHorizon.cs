using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;

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
            if (!InternalState.authorizedPlatforms.Contains(Bindings.Player.Aircraft.GetPlatformName())) {
                Plugin.Log("[AH] Platform not authorized for Artificial Horizon");
                return;
            }
            InternalState.destination = Bindings.UI.Game.GetFlightHUD();
            InternalState.canvasRectTransform = InternalState.destination?.GetComponent<RectTransform>();
            InternalState.mainCamera = Bindings.UI.Game.GetCameraStateManager()?.mainCamera;
            Plugin.Log("[AH] Logic Engine initialized");
        }

        static public void Update() {
            if (Bindings.Player.Aircraft.GetAircraft() == null || 
                InternalState.canvasRectTransform == null ||
                !InternalState.authorizedPlatforms.Contains(Bindings.Player.Aircraft.GetPlatformName()))
                return; // do not refresh anything if the player aircraft is not available

            // Horizon line
            Vector3 cameraForwardOnPlane = Vector3.ProjectOnPlane(InternalState.mainCamera.transform.forward, Vector3.up).normalized;
            Vector3 horizonCenterOfLine = cameraForwardOnPlane * 1000000f;
            Vector3 cameraRightOnPlane = Vector3.Cross(Vector3.up, cameraForwardOnPlane).normalized;
            Vector3 horizonLeftPoint = horizonCenterOfLine - cameraRightOnPlane * 10000000f;
            Vector3 horizonRightPoint = horizonCenterOfLine + cameraRightOnPlane * 10000000f;
            // Convert world space points to screen space
            Vector3 horizonLeftOfScreen = InternalState.mainCamera.WorldToScreenPoint(horizonLeftPoint);
            Vector3 horizonRightOfScreen = InternalState.mainCamera.WorldToScreenPoint(horizonRightPoint);
            // Convert screen space to local canvas coordinates
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                horizonLeftOfScreen,
                null,
                out Vector2 horizonLeftCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                horizonRightOfScreen,
                null,
                out Vector2 horizonRightCombatHUD
            );
            InternalState.horizonStart = horizonLeftCombatHUD;
            InternalState.horizonEnd = horizonRightCombatHUD;

            // Cardinal directions
            const float cardinalLineDistance = 1000000f;
            const float cardinalDistanceFromSide = 50000f;
            Vector3 northCenterOfLine = Vector3.forward * cardinalLineDistance;
            Vector3 northLeftPoint = northCenterOfLine - cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 northRightPoint = northCenterOfLine + cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 southCenterOfLine = Vector3.back * cardinalLineDistance;
            Vector3 southLeftPoint = southCenterOfLine - cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 southRightPoint = southCenterOfLine + cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 eastCenterOfLine = Vector3.right * cardinalLineDistance;
            Vector3 eastLeftPoint = eastCenterOfLine - cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 eastRightPoint = eastCenterOfLine + cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 westCenterOfLine = Vector3.left * cardinalLineDistance;
            Vector3 westLeftPoint = westCenterOfLine - cameraRightOnPlane * cardinalDistanceFromSide;
            Vector3 westRightPoint = westCenterOfLine + cameraRightOnPlane * cardinalDistanceFromSide;
            // Convert world space points to screen space
            Vector3 northLeftOfScreen = InternalState.mainCamera.WorldToScreenPoint(northLeftPoint);
            Vector3 northRightOfScreen = InternalState.mainCamera.WorldToScreenPoint(northRightPoint);
            Vector3 southLeftOfScreen = InternalState.mainCamera.WorldToScreenPoint(southLeftPoint);
            Vector3 southRightOfScreen = InternalState.mainCamera.WorldToScreenPoint(southRightPoint);
            Vector3 eastLeftOfScreen = InternalState.mainCamera.WorldToScreenPoint(eastLeftPoint);
            Vector3 eastRightOfScreen = InternalState.mainCamera.WorldToScreenPoint(eastRightPoint);
            Vector3 westLeftOfScreen = InternalState.mainCamera.WorldToScreenPoint(westLeftPoint);
            Vector3 westRightOfScreen = InternalState.mainCamera.WorldToScreenPoint(westRightPoint);
            // Convert screen space to local canvas coordinates
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                northLeftOfScreen,
                null,
                out Vector2 northLeftCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                northRightOfScreen,
                null,
                out Vector2 northRightCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                southLeftOfScreen,
                null,
                out Vector2 southLeftCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                southRightOfScreen,
                null,
                out Vector2 southRightCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                eastLeftOfScreen,
                null,
                out Vector2 eastLeftCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                eastRightOfScreen,
                null,
                out Vector2 eastRightCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                westLeftOfScreen,
                null,
                out Vector2 westLeftCombatHUD
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                InternalState.canvasRectTransform,
                westRightOfScreen,
                null,
                out Vector2 westRightCombatHUD
            );
            // set label opacity based on angle to center
            const float lowerAngleThreshold = 25f;
            const float upperAngleThreshold = 30f;
            InternalState.northLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(northCenterOfLine, Bindings.Player.Aircraft.GetAircraft().transform.forward));
            InternalState.southLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(southCenterOfLine, Bindings.Player.Aircraft.GetAircraft().transform.forward));
            InternalState.eastLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(eastCenterOfLine, Bindings.Player.Aircraft.GetAircraft().transform.forward));
            InternalState.westLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(westCenterOfLine, Bindings.Player.Aircraft.GetAircraft().transform.forward));
            // add em to the line offsets
            Vector2 lineOffset = new(0, 10);
            Vector2 labelOffset = new(0, 20);
            InternalState.northStart = northLeftCombatHUD + lineOffset*InternalState.northLabelOpacity;
            InternalState.northEnd = northRightCombatHUD + lineOffset*InternalState.northLabelOpacity;
            InternalState.northLabelPos = ((northLeftCombatHUD + northRightCombatHUD) / 2) + labelOffset;
            InternalState.southStart = southLeftCombatHUD + lineOffset*InternalState.southLabelOpacity;
            InternalState.southEnd = southRightCombatHUD + lineOffset*InternalState.southLabelOpacity;
            InternalState.southLabelPos = ((southLeftCombatHUD + southRightCombatHUD) / 2) + labelOffset;
            InternalState.eastStart = eastLeftCombatHUD + lineOffset*InternalState.eastLabelOpacity;
            InternalState.eastEnd = eastRightCombatHUD + lineOffset*InternalState.eastLabelOpacity;
            InternalState.eastLabelPos = ((eastLeftCombatHUD + eastRightCombatHUD) / 2) + labelOffset;
            InternalState.westStart = westLeftCombatHUD + lineOffset*InternalState.westLabelOpacity;
            InternalState.westEnd = westRightCombatHUD + lineOffset*InternalState.westLabelOpacity;
            InternalState.westLabelPos = ((westLeftCombatHUD + westRightCombatHUD) / 2) + labelOffset;
            // set label texts to hide when behind platform
            if (northLeftOfScreen.z > 0 && northRightOfScreen.z > 0)
                InternalState.northLabelText = "0°";
            else
                InternalState.northLabelText = "";
            if (southLeftOfScreen.z > 0 && southRightOfScreen.z > 0)
                InternalState.southLabelText = "180°";
            else
                InternalState.southLabelText = "";
            if (eastLeftOfScreen.z > 0 && eastRightOfScreen.z > 0)
                InternalState.eastLabelText = "90°";
            else
                InternalState.eastLabelText = "";
            if (westLeftOfScreen.z > 0 && westRightOfScreen.z > 0)
                InternalState.westLabelText = "270°";
            else
                InternalState.westLabelText = "";
            // do the same for line visibility
            if (northLeftOfScreen.z > 0 && northRightOfScreen.z > 0) 
                InternalState.northLineOpacity = InternalState.northLabelOpacity;
            else
                InternalState.northLineOpacity = 0f;
            if (southLeftOfScreen.z > 0 && southRightOfScreen.z > 0)
                InternalState.southLineOpacity = InternalState.southLabelOpacity;
            else
                InternalState.southLineOpacity = 0f;
            if (eastLeftOfScreen.z > 0 && eastRightOfScreen.z > 0)
                InternalState.eastLineOpacity = InternalState.eastLabelOpacity;
            else
                InternalState.eastLineOpacity = 0f;
            if (westLeftOfScreen.z > 0 && westRightOfScreen.z > 0)
                InternalState.westLineOpacity = InternalState.westLabelOpacity;
            else
                InternalState.westLineOpacity = 0f;
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
        // Screen space coordinates for the cardinal direction lines and labels
        static public Vector2 northStart;
        static public Vector2 northEnd;
        static public float northLineOpacity = 1f;
        static public Vector2 northLabelPos;
        static public string northLabelText = "0°";
        static public float northLabelOpacity = 1f;
        static public Vector2 southStart;
        static public Vector2 southEnd;
        static public float southLineOpacity = 1f;
        static public Vector2 southLabelPos;
        static public string southLabelText = "180°";
        static public float southLabelOpacity = 1f;
        static public Vector2 eastStart;
        static public Vector2 eastEnd;
        static public float eastLineOpacity = 1f;
        static public Vector2 eastLabelPos;
        static public string eastLabelText = "90°";
        static public float eastLabelOpacity = 1f;
        static public Vector2 westStart;
        static public Vector2 westEnd;
        static public float westLineOpacity = 1f;
        static public Vector2 westLabelPos;
        static public string westLabelText = "270°";
        static public float westLabelOpacity = 1f;
        static public List<String> authorizedPlatforms = [
            "SAH-46 Chicane",
            "VL-49 Tarantula",
            "UH-80 Ibis"
        ];
    }

    static class DisplayEngine {
        static public void Init() {
            if (InternalState.destination == null ||
                !InternalState.authorizedPlatforms.Contains(Bindings.Player.Aircraft.GetPlatformName())) {
                return;
            }
            InternalState.artificialHorizon = new ArtificialHorizon(InternalState.destination);
            
        }

        static public void Update() {
            if (Bindings.GameState.IsGamePaused() ||
                Bindings.Player.Aircraft.GetAircraft() == null ||
                !InternalState.authorizedPlatforms.Contains(Bindings.Player.Aircraft.GetPlatformName()))
                return; // do not refresh anything if the game is paused or the player aircraft is not available
            InternalState.artificialHorizon.horizonLine.SetCoordinates(
                InternalState.horizonStart,
                InternalState.horizonEnd
            );
            InternalState.artificialHorizon.northLine.SetCoordinates(
                InternalState.northStart,
                InternalState.northEnd
            );
            InternalState.artificialHorizon.northLine.SetOpacity(InternalState.northLineOpacity);
            InternalState.artificialHorizon.northLabel.SetPosition(InternalState.northLabelPos);
            InternalState.artificialHorizon.northLabel.SetText(InternalState.northLabelText);
            InternalState.artificialHorizon.northLabel.SetOpacity(InternalState.northLabelOpacity);
            InternalState.artificialHorizon.southLine.SetCoordinates(
                InternalState.southStart,
                InternalState.southEnd
            );
            InternalState.artificialHorizon.southLine.SetOpacity(InternalState.southLineOpacity);
            InternalState.artificialHorizon.southLabel.SetPosition(InternalState.southLabelPos);
            InternalState.artificialHorizon.southLabel.SetText(InternalState.southLabelText);
            InternalState.artificialHorizon.southLabel.SetOpacity(InternalState.southLabelOpacity);
            InternalState.artificialHorizon.eastLine.SetCoordinates(
                InternalState.eastStart,
                InternalState.eastEnd
            );
            InternalState.artificialHorizon.eastLine.SetOpacity(InternalState.eastLineOpacity);
            InternalState.artificialHorizon.eastLabel.SetPosition(InternalState.eastLabelPos);
            InternalState.artificialHorizon.eastLabel.SetText(InternalState.eastLabelText);
            InternalState.artificialHorizon.eastLabel.SetOpacity(InternalState.eastLabelOpacity);
            InternalState.artificialHorizon.westLine.SetCoordinates(
                InternalState.westStart,
                InternalState.westEnd
            );
            InternalState.artificialHorizon.westLine.SetOpacity(InternalState.westLineOpacity);
            InternalState.artificialHorizon.westLabel.SetPosition(InternalState.westLabelPos);
            InternalState.artificialHorizon.westLabel.SetText(InternalState.westLabelText);
            InternalState.artificialHorizon.westLabel.SetOpacity(InternalState.westLabelOpacity);
        }
    }

    public class ArtificialHorizon {
        public Bindings.UI.Draw.UILine horizonLine;
        const float horizonLineThickness = 1f;
        readonly Color mainColor = new(0.2f, 1f, 0.2f, 0.4f); // Green with transparency
        public Bindings.UI.Draw.UILine northLine;
        public Bindings.UI.Draw.UILabel northLabel;
        public Bindings.UI.Draw.UILine southLine;
        public Bindings.UI.Draw.UILabel southLabel;
        public Bindings.UI.Draw.UILine eastLine;
        public Bindings.UI.Draw.UILabel eastLabel;
        public Bindings.UI.Draw.UILine westLine;
        public Bindings.UI.Draw.UILabel westLabel;
        const float cardinalLineThickness = 1f;
        public Color cardinalLineColor = new(0.2f, 1f, 0.2f, 0.8f); // Green with less transparency
        public Color cardinalLabelColor = new(0.2f, 1f, 0.2f, 1f); // Green with even less transparency
        public int cardinalLabelFontSize = 16;
        public float cardinalLabelBgOpacity = 0.1f;

        public ArtificialHorizon(Transform destination) {

            // Create the horizon line
            horizonLine = new Bindings.UI.Draw.UILine(
                "horizonLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                mainColor,
                horizonLineThickness
            );
            northLine = new Bindings.UI.Draw.UILine(
                "northLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness
            );
            northLabel = new Bindings.UI.Draw.UILabel(
                "northLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity
            );
            southLine = new Bindings.UI.Draw.UILine(
                "southLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness
            );
            southLabel = new Bindings.UI.Draw.UILabel(
                "southLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity
            );
            eastLine = new Bindings.UI.Draw.UILine(
                "eastLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness
            );
            eastLabel = new Bindings.UI.Draw.UILabel(
                "eastLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity
            );
            westLine = new Bindings.UI.Draw.UILine(
                "westLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness
            );
            westLabel = new Bindings.UI.Draw.UILabel(
                "westLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity
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
