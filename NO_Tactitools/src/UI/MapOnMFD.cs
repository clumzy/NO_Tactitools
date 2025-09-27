using UnityEngine;
using HarmonyLib;
using NO_Tactitools.Core;
using UnityEngine.UI;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class MapOnMFDPlugin {
    private static bool initialized = false;

    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[MAP] Map on MFD plugin starting !");
            Plugin.harmony.PatchAll(typeof(MapOnMFDPatch));
            initialized = true;
            Plugin.Log("[MAP] Map on MFD plugin successfully started !");
        }
    }
}

[HarmonyPatch(typeof(TacScreen), "Initialize")]
class MapOnMFDPatch {
    static void Postfix() {
        if (false) {
            Transform mapTF = GameObject.Find("mapBackground")?.transform;
            if (mapTF != null)
                Plugin.Log("[MAP] Enabling map on MFD");
                mapTF.GetComponentInChildren<GraphicRaycaster>().gameObject.SetActive(false);
                mapTF.SetParent(Bindings.UI.Game.GetTacScreen().transform.Find("Radar"), false);
                mapTF.localPosition = new Vector3(0, 0, -10);
                mapTF.localScale = new Vector3(1, 1, 1);
        }
    }
}