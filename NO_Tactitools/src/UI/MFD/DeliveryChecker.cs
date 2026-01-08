using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class DeliveryCheckerPlugin {
    private static bool initialized = false;
    public static void Postfix() {
        if (!initialized) {
            Plugin.Log("[DV] DeliveryChecker plugin starting !");
            Plugin.harmony.PatchAll(typeof(StartMissilePatch));
            Plugin.harmony.PatchAll(typeof(DetonatePatch));
            Plugin.harmony.PatchAll(typeof(DeliveryBarUpdatePatch));
            Plugin.harmony.PatchAll(typeof(ResetDeliveryIndicatorsOnRespawnPatch));
            initialized = true;
            Plugin.Log("[DV] DeliveryChecker plugin succesfully started !");
        }
    }

    public static void Reset() {
        DeliveryBar.deliveryIndicator.Clear();
    }
}

class DeliveryBar {
    public static Dictionary<Missile, DeliveryIndicator> deliveryIndicator = [];

    public static void SetSuccess(Missile missile, bool success) {
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
    private Bindings.UI.Draw.UIAdvancedRectangle indicator;
    private float displayCountdown;
    private float hitTime;

    public DeliveryIndicator(Vector2 pos) {
        string randomString = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        System.Random rand = new();
        for (int i = 0; i < 6; i++) {
            randomString += chars[rand.Next(chars.Length)];
        }
        indicator = new Bindings.UI.Draw.UIAdvancedRectangle(
            "DeliveryIndicator" + randomString,
            new Vector2(0f, 0f),
            new Vector2(10f, 10f),
            Color.black,
            2f,
            Bindings.UI.Game.GetTargetScreenTransform(),
            Color.yellow);
        indicator.SetCenter(pos);
        displayCountdown = -1f;
        //ensure that both elements don't inherite their parent scaling
        indicator.GetRectObject().transform.localScale = Vector3.one;
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

    public void SetSuccess() {
        indicator.SetFillColor(Color.green);
    }

    public void SetFailure() {
        indicator.SetFillColor(Color.red);
    }

    public void Destroy() {
        GameObject.Destroy(indicator.GetRectObject());
    }
}

[HarmonyPatch(typeof(Missile), "StartMissile")]
class StartMissilePatch {
    static void Postfix(Missile __instance) {
        if (Bindings.UI.Game.GetTargetScreenTransform(true) == null) {
            // no need to check for aircraft here, as missiles can't be fired without an aircraft
            // also a missile can't NORMALLY get started without a targeting screen but we still check for it
            return;
        }
        if (__instance.owner == SceneSingleton<CombatHUD>.i.aircraft) {
            DeliveryIndicator deliveryIndicator = new(
                new Vector2(
                    -120f + DeliveryBar.deliveryIndicator.Count * 17f,
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
        if (__instance.owner == SceneSingleton<CombatHUD>.i.aircraft
            && Bindings.Player.Aircraft.GetAircraft() != null
            && Bindings.UI.Game.GetTargetScreenTransform(true) != null) {
            if (DeliveryBar.deliveryIndicator.ContainsKey(__instance)) {
                DeliveryBar.deliveryIndicator[__instance].SetDisplayCountdown(2f);
                DeliveryBar.deliveryIndicator[__instance].SetHitTime();
                if (hitArmor) DeliveryBar.SetSuccess(__instance, true);
                else DeliveryBar.SetSuccess(__instance, false);
            }
        }
    }
}

[HarmonyPatch(typeof(CombatHUD), "LateUpdate")]
class DeliveryBarUpdatePatch {
    static void Postfix() {
        if (Bindings.Player.Aircraft.GetAircraft() == null
            || Bindings.UI.Game.GetTargetScreenTransform(true) == null) {
            // no aircraft or no targeting screen, skip
            // an initialized target screen exists but is not active
            // so remove missile will still work correctly for missiles from THIS flight
            return;
        }
        // collect keys to remove to avoid modifying the collection during enumeration
        var toRemove = new List<Missile>();
        foreach (var delivery in DeliveryBar.deliveryIndicator) {
            if (delivery.Value.GetDisplayCountdown() > 0f) {
                if (Time.time - delivery.Value.GetHitTime() > delivery.Value.GetDisplayCountdown()) {
                    toRemove.Add(delivery.Key);
                }
            }
        }
        foreach (var missile in toRemove) {
            DeliveryBar.RemoveMissile(missile);
        }
    }
}

[HarmonyPatch(typeof(FlightHud), "ResetAircraft")]
class ResetDeliveryIndicatorsOnRespawnPatch {
    static void Postfix() {
        DeliveryCheckerPlugin.Reset();
    }
}