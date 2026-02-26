using HarmonyLib;
using UnityEngine.UI;
using NO_Tactitools.Core;
using UnityEngine;
using System.Collections.Generic;

namespace NO_Tactitools.UI.HUD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class BankIndicatorPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[BI] Rotor Bank Indicator plugin starting !");
            Plugin.harmony.PatchAll(typeof(BankIndicatorComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(BankIndicatorComponent.OnPlatformUpdate));
            initialized = true;
            Plugin.Log("[BI] Rotor Bank Indicator plugin successfully started !");
        }
    }
}

public class BankIndicatorComponent {
    static class LogicEngine {
        static public void Init() {
            InternalState.BIWidget?.Destroy();
            InternalState.BIWidget = null;
            InternalState.authorizedPlatforms = FileUtilities.GetListFromConfigFile("BankIndicator_AuthorizedPlatforms.txt");
            InternalState.isAuthorized = InternalState.authorizedPlatforms.Contains(GameBindings.Player.Aircraft.GetPlatformName());
            if (!InternalState.isAuthorized) return;

            InternalState.currentBankAngle = 0f;
            InternalState.maxBankAngle = Plugin.bankIndicatorMaxBank.Value;
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized)
                return;
            Transform aircraftTransform = GameBindings.Player.Aircraft.GetAircraft().transform;
            float bank = aircraftTransform.localEulerAngles.z;
            if (bank > 180f) bank -= 360f;
            InternalState.currentBankAngle = bank;
        }
    }

    public static class InternalState {
        public static float currentBankAngle = 0f;
        public static float maxBankAngle = 15f;
        public static bool isAuthorized = false;
        public static List<string> authorizedPlatforms = new();
        public static BankIndicatorWidget BIWidget = null;
    }

    static class DisplayEngine {
        static public void Init() {
            if (!InternalState.isAuthorized) return;

            if (InternalState.BIWidget == null) {
                InternalState.BIWidget = new BankIndicatorWidget(UIBindings.Game.GetFlightHUDCenterTransform());
                Plugin.Log("[BI] Bank Indicator Widget initialized and added to HUD.");
            }
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused()
                || GameBindings.Player.Aircraft.GetAircraft() == null
                || !InternalState.isAuthorized)
                return;
            
            InternalState.BIWidget.UpdateDisplay(InternalState.currentBankAngle, InternalState.maxBankAngle);
        }
    }

    public class BankIndicatorWidget {
        public GameObject containerObject;
        public Transform containerTransform;
        public Image arc;
        public Image needle;
        public UIBindings.Draw.UILabel bankLabel;

        public BankIndicatorWidget(Transform parent) {
            containerObject = new GameObject("i_RBI_Container");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = Vector3.zero;

            FuelGauge fg = GameObject.FindFirstObjectByType<FuelGauge>();
            arc = GameObject.Instantiate(
                new TraverseCache<FuelGauge, Image>("fuelArc").GetValue(fg).GetComponent<Image>(), 
                containerTransform);
            arc.name = "i_RBI_arc";
            // move center of arc to top of the screen and rotate it 90 degrees right
            arc.rectTransform.anchoredPosition = new Vector2(0, 200);
            arc.rectTransform.rotation = Quaternion.Euler(0, 0, -90);
            
            needle = GameObject.Instantiate(
                UIBindings.Game.GetFlightHUDCenterTransform().Find("compass/compassPoint").GetComponent<Image>(),
                containerTransform);
            needle.name = "i_RBI_needle";
            // make it 2/3 the size of the compass needle and move it to the same position as the arc
            needle.rectTransform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
            
            bankLabel = new UIBindings.Draw.UILabel(
                name: "i_RBI_bankLabel",
                position: new Vector2(0, 223),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, 0.8f),
                fontSize: 34,
                backgroundOpacity:0f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            // so that it looks like the bearing label
            bankLabel.GetRectTransform().localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }

        public void SetActive(bool active) => containerObject?.SetActive(active);

        public void UpdateDisplay(float currentBankAngle, float maxBankAngle) {
            float clampedBankAngle = Mathf.Clamp(currentBankAngle, -maxBankAngle, maxBankAngle);

            // needle position using trig to stay on arc
            // calculated radius = 724, center Y = -534 (relative to flight HUD center)
            // at 15 deg bank, needle is at -76 X and 186 Y (from 190 Y top)
            float yCenter = -150f;
            float radius = 350f;
            // indicator at max clamped angle is at 8.8 degrees on the arc, so we can use that to calculate the angle on the arc for any given bank angle
            float arcAngle = (-clampedBankAngle / maxBankAngle) * 12.8f;
            float x = radius * Mathf.Sin(arcAngle * Mathf.Deg2Rad);
            float y = yCenter + radius * Mathf.Cos(arcAngle * Mathf.Deg2Rad);
            needle.rectTransform.anchoredPosition = new Vector2(x, y);
            needle.rectTransform.rotation = Quaternion.Euler(
                0, 
                0, 
                -arcAngle + UIBindings.Game.GetFlightHUDCenterTransform().rotation.eulerAngles.z);
            // set text with one decimal place and a degree symbol
            bankLabel.SetText($"{(-currentBankAngle).ToString("F1")}°");
        }

        public void Destroy() {
            if (containerObject != null) {
                Object.Destroy(containerObject);
                containerObject = null;
            }
        }
    }

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
