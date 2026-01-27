using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;

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
            InternalState.deliveryIndicators.Clear();
        }
        
        static public void Update() {
            if (GameBindings.Player.Aircraft.GetAircraft() == null
                || UIBindings.Game.GetTargetScreenTransform(true) == null
                || UIBindings.Game.GetTacScreenTransform(true) == null) {
                return; // no aircraft or no targeting screen
            }
            // collect keys to remove to avoid modifying the collection during enumeration
            var toRemove = new List<Missile>();
            foreach (var delivery in InternalState.deliveryIndicators) {
                if (delivery.Value.GetDisplayCountdown() > 0f) {
                    if (Time.time - delivery.Value.GetHitTime() > delivery.Value.GetDisplayCountdown()) {
                        toRemove.Add(delivery.Key);
                    }
                }
            }
            foreach (var missile in toRemove) {
                DisplayEngine.RemoveMissile(missile);
            }
        }

        static public void OnMissileStart(Missile missile) {
            if (UIBindings.Game.GetTargetScreenTransform(true) == null ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetTacScreenTransform(true) == null) {
                return; // no targeting screen available
            }
            if (missile.owner == SceneSingleton<CombatHUD>.i.aircraft) {
                if (InternalState.deliveryBarContainer == null) {
                    DisplayEngine.CreateContainer();
                }
                DeliveryIndicator deliveryIndicator = new(
                    new Vector2(
                        -120f + InternalState.deliveryIndicators.Count * 17f,
                        -115f
                    ),
                    !missile.GetWeaponInfo().bomb // rotate missiles (bombs are not rotated)
                );
                InternalState.deliveryIndicators.Add(missile, deliveryIndicator);
                DisplayEngine.RepositionIndicators();
            }
        }

        static public void OnMissileDetonate(Missile missile, bool hitArmor) {
            if (missile.owner == SceneSingleton<CombatHUD>.i.aircraft
                && GameBindings.Player.Aircraft.GetAircraft() != null
                && UIBindings.Game.GetTargetScreenTransform(true) != null
                && UIBindings.Game.GetTacScreenTransform(true) != null) {
                if (InternalState.deliveryIndicators.ContainsKey(missile)) {
                    InternalState.deliveryIndicators[missile].SetDisplayCountdown(2f);
                    InternalState.deliveryIndicators[missile].SetHitTime();
                    InternalState.deliveryIndicators[missile].SetSuccess(hitArmor);
                }
            }
        }

    }

    public static class InternalState {
        static public Dictionary<Missile, DeliveryIndicator> deliveryIndicators = new();
        static public Transform deliveryBarContainer;
    }

    static class DisplayEngine {
        static public void CreateContainer() {
            if (UIBindings.Game.GetTargetScreenTransform(true) == null ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetTacScreenTransform(true) == null) {
                return; // no targeting screen available
            }
            
            // if delivery bar container is not null, destroy it properly
            if (InternalState.deliveryBarContainer != null) {
                GameObject.Destroy(InternalState.deliveryBarContainer.gameObject);
            }
            InternalState.deliveryBarContainer = null;
            // Create new container as a child of the target screen
            GameObject containerObj = new("DeliveryBar_Container");
            InternalState.deliveryBarContainer = containerObj.transform;
            InternalState.deliveryBarContainer.SetParent(UIBindings.Game.GetTargetScreenTransform(), false);
            InternalState.deliveryBarContainer.localPosition = Vector3.zero;
        }

        static public void RemoveMissile(Missile missile) {
            if (InternalState.deliveryIndicators.ContainsKey(missile)) {
                InternalState.deliveryIndicators[missile].Destroy();
                InternalState.deliveryIndicators.Remove(missile);
                RepositionIndicators();
            }
        }

        static public void RepositionIndicators() {
            int i = 0;
            foreach (var indicator in InternalState.deliveryIndicators.Values) {
                indicator.SetPosition(new Vector2(-120f + i * 12f, -60f));
                i++;
            }
        }
    }

    public class DeliveryIndicator {
        private UIBindings.Draw.UIAdvancedRectangle indicator;
        private float displayCountdown;
        private float hitTime;

        public DeliveryIndicator(Vector2 pos, bool rotate = false) {
            // Generate a unique name for this indicator
            string randomString = "";
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            System.Random rand = new();
            for (int i = 0; i < 6; i++) {
                randomString += chars[rand.Next(chars.Length)];
            }

            indicator = new UIBindings.Draw.UIAdvancedRectangle(
                "DeliveryIndicator" + randomString,
                new Vector2(0f, 0f),
                new Vector2(8f, 8f),
                Color.black,
                3f,
                InternalState.deliveryBarContainer,
                Color.yellow);
            
            indicator.SetCenter(pos);
            
            // Ensure that elements don't inherit their parent scaling
            indicator.GetRectObject().transform.localScale = Vector3.one;
            
            // Rotate missiles 45 degrees (bombs are not rotated)
            if (rotate) {
                SetRotation(45f);
            }
            
            displayCountdown = -1f;
        }

        public void SetPosition(Vector2 pos) {
            indicator.SetCenter(pos);
        }

        public void SetRotation(float rotation) {
            indicator.GetRectObject().transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        public float GetDisplayCountdown() {
            return displayCountdown;
        }

        public void SetDisplayCountdown(float countdown) {
            displayCountdown = countdown;
        }

        public void SetHitTime() {
            hitTime = Time.time;
        }

        public float GetHitTime() {
            return hitTime;
        }

        public void SetSuccess(bool success) {
            indicator.SetFillColor(success ? Color.green : Color.red);
        }

        public void Destroy() {
            GameObject.Destroy(indicator.GetRectObject());
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
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Update")]
    public static class OnPlatformUpdate {
        static void Postfix() {
            LogicEngine.Update();
        }
    }


}
