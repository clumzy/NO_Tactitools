using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Rewired;
using NO_Tactitools.Controls;
using NO_Tactitools.UI.HMD;
using NO_Tactitools.UI.MFD;
using NO_Tactitools.UI.HUD;

namespace NO_Tactitools.Core {
    [BepInPlugin("NO_Tactitools", "NOTT", "0.6.0.2")]
    public class Plugin : BaseUnityPlugin {
        public static Harmony harmony;
        public static RewiredInputConfig MFDNavEnter;
        public static RewiredInputConfig MFDNavUp;
        public static RewiredInputConfig MFDNavDown;
        public static RewiredInputConfig MFDNavLeft;
        public static RewiredInputConfig MFDNavRight;
        public static RewiredInputConfig MFDNavToggle;
        public static ConfigEntry<bool> targetListControllerEnabled;
        public static ConfigEntry<bool> interceptionVectorEnabled;
        public static ConfigEntry<bool> countermeasureControlsEnabled;
        public static RewiredInputConfig countermeasureControlsFlare;
        public static RewiredInputConfig countermeasureControlsJammer;
        public static ConfigEntry<bool> weaponSwitcherEnabled;
        public static ConfigEntry<bool> ammoConIndicatorEnabled;
        public static RewiredInputConfig weaponSwitcher0;
        public static RewiredInputConfig weaponSwitcher1;
        public static RewiredInputConfig weaponSwitcher2;
        public static RewiredInputConfig weaponSwitcher3;
        public static RewiredInputConfig weaponSwitcher4;
        public static RewiredInputConfig weaponSwitcher5;
        public static ConfigEntry<bool> weaponDisplayEnabled;
        public static ConfigEntry<bool> weaponDisplayVanillaUIEnabled;
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
        public static ConfigEntry<bool> autopilotMenuEnabled;
        public static ConfigEntry<bool> loadoutPreviewEnabled;
        public static ConfigEntry<bool> loadoutPreviewOnlyShowOnBoot;
        public static ConfigEntry<float> loadoutPreviewDuration;
        public static ConfigEntry<bool> loadoutPreviewSendToHMD;
        public static ConfigEntry<bool> loadoutPreviewManualPlacement;
        public static ConfigEntry<int> loadoutPreviewPositionX;
        public static ConfigEntry<int> loadoutPreviewPositionY;
        public static ConfigEntry<float> loadoutPreviewBackgroundTransparency;
        public static ConfigEntry<float> loadoutPreviewTextAndBorderTransparency;
        public static ConfigEntry<bool> resetCockpitFOVEnabled;
        public static ConfigEntry<float> resetCockpitFOVSpeed;
        public static RewiredInputConfig resetCockpitFOV;
        public static ConfigEntry<bool> ILSScreenEnabled;
        public static ConfigEntry<bool> debugModeEnabled;
        internal static new ManualLogSource Logger;
        public static Plugin Instance;

        private void Update() {
            RewiredConfigManager.Update();
        }

        private void Awake() {
            Instance = this;
            // MFD Nav
            MFDNavEnter = new RewiredInputConfig(Config, "MFD Nav", "MFD Nav - Enter", "Input you want to assign for MFD Nav - Enter", 0);
            MFDNavUp = new RewiredInputConfig(Config, "MFD Nav", "MFD Nav - Up", "Input you want to assign for MFD Nav - Up", -1);
            MFDNavDown = new RewiredInputConfig(Config, "MFD Nav", "MFD Nav - Down", "Input you want to assign for MFD Nav - Down", -2);
            MFDNavLeft = new RewiredInputConfig(Config, "MFD Nav", "MFD Nav - Left", "Input you want to assign for MFD Nav - Left", -3);
            MFDNavRight = new RewiredInputConfig(Config, "MFD Nav", "MFD Nav - Right", "Input you want to assign for MFD Nav - Right", -4);
            MFDNavToggle = new RewiredInputConfig(Config, "MFD Nav", "MFD Nav - Toggle Screens", "Input you want to assign for toggling MFD screens", 1);
            
            // Target Recall settings
            targetListControllerEnabled = Config.Bind("Target List Controller", //Category
                "Target List Controller - Enabled", // Setting name
                true, // Default value
                new ConfigDescription(
                    "Enable or disable the Target Recall feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2})); // Description of the setting
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
            countermeasureControlsFlare = new RewiredInputConfig(Config, "Countermeasures", "Countermeasure Controls - Flares", "Input you want to assign for selecting Flares", 2);
            countermeasureControlsJammer = new RewiredInputConfig(Config, "Countermeasures", "Countermeasure Controls - Jammer", "Input you want to assign for selecting Jammer", 0);
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
            weaponSwitcher0 = new RewiredInputConfig(Config, "Advanced Slot Selection", "Advanced Slot Selection - Slot 0", "Input for slot 0 (Long press to toggle Turret Auto Control)", 5);
            weaponSwitcher1 = new RewiredInputConfig(Config, "Advanced Slot Selection", "Advanced Slot Selection - Slot 1", "Input for slot 1", 4);
            weaponSwitcher2 = new RewiredInputConfig(Config, "Advanced Slot Selection", "Advanced Slot Selection - Slot 2", "Input for slot 2", 3);
            weaponSwitcher3 = new RewiredInputConfig(Config, "Advanced Slot Selection", "Advanced Slot Selection - Slot 3", "Input for slot 3", 2);
            weaponSwitcher4 = new RewiredInputConfig(Config, "Advanced Slot Selection", "Advanced Slot Selection - Slot 4", "Input for slot 4", 1);
            weaponSwitcher5 = new RewiredInputConfig(Config, "Advanced Slot Selection", "Advanced Slot Selection - Slot 5", "Input for slot 5", 0);
            // Ammo Conservation Indicator settings
            ammoConIndicatorEnabled = Config.Bind("Ammo Conservation Indicator",
                "Ammo Conservation Indicator - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Ammo Conservation Indicator feature.",
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
            // Autopilot settings
            autopilotMenuEnabled = Config.Bind("Autopilot",
                "Autopilot - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Autopilot Menu feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 7
                    }));
            // Reset Cockpit FOV settings
            resetCockpitFOVEnabled = Config.Bind("Cockpit Camera Reset FOV",
                "Cockpit Camera Reset FOV - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the Reset Cockpit FOV feature.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            resetCockpitFOVSpeed = Config.Bind("Cockpit Camera Reset FOV",
                "Cockpit Camera Reset FOV - Speed",
                150f,
                new ConfigDescription(
                    "Speed at which the FOV resets (50 - 300).",
                    new AcceptableValueRange<float>(50f, 300f),
                    new ConfigurationManagerAttributes {
                        Order = 1
                    }));
            resetCockpitFOV = new RewiredInputConfig(Config, "Cockpit Camera Reset FOV", "Cockpit Camera Reset FOV", "Input you want to assign for Resetting Cockpit FOV", 0);
            // ILS Screen settings
            ILSScreenEnabled = Config.Bind("ILS Screen",
                "ILS Screen - Enabled",
                true,
                new ConfigDescription(
                    "Enable or disable the ILS Screen feature.",
                    null,
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
            loadoutPreviewSendToHMD = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD",
                false,
                new ConfigDescription(
                    "If enabled, the loadout preview will also be sent to the HMD display.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2
                    }));
            loadoutPreviewManualPlacement = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD - Manual Placement",
                false,
                new ConfigDescription(
                    "If enabled, allows manual placement of the loadout preview on the MFD.",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -1
                    }));
            loadoutPreviewPositionX = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD - Position X",
                0,
                new ConfigDescription(
                    "X position offset for the loadout preview when manual placement is enabled.",
                    new AcceptableValueRange<int>(-1920/2, +1920/2),
                    new ConfigurationManagerAttributes {
                        Order = -2
                    }));
            loadoutPreviewPositionY = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD - Position Y",
                0,
                new ConfigDescription(
                    "Y position offset for the loadout preview when manual placement is enabled.",
                    new AcceptableValueRange<int>(-(int)1080/2, +(int)1080/2),
                    new ConfigurationManagerAttributes {
                        Order = -3
                    }));
            loadoutPreviewBackgroundTransparency = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD - Transparency",
                0.6f,
                new ConfigDescription(
                    "Transparency level for the Loadout Preview display when sent to the HMD (0 = transparent, 0.8 = fully opaque).",
                    new AcceptableValueRange<float>(0.0f, 1.0f),
                    new ConfigurationManagerAttributes {
                        Order = -4
                    }));
            loadoutPreviewTextAndBorderTransparency = Config.Bind("Loadout Preview",
                "Loadout Preview - Send To HMD - Text and Border Transparency",
                0.9f,
                new ConfigDescription(
                    "Transparency level for the Loadout Preview text and border when sent to the HMD (0 = transparent, 1 = fully opaque).",
                    new AcceptableValueRange<float>(0.0f, 1.0f),
                    new ConfigurationManagerAttributes {
                        Order = -5
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
                Log($"MFD Color is enabled, patching...");
                harmony.PatchAll(typeof(MFDColorPlugin));
            }
            // CONTROL PATCHES
            // Patch Target List Controller
            if (targetListControllerEnabled.Value) {
                Log($"Target Recall is enabled, patching...");
                harmony.PatchAll(typeof(TargetListControllerPlugin));
            }
            // Patch Countermeasure Controls
            if (countermeasureControlsEnabled.Value) {
                Log($"Countermeasure Controls is enabled, patching...");
                harmony.PatchAll(typeof(CountermeasureControlsPlugin));
            }
            // Patch Weapon Switcher
            if (weaponSwitcherEnabled.Value) {
                Log($"Weapon Switcher is enabled, patching...");
                harmony.PatchAll(typeof(WeaponSwitcherPlugin));
            }
            // COCKPIT DISPLAY PATCHES
            // Patch Interception Vector
            if (interceptionVectorEnabled.Value) {
                Log($"Interception Vector is enabled, patching...");
                harmony.PatchAll(typeof(InterceptionVectorPlugin));
            }
            // Patch Weapon Display
            if (weaponDisplayEnabled.Value) {
                Log($"Weapon Display is enabled, patching...");
                harmony.PatchAll(typeof(WeaponDisplayPlugin));
            }
            // Patch Loadout Preview
            if (loadoutPreviewEnabled.Value) {
                Log($"Loadout Preview is enabled, patching...");
                harmony.PatchAll(typeof(LoadoutPreviewPlugin));
            }
            // Patch Delivery Checker
            if (deliveryCheckerEnabled.Value) {
                Log($"Delivery Checker is enabled, patching...");
                harmony.PatchAll(typeof(DeliveryCheckerPlugin));
            }
            // Patch Ammo Conservation Indicator
            if (ammoConIndicatorEnabled.Value) {
                Log("Ammo Conservation Indicator is enabled, patching...");
                harmony.PatchAll(typeof(AmmoConIndicatorPlugin));
            }
            // we load this one last so that the boot applies to the elements we add to the cockpit as well
            // Patch Boot Screen
            if (bootScreenEnabled.Value) {
                Log($"Boot Screen is enabled, patching...");
                harmony.PatchAll(typeof(BootScreenPlugin));
            }
            // HMD DISPLAY PATCHES
            // Patch Unit Distance
            if (unitDistanceEnabled.Value) {
                Log($"Unit Marker Distance Indicator is enabled, patching...");
                harmony.PatchAll(typeof(UnitDistancePlugin));
            }
            // Patch Artificial Horizon
            if (artificialHorizonEnabled.Value) {
                Log($"Artificial Horizon is enabled, patching...");
                harmony.PatchAll(typeof(ArtificialHorizonPlugin));
            }
            // HUD DISPLAY PATCHES
            // Patch ILS Screen
            if (ILSScreenEnabled.Value) {
                Log($"ILS Screen is enabled, patching...");
                harmony.PatchAll(typeof(ILSScreenPlugin));
            }
            // MAP DISPLAY PATCHES
            // Patch Unit Icon Recolor
            if (unitIconRecolorEnabled.Value) {
                Log($"Unit Icon Recolor is enabled, patching...");
                harmony.PatchAll(typeof(UnitIconRecolorPlugin));
            }
            // COCKPIT FOV PATCHES
            // Patch Reset Cockpit FOV
            if (resetCockpitFOVEnabled.Value) {
                Log($"Reset Cockpit FOV is enabled, patching...");
                harmony.PatchAll(typeof(ResetCockpitFOV));
            }
            // MOD COMPAT PATCHES
            if (autopilotMenuEnabled.Value){
                Log($"Autopilot Menu is enabled, patching...");
                harmony.PatchAll(typeof(NOAutopilotControlPlugin));
            }
            //Finished patching
            //Load audio assets
            Log("Loading audio assets...");
            UIBindings.Sound.LoadAllSounds();
            // Log completion
            Log("NO Tactitools loaded successfully !");
        }


        public static void Log(string message) {
            if (debugModeEnabled.Value) {
                TimeSpan timeSpan = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                string formattedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                Logger.LogInfo("[" + formattedTime + "] " + message);
            }
        }
    }
}
