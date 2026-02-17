using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class ILSScreenPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[ILS] ILS Screen plugin starting !");
            Plugin.harmony.PatchAll(typeof(ILSScreenComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(ILSScreenComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[ILS] ILS Screen plugin succesfully started !");
        }
    }
}

class ILSScreenComponent {
    static public class LogicEngine {
        public static void Init() {
            //Plugin.Log("Initializing logic engine...");
        }

        public static void Update() {
            //Plugin.Log("Updating logic engine...");
        }
    }
    static public class InternalState {}

    static public class DisplayEngine {
        public static void Init() {
            //Plugin.Log("Initializing display engine...");
        }

        public static void Update() {
            //Plugin.Log("Updating display engine...");
        }
    }

    // OLS START PATCH


    // INIT AND REFRESH LOOP
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