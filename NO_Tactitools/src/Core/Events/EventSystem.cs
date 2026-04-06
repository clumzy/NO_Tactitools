using System;
using HarmonyLib;

namespace NO_Tactitools.Core.Events;

public class ModEventArgs(string eventName, object data) : EventArgs {
    public string EventName { get; } = eventName;
    public object Data { get; set; } = data;
}

public static class EventSystem {
    public delegate void EventHandler(object sender, ModEventArgs e);

    public static class Events {
        public static class TacScreen {
            public static event EventHandler OnInitialize;
            public static event EventHandler OnUpdate;

            public static void TriggerTacScreenEvent(object sender, ModEventArgs e) {
                switch (e.EventName) {
                    case "Initialize":
                        OnInitialize?.Invoke(sender, e);
                        break;
                    case "Update":
                        OnUpdate?.Invoke(sender, e);
                        break;
                }
            }
        }

        public static class CombatHUD {
            public static event EventHandler OnFixedUpdate;

            public static void TriggerCombatHUDEvent(object sender, ModEventArgs e) {
                switch (e.EventName) {
                    case "FixedUpdate":
                        OnFixedUpdate?.Invoke(sender, e);
                        break;
                }
            }
        }

        public static class Missile {
            public static event EventHandler OnStart;
            public static event EventHandler OnSetTarget;
            public static event EventHandler OnDetonate;

            public static void TriggerMissileEvent(object sender, ModEventArgs e) {
                switch (e.EventName) {
                    case "StartMissile":
                        OnStart?.Invoke(sender, e);
                        break;
                    case "SetTarget":
                        OnSetTarget?.Invoke(sender, e);
                        break;
                    case "UserCode_RpcDetonate_897349600":
                        OnDetonate?.Invoke(sender, e);
                        break;
                }
            }
        }

        public static class SystemStatus {
            public static event EventHandler OnRefresh;

            public static void TriggerSystemStatusEvent(object sender, ModEventArgs e) {
                OnRefresh?.Invoke(sender, e);
            }
        }
    }

    public static class Patches {
        public static void PatchAll() {
            Plugin.Harmony.PatchAll(typeof(OnTacScreenInitializePatch));
            Plugin.Harmony.PatchAll(typeof(OnTacScreenUpdatePatch));
            Plugin.Harmony.PatchAll(typeof(OnCombatHUDFixedUpdatePatch));
            Plugin.Harmony.PatchAll(typeof(OnMissileStartMissilePatch));
            Plugin.Harmony.PatchAll(typeof(OnMissileSetTargetPatch));
            Plugin.Harmony.PatchAll(typeof(OnMissileDetonatePatch));
            Plugin.Harmony.PatchAll(typeof(OnSystemStatusRefreshPatch));
        }

        [HarmonyPatch(typeof(TacScreen), "Initialize")]
        private static class OnTacScreenInitializePatch {
            static void Postfix() {
                Events.TacScreen.TriggerTacScreenEvent(
                    sender: null,
                    e: new ModEventArgs(
                        "Initialize",
                        null
                    ));
            }
        }

        [HarmonyPatch(typeof(TacScreen), "Update")]
        private static class OnTacScreenUpdatePatch {
            static void Postfix() {
                Events.TacScreen.TriggerTacScreenEvent(
                    sender: null,
                    e: new ModEventArgs(
                        "Update",
                        null
                    ));
            }
        }

        [HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
        private static class OnCombatHUDFixedUpdatePatch {
            static void Postfix() {
                Events.CombatHUD.TriggerCombatHUDEvent(
                    sender: null,
                    e: new ModEventArgs(
                        "FixedUpdate",
                        null
                    ));
            }
        }

        [HarmonyPatch(typeof(Missile), "StartMissile")]
        private static class OnMissileStartMissilePatch {
            static void Postfix(Missile __instance) {
                Events.Missile.TriggerMissileEvent(
                    sender: __instance,
                    e: new ModEventArgs(
                        "StartMissile",
                        null
                    ));
            }
        }

        [HarmonyPatch(typeof(Missile), "SetTarget")]
        private static class OnMissileSetTargetPatch {
            static void Postfix(Missile __instance, Unit target) {
                Events.Missile.TriggerMissileEvent(
                    sender: __instance,
                    e: new ModEventArgs(
                        "SetTarget",
                        data: target
                    ));
            }
        }

        [HarmonyPatch(typeof(Missile), "UserCode_RpcDetonate_897349600")]
        private static class OnMissileDetonatePatch {
            static void Postfix(Missile __instance, bool hitArmor) {
                Events.Missile.TriggerMissileEvent(
                    sender: __instance,
                    e: new ModEventArgs(
                        "UserCode_RpcDetonate_897349600",
                        data: hitArmor
                    ));
            }
        }

        [HarmonyPatch(typeof(SystemStatusDisplay), "Refresh")]
        private static class OnSystemStatusRefreshPatch {
            static void Postfix() {
                Events.SystemStatus.TriggerSystemStatusEvent(
                    sender: null,
                    e: new ModEventArgs(
                        "Refresh",
                        null
                    ));
            }
        }
    }
}