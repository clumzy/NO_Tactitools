﻿using System;
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
        public static ConfigEntry<string> targetRecallInput;
        public static ConfigEntry<bool> interceptionVectorEnabled;
        public static ConfigEntry<bool> onScreenVectorEnabled;
        public static ConfigEntry<bool> countermeasureControlsEnabled;
        public static ConfigEntry<string> countermeasureControlsControllerName;
        public static ConfigEntry<string> countermeasureControlsFlareButtonNumber;
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
        public static ConfigEntry<bool> MFDAlternativeAttitudeEnabled;
        public static ConfigEntry<bool> unitIconRecolorEnabled;
        public static ConfigEntry<Color> unitIconRecolorEnemyColor;
        public static ConfigEntry<bool> bootScreenEnabled;
        public static ConfigEntry<bool> artificialHorizonEnabled;
        public static ConfigEntry<bool> debugModeEnabled;
        internal static new ManualLogSource Logger;

        private void Awake() {
            // Target Recall settings
            targetRecallEnabled = Config.Bind("Target Recall", //Category
                "Target Recall - Enabled", // Setting name
                true, // Default value
                new ConfigDescription(
                    "Enable or disable the Target Recall feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2})); // Description of the setting
            targetRecallControllerName = Config.Bind("Target Recall",
                "Target Recall - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            targetRecallInput = Config.Bind("Target Recall",
                "Target Recall - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for Target Recall (short press to recall, long press to save)",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
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
                        Order = 3
                    }));
            countermeasureControlsControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            countermeasureControlsFlareButtonNumber = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for selecting Flares",
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
                "CM & Weapon Display - Controller Name",
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
                        Order = 2
                    }));
            MFDColor = Config.Bind("MFD Color",
                "MFD Color - MFD Main Color",
                new Color(0f, 1f, 0f), // Default color in RGB
                new ConfigDescription(
                    "Main color for the MFD elements in RGB format.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            MFDAlternativeAttitudeEnabled = Config.Bind("MFD Color",
                "MFD Color - MFD Alternative Attitude - Enabled",
                false,
                new ConfigDescription(
                    "Enable or disable the alternative attitude indicator color on the MFD.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0
                    }));
            // Unit Icon Recolor settings
            unitIconRecolorEnabled = Config.Bind("AA Units Icon Recolor",
                "AA Units Icon Recolor - Enabled",
                true,
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
            // Patch Input Catcher - We do the patches manually here instead of in the "master class" like the other plugins because Rewired is a bit special as my mother would say
            harmony.PatchAll(typeof(ControllerInputInterceptionPatch));
            harmony.PatchAll(typeof(RegisterControllerPatch));
            //harmony.PatchAll(typeof(TestInput));
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
            // Patch Artificial Horizon
            if (artificialHorizonEnabled.Value) {
                Logger.LogInfo($"Artificial Horizon is enabled, patching...");
                harmony.PatchAll(typeof(ArtificialHorizonPlugin));
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
            public bool? ShowRangeAsPercent;
            public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;
            public CustomHotkeyDrawerFunc CustomHotkeyDrawer;
            public delegate void CustomHotkeyDrawerFunc(BepInEx.Configuration.ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
            public bool? Browsable;
            public string Category;
            public object DefaultValue;
            public bool? HideDefaultButton;
            public bool? HideSettingName;
            public string Description;
            public string DispName;
            public int? Order;
            public bool? ReadOnly;
            public bool? IsAdvanced;
            public System.Func<object, string> ObjToStr;
            public System.Func<string, object> StrToObj;
        }
    }
}
