using HarmonyLib;
using UnityEngine;
using NO_Tactitools.Core;

namespace NO_Tactitools.UI;

[HarmonyPatch(typeof(MainMenu), "Start")]
class LoadoutPreviewPlugin
{
    private static bool initialized = false;
    static void Postfix()
    {
        if (!initialized)
        {
            Plugin.Log($"[LP] Loadout Preview plugin starting !");
            Plugin.harmony.PatchAll(typeof(LoadoutPreviewComponent.OnPlatformStart));
            Plugin.harmony.PatchAll(typeof(LoadoutPreviewComponent.OnPlatformUpdate));
            // TODO: Register a button if needed for toggling or interaction
            initialized = true;
            Plugin.Log("[LP] Loadout Preview plugin succesfully started !");
        }
    }

    // TODO: Add handler methods for button presses if any
}

public class LoadoutPreviewComponent
{
    // LOGIC ENGINE, INTERNAL STATE, DISPLAY ENGINE
    static class LogicEngine
    {
        static public void Init()
        {
        }

        static public void Update()
        {
            if (Bindings.GameState.IsGamePaused() || Bindings.Player.Aircraft.GetAircraft() == null)
                return;
            // TODO: Update internal state with loadout information
        }
    }

    public static class InternalState
    {
        // TODO: Add state variables for loadout preview
    }

    static class DisplayEngine
    {
        static public void Init()
        {
        }

        static public void Update()
        {
            if (Bindings.GameState.IsGamePaused() || Bindings.Player.Aircraft.GetAircraft() == null)
                return;
            // TODO: Refresh the display with current loadout info
        }
    }

    public class LoadoutPreview
    {
        public Transform loadoutPreview_transform;
        // TODO: Add UI elements for the loadout preview (labels, images, etc.)

        public LoadoutPreview()
        {
            // TODO: Implement the UI creation logic
            // - Hide existing MFD content if necessary
            // - Create labels, images, etc. for each weapon station
            // - Position the UI elements based on the platform
        }
    }

    // INIT AND REFRESH LOOP
    [HarmonyPatch(typeof(TacScreen), "Initialize")]
    public static class OnPlatformStart
    {
        static void Postfix()
        {
            LogicEngine.Init();
            DisplayEngine.Init();
        }
    }

    [HarmonyPatch(typeof(TacScreen), "Update")]
    public static class OnPlatformUpdate
    {
        static void Postfix()
        {
            LogicEngine.Update();
            DisplayEngine.Update();
        }
    }
}
