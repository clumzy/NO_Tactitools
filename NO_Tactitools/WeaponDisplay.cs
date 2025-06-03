using HarmonyLib;
using UnityEngine;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponDisplayPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[WD] Weapon Display plugin starting !");
            Plugin.harmony.PatchAll(typeof(WeaponDisplayPatch));
            initialized = true;
            Plugin.Log("[WD] Weapon Display plugin succesfully started !");
        }
    }
}

[HarmonyPatch(typeof(SystemStatusDisplay), "Initialize")]
class WeaponDisplayPatch {
    private static bool initialized = false;
    static void Postfix(SystemStatusDisplay __instance) {
        if (!initialized) {
            foreach (Transform child in UIUtils.MFD_List["systems"].GetMFDTransform()) {
                Object.Destroy(child.gameObject);
            }
            UIUtils.UILabel indicatorScreenLabel = new UIUtils.UILabel(
                "indicatorScreenLabel",
                new Vector2(0, 0),
                UIUtils.HMD,
                "systems",
                FontStyle.Normal,
                Color.green,
                25,
                0f
            );
            indicatorScreenLabel.SetText("TEST");
            initialized = true;
        }
    }
}