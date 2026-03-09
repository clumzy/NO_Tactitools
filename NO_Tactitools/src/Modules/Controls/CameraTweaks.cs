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
                0.0001f, // High threshold so onHold runs as long as the button is pressed
                onLongPress: HandleResetFOV
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
        if (nearest != null 
            && _airbaseOverlayCached != null) {
            Vector3 aimingPosition;
            Airbase.Runway.RunwayUsage? runwayUsage = _runwayUsageCache.GetValue(_airbaseOverlayCached);
            if (runwayUsage != null
                && runwayUsage.Value.Runway != null) {
                Airbase.Runway.RunwayUsage runwayNonNullable = _runwayUsageCache.GetValue(_airbaseOverlayCached);
                aimingPosition = runwayNonNullable.Reverse ?
                    runwayNonNullable.Runway.End.position 
                    : runwayNonNullable.Runway.Start != null ? 
                        runwayNonNullable.Runway.Start.position 
                        : runwayNonNullable.Runway.End.position;
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

    private static Coroutine _resetFOVCoroutine;
    private static void HandleResetFOV() {
        if (CameraStateManager.cameraMode != CameraMode.cockpit) return;
        if (_resetFOVCoroutine != null) return; // already resetting, do nothing
        CameraStateManager cam = UIBindings.Game.GetCameraStateManager();
        if (cam == null) return;
        _resetFOVCoroutine = cam.StartCoroutine(ResetFOVCoroutine(cam));
    }

    private static IEnumerator ResetFOVCoroutine(CameraStateManager cam) {
        CameraCockpitState cockpitState = _cockpitStateCache.GetValue(cam, true);
        if (cockpitState == null) yield break;

        float targetBase = PlayerSettings.defaultFoV;
        
        while (Mathf.Abs(_fovAdjustmentCache.GetValue(cockpitState, true)) > 0.001f || Mathf.Abs(cam.desiredFOV - targetBase) > 0.001f) {
            float currentAdj = _fovAdjustmentCache.GetValue(cockpitState, true);
            float currentBase = cam.desiredFOV;
            
            // Use Lerp for smooth deceleration as it approaches zero/target
            // resetSpeed here acts as a smoothing factor
            float smoothing = Mathf.Clamp01(Plugin.resetCockpitFOVSpeed.Value * 0.1f * Time.unscaledDeltaTime);

            float newAdj = Mathf.Lerp(currentAdj, 0f, smoothing);
            float newBase = Mathf.Lerp(currentBase, targetBase, smoothing);
            
            // Ensure we still make progress if Lerp gets too slow
            float minStep = Plugin.resetCockpitFOVSpeed.Value * 0.1f * Time.unscaledDeltaTime;
            newAdj = Mathf.MoveTowards(newAdj, 0f, minStep);
            newBase = Mathf.MoveTowards(newBase, targetBase, minStep);

            if (newAdj != currentAdj)
                _fovAdjustmentCache.SetValue(cockpitState, newAdj, true);
            if (newBase != currentBase)
                cam.SetDesiredFoV(newBase, newBase);
            
            yield return null;
        }

        _fovAdjustmentCache.SetValue(cockpitState, 0f, true);
        cam.SetDesiredFoV(targetBase, targetBase);
        _resetFOVCoroutine = null;
    }
}
