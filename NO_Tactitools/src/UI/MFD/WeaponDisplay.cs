using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI.MFD;

[HarmonyPatch(typeof(MainMenu), "Start")]
class WeaponDisplayPlugin {
    private static bool initialized = false;
    static void Postfix() {
        if (!initialized) {
            Plugin.Log($"[WD] Weapon Display plugin starting !");
            Plugin.harmony.PatchAll(typeof(WeaponDisplayComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(WeaponDisplayComponent.OnPlatformUpdate));
            // Register the new button for toggling the weapon display
            InputCatcher.RegisterNewInput(
                Plugin.weaponDisplayControllerName.Value,
                    Plugin.weaponDisplayButtonIndex.Value,
                    0.2f,
                    onRelease: HandleDisplayToggle
            );
            initialized = true;
            Plugin.Log("[WD] Weapon Display plugin succesfully started !");
        }
    }

    private static void HandleDisplayToggle() {
        if (WeaponDisplayComponent.InternalState.weaponDisplay != null) {
            if (WeaponDisplayComponent.InternalState.weaponDisplay.removeOriginalMFDContent) {
                WeaponDisplayComponent.InternalState.weaponDisplay.ToggleChildrenActiveState();

                UIBindings.Sound.PlaySound("beep_scroll");
                Plugin.Log("[WD] Weapon Display toggled.");
            }
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
            string name = GameBindings.Player.Aircraft.GetPlatformName();
            Plugin.Log("[WD] Initializing Logic Engine for platform " + name);
            InternalState.hasJammer = GameBindings.Player.Aircraft.Countermeasures.HasJammer();
            InternalState.hasIRFlare = GameBindings.Player.Aircraft.Countermeasures.HasIRFlare();
            InternalState.hasStations = GameBindings.Player.Aircraft.Weapons.GetStationCount() > 0;
            InternalState.vanillaUIEnabled = Plugin.weaponDisplayVanillaUIEnabled.Value;
            Plugin.Log("[WD] Logic Engine initialized for platform " + name);
        }

        static public void Update() {
            if (GameBindings.Player.Aircraft.GetAircraft() == null) return;
            InternalState.isFlareSelected = GameBindings.Player.Aircraft.Countermeasures.IsFlareSelected();
            InternalState.isJammerSelected = !InternalState.isFlareSelected;
            InternalState.flareAmmo01 = Mathf.Clamp01(
                (float)GameBindings.Player.Aircraft.Countermeasures.GetIRFlareAmmo() / GameBindings.Player.Aircraft.Countermeasures.GetIRFlareMaxAmmo());
            if (GameBindings.Player.Aircraft.Countermeasures.HasJammer())
                InternalState.jammerAmmo01 = Mathf.Clamp01(
                    (float)GameBindings.Player.Aircraft.Countermeasures.GetJammerAmmo() / 100f);
            if (InternalState.hasStations) { // WRITE WEAPON STATE ONLY IF THE PLAYER HAS WEAPON STATIONS
                InternalState.isOutOfAmmo = GameBindings.Player.Aircraft.Weapons.GetActiveStationAmmo() == 0;
                InternalState.reduceWeaponFontSize = GameBindings.Player.Aircraft.Weapons.GetActiveStationAmmoString().Contains("/");
                if (GameBindings.Player.Aircraft.Weapons.GetActiveStationReloadProgress() > 0f)
                    InternalState.isReloading = true;
                else
                    InternalState.isReloading = false;
            }
        }
    }

    public static class InternalState {
        static public WeaponDisplay weaponDisplay;
        static public bool hasJammer;
        static public bool hasIRFlare;
        static public bool hasStations;
        static public bool isOutOfAmmo;
        static public bool isFlareSelected;
        static public bool isJammerSelected;
        static public float flareAmmo01;
        static public float jammerAmmo01;
        static public bool reduceWeaponFontSize = false;
        static public bool isReloading = false;
        static public bool vanillaUIEnabled = true; // true by default since we need to check this value elsewhere
        static public Color mainColor = Color.green;
        static public Color textColor = Color.green;

    }

    static class DisplayEngine {
        static public void Init() {
            if (InternalState.hasIRFlare) { // In reality, this checks if the player's plane has spawned
                InternalState.weaponDisplay = new WeaponDisplay();
                if (!InternalState.vanillaUIEnabled) UIBindings.Game.HideWeaponPanel();
                else UIBindings.Game.ShowWeaponPanel();
            }
            Plugin.Log("[WD] Display Engine initialized for platform " + GameBindings.Player.Aircraft.GetPlatformName());
        }

        static public void Update() {
            if (GameBindings.GameState.IsGamePaused() ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetCombatHUDTransform() == null)
                return; // do not refresh anything if the game is paused or the player aircraft is not available
            // REFRESH WEAPON
            if (InternalState.hasStations) { // do not refresh weapon info if the player has no weapon stations
                InternalState.weaponDisplay.weaponNameLabel.SetText(GameBindings.Player.Aircraft.Weapons.GetActiveStationName());
                if (InternalState.isReloading)
                    InternalState.weaponDisplay.weaponAmmoLabel.SetText(((int)(GameBindings.Player.Aircraft.Weapons.GetActiveStationReloadProgress() * 100f)).ToString() + "%");
                else
                    InternalState.weaponDisplay.weaponAmmoLabel.SetText(GameBindings.Player.Aircraft.Weapons.GetActiveStationAmmoString().Replace(" ", ""));
                InternalState.weaponDisplay.weaponAmmoLabel.SetFontSize(
                    InternalState.weaponDisplay.originalWeaponAmmoFontSize + (InternalState.reduceWeaponFontSize ? -15 : 0));
                InternalState.weaponDisplay.weaponAmmoLabel.SetColor(InternalState.isOutOfAmmo ? Color.red : InternalState.textColor);

                Image cloneImg = InternalState.weaponDisplay.weaponImageClone.GetComponent<Image>();
                Image srcImg = GameBindings.Player.Aircraft.Weapons.GetActiveStationImage();
                cloneImg.sprite = srcImg.sprite;
                cloneImg.color = InternalState.isOutOfAmmo ? Color.red : InternalState.mainColor;
                // TODO : ENCAPSULATE IMAGES IN MY OWN CODE
            }
            // REFRESH FLARE (ALWAYS, BECAUSE EVERYONE HAS FLARES   )
            InternalState.weaponDisplay.flareLabel.SetText("IR:" + GameBindings.Player.Aircraft.Countermeasures.GetIRFlareAmmo().ToString());
            InternalState.weaponDisplay.flareLabel.SetFontStyle(InternalState.isFlareSelected ? FontStyle.Bold : FontStyle.Normal);
            InternalState.weaponDisplay.flareLabel.SetFontSize(InternalState.weaponDisplay.originalFlareFontSize + (InternalState.isFlareSelected ? 10 : 0));
            InternalState.weaponDisplay.flareLabel.SetColor(Color.Lerp(Color.red, InternalState.textColor, InternalState.flareAmmo01));
            // REFRESH JAMMER
            if (InternalState.hasJammer) {
                InternalState.weaponDisplay.jammerLabel.SetText("EW:" + GameBindings.Player.Aircraft.Countermeasures.GetJammerAmmo().ToString() + "%");
                InternalState.weaponDisplay.jammerLabel.SetFontStyle(InternalState.isJammerSelected ? FontStyle.Bold : FontStyle.Normal);
                InternalState.weaponDisplay.jammerLabel.SetFontSize(InternalState.weaponDisplay.originalJammerFontSize + (InternalState.isJammerSelected ? 10 : 0)); ;
                InternalState.weaponDisplay.jammerLabel.SetColor(Color.Lerp(Color.red, InternalState.textColor, InternalState.jammerAmmo01));
            }
        }
    }

    public class WeaponDisplay {
        public Transform weaponDisplay_transform;
        public UIBindings.Draw.UILabel flareLabel;
        public UIBindings.Draw.UILabel jammerLabel;
        public UIBindings.Draw.UILine MFD_systemsLine;
        public UIBindings.Draw.UILabel weaponNameLabel;
        public UIBindings.Draw.UILabel weaponAmmoLabel;
        public GameObject weaponImageClone;
        // Store original font sizes
        public int originalFlareFontSize;
        public int originalJammerFontSize;
        public int originalWeaponAmmoFontSize;
        //Store the main color for the MFD, can be set by the MFDColorPlugin
        public bool removeOriginalMFDContent = true; // by default, we remove the original MFD content


        public WeaponDisplay() {
            static Transform Get(string path) {
                return UIBindings.Game.GetTacScreenTransform().Find(path)?.transform;
            }
            string platformName = GameBindings.Player.Aircraft.GetPlatformName();
            Transform destination = platformName switch {
                "T/A-30 Compass" or "FS-12 Revoker" or
                "FS-20 Vortex" or "KR-67 Ifrit" or
                "UH-80 Ibis" or "A-19 Brawler"
                or "FQ-106 Kestrel" or "FS-3 Ternion" => Get("SystemStatus"),
                "EW-1 Medusa" => Get("engPanel1"),
                "CI-22 Cricket" => Get("EngPanel"),
                "SAH-46 Chicane" => Get("BasicFlightInstrument"),
                "VL-49 Tarantula" => Get("RightScreenBorder/WeaponPanel"),
                "SFB-81" => Get("weaponPanel"),
                _ => null
            };
            weaponDisplay_transform = destination;
            // Default settings for the weapon display
            bool rotateWeaponImage = false;
            float imageScaleFactor = 0.6f;
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
                    flarePos = new Vector2(-40, -105);
                    jammerPos = new Vector2(40, -105);
                    lineStart = new Vector2(-80, -10);
                    lineEnd = new Vector2(80, -10);
                    weaponNamePos = new Vector2(0, -45);
                    weaponAmmoPos = new Vector2(0, -70);
                    weaponImagePos = new Vector2(0, -25);
                    flareFont = 18;
                    jammerFont = 18;
                    weaponNameFont = 20;
                    weaponAmmoFont = 35;
                    imageScaleFactor = 0.4f; // Scale the image for SAH-46 Chicane
                    removeOriginalMFDContent = false; // Do not remove original MFD content for SAH-46 Chicane
                    rotateWeaponImage = false;
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
                case "FS-3 Ternion":
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
                    flarePos = new Vector2(-60, -70);
                    jammerPos = new Vector2(60, -70);
                    lineStart = new Vector2(-120, -20);
                    lineEnd = new Vector2(120, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(60, 20);
                    weaponImagePos = new Vector2(-60, 20);
                    flareFont = 35;
                    jammerFont = 35;
                    weaponNameFont = 30;
                    weaponAmmoFont = 55;
                    imageScaleFactor = 0.7f; // Scale the image for FS-20 Vortex
                    break;
                case "KR-67 Ifrit":
                    flarePos = new Vector2(-80, -70);
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
                    flarePos = new Vector2(60, -40);
                    jammerPos = new Vector2(60, -80);
                    lineStart = new Vector2(20, 0);
                    lineEnd = new Vector2(100, 0);
                    weaponNamePos = new Vector2(60, 80);
                    weaponAmmoPos = new Vector2(60, 40);
                    weaponImagePos = new Vector2(-60, 0);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 25;
                    weaponAmmoFont = 40;
                    rotateWeaponImage = true; // Rotate the weapon image for SFB-81
                    imageScaleFactor = 0.8f; // Scale the image for SFB-81
                    break;
                case "UH-80 Ibis":
                    flarePos = new Vector2(-60, -70);
                    jammerPos = new Vector2(60, -70);
                    lineStart = new Vector2(-100, -20);
                    lineEnd = new Vector2(100, -20);
                    weaponNamePos = new Vector2(0, 70);
                    weaponAmmoPos = new Vector2(80, 20);
                    weaponImagePos = new Vector2(-60, 20);
                    flareFont = 35;
                    jammerFont = 35;
                    weaponNameFont = 35;
                    weaponAmmoFont = 50;
                    imageScaleFactor = 0.6f; // Scale the image for EW-1 Medusa
                    break;
                case "A-19 Brawler":
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -75);
                    lineStart = new Vector2(-80, -10);
                    lineEnd = new Vector2(80, -10);
                    weaponNamePos = new Vector2(0, 45);
                    weaponAmmoPos = new Vector2(0, 15);
                    weaponImagePos = new Vector2(0, 75);
                    flareFont = 30;
                    jammerFont = 30;
                    weaponNameFont = 25;
                    weaponAmmoFont = 40;
                    break;
                case "FQ-106 Kestrel":
                    flarePos = new Vector2(0, -40);
                    jammerPos = new Vector2(0, -80);
                    lineStart = new Vector2(-60, -7);
                    lineEnd = new Vector2(60, -7);
                    weaponNamePos = new Vector2(0, 60);
                    weaponAmmoPos = new Vector2(0, 22);
                    weaponImagePos = new Vector2(0, 87);
                    flareFont = 35;
                    jammerFont = 35;
                    weaponNameFont = 25;
                    weaponAmmoFont = 45;
                    break;
                default:
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
                    weaponAmmoFont = 30;
                    break;
            }
            // Store original font sizes
            originalFlareFontSize = flareFont;
            originalJammerFontSize = jammerFont;
            originalWeaponAmmoFontSize = weaponAmmoFont;

            // Hide the existing MFD content and kill the layout
            if (removeOriginalMFDContent) {
                UIBindings.Generic.HideChildren(destination);
            }
            UIBindings.Generic.KillLayout(destination);
            // rotate the destination canvas 90 degrees clockwise if Darkreach
            if (platformName == "SFB-81") {
                destination.localRotation = Quaternion.Euler(0, 0, -90);
                destination.GetComponent<Image>().enabled = false; // hide the background image
            }
            // move the BasicFlightInstruments higher on Chicane screen
            if (platformName == "SAH-46 Chicane") {
                Transform toMove;
                toMove = destination.Find("Heading");
                toMove.transform.localPosition += new Vector3(-40, 40, 0);
                toMove.transform.localScale *= 0.8f;
                toMove = destination.Find("Airspeed");
                toMove.transform.localPosition += new Vector3(40, 60, 0);
                toMove.transform.localScale *= 0.8f;
                toMove = destination.Find("RadarAlt");
                toMove.transform.localPosition += new Vector3(-40, 80, 0);
                toMove.transform.localScale *= 0.8f;
                toMove = destination.Find("Horizon");
                toMove.transform.localPosition += new Vector3(0, 60, 0);
                toMove.transform.localScale *= 0.8f;
                toMove = destination.Find("ClimbRate");
                toMove.transform.localPosition += new Vector3(40, 60, 0);
                toMove.transform.localScale *= 0.8f;
                toMove = destination.Find("VerticalLadder");
                toMove.transform.localPosition += new Vector3(0, 55, 0);
                toMove.transform.localScale *= 0.8f;
                toMove = destination.Find("AoAlLadder");
                toMove.transform.localPosition += new Vector3(0, 55, 0);
                toMove.transform.localScale *= 0.8f;
            }

            // Create the labels and line for the systems MFD
            flareLabel = new(
                "flareLabel",
                flarePos,
                destination,
                FontStyle.Normal,
                InternalState.textColor,
                flareFont,
                0f
            );
            flareLabel.SetText("");
            jammerLabel = new(
                "radarLabel",
                jammerPos,
                destination,
                FontStyle.Normal,
                InternalState.textColor,
                jammerFont,
                0f
            );
            jammerLabel.SetText("⇌");
            MFD_systemsLine = new(
                "MFD_systemsLine",
                lineStart,
                lineEnd,
                destination,
                InternalState.mainColor,
                1f
            );
            weaponNameLabel = new(
                "weaponNameLabel",
                weaponNamePos,
                destination,
                FontStyle.Normal,
                InternalState.textColor,
                weaponNameFont,
                0f
            );
            weaponNameLabel.SetText("");
            weaponAmmoLabel = new(
                "weaponAmmoLabel",
                weaponAmmoPos,
                destination,
                FontStyle.Normal,
                InternalState.textColor,
                weaponAmmoFont,
                0f
            );
            weaponAmmoLabel.SetText("");
            // Clone the weapon image and set it as a child of the systems MFD
            if (GameBindings.Player.Aircraft.Weapons.GetStationCount() != 0)
                weaponImageClone = GameObject.Instantiate(GameBindings.Player.Aircraft.Weapons.GetActiveStationImage().gameObject, destination);
            else
                weaponImageClone = new UIBindings.Draw.UIRectangle(
                    "empty_texture",
                    new Vector2(0, 0),
                    new Vector2(1, 1),
                    destination,
                    Color.black
                ).GetGameObject();
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
            if (GameBindings.Player.Aircraft.GetPlatformName() == "SFB-81") {
                if (weaponDisplay_transform.localRotation.eulerAngles.z == 0) {
                    weaponDisplay_transform.localRotation = Quaternion.Euler(0, 0, -90);
                    weaponDisplay_transform.GetComponent<Image>().enabled = false;
                }
                else {
                    weaponDisplay_transform.localRotation = Quaternion.Euler(0, 0, 0);
                    weaponDisplay_transform.GetComponent<Image>().enabled = true;
                }
            }
            LayoutGroup lg = weaponDisplay_transform.GetComponent<LayoutGroup>();
            if (lg != null)
                lg.enabled = !lg.enabled;
            foreach (Transform childTransform in weaponDisplay_transform) {
                GameObject child = childTransform.gameObject;
                //Specific fix for the Medusa, ThrottleGauge1 was initially hidden
                if (child.name != "ThrottleGauge1") {
                    child.SetActive(!child.activeSelf);
                }
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


