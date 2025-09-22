using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Controls;
using NO_Tactitools.UI;

namespace NO_Tactitools.Core {
    [BepInPlugin("NO_Tactitools", "NOTT", "0.2.1")]
    public class Plugin : BaseUnityPlugin {
        public static Harmony harmony;
        public static ConfigEntry<bool> targetRecallEnabled;
        public static ConfigEntry<string> targetRecallControllerName;
        public static ConfigEntry<int> targetRecallButtonNumber;
        public static ConfigEntry<bool> interceptionVectorEnabled;
        public static ConfigEntry<bool> onScreenVectorEnabled;
        public static ConfigEntry<bool> countermeasureControlsEnabled;
        public static ConfigEntry<string> countermeasureControlsFlareControllerName;
        public static ConfigEntry<int> countermeasureControlsFlareButtonNumber;
        public static ConfigEntry<string> countermeasureControlsJammerControllerName;
        public static ConfigEntry<int> countermeasureControlsJammerButtonNumber;
        public static ConfigEntry<bool> weaponSwitcherEnabled;
        public static ConfigEntry<string> weaponSwitcherControllerName;
        public static ConfigEntry<int> weaponSwitcherButton0;
        public static ConfigEntry<int> weaponSwitcherButton1;
        public static ConfigEntry<int> weaponSwitcherButton2;
        public static ConfigEntry<int> weaponSwitcherButton3;
        public static ConfigEntry<int> weaponSwitcherButton4;
        public static ConfigEntry<bool> weaponDisplayEnabled;
        public static ConfigEntry<bool> weaponDisplayVanillaUIEnabled;
        public static ConfigEntry<string> weaponDisplayControllerName;
        public static ConfigEntry<int> weaponDisplayButtonNumber;
        public static ConfigEntry<bool> unitDistanceEnabled;
        public static ConfigEntry<int> unitDistanceThreshold;
        public static ConfigEntry<bool> unitDistanceSoundEnabled;
        public static ConfigEntry<bool> deliveryCheckerEnabled;
        public static ConfigEntry<bool> MFDColorEnabled;
        public static ConfigEntry<Color> MFDColor;
        public static ConfigEntry<bool> MFDAlternativeAttitudeEnabled;
        public static ConfigEntry<bool> unitIconRecolorEnabled;
        public static ConfigEntry<bool> bootScreenEnabled;
        public static ConfigEntry<bool> debugModeEnabled;
        internal static new ManualLogSource Logger;

        private void Awake() {
            // Target Recall settings
            targetRecallEnabled = Config.Bind("Target Recall", //Category
                "Target Recall Enabled", // Setting name
                true, // Default value
                "Enable or disable the Target Recall feature"); // Description of the setting
            targetRecallControllerName = Config.Bind("Target Recall",
                "Target Recall Controller Name",
                "",
                "Name of the peripheral");
            targetRecallButtonNumber = Config.Bind("Target Recall",
                "Target Recall Button Number",
                37,
                "Number of the button");
            // Interception Vector settings
            interceptionVectorEnabled = Config.Bind("Interception Vector",
                "Interception Vector Enabled",
                true,
                "Enable or disable the Interception Vector feature");
            // Countermeasure Controls settings
            countermeasureControlsEnabled = Config.Bind("Countermeasures",
                "Countermeasure Controls Enabled",
                true,
                "Enable or disable the Countermeasure Controls feature");
            countermeasureControlsFlareControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls Controller Name",
                "",
                "Name of the peripheral");
            countermeasureControlsFlareButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Button Number",
                39,
                "Number of the button");
            countermeasureControlsJammerControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls Controller Name",
                "",
                "Name of the peripheral");
            countermeasureControlsJammerButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Button Number",
                40,
                "Number of the button");
            // Weapon Switcher settings
            weaponSwitcherEnabled = Config.Bind("Weapon Switcher",
                "Weapon Switcher Enabled",
                true,
                "Enable or disable the Weapon Switcher feature");
            weaponSwitcherControllerName = Config.Bind("Weapon Switcher",
                "Weapon Switcher Controller Name",
                "",
                "Name of the peripheral for weapon switching");
            weaponSwitcherButton0 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 0",
                41,
                "Button number for weapon slot 0 (Long press to toggle Turret Auto Control)");
            weaponSwitcherButton1 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 1",
                42,
                "Button number for weapon slot 1");
            weaponSwitcherButton2 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 2",
                43,
                "Button number for weapon slot 2");
            weaponSwitcherButton3 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 3",
                44,
                "Button number for weapon slot 3");
            weaponSwitcherButton4 = Config.Bind("Weapon Switcher",
                "Weapon Switcher Button 4",
                45,
                "Button number for weapon slot 4");
            // Weapon Display settings
            weaponDisplayEnabled = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display Enabled",
                true,
                "Enable or disable the Weapon Display feature");
            weaponDisplayVanillaUIEnabled = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display Vanilla UI Enabled",
                false,
                "Enable or disable the vanilla UI for weapon & CM display while the mod is active");
            weaponDisplayControllerName = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display Controller Name",
                "",
                "Name of the peripheral for weapon display");
            weaponDisplayButtonNumber = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display Button Number",
                46,
                "Button number for swapping between weapon display modes (modded or vanilla content)");
            // Unit Distance settings
            unitDistanceEnabled = Config.Bind("HMD Tweaks",
                "Unit Marker Distance Indicator Enabled",
                true,
                "Enable or disable the Unit Marker Distance Indicator feature");
            unitDistanceThreshold = Config.Bind("HMD Tweaks",
                "Unit Marker Distance Indicator Threshold",
                10,
                new ConfigDescription(
                    "Distance threshold in meters for the Unit Marker Distance Indicator to change the marker orientation.",
                    new AcceptableValueRange<int>(5, 50)));
            unitDistanceSoundEnabled = Config.Bind("HMD Tweaks",
                "Unit Marker Distance Sound Enabled",
                true,
                "Enable or disable the sound when the Unit Marker Distance Indicator is in \'near\' state.");
            // Delivery Checker settings
            deliveryCheckerEnabled = Config.Bind("Delivery Checker",
                "Delivery Checker Enabled",
                true,
                "Enable or disable the Delivery Checker feature");
            // MFD Color settings
            MFDColorEnabled = Config.Bind("MFD Color",
                "MFD Color Enabled",
                true,
                "Enable or disable the MFD Color feature");
            MFDColor = Config.Bind("MFD Color",
                "MFD Main Color",
                new Color(0f, 1f, 0f), // Default color in RGB
                "Main color for the MFD in RGB format. This will be used to set the MFD main color.");
            MFDAlternativeAttitudeEnabled = Config.Bind("MFD Color",
                "MFD Alternative Attitude Enabled",
                false,
                "Enable or disable the alternative attitude colors for the MFD horizon and ground indicators.");
            // PREVIEW FEATURES
            // Unit Icon Recolor settings
            unitIconRecolorEnabled = Config.Bind("PREVIEW - EXPECT BUGS",
                "Unit Icon Recolor Enabled",
                false,
                "Enable or disable the Unit Icon Recolor feature");
            // Boot Screen settings
            bootScreenEnabled = Config.Bind("PREVIEW - EXPECT BUGS",
                "Boot Screen Enabled",
                false,
                "Enable or disable the Boot Screen feature");
            // Debug Mode settings
            debugModeEnabled = Config.Bind("Debug Mode",
                "Debug Mode Enabled",
                true,
                "Enable or disable the debug mode for logging");
            // Plugin startup logic
            harmony = new Harmony("george.no_tactitools");
            Logger = base.Logger;
            // Patch Input Catcher - We do the patches manually here instead of in the "master class" like the other plugins because Rewired is a bit special as my mother would say
            harmony.PatchAll(typeof(InputInterceptionPatch));
            harmony.PatchAll(typeof(RegisterControllerPatch));
            // Patch Interception Vector
            if (interceptionVectorEnabled.Value) {
                Logger.LogInfo($"Interception Vector is enabled, patching...");
                harmony.PatchAll(typeof(InterceptionVectorPlugin));
            }
            // Patch Target Recall
            if (targetRecallEnabled.Value) {
                Logger.LogInfo($"Target Recall is enabled, patching...");
                harmony.PatchAll(typeof(TargetRecallPlugin));
            }
            // Patch Countermeasure Controls
            if (countermeasureControlsEnabled.Value) {
                Logger.LogInfo($"Countermeasure Controls is enabled, patching...");
                harmony.PatchAll(typeof(CountermeasureControlsPlugin));
            }
            // Patch Weapon Switcher
            if (weaponSwitcherEnabled.Value) {
                Logger.LogInfo($"Weapon Switcher is enabled, patching...");
                harmony.PatchAll(typeof(WeaponSwitcherPlugin));
            }
            // Patch Weapon Display
            if (weaponDisplayEnabled.Value) {
                Logger.LogInfo($"Weapon Display is enabled, patching...");
                harmony.PatchAll(typeof(WeaponDisplayPlugin));
            }
            // Patch Unit Distance
            if (unitDistanceEnabled.Value) {
                Logger.LogInfo($"Unit Marker Distance Indicator is enabled, patching...");
                harmony.PatchAll(typeof(UnitDistancePlugin));
            }
            // Patch Delivery Checker
            if (deliveryCheckerEnabled.Value) {
                Logger.LogInfo($"Delivery Checker is enabled, patching...");
                harmony.PatchAll(typeof(DeliveryCheckerPlugin));
            }
            // Patch MFD Color
            if (MFDColorEnabled.Value) {
                Logger.LogInfo($"MFD Color is enabled, patching...");
                harmony.PatchAll(typeof(MFDColorPlugin));
            }
            // Patch Unit Icon Recolor
            if (unitIconRecolorEnabled.Value) {
                Logger.LogInfo($"Unit Icon Recolor is enabled, patching...");
                harmony.PatchAll(typeof(UnitIconRecolorPlugin));
            }
            // Patch Boot Screen
            if (bootScreenEnabled.Value) {
                Logger.LogInfo($"Boot Screen is enabled, patching...");
                harmony.PatchAll(typeof(BootScreenPlugin));
            }
        }

        public static void Log(string message) {
            if (debugModeEnabled.Value)
                Logger.LogInfo(message);
        }
    }
}
