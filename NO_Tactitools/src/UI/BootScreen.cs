using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class BootScreenPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[BS] Boot Screen plugin starting !");
            Plugin.harmony.PatchAll(typeof(BootScreenTask));
            Plugin.harmony.PatchAll(typeof(BootScreenUpdatePatch));
            initialized = true;
            Plugin.Log("[BS] Boot Screen plugin succesfully started !");
        }
    }
}

[HarmonyPatch(typeof(TacScreen), "Initialize")]
class BootScreenTask {
    public static DateTime startTime;
    public static bool hasBooted = false;
    public static UIUtils.UILabel bootLabel;
    public static List<GameObject> previouslyActiveObjects = [];
    static void Postfix() {
        previouslyActiveObjects.Clear();
        foreach (Transform child in UIUtils.tacticalScreen) {
            if (child == null || child.gameObject == null) continue;
            if (child.gameObject.activeSelf) {
                previouslyActiveObjects.Add(child.gameObject);
            }
            child.gameObject.SetActive(false);
        }
        bootLabel = new UIUtils.UILabel(
            "Boot Label",
            new Vector2(0f, 0f),
            UIUtils.tacticalScreen);
        bootLabel.SetText("Booting...");
        startTime = DateTime.Now;
        hasBooted = false;
    }
}

[HarmonyPatch(typeof(TacScreen), "Update")]
class BootScreenUpdatePatch {
    static void Postfix(TacScreen __instance) {
        if (
            ((System.DateTime.Now - BootScreenTask.startTime).TotalSeconds > 3) &&
            !BootScreenTask.hasBooted)
        {
            // pour déterminer les valeurs de minJitter et maxJitter
            const float minJitter = 0.05f;
            const float maxJitter = 1f; // normalement 1f
            foreach (GameObject child in BootScreenTask.previouslyActiveObjects) {
                if (child == null) continue;
                float delay = UnityEngine.Random.Range(minJitter, maxJitter);
                __instance.StartCoroutine(ActivateWithDelay(child, delay));
            }
            BootScreenTask.hasBooted = true;
            GameObject.Destroy(BootScreenTask.bootLabel.GetGameObject());
        }
        }

    private static IEnumerator ActivateWithDelay(GameObject go, float delay)
    {
        // initial random delay before activation
        yield return new WaitForSeconds(delay);

        if (go == null) yield break;
        go.SetActive(true);
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        float baseAlpha = Mathf.Clamp01(cg.alpha <= 0f ? 1f : cg.alpha);
        float lowAlpha = 0.25f;
        int pulses = 2;
        float pulseDuration = 0.14f; // total time per pulse (down+up)

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