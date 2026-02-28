using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Collections.Generic;
using UnityEngine.UI;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class AmmoConIndicatorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[AC] Ammo Conservation Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(AmmoConIndicatorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(AmmoConIndicatorComponent.OnPlatformUpdate));
            Plugin.harmony.PatchAll(typeof(AmmoConIndicatorComponent.OnMissileStart));
            Plugin.harmony.PatchAll(typeof(AmmoConIndicatorComponent.OnMissileSetTarget));
            initialized = true;
            Plugin.Log("[AC] Ammo Conservation Indicator plugin successfully started !");
        }
    }
}

class AmmoConIndicatorComponent {
    static public class LogicEngine {
        public static void Init() {
            InternalState.activeMissiles.Clear();
        }

        public static void Update() {
            if (
                GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null)
                return;

            // Prune null or inactive missiles
            List<Missile> toRemove = [];
            foreach (Missile missile in InternalState.activeMissiles.Keys) {
                if (missile == null) {
                    toRemove.Add(missile);
                }
            }
            foreach (var missile in toRemove) {
                InternalState.activeMissiles.Remove(missile);
            }
        }

        public static void OnMissileStart(Missile missile) {
            if (UIBindings.Game.GetTargetScreenTransform(true) == null ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetTacScreenTransform(true) == null) {
                return;
            }
            // no target
            if (missile.targetID == null) return;
            if (missile.targetID.TryGetUnit(out Unit unit)) {
                InternalState.activeMissiles[missile] = unit;
            }
        }

        public static void OnMissileSetTarget(Missile missile, Unit unit) {
            if (UIBindings.Game.GetTargetScreenTransform(true) == null ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetTacScreenTransform(true) == null) {
                return;
            }
            // make the mod update proof
            if (unit == null) // this always happens when a missile detonates
                InternalState.activeMissiles.Remove(missile);
            else
                InternalState.activeMissiles[missile] = unit;
        }
    }

    static public class InternalState {
        static public Dictionary<Missile, Unit> activeMissiles = [];
        public static readonly TraverseCache<TargetScreenUI, List<Image>> _targetBoxesCache = new("targetBoxes");
    }

    static public class DisplayEngine {
        public static void Init() {
        }

        public static void Update() {
            if (
                GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || UIBindings.Game.GetTargetScreenTransform(silent: true) == null) {
                return;
            }

            TargetScreenUI targetScreen = UIBindings.Game.GetTargetScreenUIComponent();
            if (targetScreen == null) return;

            List<Unit> targets = GameBindings.Player.TargetList.GetTargets();
            List<Image> targetIcons = InternalState._targetBoxesCache.GetValue(targetScreen);

            if (targetIcons == null || targetIcons.Count < targets.Count) return;

            for (int i = 0; i < targets.Count; i++) {
                bool isTracked = InternalState.activeMissiles.ContainsValue(targets[i]);

                UIBindings.Draw.UIRectangle trackerDot = new(
                    "TrackerDot",
                    new Vector2(-5, -30),
                    new Vector2(5, -40),
                    fillColor: new Color(0f, 1f, 0f, 0.95f),
                    UIParent: targetIcons[i].rectTransform
                );
                trackerDot.GetGameObject().SetActive(isTracked);
                trackerDot.GetImageComponent().raycastTarget = false;
            }
        }
    }

    // HARMONY PATCHES
    [HarmonyPatch(typeof(Missile), "StartMissile")]
    public static class OnMissileStart {
        static void Postfix(Missile __instance) {
            LogicEngine.OnMissileStart(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "SetTarget")]
    public static class OnMissileSetTarget {
        static void Postfix(Missile __instance, Unit target) {
            LogicEngine.OnMissileSetTarget(__instance, target);
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