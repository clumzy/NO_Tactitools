using System.Collections;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NO_Tactitools.Core;
using UnityEngine;

namespace NO_Tactitools.Controls;

[HarmonyPatch(typeof(MainMenu), "Start")]
class CameraTweaksPlugin {
    private static bool initialized = false;
    private static TraverseCache<CameraStateManager, CameraCockpitState> _cockpitStateCache = new("cockpitState");
    private static TraverseCache<CameraCockpitState, float> _fovAdjustmentCache = new("FOVAdjustment");

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[CT] Camera Tweaks plugin starting !");

            InputCatcher.RegisterNewInput(
                Plugin.resetCockpitFOV,
                999f, // High threshold so onHold runs as long as the button is pressed
                onHold: HandleResetFOV
            );

            InputCatcher.RegisterNewInput(
                Plugin.lookAtNearestAirbase,
                999f,
                onHold: HandleLookAtNearest,
                onRelease: HandleResetToCenter
            );

            initialized = true;
            Plugin.Log("[CT] Camera Tweaks plugin successfully started !");
        }
    }

    private static TraverseCache<CameraCockpitState, bool> _padlockCache = new("padlock");
    private static TraverseCache<CameraCockpitState, float> _panViewCache = new("panView");
    private static TraverseCache<CameraCockpitState, float> _tiltViewCache = new("tiltView");
    private static AirbaseOverlay _airbaseOverlayCached = null;
    private static TraverseCache<AirbaseOverlay, bool> _landingCache = new("landing");
    private static TraverseCache<AirbaseOverlay, Airbase.Runway.RunwayUsage> _runwayUsageCache = new("runwayUsage");
    private static void HandleLookAtNearest() {
        if (
            CameraStateManager.cameraMode != CameraMode.cockpit
            || _padlockCache.GetValue(_cockpitStateCache.GetValue(UIBindings.Game.GetCameraStateManager()))) {
            return;
        }
        if (_airbaseOverlayCached == null) {
            _airbaseOverlayCached = GameObject.FindFirstObjectByType<AirbaseOverlay>();
            if (_airbaseOverlayCached != null) {
                Plugin.Log("[ILS] Cached AirbaseOverlay instance successfully.");
            }
            else {
                Plugin.Log("[ILS] Failed to cache AirbaseOverlay instance.");
            }
        }
        Aircraft playerAircraft = GameBindings.Player.Aircraft.GetAircraft();
        Airbase nearest = GameBindings.GameState.GetCurrentFactionHQ().GetNearestAirbase(playerAircraft.transform.position);
        if (nearest != null) {
            Vector3 aimingPosition;
            if (_airbaseOverlayCached != null
                && _landingCache.GetValue(_airbaseOverlayCached)) {
                Airbase.Runway.RunwayUsage runwayUsage = _runwayUsageCache.GetValue(_airbaseOverlayCached);
                aimingPosition = runwayUsage.Reverse ?
                    runwayUsage.Runway.End.position : runwayUsage.Runway.Start != null ? runwayUsage.Runway.Start.position : runwayUsage.Runway.End.position;
            }
            else {
                aimingPosition = nearest.center.position;

            }
            Vector3 normalized = (aimingPosition - playerAircraft.transform.position).normalized;
            Vector3 vector = Vector3.Dot(normalized, playerAircraft.transform.forward) * playerAircraft.transform.forward
                             + Vector3.Dot(normalized, playerAircraft.transform.right) * playerAircraft.transform.right;
            Vector3 vector2 = Vector3.Cross(vector.normalized, playerAircraft.transform.up);
            _panViewCache.SetValue(UIBindings.Game.GetCameraStateManager().cockpitState,
                                    Vector3.SignedAngle(vector.normalized, playerAircraft.transform.forward, -playerAircraft.transform.up));
            _tiltViewCache.SetValue(UIBindings.Game.GetCameraStateManager().cockpitState,
                                    Vector3.SignedAngle(normalized, playerAircraft.transform.up, vector2.normalized) - 90f);
        }
        _panViewCache.SetValue(UIBindings.Game.GetCameraStateManager().cockpitState,
                                Mathf.Clamp(_panViewCache.GetValue(UIBindings.Game.GetCameraStateManager().cockpitState), -165f, 165f));
        _tiltViewCache.SetValue(UIBindings.Game.GetCameraStateManager().cockpitState,
                                Mathf.Clamp(_tiltViewCache.GetValue(UIBindings.Game.GetCameraStateManager().cockpitState), -65f, 45f));
    }

    private static void HandleResetToCenter() {
        if (CameraStateManager.cameraMode != CameraMode.cockpit) return;
        _panViewCache.SetValue(UIBindings.Game.GetCameraStateManager().cockpitState, 0f);
        _tiltViewCache.SetValue(UIBindings.Game.GetCameraStateManager().cockpitState, 0f);
    }
    private static void HandleResetFOV() {
        if (CameraStateManager.cameraMode != CameraMode.cockpit) return;

        CameraStateManager cam = UIBindings.Game.GetCameraStateManager();
        if (cam == null) return;

        CameraCockpitState cockpitState = _cockpitStateCache.GetValue(cam, true);
        if (cockpitState == null) return;

        float currentAdj = _fovAdjustmentCache.GetValue(cockpitState, true);
        float currentBase = cam.desiredFOV;
        float targetBase = PlayerSettings.defaultFoV;

        // unzoom speed
        float resetSpeed = Plugin.resetCockpitFOVSpeed.Value * Time.unscaledDeltaTime;

        float newAdj = Mathf.MoveTowards(currentAdj, 0f, resetSpeed);
        float newBase = Mathf.MoveTowards(currentBase, targetBase, resetSpeed);

        // adjust game FOV
        if (newAdj != currentAdj)
            _fovAdjustmentCache.SetValue(cockpitState, newAdj, true);
        if (newBase != currentBase)
            cam.SetDesiredFoV(newBase, newBase);
    }
}
