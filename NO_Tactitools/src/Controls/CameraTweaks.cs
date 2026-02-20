using System;
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
                Plugin.lookAtNearestTarget,
                0.2f,
                onRelease: HandleLookAtNearest
            );
            
            initialized = true;
            Plugin.Log("[CT] Camera Tweaks plugin successfully started !");
        }
    }

    private static void HandleLookAtNearest() {
        if (CameraStateManager.cameraMode != CameraMode.cockpit) return;
    }

    private static void HandleResetFOV() {
        if (CameraStateManager.cameraMode != CameraMode.cockpit) return;

        CameraStateManager cam = SceneSingleton<CameraStateManager>.i;
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
