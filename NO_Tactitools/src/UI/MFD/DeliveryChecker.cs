using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;
using System.Linq;
using System;
using JetBrains.Annotations;

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
                    Type = missile.GetWeaponInfo().bomb ? InternalState.DeliveryType.Bomb : InternalState.DeliveryType.Missile,
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
            int missilesInFlight = InternalState.deliveries.Values.Count(d => d.Type == InternalState.DeliveryType.Missile && d.Status == InternalState.DeliveryStatus.InFlight);
            int bombsInFlight = InternalState.deliveries.Values.Count(d => d.Type == InternalState.DeliveryType.Bomb && d.Status == InternalState.DeliveryStatus.InFlight);
            int missilesHit = InternalState.deliveries.Values.Count(d => d.Type == InternalState.DeliveryType.Missile && d.Status == InternalState.DeliveryStatus.Hit);
            int bombsHit = InternalState.deliveries.Values.Count(d => d.Type == InternalState.DeliveryType.Bomb && d.Status == InternalState.DeliveryStatus.Hit);
            int missilesMissed = InternalState.deliveries.Values.Count(d => d.Type == InternalState.DeliveryType.Missile && d.Status == InternalState.DeliveryStatus.Missed);
            int bombsMissed = InternalState.deliveries.Values.Count(d => d.Type == InternalState.DeliveryType.Bomb && d.Status == InternalState.DeliveryStatus.Missed);

            // Toggle visibility of M/B category labels based on whether there are any deliveries
            InternalState.deliveryChecker.missileLabel.GetGameObject().SetActive(missilesInFlight + missilesHit + missilesMissed > 0);
            InternalState.deliveryChecker.bombLabel.GetGameObject().SetActive(bombsInFlight + bombsHit + bombsMissed > 0);

            // Update missile in flight label
            // - update value and vis
            InternalState.deliveryChecker.missileInFlightLabel.SetText(missilesInFlight > 0 ? missilesInFlight.ToString() : "");
            InternalState.deliveryChecker.missileInFlightLabel.GetGameObject().SetActive(missilesInFlight > 0);
            InternalState.deliveryChecker.missileInFlightLabel.SetCorners(
                new Vector2(InternalState.deliveryChecker.xOffsetMissile - 18f, InternalState.deliveryChecker.yOffset + 10f),
                new Vector2(InternalState.deliveryChecker.xOffsetMissile + 18f, InternalState.deliveryChecker.yOffset + 30f)
            );
            // Update bomb in flight label
            InternalState.deliveryChecker.bombInFlightLabel.SetText(bombsInFlight > 0 ? bombsInFlight.ToString() : "");
            InternalState.deliveryChecker.bombInFlightLabel.GetGameObject().SetActive(bombsInFlight > 0);
            InternalState.deliveryChecker.bombInFlightLabel.SetCorners(
                new Vector2(InternalState.deliveryChecker.xOffsetBomb - 18f, InternalState.deliveryChecker.yOffset + 10f),
                new Vector2(InternalState.deliveryChecker.xOffsetBomb + 18f, InternalState.deliveryChecker.yOffset + 30f)
            );
            // Update missile hit label
            InternalState.deliveryChecker.missileHitLabel.SetText(missilesHit > 0 ? missilesHit.ToString() : "");
            InternalState.deliveryChecker.missileHitLabel.GetGameObject().SetActive(missilesHit > 0);
            InternalState.deliveryChecker.missileHitLabel.SetCorners(
                new Vector2(InternalState.deliveryChecker.xOffsetMissile - 18f, InternalState.deliveryChecker.yOffset + 10f + 20f * (missilesInFlight > 0 ? 1 : 0)),
                new Vector2(InternalState.deliveryChecker.xOffsetMissile + 18f, InternalState.deliveryChecker.yOffset + 30f + 20f * (missilesInFlight > 0 ? 1 : 0))
            );
            // Update bomb hit label
            InternalState.deliveryChecker.bombHitLabel.SetText(bombsHit > 0 ? bombsHit.ToString() : "");
            InternalState.deliveryChecker.bombHitLabel.GetGameObject().SetActive(bombsHit > 0);
            InternalState.deliveryChecker.bombHitLabel.SetCorners(
                new Vector2(InternalState.deliveryChecker.xOffsetBomb - 18f, InternalState.deliveryChecker.yOffset + 10f + 20f * (bombsInFlight > 0 ? 1 : 0)),
                new Vector2(InternalState.deliveryChecker.xOffsetBomb + 18f, InternalState.deliveryChecker.yOffset + 30f + 20f * (bombsInFlight > 0 ? 1 : 0))
            );
            // Update missile missed label
            InternalState.deliveryChecker.missileMissedLabel.SetText(missilesMissed > 0 ? missilesMissed.ToString() : "");
            InternalState.deliveryChecker.missileMissedLabel.GetGameObject().SetActive(missilesMissed > 0);
            InternalState.deliveryChecker.missileMissedLabel.SetCorners(
                new Vector2(InternalState.deliveryChecker.xOffsetMissile - 18f, InternalState.deliveryChecker.yOffset + 10f + 20f * (missilesInFlight > 0 ? 1 : 0) + 20f * (missilesHit > 0 ? 1 : 0)),
                new Vector2(InternalState.deliveryChecker.xOffsetMissile + 18f, InternalState.deliveryChecker.yOffset + 30f + 20f * (missilesInFlight > 0 ? 1 : 0) + 20f * (missilesHit > 0 ? 1 : 0))
            );
            // Update bomb missed label
            InternalState.deliveryChecker.bombMissedLabel.SetText(bombsMissed > 0 ? bombsMissed.ToString() : "");
            InternalState.deliveryChecker.bombMissedLabel.GetGameObject().SetActive(bombsMissed > 0);
            InternalState.deliveryChecker.bombMissedLabel.SetCorners(
                new Vector2(InternalState.deliveryChecker.xOffsetBomb - 18f, InternalState.deliveryChecker.yOffset + 10f + 20f * (bombsInFlight > 0 ? 1 : 0) + 20f * (bombsHit > 0 ? 1 : 0)),
                new Vector2(InternalState.deliveryChecker.xOffsetBomb + 18f, InternalState.deliveryChecker.yOffset + 30f + 20f * (bombsInFlight > 0 ? 1 : 0) + 20f * (bombsHit > 0 ? 1 : 0))
            );
            // end update work
            InternalState.lastRenderedVersion = InternalState.deliveryStateVersion;
        }
    }

    public class DeliveryChecker {
        public GameObject containerObject;
        public Transform containerTransform;
        public UIBindings.Draw.UIAdvancedRectangleLabeled missileLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled bombLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled missileInFlightLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled bombInFlightLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled missileHitLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled bombHitLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled missileMissedLabel;
        public UIBindings.Draw.UIAdvancedRectangleLabeled bombMissedLabel;
        public float xOffsetMissile;
        public float xOffsetBomb;
        public float yOffset;

        public DeliveryChecker() {
            Transform parentTransform = UIBindings.Game.GetTargetScreenTransform();
            RectTransform rectTransform = parentTransform as RectTransform;

            if (rectTransform != null) {
                float halfWidth = rectTransform.rect.width / 2f;
                float halfHeight = rectTransform.rect.height / 2f;
                xOffsetMissile = -halfWidth + 18f;
                xOffsetBomb = halfWidth - 18f;
                yOffset = -halfHeight + 10f + 64f; // 64f is the height of the bearing
            } else {
                xOffsetMissile = -110f;
                xOffsetBomb = 110f;
                yOffset = -110f;
            }

            // Create container GameObject to hold all DeliveryChecker elements
            containerObject = new GameObject("i_dc_DeliveryCheckerContainer");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parentTransform, false);
            // Create a M for missile and B for bomb labels using AdvancedUIRectangleLabel
            missileLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_MissileLabel",
                cornerA: new Vector2(xOffsetMissile - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetMissile + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(0f, 0f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.white,
                fontSize: 20
            );
            missileLabel.SetText("M");
            bombLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_BombLabel",
                cornerA: new Vector2(xOffsetBomb - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetBomb + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(0f, 0f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.white,
                fontSize: 20
            );
            bombLabel.SetText("B");
            missileInFlightLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_MissileInFlightLabel",
                cornerA: new Vector2(xOffsetMissile - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetMissile + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(1f, 1f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.black,
                fontSize: 20
            );
            missileInFlightLabel.SetText("");
            missileInFlightLabel.GetGameObject().SetActive(false);
            bombInFlightLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_BombInFlightLabel",
                cornerA: new Vector2(xOffsetBomb - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetBomb + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(1f, 1f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.black,
                fontSize: 20
            );
            bombInFlightLabel.SetText("");
            bombInFlightLabel.GetGameObject().SetActive(false);
            missileHitLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_MissileHitLabel",
                cornerA: new Vector2(xOffsetMissile - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetMissile + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(0f, 1f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.black,
                fontSize: 20
            );
            missileHitLabel.SetText("");
            missileHitLabel.GetGameObject().SetActive(false);
            bombHitLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_BombHitLabel",
                cornerA: new Vector2(xOffsetBomb - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetBomb + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(0f, 1f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.black,
                fontSize: 20
            );
            bombHitLabel.SetText("");
            bombHitLabel.GetGameObject().SetActive(false);
            missileMissedLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_MissileMissedLabel",
                cornerA: new Vector2(xOffsetMissile - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetMissile + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(1f, 0f, 0f, 0.8f),
                fontStyle: FontStyle.Bold,
                textColor: Color.black,
                fontSize: 20
            );
            missileMissedLabel.SetText("");
            missileMissedLabel.GetGameObject().SetActive(false);
            bombMissedLabel = new UIBindings.Draw.UIAdvancedRectangleLabeled(
                name: "i_dc_BombMissedLabel",
                cornerA: new Vector2(xOffsetBomb - 18f, yOffset - 10f),
                cornerB: new Vector2(xOffsetBomb + 18f, yOffset + 10f),
                borderColor: Color.clear,
                borderThickness: 0f,
                UIParent: containerTransform,
                fillColor: new Color(1f, 0f, 0f, 0.8f),
                fontStyle: FontStyle.Normal,
                textColor: Color.black,
                fontSize: 20
            );
            bombMissedLabel.SetText("");
            bombMissedLabel.GetGameObject().SetActive(false);
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
