using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;
using UnityEngine.PlayerLoop;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponDisplayPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[WD] Weapon Display plugin starting !");
            Plugin.harmony.PatchAll(typeof(WeaponDisplayComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(WeaponDisplayComponent.OnPlatformUpdate));
            // Register the new button for toggling the weapon display
            InputCatcher.RegisterControllerButton(
                Plugin.weaponDisplayControllerName.Value,
                new ControllerButton(
                    Plugin.weaponDisplayButtonNumber.Value,
                    0.2f,
                    onShortPress: HandleDisplayToggle
                )
            );
            initialized = true;
            Plugin.Log("[WD] Weapon Display plugin succesfully started !");
        }
    }

    private static void HandleDisplayToggle() {
        if (WeaponDisplayComponent.InternalState.weaponDisplay != null) {
            WeaponDisplayComponent.InternalState.weaponDisplay.ToggleChildrenActiveState();
            Plugin.Log("[WD] Weapon Display toggled.");
        }
        else {
            Plugin.Log("[WD] Weapon Display not initialized, cannot toggle.");
        }
    }
}

public class WeaponDisplayComponent {
    // LOGIC ENGINE, INTERNAL STATE, DISPLAY ENGINE
    static class LogicEngine {
        static public void Init() {
            string name = Bindings.Player.Aircraft.GetPlatformName();
            static Transform Get(string path) {
                return Bindings.UI.Game.GetTacScreen().Find(path)?.transform;
            }
            InternalState.destination = name switch {
                "T/A-30 Compass" or "FS-12 Revoker" or "FS-20 Vortex" or "KR-67 Ifrit" => Get("SystemStatus"),
                "EW-1 Medusa" => Get("engPanel1"),
                "CI-22 Cricket" => Get("EngPanel"),
                "SAH-46 Chicane" => Get("TelemetryPanel"),
                "VL-49 Tarantula" => Get("RightScreenBorder/WeaponPanel"),
                "SFB-81" => Get("weaponPanel"),
                _ => null
            };
            InternalState.hasJammer = Bindings.Player.Aircraft.Countermeasures.HasJammer();
            Plugin.Log("[WD] Logic Engine initialized for platform " + name);
        }

        static public void Update() {
            InternalState.isOutOfAmmo = Bindings.Player.Weapons.GetActiveStationAmmo() == 0;
            InternalState.isFlareSelected = Bindings.Player.Aircraft.Countermeasures.GetCurrentIndex() == 0;
            InternalState.isJammerSelected = ! InternalState.isFlareSelected;
            InternalState.flareAmmo01 = Mathf.Clamp01(
                (float)Bindings.Player.Aircraft.Countermeasures.GetIRFlareAmmo() / Bindings.Player.Aircraft.Countermeasures.GetIRFlareMaxAmmo());
            if (Bindings.Player.Aircraft.Countermeasures.HasJammer())
                InternalState.jammerAmmo01 = Mathf.Clamp01(
                    (float)Bindings.Player.Aircraft.Countermeasures.GetJammerAmmo() / 100f);

        }
    }

    public static class InternalState {
        static public Transform destination;
        static public WeaponDisplay weaponDisplay;
        static public bool hasJammer;
        static public bool isOutOfAmmo;
        static public bool isFlareSelected;
        static public bool isJammerSelected;
        static public float flareAmmo01;
        static public float jammerAmmo01;

    }

    static class DisplayEngine {
        static public void Init() {
            if (Bindings.Player.Aircraft.Countermeasures.HasIRFlare()) {
                InternalState.weaponDisplay = new WeaponDisplay(
                    Bindings.Player.Aircraft.GetPlatformName(),
                    InternalState.destination);
                if (!Plugin.weaponDisplayVanillaUIEnabled.Value) Bindings.UI.Game.HideWeaponPanel();
            }
            Plugin.Log("[WD] Display Engine initialized for platform " + Bindings.Player.Aircraft.GetPlatformName());
        }

        static public void Update() {

            // REFRESH WEAPON
            InternalState.weaponDisplay.weaponNameLabel.SetText(Bindings.Player.Weapons.GetActiveStationName());
            InternalState.weaponDisplay.weaponAmmoLabel.SetText(Bindings.Player.Weapons.GetActiveStationAmmo().ToString());
            InternalState.weaponDisplay.weaponAmmoLabel.SetColor(InternalState.isOutOfAmmo ? Color.red : WeaponDisplay.mainColor);

            Image cloneImg = InternalState.weaponDisplay.weaponImageClone.GetComponent<Image>();
            Image srcImg = SceneSingleton<CombatHUD>.i.weaponImage;
            cloneImg.sprite = srcImg.sprite;
            cloneImg.color = InternalState.isOutOfAmmo ? Color.red : WeaponDisplay.mainColor;
            // TODO : ENCAPSULATE IMAGES IN MY OWN CODE

            // REFRESH FLARE
            InternalState.weaponDisplay.flareLabel.SetText("IR:" + Bindings.Player.Aircraft.Countermeasures.GetIRFlareAmmo().ToString());
            InternalState.weaponDisplay.flareLabel.SetFontStyle(InternalState.isFlareSelected ? FontStyle.Bold : FontStyle.Normal);
            InternalState.weaponDisplay.flareLabel.SetFontSize(InternalState.weaponDisplay.originalFlareFontSize + (InternalState.isFlareSelected ? 10 : 0));
            InternalState.weaponDisplay.flareLabel.SetColor(Color.Lerp(Color.red, WeaponDisplay.mainColor, InternalState.flareAmmo01));

            // REFRESH JAMMER
            if (InternalState.hasJammer) {
                InternalState.weaponDisplay.jammerLabel.SetText("EW:" + Bindings.Player.Aircraft.Countermeasures.GetJammerAmmo().ToString() + "%");
                InternalState.weaponDisplay.jammerLabel.SetFontStyle(InternalState.isJammerSelected ? FontStyle.Bold : FontStyle.Normal);
                InternalState.weaponDisplay.jammerLabel.SetFontSize(InternalState.weaponDisplay.originalJammerFontSize + (InternalState.isJammerSelected ? 10 : 0)); ;
                InternalState.weaponDisplay.jammerLabel.SetColor(Color.Lerp(Color.red, WeaponDisplay.mainColor, InternalState.jammerAmmo01));
            }
        }


    }

    public class WeaponDisplay {
        public Transform weaponDisplay_transform;
        public Bindings.UI.Draw.UILabel flareLabel;
        public Bindings.UI.Draw.UILabel jammerLabel;
        public Bindings.UI.Draw.UILine MFD_systemsLine;
        public Bindings.UI.Draw.UILabel weaponNameLabel;
        public Bindings.UI.Draw.UILabel weaponAmmoLabel;
        public GameObject weaponImageClone;
        // Store original font sizes
        public int originalFlareFontSize;
        public int originalJammerFontSize;

        //Store the main color for the MFD, can be set by the MFDColorPlugin
        public static Color mainColor = Color.green;


        public WeaponDisplay(string platformName, Transform destination) {
            // Default settings for the weapon display
            bool rotateWeaponImage = false;
            float imageScaleFactor = 0.6f;
            weaponDisplay_transform = destination;
            // Layout settings for each supported platform
            Vector2 flarePos, jammerPos, lineStart, lineEnd, weaponNamePos, weaponAmmoPos, weaponImagePos;
            int flareFont, jammerFont, weaponNameFont, weaponAmmoFont;
            switch (platformName) {
                case "CI-22 Cricket":
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -80);
                    lineStart = new Vector2(-60, 0);
                    lineEnd = new Vector2(60, 0);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 30);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 18;
                    weaponAmmoFont = 40;
                    break;
                case "SAH-46 Chicane":
                    flarePos = new Vector2(0, -80);
                    jammerPos = new Vector2(0, -160);
                    lineStart = new Vector2(-150, 0);
                    lineEnd = new Vector2(150, 0);
                    weaponNamePos = new Vector2(0, 110);
                    weaponAmmoPos = new Vector2(0, 60);
                    weaponImagePos = new Vector2(0, 160);
                    flareFont = 55;
                    jammerFont = 55;
                    weaponNameFont = 35;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 1.2f; // Scale the image for SAH-46 Chicane
                    break;
                case "T/A-30 Compass":
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -80);
                    lineStart = new Vector2(-60, 0);
                    lineEnd = new Vector2(60, 0);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 30);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 18;
                    weaponAmmoFont = 40;
                    break;
                case "FS-12 Revoker":
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -80);
                    lineStart = new Vector2(-100, -10);
                    lineEnd = new Vector2(100, -10);
                    weaponNamePos = new Vector2(0, 50);
                    weaponAmmoPos = new Vector2(0, 20);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 25;
                    weaponAmmoFont = 35;
                    imageScaleFactor = 0.8f; // Scale the image for FS-12 Revoker
                    break;
                case "FS-20 Vortex":
                    flarePos = new Vector2(-70, -70);
                    jammerPos = new Vector2(70, -70);
                    lineStart = new Vector2(-120, -20);
                    lineEnd = new Vector2(120, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(70, 20);
                    weaponImagePos = new Vector2(-60, 20);
                    flareFont = 35;
                    jammerFont = 35;
                    weaponNameFont = 30;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 0.7f; // Scale the image for FS-20 Vortex
                    break;
                case "KR-67 Ifrit":
                    flarePos = new Vector2(-75, -70);
                    jammerPos = new Vector2(70, -70);
                    lineStart = new Vector2(-100, -20);
                    lineEnd = new Vector2(100, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(80, 20);
                    weaponImagePos = new Vector2(-70, 20);
                    flareFont = 45;
                    jammerFont = 45;
                    weaponNameFont = 45;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 0.8f; // Scale the image for KR-67 Ifrit
                    break;
                case "VL-49 Tarantula":
                    flarePos = new Vector2(105, 40);
                    jammerPos = new Vector2(105, -40);
                    lineStart = new Vector2(50, -60);
                    lineEnd = new Vector2(50, 60);
                    weaponNamePos = new Vector2(-60, 0);
                    weaponAmmoPos = new Vector2(-60, -50);
                    weaponImagePos = new Vector2(-60, 40);
                    flareFont = 25;
                    jammerFont = 25;
                    weaponNameFont = 18;
                    weaponAmmoFont = 40;
                    imageScaleFactor = 0.8f;
                    break;
                case "EW-1 Medusa":
                    flarePos = new Vector2(-60, -70);
                    jammerPos = new Vector2(60, -70);
                    lineStart = new Vector2(-100, -20);
                    lineEnd = new Vector2(100, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(80, 20);
                    weaponImagePos = new Vector2(-60, 20);
                    flareFont = 35;
                    jammerFont = 35;
                    weaponNameFont = 30;
                    weaponAmmoFont = 50;
                    imageScaleFactor = 0.6f; // Scale the image for EW-1 Medusa
                    break;
                case "SFB-81":
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -80);
                    lineStart = new Vector2(-50, 0);
                    lineEnd = new Vector2(50, 0);
                    weaponNamePos = new Vector2(0, 80);
                    weaponAmmoPos = new Vector2(0, 40);
                    weaponImagePos = new Vector2(-120, 0);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 25;
                    weaponAmmoFont = 40;
                    rotateWeaponImage = true; // Rotate the weapon image for SFB-81
                    imageScaleFactor = 0.8f; // Scale the image for SFB-81
                    break;
                default:
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -80);
                    lineStart = new Vector2(0, -60);
                    lineEnd = new Vector2(0, 60);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 30);
                    weaponImagePos = new Vector2(0, 80);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 18;
                    weaponAmmoFont = 30;
                    break;
            }
            // Store original font sizes
            originalFlareFontSize = flareFont;
            originalJammerFontSize = jammerFont;

            // Hide the existing MFD content and kill the layout
            Bindings.UI.Generic.HideChildren(destination);
            Bindings.UI.Generic.KillLayout(destination);
            // rotate the destination canvas 90 degrees clockwise
            if (platformName == "SFB-81") destination.localRotation = Quaternion.Euler(0, 0, -90);

            // Create the labels and line for the systems MFD
            flareLabel = new(
                "flareLabel",
                flarePos,
                destination,
                FontStyle.Normal,
                mainColor,
                flareFont,
                0f
            );
            flareLabel.SetText("");
            jammerLabel = new(
                "radarLabel",
                jammerPos,
                destination,
                FontStyle.Normal,
                mainColor,
                jammerFont,
                0f
            );
            jammerLabel.SetText("⇌");
            MFD_systemsLine = new(
                "MFD_systemsLine",
                lineStart,
                lineEnd,
                destination,
                Color.white,
                2f
            );
            weaponNameLabel = new(
                "weaponNameLabel",
                weaponNamePos,
                destination,
                FontStyle.Normal,
                mainColor,
                weaponNameFont,
                0f
            );
            weaponAmmoLabel = new(
                "weaponAmmoLabel",
                weaponAmmoPos,
                destination,
                FontStyle.Normal,
                mainColor,
                weaponAmmoFont,
                0f
            );
            // Clone the weapon image and set it as a child of the systems MFD
            weaponImageClone = GameObject.Instantiate(SceneSingleton<CombatHUD>.i.weaponImage.gameObject, destination);
            var cloneImg = weaponImageClone.GetComponent<Image>();
            cloneImg.rectTransform.sizeDelta = new Vector2(
                cloneImg.rectTransform.sizeDelta.x * imageScaleFactor,
                cloneImg.rectTransform.sizeDelta.y * imageScaleFactor);
            cloneImg.rectTransform.anchoredPosition = weaponImagePos;
            //rotate the image 90 degrees clockwise
            if (rotateWeaponImage) cloneImg.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
        }

        public void ToggleChildrenActiveState() {
            if (weaponDisplay_transform == null) return;
            if (SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName == "SFB-81") {
                if (weaponDisplay_transform.localRotation.eulerAngles.z == 0)
                    weaponDisplay_transform.localRotation = Quaternion.Euler(0, 0, -90);
                else
                    weaponDisplay_transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            for (int i = 0; i < weaponDisplay_transform.childCount; i++) {
                var child = weaponDisplay_transform.GetChild(i).gameObject;
                //Specific fix for the Medusa, ThrottleGauge1 was initially hidden
                if (child.name != "ThrottleGauge1")
                    child.SetActive(!child.activeSelf);
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