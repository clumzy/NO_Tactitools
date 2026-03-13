using System;
using HarmonyLib;

namespace NO_Tactitools.Core.Events;

public class ModEventArgs(string eventName, object data) : EventArgs {
    public string EventName { get; set; } = eventName;
    public object Data { get; set; } = data;
}

public static class EventSystem {
    // pointer to a function that takes an object and a ModEventArgs and returns void
    public delegate void EventHandler(object sender, ModEventArgs e);
    // event that is triggered when the TacScreen is initialized
    public static event EventHandler OnTacScreenInit;
    public static event EventHandler OnTacScreenUpdate;
    public static event EventHandler OnCombatHUDFixedUpdate;
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