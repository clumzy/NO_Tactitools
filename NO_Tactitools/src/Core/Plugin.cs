using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

using NO_Tactitools.Core.Inputs;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Events;
using NO_Tactitools.Modules.UI.MFD;

namespace NO_Tactitools.Core {
    [BepInPlugin("com.george.NO_Tactitools", "NOTT", "0.7.0.3")]
    public class Plugin : BaseUnityPlugin {
        public static Harmony Harmony;
        private static ConfigEntry<bool> _debugModeEnabled;
        internal new static ManualLogSource Logger;
        public static Plugin Instance;
        private void Update() {
            RewiredConfigManager.Update();
        }
        

        private void Awake() {
            Instance = this;
            // Plugin startup logic
            Harmony = new Harmony("george.no_tactitools");
            Logger = base.Logger;
            // Debug Mode setting
            _debugModeEnabled = Config.Bind("Debug Mode",
                "Debug Mode - Enabled",
                true,
                new ConfigDescription(
                    "Enable Debug Mode for logging.",
                    null,
                    new ConfigurationManagerAttributes { Order = 9999}
                ));
            //Load audio assets
            Log("Loading audio assets...");
            UIBindings.Sound.LoadAllSounds();
            // Patch all events
            EventSystem.PatchAll();
            // Patch Rewired patches
            Harmony.PatchAll(typeof(RegisterControllerPatch));
            Harmony.PatchAll(typeof(ControllerInputInterceptionPatch));
            // Weapon Display Module
            ModuleManager.TryAddModule(new WeaponDisplayModule(this));
        }


        public static void Log(string message) {
            if (_debugModeEnabled.Value) {
                TimeSpan timeSpan = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                string formattedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                Logger.LogInfo("[" + formattedTime + "] " + message);
            }
        }
    }
}
