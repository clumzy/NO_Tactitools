using System;
using HarmonyLib;

namespace NO_Tactitools.Core.Events;

public class ModEventArgs(string eventName, object data) : EventArgs {
    public string EventName { get; } = eventName;
    public object Data { get; set; } = data;
}

public static class EventSystem {
    // pointer to a function that takes an object and a ModEventArgs and returns void
    public delegate void EventHandler(object sender, ModEventArgs e);
    // Tac Screen events
    public static event EventHandler OnTacScreenInit;
    public static event EventHandler OnTacScreenUpdate;
    // Combat HUD events
    public static event EventHandler OnCombatHUDFixedUpdate;
    // Missile events
    public static event EventHandler OnMissileStartMissile;
    public static event EventHandler OnMissileSetTarget;
    // triggers
    public static void TriggerInitEvent(object sender, ModEventArgs e) {
        if (e.EventName == "TacScreen_Initialize")
            OnTacScreenInit?.Invoke(sender, e);
    }
    public static void TriggerUpdateEvent(object sender, ModEventArgs e) {
        switch (e.EventName)
        {
            case "TacScreen_Update":
                OnTacScreenUpdate?.Invoke(sender, e);
                break;
            case "CombatHUD_FixedUpdate":
                OnCombatHUDFixedUpdate?.Invoke(sender, e);
                break;
        }
    }

    public static void TriggerMissileEvent(object sender, ModEventArgs e) {
        switch (e.EventName) {
            case "Missile_Start":
                OnMissileStartMissile?.Invoke(sender, e);
                break;
            case "Missile_SetTarget":
                OnMissileSetTarget?.Invoke(sender, e);
                break;
        }
    }

    // PATCH ALL
    public static void PatchAll() {
        Plugin.Harmony.PatchAll(typeof(OnTacScreenInitializePatch));
        Plugin.Harmony.PatchAll(typeof(OnTacScreenUpdatePatch));
        Plugin.Harmony.PatchAll(typeof(OnCombatHUDFixedUpdatePatch));
        Plugin.Harmony.PatchAll(typeof(OnMissileStartMissilePatch));
        Plugin.Harmony.PatchAll(typeof(OnMissileSetTargetPatch));
    }

    // patches that trigger
    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnTacScreenInitializePatch {
        static void Postfix() {
            TriggerInitEvent(
                sender: null,
                e: new ModEventArgs(
                    "TacScreen_Initialize",
                    null
                ));
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Update")]
    public static class OnTacScreenUpdatePatch {
        static void Postfix() {
            TriggerUpdateEvent(
                sender: null,
                e: new ModEventArgs(
                    "TacScreen_Update",
                    null
                ));
        }
    }

    [HarmonyPatch(typeof(CombatHUD), "FixedUpdate")]
    public static class OnCombatHUDFixedUpdatePatch {
        static void Postfix() {
            TriggerUpdateEvent(
                sender: null,
                e: new ModEventArgs(
                    "CombatHUD_FixedUpdate",
                    null
                ));
        }
    }
    
    [HarmonyPatch(typeof(Missile), "StartMissile")]
    public static class OnMissileStartMissilePatch {
        static void Postfix() {
            TriggerMissileEvent(
                sender: null,
                e: new ModEventArgs(
                    "Missile_StartMissile",
                    null
                ));
        }
    }
    
    [HarmonyPatch(typeof(Missile), "SetTarget")]
    public static class OnMissileSetTargetPatch {
        static void Postfix() {
            TriggerMissileEvent(
                sender: null,
                e: new ModEventArgs(
                    "Missile_SetTarget",
                    null
                ));
        }
    }
}