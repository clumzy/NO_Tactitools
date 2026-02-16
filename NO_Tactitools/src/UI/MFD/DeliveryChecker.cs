using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Linq;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class DeliveryCheckerPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log("[DV] DeliveryChecker plugin starting !");
            Plugin.harmony.PatchAll(typeof(DeliveryCheckerComponent.OnMissileStart));
            Plugin.harmony.PatchAll(typeof(DeliveryCheckerComponent.OnMissileDetonate));
            Plugin.harmony.PatchAll(typeof(DeliveryCheckerComponent.OnPlatformUpdate));
            Plugin.harmony.PatchAll(typeof(DeliveryCheckerComponent.OnPlatformStart));
            initialized = true;
            Plugin.Log("[DV] DeliveryChecker plugin succesfully started !");
        }
    }
}

public class DeliveryCheckerComponent {
    // LOGIC ENGINE, INTERNAL STATE, DISPLAY ENGINE

    static class LogicEngine {
        static public void Init() {
            InternalState.deliveryChecker?.Destroy();
            InternalState.deliveryChecker = null;
            InternalState.deliveryStateVersion = 0;
            InternalState.lastRenderedVersion = -1;
            InternalState.deliveries.Clear();
        }

        static public void Update() {
            if (GameBindings.Player.Aircraft.GetAircraft() == null
                || UIBindings.Game.GetTargetScreenTransform(true) == null
                || UIBindings.Game.GetTacScreenTransform(true) == null) {
                return; // no aircraft or no targeting screen
            }
            List<Missile> deliveriesToRemove = [];
            for (int i = 0; i < InternalState.deliveries.Count; i++) {
                if (InternalState.deliveries.ElementAt(i).Value.Status == InternalState.DeliveryStatus.InFlight) {
                    // check if the delivery has been in flight for more than 120 seconds
                    if (Time.time - InternalState.deliveries.ElementAt(i).Value.startTime > 120f) {
                        InternalState.deliveries.ElementAt(i).Value.Status = InternalState.DeliveryStatus.Missed;
                        InternalState.deliveries.ElementAt(i).Value.hitTime = Time.time;
                        InternalState.IncrementVersion();
                    }
                }
                else if (
                    InternalState.deliveries.ElementAt(i).Value.Status == InternalState.DeliveryStatus.Hit
                    || InternalState.deliveries.ElementAt(i).Value.Status == InternalState.DeliveryStatus.Missed) {
                    // check if the delivery has been in hit/missed status for more than 3 seconds
                    if (Time.time - InternalState.deliveries.ElementAt(i).Value.hitTime > 3f) {
                        // add the delivery to the list of deliveries to remove
                        deliveriesToRemove.Add(InternalState.deliveries.ElementAt(i).Key);
                        InternalState.IncrementVersion();
                    }
                }
            }
            // remove the deliveries that have been in hit/missed status for more than 3 seconds
            foreach (Missile missile in deliveriesToRemove) {
                InternalState.deliveries.Remove(missile);
            }
        }

        static public void OnMissileStart(Missile missile) {
            if (UIBindings.Game.GetTargetScreenTransform(true) == null ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetTacScreenTransform(true) == null) {
                return; // no targeting screen available
            }
            if (missile.owner == SceneSingleton<CombatHUD>.i.aircraft) {
                InternalState.deliveries[missile] = new InternalState.DeliveryInfo() {
                    Type = InternalState.DeliveryType.Missile,
                    Status = InternalState.DeliveryStatus.InFlight,
                    startTime = Time.time,
                    hitTime = -1f
                };
                InternalState.IncrementVersion();
            }
        }

        static public void OnMissileDetonate(Missile missile, bool hitArmor) {
            if (missile.owner == SceneSingleton<CombatHUD>.i.aircraft
                && GameBindings.Player.Aircraft.GetAircraft() != null
                && UIBindings.Game.GetTargetScreenTransform(true) != null
                && UIBindings.Game.GetTacScreenTransform(true) != null) {
                if (InternalState.deliveries.ContainsKey(missile)) {
                    InternalState.deliveries[missile].Status = hitArmor ? InternalState.DeliveryStatus.Hit : InternalState.DeliveryStatus.Missed;
                    InternalState.deliveries[missile].hitTime = Time.time;
                    InternalState.IncrementVersion();
                }
            }
        }

    }

    public static class InternalState {
        public enum DeliveryType {
            Missile,
            Bomb
        }
        public enum DeliveryStatus {
            InFlight,
            Hit,
            Missed
        }
        public class DeliveryInfo {
            public DeliveryType Type;
            public DeliveryStatus Status;
            public float startTime;
            public float hitTime;
        }
        static public Dictionary<Missile, DeliveryInfo> deliveries = [];
        static public DeliveryChecker deliveryChecker;
        static public int deliveryStateVersion = 0;
        static public int lastRenderedVersion = -1;
        static public void IncrementVersion() => ++deliveryStateVersion;
    }

    static class DisplayEngine {
        static public void Init() {
            Plugin.Log("[DC] Initializing Delivery Checker for Target Screen");
            InternalState.deliveryChecker = new DeliveryChecker();
            Plugin.Log("[DC] Delivery Checker for Target Screen initialized");
        }
        static public void Update() {
            if (GameBindings.Player.Aircraft.GetAircraft() == null
                || UIBindings.Game.GetTargetScreenTransform(true) == null
                || UIBindings.Game.GetTacScreenTransform(true) == null) {
                return; // no aircraft or no targeting screen
            }
            if (InternalState.deliveryChecker == null) {
                Init();
            }
            // skip ui
            if (InternalState.deliveryStateVersion == InternalState.lastRenderedVersion) return;
            // do update work
            // TODO
            // end update work
            InternalState.lastRenderedVersion = InternalState.deliveryStateVersion;
        }
    }

    public class DeliveryChecker {
        public GameObject containerObject;
        public Transform containerTransform;
        public UIBindings.Draw.UIAdvancedRectangleLabeled missileLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled bombLabel;
        private const float xOffset = -110f;
        private const float yOffset = -110f;

        public DeliveryChecker() {
            Transform parentTransform = UIBindings.Game.GetTargetScreenTransform();
            // Create container GameObject to hold all DeliveryChecker elements
            containerObject = new GameObject("i_dc_DeliveryCheckerContainer");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parentTransform, false);
            // Create a M for missile and B for bomb labels using AdvancedUIRectangleLabel
            missileLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_MissileLabel",
                cornerA: new Vector2(xOffset - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffset + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(0f, 0f, 0f, 0.8f),
                fontStyle: FontStyle.Normal,
                textColor: Color.white,
                fontSize: 20
            );
            missileLabel.SetText("M");
            bombLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_BombLabel",
                cornerA: new Vector2(xOffset + 18f, yOffset - 10f),
                cornerB: new Vector2(xOffset + 18 * 3f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(0f, 0f, 0f, 0.8f),
                fontStyle: FontStyle.Normal,
                textColor: Color.white,
                fontSize: 20
            );
            bombLabel.SetText("B");
        }

        public void Destroy() {
            GameObject.Destroy(containerObject);
        }
    }

    // HARMONY PATCHES
    [HarmonyPatch(typeof(Missile), "StartMissile")]
    public static class OnMissileStart {
        static void Postfix(Missile __instance) {
            LogicEngine.OnMissileStart(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "UserCode_RpcDetonate_897349600")]
    public static class OnMissileDetonate {
        static void Postfix(Missile __instance, bool hitArmor) {
            LogicEngine.OnMissileDetonate(__instance, hitArmor);
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart {
        static void Postfix() {
            LogicEngine.Init();
            // DisplayEngine.Init();
            // Since there is no target screen at the start of the game, we will initialize the display engine on the first update instead
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
