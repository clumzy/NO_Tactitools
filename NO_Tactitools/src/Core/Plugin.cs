using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Controls;
using NO_Tactitools.UI.HMD;
using NO_Tactitools.UI.MFD;

namespace NO_Tactitools.Core {
    [BepInPlugin("NO_Tactitools", "NOTT", "0.5.0")]
    public class Plugin : BaseUnityPlugin {
        public static Harmony harmony;
        public static ConfigEntry<bool> targetListControllerEnabled;
        public static ConfigEntry<string> targetRecallControllerName;
        public static ConfigEntry<string> targetRecallInput;
        public static ConfigEntry<string> targetNextControllerName;
        public static ConfigEntry<string> targetNextInput;
        public static ConfigEntry<string> targetPreviousControllerName;
        public static ConfigEntry<string> targetPreviousInput;
        public static ConfigEntry<string> targetPopOrKeepControllerName;
        public static ConfigEntry<string> targetPopOrKeepInput;
        public static ConfigEntry<string> targetSmartControlControllerName;
        public static ConfigEntry<string> targetSmartControlInput;
        public static ConfigEntry<bool> interceptionVectorEnabled;
        public static ConfigEntry<bool> onScreenVectorEnabled;
        public static ConfigEntry<bool> countermeasureControlsEnabled;
        public static ConfigEntry<string> countermeasureControlsFlareControllerName;
        public static ConfigEntry<string> countermeasureControlsFlareButtonNumber;
        public static ConfigEntry<string> countermeasureControlsJammerControllerName;
        public static ConfigEntry<string> countermeasureControlsJammerButtonNumber;
        public static ConfigEntry<bool> weaponSwitcherEnabled;
        public static ConfigEntry<string> weaponSwitcherControllerName;
        public static ConfigEntry<string> weaponSwitcherButton0;
        public static ConfigEntry<string> weaponSwitcherButton1;
        public static ConfigEntry<string> weaponSwitcherButton2;
        public static ConfigEntry<string> weaponSwitcherButton3;
        public static ConfigEntry<string> weaponSwitcherButton4;
        public static ConfigEntry<string> weaponSwitcherButton5;
        public static ConfigEntry<bool> weaponDisplayEnabled;
        public static ConfigEntry<bool> weaponDisplayVanillaUIEnabled;
        public static ConfigEntry<string> weaponDisplayControllerName;
        public static ConfigEntry<string> weaponDisplayButtonNumber;
        public static ConfigEntry<bool> unitDistanceEnabled;
        public static ConfigEntry<int> unitDistanceThreshold;
        public static ConfigEntry<bool> unitDistanceSoundEnabled;
        public static ConfigEntry<bool> deliveryCheckerEnabled;
        public static ConfigEntry<bool> MFDColorEnabled;
        public static ConfigEntry<Color> MFDColor;
        public static ConfigEntry<Color> MFDTextColor;
        public static ConfigEntry<bool> MFDAlternativeAttitudeEnabled;
        public static ConfigEntry<bool> unitIconRecolorEnabled;
        public static ConfigEntry<Color> unitIconRecolorEnemyColor;
        public static ConfigEntry<bool> bootScreenEnabled;
        public static ConfigEntry<bool> artificialHorizonEnabled;
        public static ConfigEntry<float> artificialHorizonTransparency;
        public static ConfigEntry<bool> loadoutPreviewEnabled;
        public static ConfigEntry<bool> loadoutPreviewSendToHMD;
        public static ConfigEntry<bool> loadoutPreviewOnlyShowOnBoot;
        public static ConfigEntry<float> loadoutPreviewDuration;
        public static ConfigEntry<bool> debugModeEnabled;
        internal static new ManualLogSource Logger;
        public static Plugin Instance;

        private void Awake() {
            Instance = this;
            // Target Recall settings
            targetListControllerEnabled = Config.Bind("Target List Controller", //Category
                "Target List Controller - Enabled", // Setting name
                true, // Default value
                new ConfigDescription(
                    "Enable or disable the Target Recall feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2})); // Description of the setting
            targetRecallControllerName = Config.Bind("Target List Controller",
                "Target List Controller - Target Recall - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            targetRecallInput = Config.Bind("Target List Controller",
                "Target List Controller - Target Recall - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for Target Recall (short press to recall, long press to save)",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            targetNextControllerName = Config.Bind("Target List Controller",
                "Target List Controller - Next Target - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -1
                    }));
            targetNextInput = Config.Bind("Target List Controller",
                "Target List Controller - Next Target - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for Next Target",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -2
                    }));
            targetPreviousControllerName = Config.Bind("Target List Controller",
                "Target List Controller - Previous Target - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -3
                    }));
            targetPreviousInput = Config.Bind("Target List Controller",
                "Target List Controller - Previous Target - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for Previous Target",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -4
                    }));
            targetPopOrKeepControllerName = Config.Bind("Target List Controller",
                "Target List Controller - Remove or Keep Target - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -5
                    }));
            targetPopOrKeepInput = Config.Bind("Target List Controller",
                "Target List Controller - Remove or Keep Target - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for Remove or Keep Target (short press to remove, long press to keep)",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -6
                    }));
            targetSmartControlControllerName = Config.Bind("Target List Controller",
                "Target List Controller - Smart Control - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -9
                    }));
            targetSmartControlInput = Config.Bind("Target List Controller",
                "Target List Controller - Smart Control - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for Smart Control (short press to keep only datalinked targets, long press to keep closest targets based on remaining ammo)",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -10
                    }));
            // Interception Vector settings
            interceptionVectorEnabled = Config.Bind("Interception Vector",
                "Interception Vector - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Interception Vector feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Countermeasure Controls settings
            countermeasureControlsEnabled = Config.Bind("Countermeasures",
                "Countermeasure Controls - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Countermeasure Controls feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 4
                    }));
            countermeasureControlsFlareControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3
                    }));
            countermeasureControlsFlareButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for selecting Flares",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            countermeasureControlsJammerControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            countermeasureControlsJammerButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for selecting Jammer",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Weapon Switcher settings
            weaponSwitcherEnabled = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Advanced Slot Selection feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 7
                    }));
            weaponSwitcherControllerName = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 6
                    }));
            weaponSwitcherButton0 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 0 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 0 (Long press to toggle Turret Auto Control)",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 5
                    }));
            weaponSwitcherButton1 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 1 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 1",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 4
                    }));
            weaponSwitcherButton2 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 2 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 2",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3
                    }));
            weaponSwitcherButton3 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 3 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 3",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            weaponSwitcherButton4 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 4 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 4",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            weaponSwitcherButton5 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 5 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 5",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Weapon Display settings
            weaponDisplayEnabled = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the CM & Weapon Display feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3
                    }));
            weaponDisplayVanillaUIEnabled = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display - Vanilla UI - Enabled",
                false,
                new ConfigDescription(
                    "Enable or disable the vanilla weapon display UI when using the weapon display feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            weaponDisplayControllerName = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display - Content Toggling - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            weaponDisplayButtonNumber = Config.Bind("CM & Weapon Display",
                "CM & Weapon Display - Content Toggling - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for toggling the weapon display back to its original content",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Unit Distance settings
            unitDistanceEnabled = Config.Bind("HMD Tweaks",
                "Unit Marker Distance Indicator - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Unit Marker Distance Indicator feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            unitDistanceThreshold = Config.Bind("HMD Tweaks",
                "Unit Marker Distance Indicator - Threshold",
                10,
                new ConfigDescription(
                    "Distance threshold in kilometers for the Unit Marker Distance Indicator to change the marker's orientation.",
                    new AcceptableValueRange<int>(5, 50),
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            unitDistanceSoundEnabled = Config.Bind("HMD Tweaks",
                "Unit Marker Distance Sound - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the sound notification indicating that an enemy unit has crossed the distance threshold.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Delivery Checker settings
            deliveryCheckerEnabled = Config.Bind("Delivery Checker",
                "Delivery Checker - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Delivery Checker feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // MFD Color settings
            MFDColorEnabled = Config.Bind("MFD Color",
                "MFD Color - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the MFD Color feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3
                    }));
            MFDColor = Config.Bind("MFD Color",
                "MFD Color - MFD Main Color",
                new Color(0f, 1f, 0f), // Default color in RGB
                new ConfigDescription(
                    "Main color for the MFD elements in RGB format.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            MFDTextColor = Config.Bind("MFD Color",
                "MFD Color - MFD Text Color",
                new Color(0f, 1f, 0f), // Default color in RGB
                new ConfigDescription(
                    "Color for the MFD text elements in RGB format.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            MFDAlternativeAttitudeEnabled = Config.Bind("MFD Color",
                "MFD Color - MFD Alternative Attitude - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the alternative attitude indicator color on the MFD.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Unit Icon Recolor settings
            unitIconRecolorEnabled = Config.Bind("AA Units Icon Recolor",
                "AA Units Icon Recolor - Enabled",
                false,
                new ConfigDescription(
                    "Enable or disable the AA Units Icon Recolor feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            unitIconRecolorEnemyColor = Config.Bind("AA Units Icon Recolor",
                "AA Units Icon Recolor - Enemy Unit Color",
                new Color(0.8f, 0.2f, 1f),
                new ConfigDescription(
                    "Color for enemy AA unit icons in RGB format.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Boot Screen settings
            bootScreenEnabled = Config.Bind("Boot Screen Animation",
                "Boot Screen Animation - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Boot Screen Animation feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Artificial Horizon settings
            artificialHorizonEnabled = Config.Bind("Artificial Horizon",
                "Artificial Horizon - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Artificial Horizon feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            artificialHorizonTransparency = Config.Bind("Artificial Horizon",
                "Artificial Horizon - Transparency",
                0.4f,
                new ConfigDescription(
                    "Transparency level for the Artificial Horizon display (0.2 = almost transparent, 1 = fully opaque).",
                    new AcceptableValueRange<float>(0.2f, 1f),
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Loadout Preview settings
            loadoutPreviewEnabled = Config.Bind("Loadout Preview",
                "Loadout Preview - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Loadout Preview feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3
                    }));
            loadoutPreviewSendToHMD = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD",
                false,
                new ConfigDescription(
                    "If enabled, the loadout preview will also be sent to the HMD display.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            loadoutPreviewOnlyShowOnBoot = Config.Bind("Loadout Preview",
                "Loadout Preview - Only Show On Boot",
                false,
                new ConfigDescription(
                    "If enabled, the loadout preview will only be shown on aircraft startup.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            loadoutPreviewDuration = Config.Bind("Loadout Preview",
                "Loadout Preview - Duration",
                1f,
                new ConfigDescription(
                    "Duration (in seconds) for which the loadout preview is displayed.",
                    new AcceptableValueRange<float>(0.5f, 3f),
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Debug Mode settings
            debugModeEnabled = Config.Bind("Debug Mode",
                "Debug Mode - Enabled",
                true,
                "Enable or disable the debug mode for logging");
            // Plugin startup logic
            harmony = new Harmony("george.no_tactitools");
            Logger = base.Logger;
            // CORE PATCHES
            harmony.PatchAll(typeof(RegisterControllerPatch));
            harmony.PatchAll(typeof(ControllerInputInterceptionPatch));
            //harmony.PatchAll(typeof(TestInput));
            // Patch MFD Color
            if (MFDColorEnabled.Value) {
                Logger.LogInfo($"MFD Color is enabled, patching...");
                harmony.PatchAll(typeof(MFDColorPlugin));
            }
            // CONTROL PATCHES
            // Patch Target List Controller
            if (targetListControllerEnabled.Value) {
                Logger.LogInfo($"Target Recall is enabled, patching...");
                harmony.PatchAll(typeof(TargetListControllerPlugin));
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
            // COCKPIT DISPLAY PATCHES
            // Patch Loadout Preview
            if (loadoutPreviewEnabled.Value) {
                Logger.LogInfo($"Loadout Preview is enabled, patching...");
                harmony.PatchAll(typeof(LoadoutPreviewPlugin));
            }
            // Patch Interception Vector
            if (interceptionVectorEnabled.Value) {
                Logger.LogInfo($"Interception Vector is enabled, patching...");
                harmony.PatchAll(typeof(InterceptionVectorPlugin));
            }
            // Patch Weapon Display
            if (weaponDisplayEnabled.Value) {
                Logger.LogInfo($"Weapon Display is enabled, patching...");
                harmony.PatchAll(typeof(WeaponDisplayPlugin));
            }
            // Patch Delivery Checker
            if (deliveryCheckerEnabled.Value) {
                Logger.LogInfo($"Delivery Checker is enabled, patching...");
                harmony.PatchAll(typeof(DeliveryCheckerPlugin));
            }
            // Patch Boot Screen
            if (bootScreenEnabled.Value) {
                Logger.LogInfo($"Boot Screen is enabled, patching...");
                harmony.PatchAll(typeof(BootScreenPlugin));
            }
            // HMD DISPLAY PATCHES
            // Patch Unit Distance
            if (unitDistanceEnabled.Value) {
                Logger.LogInfo($"Unit Marker Distance Indicator is enabled, patching...");
                harmony.PatchAll(typeof(UnitDistancePlugin));
            }
            // Patch Artificial Horizon
            if (artificialHorizonEnabled.Value) {
                Logger.LogInfo($"Artificial Horizon is enabled, patching...");
                harmony.PatchAll(typeof(ArtificialHorizonPlugin));
            }
            // MAP DISPLAY PATCHES
            // Patch Unit Icon Recolor
            if (unitIconRecolorEnabled.Value) {
                Logger.LogInfo($"Unit Icon Recolor is enabled, patching...");
                harmony.PatchAll(typeof(UnitIconRecolorPlugin));
            }
        }

        public static void Log(string message) {
            if (debugModeEnabled.Value) {
                TimeSpan timeSpan = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                string formattedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                Logger.LogInfo("[" + formattedTime + "] " + message);
            }
        }

        internal sealed class ConfigurationManagerAttributes {
            public int? Order;
        }
    }
}
