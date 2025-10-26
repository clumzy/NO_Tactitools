using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class BootScreenPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[BS] Boot Screen plugin starting !");
            // Patch using the WeaponDisplay-style component structure
            Plugin.harmony.PatchAll(typeof(BootScreenComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(BootScreenComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[BS] Boot Screen plugin succesfully started !");
        }
    }
}

public static class BootScreenComponent {
    // Mirrors WeaponDisplay structure: LogicEngine, InternalState, DisplayEngine
    static class LogicEngine {
        public static void Init() {
            InternalState.tacScreenTransform = Bindings.UI.Game.GetTacScreen();
            InternalState.previouslyActiveObjects.Clear();
            InternalState.hasBooted = false;
            foreach (Transform child in InternalState.tacScreenTransform) {
                if (child == null || child.gameObject == null || child.name.StartsWith("i_")) continue;
                if (child.gameObject.activeSelf) InternalState.previouslyActiveObjects.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
            int horizontalOffset = 0;
            int verticalOffset = 0;
            string platformName = Bindings.Player.Aircraft.GetPlatformName();
            switch (platformName) {
                case "CI-22 Cricket":
                    horizontalOffset = -105;
                    verticalOffset = 0;
                    break;
                case "SAH-46 Chicane":
                    horizontalOffset = -130;
                    verticalOffset = 65;
                    break;
                case "T/A-30 Compass":
                    horizontalOffset = 0;
                    verticalOffset = 80;
                    break;
                case "FS-12 Revoker":
                    horizontalOffset = 0;
                    verticalOffset = 75;
                    break;
                case "FS-20 Vortex":
                    horizontalOffset = 0;
                    verticalOffset = 75;
                    break;
                case "KR-67 Ifrit":
                    horizontalOffset = -130;
                    verticalOffset = 65;
                    break;
                case "VL-49 Tarantula":
                    horizontalOffset = -255;
                    verticalOffset = 60;
                    break;
                case "EW-1 Medusa":
                    horizontalOffset = -225;
                    verticalOffset = 65;
                    break;
                case "SFB-81":
                    horizontalOffset = -180;
                    verticalOffset = 60;
                    break;
                case "UH-80 Ibis":
                    horizontalOffset = -245;
                    verticalOffset = 65;
                    break;
                default:
                    break;
            }
            InternalState.bootLabel = new Bindings.UI.Draw.UILabel(
                "Boot Label",
                new Vector2(horizontalOffset, verticalOffset),
                InternalState.tacScreenTransform
            );
            InternalState.startTime = DateTime.Now;
            InternalState.hasBooted = false;
        }

        public static void Update() {
            if (InternalState.hasBooted) return;
            if ((DateTime.Now - InternalState.startTime).TotalSeconds <= 3) return;

            const float minJitter = 0.05f;
            const float maxJitter = 1f; // normally 1f
            foreach (GameObject child in InternalState.previouslyActiveObjects) {
                if (child == null) continue;
                float delay = UnityEngine.Random.Range(minJitter, maxJitter);
                Bindings.UI.Game.GetTacScreenComponent()?.StartCoroutine(DisplayEngine.ActivateWithDelay(child, delay));
            }

            if (InternalState.bootLabel != null) {
                GameObject.Destroy(InternalState.bootLabel.GetGameObject());
                InternalState.bootLabel = null;
            InternalState.hasBooted = true;
            }
        }
    }

    public static class InternalState {
        public static DateTime startTime;
        public static bool hasBooted=true; // so that other plugins can check if boot is done, and initialized to true to avoid null refs
        public static Bindings.UI.Draw.UILabel bootLabel;
        public static List<GameObject> previouslyActiveObjects = [];
        public static Transform tacScreenTransform;
    }
    static class DisplayEngine {
        public static void Init() {
            InternalState.bootLabel.SetText("Booting "+Bindings.Player.Aircraft.GetPlatformName()+"...");
        }
        public static void Update() {
            // Nothing to update visually for now
        }
        public static IEnumerator ActivateWithDelay(GameObject go, float delay) {
            // initial random delay before activation
            yield return new WaitForSeconds(delay);

            if (go == null) yield break;
            go.SetActive(true);
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            float baseAlpha = Mathf.Clamp01(cg.alpha <= 0f ? 1f : cg.alpha);
            float lowAlpha = 0.25f;
            int pulses = 2;
            float pulseDuration = 0.15f; // total time per pulse (down+up)

            for (int i = 0; i < pulses; i++) {
                // fade down
                float half = pulseDuration * 0.5f;
                float t = 0f;
                while (t < half) {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / half);
                    cg.alpha = Mathf.Lerp(baseAlpha, lowAlpha, p);
                    yield return null;
                }
                // fade up
                t = 0f;
                while (t < half) {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / half);
                    cg.alpha = Mathf.Lerp(lowAlpha, baseAlpha, p);
                    yield return null;
                }
            }
            cg.alpha = baseAlpha;
        }
    }

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