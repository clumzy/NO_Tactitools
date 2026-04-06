using System.Collections.Generic;
using NO_Tactitools.Core;
using NO_Tactitools.Core.Bindings;
using NO_Tactitools.Core.Events;
using NO_Tactitools.Core.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace NO_Tactitools.Modules.UI.MFD;

internal class AmmoConIndicatorModule : Module {
    public AmmoConIndicatorModule(Plugin pluginInstance) : base(
        pluginInstance: pluginInstance,
        moduleName: "Ammo Conservation Indicator",
        initType: ModuleInitType.TacScreen,
        updateType: ModuleUpdateType.TacScreen,
        hasDrawableElement: false
    ) {
        // Register events
        EventSystem.Events.Missile.OnStart += OnMissileStart;
        EventSystem.Events.Missile.OnSetTarget += OnMissileSetTarget;
        EventSystem.Events.Missile.OnDetonate += OnMissileDetonate;
    }

    private abstract class InternalState {
        public static readonly Dictionary<Missile, Unit> ActiveMissiles = [];
        public static readonly TraverseCache<TargetScreenUI, List<Image>> TargetBoxesCache = new("targetBoxes");
    }

    protected override void OnInitInternal(object sender, ModEventArgs e) {
        InternalState.ActiveMissiles.Clear();
    }

    protected override void OnUpdateInternal(object sender, ModEventArgs e) {
        // In addition to the usual checks performed by the Module class
        // we want to ensure that this function DOES not run if the Target Screen doesn't exist

        TargetScreenUI targetScreen = UIBindings.Game.GetTargetScreenUIComponent();

        if (targetScreen == null)
            return;

        // We list all the player's targets
        List<Unit> targets = GameBindings.Player.TargetList.GetTargets();
        // We list all the existing target boxes
        List<Image> targetBoxes = InternalState.TargetBoxesCache.GetValue(targetScreen);

        // We check that all the targets have a corresponding target box
        if (targetBoxes == null || targets.Count != targetBoxes.Count)
            return;
        // We iterate over all the targets
        for (int i = 0; i < targets.Count; i++) {
            bool isTrackedByMissile = InternalState.ActiveMissiles.ContainsValue(targets[i]);
            // The constructor will return the existing object if it's already been created !
            UIBindings.Draw.UIRectangle trackerDot = new(
                name: "Tracker Dot",
                cornerA: new Vector2(-5, -30),
                cornerB: new Vector2(5, -40),
                fillColor: new Color(0f, 1f, 0f, 0.95f),
                UIParent: targetBoxes[i].rectTransform // boxes and target list have the same order
            );
            // We set the active status of the dot to the value of isTrackedByMissile
            trackerDot.GetGameObject().SetActive(isTrackedByMissile);
        }
    }

    private static void OnMissileStart(object sender, ModEventArgs e) {
        // No logging if the player is not in the air
        if (GameBindings.Player.Aircraft.GetAircraft() == null) return;

        // We cast the sender to a Missile
        Missile missile = (Missile)sender;

        // If the missile is target-less
        if (missile.targetID == null) return;

        // Add the missile to the active missiles list
        if (missile.targetID.TryGetUnit(out Unit unit)) {
            InternalState.ActiveMissiles[missile] = unit;
        }
    }

    private void OnMissileSetTarget(object sender, ModEventArgs e) {
        // No logging if the player is not in the air
        if (GameBindings.Player.Aircraft.GetAircraft() == null) return;

        // We cast the sender to a Missile and the data to a Unit
        Missile missile = (Missile)sender;
        Unit unit = (Unit)e.Data;

        // Check if the missile is detonating or actually changing targets
        if (unit == null)
            InternalState.ActiveMissiles.Remove(missile);
        else
            InternalState.ActiveMissiles[missile] = unit;
    }

    private void OnMissileDetonate(object sender, ModEventArgs e) {
        // No logging if the player is not in the air
        if (GameBindings.Player.Aircraft.GetAircraft() == null) return;

        // We cast the sender to a Missile and the data to a Unit
        Missile missile = (Missile)sender;

        // Remove the missile from the list
        InternalState.ActiveMissiles.Remove(missile);
    }
}