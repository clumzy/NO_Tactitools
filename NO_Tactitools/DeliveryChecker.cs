using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace NO_Tactitools;

[HarmonyPatch(typeof(MainMenu), "Start")]
class DeliveryCheckerPlugin {
    public static DeliveryBar deliveryBar;
    public static void Postfix() {
        Plugin.Logger.LogInfo("[DV] DeliveryChecker plugin starting !");
        deliveryBar = new();
        Plugin.Logger.LogInfo("[DV] DeliveryChecker plugin succesfully started ! !");
    }
}

class DeliveryBar {
    public Dictionary<Missile, DeliveryIndicator> deliveryIndicator = [];
    public DeliveryBar() {
        Plugin.Logger.LogInfo("[DV] DeliveryBar created !");
    }

    public void TriggerRemoval(Missile missile) {
        if (deliveryIndicator.ContainsKey(missile)) {
            deliveryIndicator[missile].SetSuccess();

        }
    }

    public void RemoveMissile(Missile missile) {
        if (deliveryIndicator.ContainsKey(missile)) {
            deliveryIndicator[missile].Destroy();
            deliveryIndicator.Remove(missile);
            RepositionIndicators();
        }
    }

    // Repositions all indicators in a single row
    private void RepositionIndicators() {
        int i = 0;
        foreach (var indicator in deliveryIndicator.Values) {
            indicator.SetPosition(new Vector2(-120f + i * 12f, -115f));
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
        frame = new UIUtils.UIRectangle(
            "DeliveryFrame" + DeliveryCheckerPlugin.deliveryBar.deliveryIndicator.Count.ToString(),
            new Vector2(0f, 0f),
            new Vector2(12f, 12f),
            Plugin.combatHUD.transform,
            true,
            Color.black);
        indicator = new UIUtils.UIRectangle(
            "DeliveryIndicator" + DeliveryCheckerPlugin.deliveryBar.deliveryIndicator.Count.ToString(),
            new Vector2(0f, 0f),
            new Vector2(8, 8f),
            Plugin.combatHUD.transform,
            true,
            Color.yellow);
        frame.SetCenter(pos);
        indicator.SetCenter(pos);
        displayCountdown = -1f;
    }

    public void SetPosition(Vector2 pos) {
        frame.SetCenter(pos);
        indicator.SetCenter(pos);
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
        displayCountdown = 3f;
    }

    public void SetFailure() {
        indicator.SetFillColor(Color.red);
        displayCountdown = 3f;
    }

    public void Destroy() {
        //destroy both unity game objects
        GameObject.Destroy(frame.GetRectObject());
        GameObject.Destroy(indicator.GetRectObject());
    }
}

[HarmonyPatch(typeof(Missile), "StartMissile")]
class StartMissilePatch {
    static void Postfix(Missile __instance) {
        if (__instance.owner == Plugin.combatHUD.aircraft) {
            DeliveryIndicator deliveryIndicator = new(
                new Vector2(
                    -120f+DeliveryCheckerPlugin.deliveryBar.deliveryIndicator.Count * 12f,
                    -115f
                )
            );
            DeliveryCheckerPlugin.deliveryBar.deliveryIndicator.Add(__instance, deliveryIndicator);
        }
    }
}

[HarmonyPatch(typeof(Missile), "UnitDisabled")]
class DetonatePatch {
    static void Postfix(Missile __instance) {
        if (__instance.owner == Plugin.combatHUD.aircraft) {
            if (DeliveryCheckerPlugin.deliveryBar.deliveryIndicator.ContainsKey(__instance)) {
                DeliveryCheckerPlugin.deliveryBar.deliveryIndicator[__instance].SetDisplayCountdown(3f);
                DeliveryCheckerPlugin.deliveryBar.deliveryIndicator[__instance].SetHitTime();
                DeliveryCheckerPlugin.deliveryBar.TriggerRemoval(__instance);
            }
        }
    }
}

[HarmonyPatch(typeof(CombatHUD), "LateUpdate")]
class DeliveryBarUpdatePatch {
    static void Postfix() {
        foreach (var delivery in DeliveryCheckerPlugin.deliveryBar.deliveryIndicator) {
            if (delivery.Value.GetDisplayCountdown() > 0f) {
                if (Time.time - delivery.Value.GetHitTime() > delivery.Value.GetDisplayCountdown()) {
                    DeliveryCheckerPlugin.deliveryBar.RemoveMissile(delivery.Key);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetDeliveryIndicatorsOnRespawnPatch {
    static void Postfix() {
        DeliveryCheckerPlugin.deliveryBar = new();
    }
}
