using NO_Tactitools.Core;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Events;
using NO_Tactitools.Core.Inputs;
using UnityEngine;
using UnityEngine.UI;

namespace NO_Tactitools.Modules.UI.MFD;

internal class WeaponDisplayModule : Module {
    public WeaponDisplayModule(Plugin pluginInstance) : base(
        pluginInstance,
        "Weapon Display",
        ModuleInitType.TacScreen,
        ModuleUpdateType.TacScreen) {
        // Register new configs
        // Vanilla UI Enabled
        AddNewConfigEntry(
            key: "Vanilla UI Enabled",
            defaultValue: false,
            description: "Enable or disable the vanilla weapon display UI when using the weapon display feature.",
            acceptableValues: null,
            new ConfigurationManagerAttributes { Order = 0 }
        );
        // Add new inputs
        InputCatcher.RegisterNewInput(
            AddNewInputConfig(
                "Toggle Screens",
                "Press to toggle."
            ),
            onLongPress: HandleDisplayToggle
        );
    }

    private static void HandleDisplayToggle() {
    }

    protected class WeaponDisplayDrawableElement : DrawableElement {
        // UI Elements
        public readonly UIBindings.Draw.UILabel flareLabel;
        public readonly UIBindings.Draw.UILabel jammerLabel;
        public UIBindings.Draw.UILine mfdSystemsLine;
        public readonly UIBindings.Draw.UILabel weaponNameLabel;
        public readonly UIBindings.Draw.UILabel weaponAmmoLabel;

        public readonly GameObject weaponImageClone;

        // by default, we remove the original MFD content
        public readonly bool removeOriginalContent = true;

        // Store original font sizes
        public int originalFlareFontSize;
        public int originalJammerFontSize;
        public int originalWeaponAmmoFontSize;

        public WeaponDisplayDrawableElement() : base(
            parentTransform: Destination(GameBindings.Player.Aircraft.GetPlatformName()),
            drawableName: "Weapon Display"
        ) {
            string platformName = GameBindings.Player.Aircraft.GetPlatformName();
            Transform destination = gameObject.transform;
            // Default settings for the weapon display
            bool rotateWeaponImage = false;
            float imageScaleFactor = 0.6f;
            Vector2 flarePos,
                jammerPos,
                lineStart,
                lineEnd,
                weaponNamePos,
                weaponAmmoPos,
                weaponImagePos;
            int flareFont,
                jammerFont,
                weaponNameFont,
                weaponAmmoFont;
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
                    removeOriginalContent = false; // Do not remove original MFD content for SAH-46 Chicane
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
                    lineStart = new Vector2(30, -60);
                    lineEnd = new Vector2(30, 60);
                    weaponNamePos = new Vector2(-60, -10);
                    weaponAmmoPos = new Vector2(-60, -50);
                    weaponImagePos = new Vector2(-60, 40);
                    flareFont = 25;
                    jammerFont = 25;
                    weaponNameFont = 18;
                    weaponAmmoFont = 40;
                    imageScaleFactor = 0.6f;
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

            // Remove original content if needed
            if (removeOriginalContent) {
                UIBindings.Generic.HideChildren(destination);
            }

            // Remove layout groups
            UIBindings.Generic.KillLayoutGroup(destination);
            // Platform specific patches
            switch (platformName) {
                // rotate the destination canvas 90 degrees clockwise if Darkreach
                case "SFB-81":
                    destination.localRotation = Quaternion.Euler(0, 0, -90);
                    destination.GetComponent<Image>().enabled = false; // hide the background image
                    break;
                // move the BasicFlightInstruments higher on Chicane screen
                case "SAH-46 Chicane": {
                    Transform toMove = destination.Find("Heading");
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
                    break;
                }
            }

            // Create the two colors
            Color textColor = new Color(r: 0, g: 1, b: 0, a: (float)0.8);
            Color mainColor = new Color(r: 0, g: 1, b: 0, a: (float)0.8);
            // Create the labels and line for the systems MFD
            flareLabel = new(
                "flareLabel",
                flarePos,
                destination,
                FontStyle.Normal,
                textColor,
                flareFont,
                0f
            );
            flareLabel.SetText("");
            jammerLabel = new(
                "radarLabel",
                jammerPos,
                destination,
                FontStyle.Normal,
                textColor,
                jammerFont,
                0f
            );
            jammerLabel.SetText("⇌");
            mfdSystemsLine = new(
                "MFD_systemsLine",
                lineStart,
                lineEnd,
                destination,
                mainColor,
                1f
            );
            weaponNameLabel = new(
                "weaponNameLabel",
                weaponNamePos,
                destination,
                FontStyle.Normal,
                textColor,
                weaponNameFont,
                0f
            );
            weaponNameLabel.SetText("");
            weaponAmmoLabel = new(
                "weaponAmmoLabel",
                weaponAmmoPos,
                destination,
                FontStyle.Normal,
                textColor,
                weaponAmmoFont,
                0f
            );
            weaponAmmoLabel.SetText("");
            if (GameBindings.Player.Aircraft.Weapons.GetStationCount() != 0)
                weaponImageClone =
                    Object.Instantiate(GameBindings.Player.Aircraft.Weapons.GetActiveStationImage().gameObject,
                        destination);
            else
                weaponImageClone = new UIBindings.Draw.UIRectangle(
                    "empty_texture",
                    new Vector2(0, 0),
                    new Vector2(1, 1),
                    destination,
                    Color.black
                ).GetGameObject();
            Image cloneImg = weaponImageClone.GetComponent<Image>();
            cloneImg.rectTransform.sizeDelta = new Vector2(
                cloneImg.rectTransform.sizeDelta.x * imageScaleFactor,
                cloneImg.rectTransform.sizeDelta.y * imageScaleFactor);
            cloneImg.rectTransform.anchoredPosition = weaponImagePos;
            //rotate the image 90 degrees clockwise
            if (rotateWeaponImage)
                cloneImg.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
        }

        public void ToggleChildrenActiveState() {
            if (gameObject == null) return;
            if (GameBindings.Player.Aircraft.GetPlatformName() == "SFB-81") {
                if (gameObject.transform.localRotation.eulerAngles.z == 0) {
                    gameObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
                    gameObject.transform.GetComponent<Image>().enabled = false;
                }
                else {
                    gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    gameObject.transform.GetComponent<Image>().enabled = true;
                }
            }

            LayoutGroup lg = gameObject.transform.GetComponent<LayoutGroup>();
            if (lg != null)
                lg.enabled = !lg.enabled;
            foreach (Transform childTransform in gameObject.transform) {
                GameObject child = childTransform.gameObject;
                //Specific fix for the Medusa, ThrottleGauge1 was initially hidden
                if (child.name != "ThrottleGauge1") {
                    child.SetActive(!child.activeSelf);
                }
            }
        }

        private static Transform Destination(string platformName) {
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
            return destination;

            static Transform Get(string path) {
                return UIBindings.Game.GetTacScreenTransform().Find(path)?.transform;
            }
        }
    }

    protected override void OnInit(object sender, ModEventArgs modEventArgs) {
        // Call base method
        base.OnInit(sender, modEventArgs);
        // TODO - Implement Init Logic
        Plugin.Log("[WD] Initializing for platform "
                   + GameBindings.Player.Aircraft.GetPlatformName());
    }

    protected override void OnUpdate(object sender, ModEventArgs modEventArgs) {
        // Call base method
        base.OnUpdate(sender, modEventArgs);
        // TODO - Implement Update Logic
    }
}