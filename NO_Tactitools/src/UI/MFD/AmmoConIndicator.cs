using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class AmmoConIndicatorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[AmmoCon] Ammo Conservation Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(AmmoConIndicatorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(AmmoConIndicatorComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[AmmoCon] Ammo Conservation Indicator plugin successfully started !");
        }
    }
}

class AmmoConIndicatorComponent {
    static public class LogicEngine {
        public static void Init() {
        }

        public static void Update() {
            if (GameBindings.GameState.IsGamePaused() || GameBindings.Player.Aircraft.GetAircraft() == null)
                return;

            // Add calculations here
        }
    }

    static public class InternalState {
    }

    static public class DisplayEngine {
        public static void Init() {
        }

        public static void Update() {
            if (GameBindings.GameState.IsGamePaused() || GameBindings.Player.Aircraft.GetAircraft() == null) {
                return;
            }

            // Update UI components based on logic results
        }
    }

    public class AmmoConWidget {
        public GameObject containerObject;
        public Transform containerTransform;

        public AmmoConWidget(Transform parent) {
            containerObject = new GameObject("i_AmmoConContainer");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = Vector3.zero;

            // Create UI elements using UIBindings.Draw here
        }

        public void SetActive(bool active) => containerObject?.SetActive(active);

        public void Destroy() {
            if (containerObject != null) {
                Object.Destroy(containerObject);
                containerObject = null;
            }
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