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

namespace NO_Tactitools.Core {
    [BepInPlugin("NO_Tactitools", "NOTT", "0.5.2")]
    public class Plugin : BaseUnityPlugin {
        public static Harmony harmony;
        public static ConfigEntry<string> menuNavigationEnterInput;
        public static ConfigEntry<string> menuNavigationEnterControllerName;
        public static ConfigEntry<int> menuNavigationEnterButtonIndex;
        public static ConfigEntry<string> menuNavigationUpInput;
        public static ConfigEntry<string> menuNavigationUpControllerName;
        public static ConfigEntry<int> menuNavigationUpButtonIndex;
        public static ConfigEntry<string> menuNavigationDownInput;
        public static ConfigEntry<string> menuNavigationDownControllerName;
        public static ConfigEntry<int> menuNavigationDownButtonIndex;
        public static ConfigEntry<string> menuNavigationLeftInput;
        public static ConfigEntry<string> menuNavigationLeftControllerName;
        public static ConfigEntry<int> menuNavigationLeftButtonIndex;
        public static ConfigEntry<string> menuNavigationRightInput;
        public static ConfigEntry<string> menuNavigationRightControllerName;
        public static ConfigEntry<int> menuNavigationRightButtonIndex;
        public static ConfigEntry<bool> targetListControllerEnabled;
        public static ConfigEntry<bool> interceptionVectorEnabled;
        public static ConfigEntry<bool> countermeasureControlsEnabled;
        public static ConfigEntry<string> countermeasureControlsFlareInput;
        public static ConfigEntry<string> countermeasureControlsFlareControllerName;
        public static ConfigEntry<int> countermeasureControlsFlareButtonIndex;
        public static ConfigEntry<string> countermeasureControlsJammerInput;
        public static ConfigEntry<string> countermeasureControlsJammerControllerName;
        public static ConfigEntry<int> countermeasureControlsJammerButtonIndex;
        public static ConfigEntry<bool> weaponSwitcherEnabled;
        public static ConfigEntry<string> weaponSwitcherInput0;
        public static ConfigEntry<string> weaponSwitcherControllerName0;
        public static ConfigEntry<int> weaponSwitcherButtonIndex0;
        public static ConfigEntry<string> weaponSwitcherInput1;
        public static ConfigEntry<string> weaponSwitcherControllerName1;
        public static ConfigEntry<int> weaponSwitcherButtonIndex1;
        public static ConfigEntry<string> weaponSwitcherInput2;
        public static ConfigEntry<string> weaponSwitcherControllerName2;
        public static ConfigEntry<int> weaponSwitcherButtonIndex2;
        public static ConfigEntry<string> weaponSwitcherInput3;
        public static ConfigEntry<string> weaponSwitcherControllerName3;
        public static ConfigEntry<int> weaponSwitcherButtonIndex3;
        public static ConfigEntry<string> weaponSwitcherInput4;
        public static ConfigEntry<string> weaponSwitcherControllerName4;
        public static ConfigEntry<int> weaponSwitcherButtonIndex4;
        public static ConfigEntry<string> weaponSwitcherInput5;
        public static ConfigEntry<string> weaponSwitcherControllerName5;
        public static ConfigEntry<int> weaponSwitcherButtonIndex5;
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
        public static ConfigEntry<string> autopilotToggleMenuInput;
        public static ConfigEntry<string> autopilotToggleMenuControllerName;
        public static ConfigEntry<int> autopilotToggleMenuButtonIndex;
        public static ConfigEntry<bool> loadoutPreviewEnabled;
        public static ConfigEntry<bool> loadoutPreviewOnlyShowOnBoot;
        public static ConfigEntry<float> loadoutPreviewDuration;
        public static ConfigEntry<bool> loadoutPreviewSendToHMD;
        public static ConfigEntry<bool> loadoutPreviewManualPlacement;
        public static ConfigEntry<int> loadoutPreviewPositionX;
        public static ConfigEntry<int> loadoutPreviewPositionY;
        public static ConfigEntry<float> loadoutPreviewBackgroundTransparency;
        public static ConfigEntry<float> loadoutPreviewTextAndBorderTransparency;
        public static ConfigEntry<bool> debugModeEnabled;
        public static ConfigEntry<string> customInputExample;
        public static ConfigEntry<string> customInputExampleControllerName;
        public static ConfigEntry<int> customInputExampleButtonIndex;
        internal static new ManualLogSource Logger;
        public static Plugin Instance;

        private void Update() {
            RewiredConfigManager.Update();
        }

        private void Awake() {
            Instance = this;
            // Menu navigation
            menuNavigationEnterInput = Config.Bind("Menu Navigation",
                "Menu Navigation - Enter Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for menu navigation - Enter",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = menuNavigationEnterControllerName,
                        ButtonIndex = menuNavigationEnterButtonIndex
                    }));
            menuNavigationEnterControllerName = Config.Bind("Menu Navigation",
                "Menu Navigation - Enter - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationEnterButtonIndex = Config.Bind("Menu Navigation",
                "Menu Navigation - Enter - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationUpInput = Config.Bind("Menu Navigation",
                "Menu Navigation - Up Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for menu navigation - Up",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -1,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = menuNavigationUpControllerName,
                        ButtonIndex = menuNavigationUpButtonIndex
                    }));
            menuNavigationUpControllerName = Config.Bind("Menu Navigation",
                "Menu Navigation - Up - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationUpButtonIndex = Config.Bind("Menu Navigation",
                "Menu Navigation - Up - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationDownInput = Config.Bind("Menu Navigation",
                "Menu Navigation - Down Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for menu navigation - Down",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -2,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = menuNavigationDownControllerName,
                        ButtonIndex = menuNavigationDownButtonIndex
                    }));
            menuNavigationDownControllerName = Config.Bind("Menu Navigation",
                "Menu Navigation - Down - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationDownButtonIndex = Config.Bind("Menu Navigation",
                "Menu Navigation - Down - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationLeftInput = Config.Bind("Menu Navigation",
                "Menu Navigation - Left Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for menu navigation - Left",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -3,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = menuNavigationLeftControllerName,
                        ButtonIndex = menuNavigationLeftButtonIndex
                    }));
            menuNavigationLeftControllerName = Config.Bind("Menu Navigation",
                "Menu Navigation - Left - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationLeftButtonIndex = Config.Bind("Menu Navigation",
                "Menu Navigation - Left - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationRightInput = Config.Bind("Menu Navigation",
                "Menu Navigation - Right Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for menu navigation - Right",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = -4,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = menuNavigationRightControllerName,
                        ButtonIndex = menuNavigationRightButtonIndex
                    }));
            menuNavigationRightControllerName = Config.Bind("Menu Navigation",
                "Menu Navigation - Right - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
            menuNavigationRightButtonIndex = Config.Bind("Menu Navigation",
                "Menu Navigation - Right - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Browsable = false
                    }));
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
            countermeasureControlsFlareControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3,
                        Browsable = false
                    }));
            countermeasureControlsFlareButtonIndex = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3,
                        Browsable = false
                    }));
            countermeasureControlsFlareInput = Config.Bind("Countermeasures",
                "Countermeasure Controls - Flares - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for selecting Flares",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = countermeasureControlsFlareControllerName,
                        ButtonIndex = countermeasureControlsFlareButtonIndex
                    }));
            countermeasureControlsJammerControllerName = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1,
                        Browsable = false
                    }));
            countermeasureControlsJammerButtonIndex = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1,
                        Browsable = false
                    }));
            countermeasureControlsJammerInput = Config.Bind("Countermeasures",
                "Countermeasure Controls - Jammer - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign for selecting Jammer",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = countermeasureControlsJammerControllerName,
                        ButtonIndex = countermeasureControlsJammerButtonIndex
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
            weaponSwitcherControllerName0 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 0 - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 6,
                        Browsable = false
                    }));
            weaponSwitcherButtonIndex0 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 0 - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 6,
                        Browsable = false
                    }));
            weaponSwitcherInput0 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 0 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 0 (Long press to toggle Turret Auto Control)",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 5,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = weaponSwitcherControllerName0,
                        ButtonIndex = weaponSwitcherButtonIndex0
                    }));
            weaponSwitcherControllerName1 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 1 - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 4,
                        Browsable = false
                    }));
            weaponSwitcherButtonIndex1 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 1 - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 4,
                        Browsable = false
                    }));
            weaponSwitcherInput1 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 1 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 1",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 4,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = weaponSwitcherControllerName1,
                        ButtonIndex = weaponSwitcherButtonIndex1
                    }));
            weaponSwitcherControllerName2 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 2 - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3,
                        Browsable = false
                    }));
            weaponSwitcherButtonIndex2 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 2 - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3,
                        Browsable = false
                    }));
            weaponSwitcherInput2 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 2 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 2",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 3,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = weaponSwitcherControllerName2,
                        ButtonIndex = weaponSwitcherButtonIndex2
                    }));
            weaponSwitcherControllerName3 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 3 - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2,
                        Browsable = false
                    }));
            weaponSwitcherButtonIndex3 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 3 - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2,
                        Browsable = false
                    }));
            weaponSwitcherInput3 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 3 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 3",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 2,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = weaponSwitcherControllerName3,
                        ButtonIndex = weaponSwitcherButtonIndex3
                    }));
            weaponSwitcherControllerName4 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 4 - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1,
                        Browsable = false
                    }));
            weaponSwitcherButtonIndex4 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 4 - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1,
                        Browsable = false
                    }));
            weaponSwitcherInput4 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 4 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 4",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 1,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = weaponSwitcherControllerName4,
                        ButtonIndex = weaponSwitcherButtonIndex4
                    }));
            weaponSwitcherControllerName5 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 5 - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0,
                        Browsable = false
                    }));
            weaponSwitcherButtonIndex5 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 5 - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0,
                        Browsable = false
                    }));
            weaponSwitcherInput5 = Config.Bind("Advanced Slot Selection",
                "Advanced Slot Selection - Slot 5 - Input",
                "",
                new ConfigDescription(
                    "Input for slot 5",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 0,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = weaponSwitcherControllerName5,
                        ButtonIndex = weaponSwitcherButtonIndex5
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
            autopilotToggleMenuControllerName = Config.Bind("Autopilot",
                "Autopilot - Open Menu - Controller Name",
                "",
                new ConfigDescription(
                    "Name of the peripheral",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 5,
                        Browsable = false
                    }));
            autopilotToggleMenuButtonIndex = Config.Bind("Autopilot",
                "Autopilot - Open Menu - Button Index",
                -1,
                new ConfigDescription(
                    "Index of the button",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 5,
                        Browsable = false
                    }));
            autopilotToggleMenuInput = Config.Bind("Autopilot",
                "Autopilot - Open Menu - Input",
                "",
                new ConfigDescription(
                    "Input you want to assign to Open Menu",
                    null,
                    new ConfigurationManagerAttributes {
                        Order = 5,
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = autopilotToggleMenuControllerName,
                        ButtonIndex = autopilotToggleMenuButtonIndex
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

            customInputExample = Config.Bind("Input Capture Example",
                "Example Input",
                "",
                new ConfigDescription(
                    "Click to capture a Rewired button input.",
                    null,
                    new ConfigurationManagerAttributes {
                        CustomDrawer = RewiredConfigManager.RewiredButtonDrawer,
                        ControllerName = customInputExampleControllerName,
                        ButtonIndex = customInputExampleButtonIndex
                    }));

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
            // MAP DISPLAY PATCHES
            // Patch Unit Icon Recolor
            if (unitIconRecolorEnabled.Value) {
                Log($"Unit Icon Recolor is enabled, patching...");
                harmony.PatchAll(typeof(UnitIconRecolorPlugin));
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
