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
            InternalState.maxBankAngle = (int)Mathf.Clamp(
                Mathf.Round((float)Plugin.bankIndicatorMaxBank.Value / 5f) * 5f,
                5,
                45);

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
        public static int maxBankAngle = 15;
        public static bool isAuthorized = false;
        public static List<string> authorizedPlatforms = [];
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

            InternalState.BIWidget.UpdateDisplay(InternalState.currentBankAngle);
        }
    }

    public class BankIndicatorWidget {
        public GameObject containerObject;
        public Transform containerTransform;
        public Image arc;
        public Image needle;
        public UIBindings.Draw.UILabel bankLabel;
        public List<UIBindings.Draw.UILine> increments = new();

        public BankIndicatorWidget(Transform parent) {
            containerObject = new GameObject("i_RBI_Container");
            containerObject.AddComponent<RectTransform>();
            containerTransform = containerObject.transform;
            containerTransform.SetParent(parent, false);
            containerTransform.localPosition = Vector3.zero;
            int radius = 70;

            needle = GameObject.Instantiate(
                UIBindings.Game.GetFlightHUDCenterTransform().Find("compass/compassPoint").GetComponent<Image>(),
                containerTransform);
            needle.name = "i_RBI_needle";
            // make it 2/3 the size of the compass needle and move it to the same position as the arc
            needle.rectTransform.localScale = new Vector3(0.66f, 0.66f, 0.66f);
            needle.rectTransform.localPosition = new Vector3(0, -radius - 10, 0);
            needle.color = new Color(needle.color.r, needle.color.g, needle.color.b, Plugin.bankIndicatorTransparency.Value);

            bankLabel = new UIBindings.Draw.UILabel(
                name: "i_RBI_bankLabel",
                position: new Vector2(0, -radius - 25),
                UIParent: containerTransform,
                color: new Color(0f, 1f, 0f, Plugin.bankIndicatorTransparency.Value),
                fontSize: 34,
                backgroundOpacity: 0f,
                material: UIBindings.Game.GetFlightHUDFontMaterial()
            );
            // so that it looks like the bearing label
            bankLabel.GetRectTransform().localScale = new Vector3(0.5f, 0.5f, 0.5f);
            bankLabel.GetGameObject().SetActive(Plugin.bankIndicatorShowLabel.Value);
            
            int smallIncrement = InternalState.maxBankAngle <= 10 ? 1 : 5;
            int bigIncrement = InternalState.maxBankAngle <= 10 ? 5 : 15;
            float visualScale = 45f / InternalState.maxBankAngle;

            // now we create the arc with only the increments, with increments as lines, thick ones for big increments and thin ones for small increments
            // they stay fixed with the aircraft, and below the needle that stays fixed with the horizon, so we put them in the same container as the needle but behind it
            for (int i = -InternalState.maxBankAngle; i <= InternalState.maxBankAngle; i += smallIncrement) {
                bool isBigIncrement = i % bigIncrement == 0 || i == -InternalState.maxBankAngle || i == InternalState.maxBankAngle;
                UIBindings.Draw.UILine line = new (
                    name: $"i_RBI_increment_{i.ToString()}",
                    start: new Vector2(0, isBigIncrement ? -radius+10 : -radius+5),
                    end: new Vector2(0, -radius),
                    UIParent: containerTransform,
                    color: new Color(0f, 1f, 0f, Plugin.bankIndicatorTransparency.Value),
                    thickness: isBigIncrement ? 2f : 1f,
                    material: UIBindings.Game.GetFlightHUDFontMaterial()
                );
                line.GetRectTransform().transform.RotateAround(containerTransform.position, Vector3.forward, -i * visualScale);
                increments.Add(line);
            }
        }

        public void UpdateDisplay(float currentBankAngle) {
            if (containerObject == null) return;
            float clampedBankAngle = Mathf.Clamp(currentBankAngle, -InternalState.maxBankAngle, InternalState.maxBankAngle);
            float visualScale = 45f / InternalState.maxBankAngle;
            // we first reset the need to 0 rotation, then we rotate it around the center in the opposite direction of the clamped bank angle so that it stays fixed with the horizon
            needle.rectTransform.transform.RotateAround(containerTransform.position, Vector3.forward, -needle.rectTransform.transform.rotation.eulerAngles.z - (clampedBankAngle * visualScale));
            
            if (bankLabel.GetGameObject().activeSelf) {
                bankLabel.SetText($"{Mathf.RoundToInt(-currentBankAngle).ToString()}°");
            }
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
