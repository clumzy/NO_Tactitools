using System;
using HarmonyLib;

namespace NO_Tactitools.Core.Events;

public class ModEventArgs(string eventName, object data) : EventArgs {
    public string EventName { get; set; } = eventName;
    public object Data { get; set; } = data;
}

public static class EventSystem {
    // event types
    public delegate void InitEventHandler(object sender, ModEventArgs e);
    public delegate void UpdateEventHandler(object sender, ModEventArgs e);
    // events themselves
    public static event InitEventHandler OnTacScreenInit;
    public static event UpdateEventHandler OnTacScreenUpdate;
    public static event UpdateEventHandler OnCombatHUDFixedUpdate;
    // triggers
    public static void TriggerInitEvent(object sender, ModEventArgs e) {
        if (e.EventName == "TacScreen_Initialize")
            OnTacScreenInit?.Invoke(sender, e);
    }
    public static void TriggerUpdateEvent(object sender, ModEventArgs e) {
        if (e.EventName == "TacScreen_Update")
            OnTacScreenUpdate?.Invoke(sender, e);
        if (e.EventName == "CombatHUD_FixedUpdate")
            OnCombatHUDFixedUpdate?.Invoke(sender, e);
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
}