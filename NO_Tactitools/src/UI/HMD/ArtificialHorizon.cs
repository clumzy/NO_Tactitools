using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Collections.Generic;
using System;
using UnityEngine.Rendering;
using JetBrains.Annotations;

namespace NO_Tactitools.UI.HMD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class ArtificialHorizonPlugin {
    private static bool initialized = false;
    private const string ThirdPersonModGUID = "com.gnol.thirdpersonhud";
    public static bool IsThirdPersonModLoaded = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[AH] Artificial Horizon plugin starting !");
            if (Chainloader.PluginInfos.ContainsKey(ThirdPersonModGUID)) IsThirdPersonModLoaded = true;
            Plugin.Log($"[AH] Artificial Horizon plugin dependency check complete. IsThirdPersonModLoaded: {IsThirdPersonModLoaded.ToString()}");
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
            InternalState.authorizedPlatforms = FileUtilities.GetListFromConfigFile("ArtificialHorizon_AuthorizedPlatforms.txt");
            InternalState.isAuthorized = InternalState.authorizedPlatforms.Contains(GameBindings.Player.Aircraft.GetPlatformName());
            if (!InternalState.isAuthorized) {
                Plugin.Log("[AH] Platform not authorized for Artificial Horizon");
                if (InternalState.artificialHorizon != null) {
                    InternalState.artificialHorizon.Reset();
                    Plugin.Log("[AH] Artificial Horizon display removed");
                }
                return;
            }
            InternalState.canvasRectTransform = UIBindings.Game.GetCombatHUDTransform()?.GetComponent<RectTransform>();
            InternalState.mainCamera = UIBindings.Game.GetCameraStateManager()?.mainCamera;
            Plugin.Log("[AH] Logic Engine initialized");
        }

        static public void Update() {
            if (GameBindings.Player.Aircraft.GetAircraft() == null ||
                InternalState.canvasRectTransform == null ||
                !InternalState.isAuthorized)
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
            InternalState.northLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(northCenterOfLine, GameBindings.Player.Aircraft.GetAircraft().transform.forward));
            InternalState.southLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(southCenterOfLine, GameBindings.Player.Aircraft.GetAircraft().transform.forward));
            InternalState.eastLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(eastCenterOfLine, GameBindings.Player.Aircraft.GetAircraft().transform.forward));
            InternalState.westLabelOpacity = Mathf.InverseLerp(lowerAngleThreshold, upperAngleThreshold, Vector3.Angle(westCenterOfLine, GameBindings.Player.Aircraft.GetAircraft().transform.forward));
            // add em to the line offsets
            Vector2 lineOffset = new(0, 5);
            Vector2 labelOffset = new(0, 15);
            InternalState.northStart = northLeftCombatHUD + lineOffset * InternalState.northLabelOpacity;
            InternalState.northEnd = northRightCombatHUD + lineOffset * InternalState.northLabelOpacity;
            InternalState.northLabelPos = ((northLeftCombatHUD + northRightCombatHUD) / 2) + labelOffset;
            InternalState.southStart = southLeftCombatHUD + lineOffset * InternalState.southLabelOpacity;
            InternalState.southEnd = southRightCombatHUD + lineOffset * InternalState.southLabelOpacity;
            InternalState.southLabelPos = ((southLeftCombatHUD + southRightCombatHUD) / 2) + labelOffset;
            InternalState.eastStart = eastLeftCombatHUD + lineOffset * InternalState.eastLabelOpacity;
            InternalState.eastEnd = eastRightCombatHUD + lineOffset * InternalState.eastLabelOpacity;
            InternalState.eastLabelPos = ((eastLeftCombatHUD + eastRightCombatHUD) / 2) + labelOffset;
            InternalState.westStart = westLeftCombatHUD + lineOffset * InternalState.westLabelOpacity;
            InternalState.westEnd = westRightCombatHUD + lineOffset * InternalState.westLabelOpacity;
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
        static public bool isAuthorized = false;
        static public List<String> authorizedPlatforms = [];
    }

    static class DisplayEngine {
        static public void Init() {
            if (UIBindings.Game.GetCombatHUDTransform() == null ||
                !InternalState.isAuthorized) {
                return;
            }
            InternalState.artificialHorizon = new ArtificialHorizon(UIBindings.Game.GetCombatHUDTransform());

        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused() ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                !InternalState.isAuthorized)
                return; // do not refresh anything if the game is paused or the player aircraft is not available
            // Third person view mod patch
            if (CameraStateManager.cameraMode != CameraMode.cockpit
                && ArtificialHorizonPlugin.IsThirdPersonModLoaded) {
                InternalState.artificialHorizon?.SetActive(false);
                return;
            }
            InternalState.artificialHorizon.SetActive(true);
            // patch end
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
        public UIBindings.Draw.UILine horizonLine;
        const float horizonLineThickness = 1f;
        readonly Color mainColor = new(0f, 1f, 0f, 0.8f); // Green with transparency
        public UIBindings.Draw.UILine northLine;
        public UIBindings.Draw.UILabel northLabel;
        public UIBindings.Draw.UILine southLine;
        public UIBindings.Draw.UILabel southLabel;
        public UIBindings.Draw.UILine eastLine;
        public UIBindings.Draw.UILabel eastLabel;
        public UIBindings.Draw.UILine westLine;
        public UIBindings.Draw.UILabel westLabel;
        const float cardinalLineThickness = 1f;
        public Color cardinalLineColor = new(0f, 1f, 0f, 0.6f); // Green with less transparency
        public Color cardinalLabelColor = new(0f, 1f, 0f, 0.8f); // Green with even less transparency
        public int cardinalLabelFontSize = 16;
        public float cardinalLabelBgOpacity = 0.1f;

        public ArtificialHorizon(Transform destination) {
            mainColor.a = Plugin.artificialHorizonTransparency.Value;
            cardinalLineColor.a = Mathf.Clamp(Plugin.artificialHorizonTransparency.Value + 0.2f, 0f, 1f);
            cardinalLabelColor.a = Mathf.Clamp(Plugin.artificialHorizonTransparency.Value + 0.4f, 0f, 1f);
            // Create the horizon line
            horizonLine = new UIBindings.Draw.UILine(
                "horizonLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                mainColor,
                horizonLineThickness,
                UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            northLine = new UIBindings.Draw.UILine(
                "northLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness,
                UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            northLabel = new UIBindings.Draw.UILabel(
                "northLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity,
                UIBindings.Game.GetFlightHUDFontMaterial()
            );
            southLine = new UIBindings.Draw.UILine(
                "southLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness,
                UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            southLabel = new UIBindings.Draw.UILabel(
                "southLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity,
                UIBindings.Game.GetFlightHUDFontMaterial()
            );
            eastLine = new UIBindings.Draw.UILine(
                "eastLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness,
                UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            eastLabel = new UIBindings.Draw.UILabel(
                "eastLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity,
                UIBindings.Game.GetFlightHUDFontMaterial()
            );
            westLine = new UIBindings.Draw.UILine(
                "westLine",
                new Vector2(0, 0),
                new Vector2(0, 0),
                destination,
                cardinalLineColor,
                cardinalLineThickness,
                UIBindings.Game.GetFlightHUDFontMaterial(),
                antialiased: true
            );
            westLabel = new UIBindings.Draw.UILabel(
                "westLabel",
                new Vector2(0, 0),
                destination,
                FontStyle.Normal,
                cardinalLabelColor,
                cardinalLabelFontSize,
                cardinalLabelBgOpacity,
                UIBindings.Game.GetFlightHUDFontMaterial()
            );
            Plugin.Log("[AH] Artificial Horizon display created");
        }

        public void Reset() {
            horizonLine.Destroy();
            northLine.Destroy();
            northLabel.Destroy();
            southLine.Destroy();
            southLabel.Destroy();
            eastLine.Destroy();
            eastLabel.Destroy();
            westLine.Destroy();
            westLabel.Destroy();
        }

        public void SetActive(bool active) {
            horizonLine.GetGameObject().SetActive(active);
            northLine.GetGameObject().SetActive(active);
            northLabel.GetGameObject().SetActive(active);
            southLine.GetGameObject().SetActive(active);
            southLabel.GetGameObject().SetActive(active);
            eastLine.GetGameObject().SetActive(active);
            eastLabel.GetGameObject().SetActive(active);
            westLine.GetGameObject().SetActive(active);
            westLabel.GetGameObject().SetActive(active);
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

