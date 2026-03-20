using NO_Tactitools.Core;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Inputs;
using UnityEngine;

namespace NO_Tactitools.Modules.UI.MFD;

class WeaponDisplayModule : Module {
    public WeaponDisplayModule(Plugin pluginInstance) : base(
        pluginInstance,
        "Weapon Display",
        ModuleInitType.TacScreen,
        ModuleUpdateType.TacScreen) {
        if (!this.Enabled) return; // exit if module is disabled
        // Register new configs
        // Create Logic and Display Engines
        LogicEngineInstance = new LogicEngineWeaponDisplay();
        DisplayEngineInstance = new DisplayEngineWeaponDisplay();
        // Add new inputs
        InputCatcher.RegisterNewInput(
            AddNewInputConfig(
                "Toggle Screens",
                "Press to toggle."
            ),
            onLongPress: HandleDisplayToggle
        );
    }

    private void HandleDisplayToggle() {
    }

    // Concrete implementation of LogicEngine
    private class LogicEngineWeaponDisplay : LogicEngine {
        public override void Init() {
            string name = GameBindings.Player.Aircraft.GetPlatformName();
            Plugin.Log("[WD] Initializing Logic Engine for platform " + name);
            InternalStateWeaponDisplay.HasJammer = GameBindings.Player.Aircraft.Countermeasures.HasJammer();
            InternalStateWeaponDisplay.HasIrFlare = GameBindings.Player.Aircraft.Countermeasures.HasIRFlare();
            InternalStateWeaponDisplay.HasStations = GameBindings.Player.Aircraft.Weapons.GetStationCount() > 0;
            InternalStateWeaponDisplay.VanillaUIEnabled = Plugin.weaponDisplayVanillaUIEnabled.Value;
            Plugin.Log("[WD] Logic Engine initialized for platform " + name);
        }

        public override void Update() {
            // Update logic here
        }
    }

    // Concrete implementation of InternalState
    private class InternalStateWeaponDisplay : InternalState {
        public static bool HasJammer;
        public static bool HasIrFlare;
        public static bool HasStations;
        public static bool IsOutOfAmmo;
        public static bool IsFlareSelected;
        public static bool IsJammerSelected;
        public static float FlareAmmo01;
        public static float JammerAmmo01;
        public static bool ReduceWeaponFontSize = false;
        public static bool IsReloading = false;
        public static bool VanillaUIEnabled = true; // true by default since we need to check this value elsewhere
        public static Color MainColor = Color.green;
        public static Color TextColor = Color.green;
    }

    // Concrete implementation of DisplayEngine
    private class DisplayEngineWeaponDisplay : DisplayEngine {
        public override void Init() {
            if (InternalStateWeaponDisplay.HasIrFlare) { // In reality, this checks if the player's plane has spawned
                InternalStateWeaponDisplay.weaponDisplay = new WeaponDisplay();
                if (!InternalStateWeaponDisplay.VanillaUIEnabled) UIBindings.Game.HideWeaponPanel();
                else UIBindings.Game.ShowWeaponPanel();
            }
            Plugin.Log("[WD] Display Engine initialized for platform " + GameBindings.Player.Aircraft.GetPlatformName());
        }

        public override void Update() {
            if (GameBindings.GameState.IsGamePaused() ||
                GameBindings.Player.Aircraft.GetAircraft() == null ||
                UIBindings.Game.GetCombatHUDTransform() == null)
                return; // do not refresh anything if the game is paused or the player aircraft is not available
            // REFRESH WEAPON
            if (InternalStateWeaponDisplay.HasStations) { // do not refresh weapon info if the player has no weapon stations
                InternalStateWeaponDisplay.weaponDisplay.weaponNameLabel.SetText(GameBindings.Player.Aircraft.Weapons.GetActiveStationName());
                if (InternalStateWeaponDisplay.IsReloading)
                    InternalStateWeaponDisplay.weaponDisplay.weaponAmmoLabel.SetText(((int)(100f - GameBindings.Player.Aircraft.Weapons.GetActiveStationReloadProgress() * 100f)).ToString() + "%");
                else
                    InternalStateWeaponDisplay.weaponDisplay.weaponAmmoLabel.SetText(GameBindings.Player.Aircraft.Weapons.GetActiveStationAmmoString().Replace(" ", ""));
                InternalStateWeaponDisplay.weaponDisplay.weaponAmmoLabel.SetFontSize(
                    InternalStateWeaponDisplay.weaponDisplay.originalWeaponAmmoFontSize + (InternalState.ReduceWeaponFontSize ? -15 : 0));
                InternalStateWeaponDisplay.weaponDisplay.weaponAmmoLabel.SetColor(InternalState.IsOutOfAmmo ? Color.red : InternalState.TextColor);

                Image cloneImg = InternalState.weaponDisplay.weaponImageClone.GetComponent<Image>();
                Image srcImg = GameBindings.Player.Aircraft.Weapons.GetActiveStationImage();
                cloneImg.sprite = srcImg.sprite;
                cloneImg.color = InternalState.IsOutOfAmmo ? Color.red : InternalState.MainColor;
                // TODO : ENCAPSULATE IMAGES IN MY OWN CODE
            }
            // REFRESH FLARE (ALWAYS, BECAUSE EVERYONE HAS FLARES   )
            InternalState.weaponDisplay.flareLabel.SetText("IR:" + GameBindings.Player.Aircraft.Countermeasures.GetIRFlareAmmo().ToString());
            InternalState.weaponDisplay.flareLabel.SetFontStyle(InternalState.IsFlareSelected ? FontStyle.Bold : FontStyle.Normal);
            InternalState.weaponDisplay.flareLabel.SetFontSize(InternalState.weaponDisplay.originalFlareFontSize + (InternalState.IsFlareSelected ? 10 : 0));
            InternalState.weaponDisplay.flareLabel.SetColor(Color.Lerp(Color.red, InternalState.TextColor, InternalState.FlareAmmo01));
            // REFRESH JAMMER
            if (InternalState.HasJammer) {
                InternalState.weaponDisplay.jammerLabel.SetText("EW:" + GameBindings.Player.Aircraft.Countermeasures.GetJammerAmmo().ToString() + "%");
                InternalState.weaponDisplay.jammerLabel.SetFontStyle(InternalState.IsJammerSelected ? FontStyle.Bold : FontStyle.Normal);
                InternalState.weaponDisplay.jammerLabel.SetFontSize(InternalState.weaponDisplay.originalJammerFontSize + (InternalState.IsJammerSelected ? 10 : 0)); ;
                InternalState.weaponDisplay.jammerLabel.SetColor(Color.Lerp(Color.red, InternalState.TextColor, InternalState.JammerAmmo01));
            }
        }
    }
}