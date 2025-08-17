using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class DeliveryCheckerPlugin {
    private static bool initialized = false;
    public static void Postfix() {
        if (!initialized) {
            Plugin.Logger.LogInfo("[DV] DeliveryChecker plugin starting !");
            Plugin.harmony.PatchAll(typeof(StartMissilePatch));
            Plugin.harmony.PatchAll(typeof(DetonatePatch));
            Plugin.harmony.PatchAll(typeof(DeliveryBarUpdatePatch));
            Plugin.harmony.PatchAll(typeof(ResetDeliveryIndicatorsOnRespawnPatch));
            initialized = true;
            Plugin.Logger.LogInfo("[DV] DeliveryChecker plugin succesfully started !");
        }
    }
}

class DeliveryBar {
    public static Dictionary<Missile, DeliveryIndicator> deliveryIndicator = [];

    public static void TriggerRemoval(Missile missile, bool success) {
        if (deliveryIndicator.ContainsKey(missile)) {
            if (success) deliveryIndicator[missile].SetSuccess();
            else deliveryIndicator[missile].SetFailure();
        }
    }

    public static void RemoveMissile(Missile missile) {
        if (deliveryIndicator.ContainsKey(missile)) {
            deliveryIndicator[missile].Destroy();
            deliveryIndicator.Remove(missile);
            RepositionIndicators();
        }
    }

    public static void RepositionIndicators() {
        int i = 0;
        foreach (var indicator in deliveryIndicator.Values) {
            indicator.SetPosition(new Vector2(-120f + i * 12f, -60f));
            i++;
        }
    }
}

class DeliveryIndicator {
    private UIUtils.UIRectangle frame;
    private UIUtils.UIRectangle indicator;
    private float displayCountdown;
    private float hitTime;

    public DeliveryIndicator(Vector2 pos) {
        string randomString = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        System.Random rand = new();
        for (int i = 0; i < 6; i++) {
            randomString += chars[rand.Next(chars.Length)];
        }
        frame = new UIUtils.UIRectangle(
            "DeliveryFrame" + randomString,
            new Vector2(0f, 0f),
            new Vector2(12f, 12f),
            UIUtils.targetScreen,
            Color.black);
        indicator = new UIUtils.UIRectangle(
            "DeliveryIndicator" + randomString,
            new Vector2(0f, 0f),
            new Vector2(8, 8f),
            UIUtils.targetScreen,
            Color.yellow);
        frame.SetCenter(pos);
        indicator.SetCenter(pos);
        displayCountdown = -1f;
        //ensure that both elements don't inherite their parent scaling
        frame.GetRectObject().transform.localScale = Vector3.one;
        indicator.GetRectObject().transform.localScale = Vector3.one;
    }

    public void SetPosition(Vector2 pos) {
        frame.SetCenter(pos);
        indicator.SetCenter(pos);
    }

    public void SetRotation(float rotation) {
        frame.GetRectObject().transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
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

    public void SetSuccess() {
        indicator.SetFillColor(Color.green);
    }

    public void SetFailure() {
        indicator.SetFillColor(Color.red);
    }

    public void Destroy() {
        GameObject.Destroy(frame.GetRectObject());
        GameObject.Destroy(indicator.GetRectObject());
    }
}

[HarmonyPatch(typeof(Missile), "StartMissile")]
class StartMissilePatch {
    static void Postfix(Missile __instance) {
        if (__instance.owner == SceneSingleton<CombatHUD>.i.aircraft) {
            DeliveryIndicator deliveryIndicator = new(
                new Vector2(
                    -120f + DeliveryBar.deliveryIndicator.Count * 12f,
                    -115f
                )
            );
            // IF IT'S A BOMB, ROTATE IT 45 DEGREES
            // HACKY BUT IT WORKS
            if (!__instance.GetWeaponInfo().bomb) deliveryIndicator.SetRotation(45f);
            DeliveryBar.deliveryIndicator.Add(__instance, deliveryIndicator);
            DeliveryBar.RepositionIndicators();
        }
    }
}

[HarmonyPatch(typeof(Missile), "UserCode_RpcDetonate_897349600")]
class DetonatePatch {
    static void Postfix(Missile __instance, bool hitArmor) {
        if (__instance.owner == SceneSingleton<CombatHUD>.i.aircraft) {
            if (DeliveryBar.deliveryIndicator.ContainsKey(__instance)) {
                DeliveryBar.deliveryIndicator[__instance].SetDisplayCountdown(3f);
                DeliveryBar.deliveryIndicator[__instance].SetHitTime();
                if (hitArmor) DeliveryBar.TriggerRemoval(__instance, true);
                else DeliveryBar.TriggerRemoval(__instance, false);
            }
        }
    }
}

[HarmonyPatch(typeof(CombatHUD), "LateUpdate")]
class DeliveryBarUpdatePatch {
    static void Postfix() {
        foreach (var delivery in DeliveryBar.deliveryIndicator) {
            if (delivery.Value.GetDisplayCountdown() > 0f) {
                if (Time.time - delivery.Value.GetHitTime() > delivery.Value.GetDisplayCountdown()) {
                    DeliveryBar.RemoveMissile(delivery.Key);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetDeliveryIndicatorsOnRespawnPatch {
    static void Postfix() {
        DeliveryBar.deliveryIndicator.Clear();
        Plugin.Logger.LogInfo("[DV] Delivery indicators reset on aircraft respawn.");
    }
}