using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NO_Tactitools.Core;

public class GameBindings {
    public class Units {
        public static bool IsImperial() {
            try {
                return PlayerSettings.unitSystem == PlayerSettings.UnitSystem.Imperial;
            }
            catch (Exception e) {
                Plugin.Log(e.ToString());
                return false;
            }
        }

        public static float ConvertAltitude_ToDisplay(float meters) {
            return IsImperial() ? meters * 3.28084f : meters;
        }

        public static float ConvertVerticalSpeed_ToDisplay(float metersPerSecond) {
            return IsImperial() ? metersPerSecond * 196.850394f : metersPerSecond;
        }

        public static float ConvertSpeed_ToDisplay(float metersPerSecond) {
            return IsImperial() ? metersPerSecond * 1.94384f : metersPerSecond * 3.6f;
        }

        public static float ConvertSpeed_FromDisplay(float displayValue) {
            return IsImperial() ? displayValue / 1.94384f : displayValue / 3.6f;
        }

        public static string GetAltitudeUnit() {
            return IsImperial() ? "ft" : "m";
        }

        public static string GetVerticalSpeedUnit() {
            return IsImperial() ? "fpm" : "m/s";
        }

        public static string GetSpeedUnit() {
            return IsImperial() ? "kts" : "km/h";
        }
    }

    public class GameState {
        public static bool IsGamePaused() {
            try {
                return GameplayUI.GameIsPaused;
            }
            catch (NullReferenceException e) { Plugin.Log(e.ToString()); return false; }
        }

        public static FactionHQ GetCurrentFactionHQ() {
            try {
                return Player.Aircraft.GetAircraft().NetworkHQ;
            }
            catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
        }
    }

    public class Player { 
        public class Aircraft {
            private static readonly TraverseCache<global::Aircraft, Radar> _radarCache = new("radar");
            
            public static global::Aircraft GetAircraft(bool silent = false) {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft;
                }
                catch (NullReferenceException e) {
                    if (!silent)
                        Plugin.Log(e.ToString());
                    return null;
                }
            }

            public static string GetPlatformName() {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName;
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return "Unknown"; }
            }

            public static void ToggleAutoControl() {
                try {
                    SceneSingleton<CombatHUD>.i.ToggleAutoControl();
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
            }

            public static bool IsRadarJammed() {
                try {
                    global::Aircraft aircraft = GetAircraft();
                    if (aircraft == null) return false;
                    Radar radar = _radarCache.GetValue(aircraft);
                    return radar != null && radar.IsJammed();
                }
                catch (Exception e) {
                    Plugin.Log(e.ToString());
                    return false;
                }
            }
            public class Countermeasures {
                private static readonly TraverseCache<CountermeasureManager, IList> _countermeasureStationsCache = new("countermeasureStations");
                private static readonly TraverseCache<object, IList> _irStationListCache = new("countermeasures"); // DIFFERENT STATIONS FOR DIFFERENT TYPES, same name however
                private static readonly TraverseCache<object, IList> _jammerStationListCache = new("countermeasures"); // DIFFERENT STATIONS FOR DIFFERENT TYPES, same name however
                private static readonly TraverseCache<object, IList> _ecmCheckListCache = new("countermeasures"); // DIFFERENT STATIONS FOR DIFFERENT TYPES, same name however
                private static readonly TraverseCache<RadarJammer, PowerSupply> _powerSupplyCache = new("powerSupply");
                private static readonly TraverseCache<object, int> _irStationAmmoCache = new("ammo");

                private static IList GetStationsList() {
                    try {
                        CountermeasureManager currentManager = SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager;
                        return _countermeasureStationsCache.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);
                    }
                    catch (NullReferenceException e) {
                        Plugin.Log(e.ToString());
                        return null;
                    }
                }

                public static int GetCurrentIndex() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return -1; }
                }

                public static int GetIRFlareAmmo() {
                    try {
                        IList stationsList = GetStationsList();
                        object IRStation = stationsList[HasECMPod() ? 1 : 0];
                        int count = _irStationAmmoCache.GetValue(IRStation);
                        return count;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0; }
                }

                public static int GetIRFlareMaxAmmo() {
                    try {
                        IList stationsList = GetStationsList();
                        object IRStation = stationsList[HasECMPod() ? 1 : 0];
                        IList countermeasuresList = _irStationListCache.GetValue(IRStation);
                        FlareEjector ejectorStation = (FlareEjector)countermeasuresList[0];
                        int maxCount = ejectorStation.GetMaxAmmo();
                        return maxCount;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0; }
                }

                public static int GetJammerAmmo() {
                    try {
                        IList stationsList = GetStationsList();
                        object JammerStation = stationsList[HasECMPod() ? 0 : 1];
                        IList countermeasuresList = _jammerStationListCache.GetValue(JammerStation);
                        RadarJammer jammerStation = (RadarJammer)countermeasuresList[0];
                        PowerSupply supply = _powerSupplyCache.GetValue(jammerStation);
                        int charge = (int)(supply.GetCharge() * 100f);
                        return charge;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0; }
                }

                public static bool HasIRFlare() {
                    try {
                        IList stationsList = GetStationsList();
                        return stationsList.Count > 0;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return false; }
                }

                public static bool HasECMPod() {
                    try {
                        IList stationsList = GetStationsList();

                        if (stationsList != null && stationsList.Count > 0) {
                            object firstStation = stationsList[0];
                            IList countermeasuresList = _ecmCheckListCache.GetValue(firstStation);

                            if (countermeasuresList != null && countermeasuresList.Count > 0) {
                                return countermeasuresList[0] is RadarJammer;
                            }
                        }
                        return false;
                    }
                    catch (Exception e) { Plugin.Log(e.ToString()); return false; }
                }

                public static bool HasJammer() {
                    try {
                        IList stationsList = GetStationsList();
                        return stationsList.Count > 1;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return false; }
                }

                public static bool IsFlareSelected() {
                    try {
                        if (HasECMPod())
                            return GetCurrentIndex() == 1;
                        else
                            return GetCurrentIndex() == 0;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return false; }
                }

                public static void SetIRFlare() {
                    try {
                        if (HasECMPod())
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 1;
                        else
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
                }

                public static void SetJammer() {
                    try {
                        if (HasECMPod())
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
                        else
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 1;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
                }
            }

            public class Weapons {
                private static readonly TraverseCache<WeaponStatus, Image> _weaponImageCache = new("weaponImage");

                public static string GetActiveStationName() {
                    try {
                        string name = SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.WeaponInfo.shortName;
                        if (name == "") {
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.WeaponInfo.weaponName;
                        }
                        else {
                            return name;
                        }
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return "Unknown Weapon"; }
                }

                public static int GetActiveStationAmmo() {
                    try {
                        if (GetStationCount() == 0)
                            return 0;
                        else
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.Ammo;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0; }
                }

                public static string GetActiveStationAmmoString() {
                    try {
                        if (GetStationCount() == 0)
                            return "0";
                        else
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.GetAmmoReadout();
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return "0"; }
                }

                public static float GetActiveStationReloadProgress() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.GetReloadStatusMax();
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0f; }
                }

                public static Image GetActiveStationImage() {
                    try {
                        WeaponStatus currentWeaponStatus = UIBindings.Game.GetWeaponStatus();
                        Image weaponImage = _weaponImageCache.GetValue(currentWeaponStatus);
                        return weaponImage;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return null; }
                }

                public static string GetStationNameByIndex(int index) {
                    try {
                        if (index < GetStationCount()) {
                            if (SceneSingleton<CombatHUD>.i.aircraft.weaponStations[index].WeaponInfo.shortName == "") {
                                return SceneSingleton<CombatHUD>.i.aircraft.weaponStations[index].WeaponInfo.weaponName;
                            }
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponStations[index].WeaponInfo.shortName;
                        }
                        else {
                            Plugin.Log("[BD] Station index out of range !");
                            return null;
                        }
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return "Unknown Weapon"; }
                }

                public static int GetStationAmmoByIndex(int index) {
                    try {
                        if (index < GetStationCount()) {
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponStations[index].Ammo;
                        }
                        else {
                            Plugin.Log("[BD] Station index out of range !");
                            return 0;
                        }
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0; }
                }

                public static int GetStationMaxAmmoByIndex(int index) {
                    try {
                        if (index < GetStationCount()) {
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponStations[index].FullAmmo;
                        }
                        else {
                            Plugin.Log("[BD] Station index out of range !");
                            return 0;
                        }
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 0; }
                }
                public static int GetStationCount() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count;
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); return 5; }
                }

                public static void SetActiveStation(byte index) {
                    try {
                        if (index < GetStationCount()) {
                            SceneSingleton<CombatHUD>.i.aircraft.weaponManager.SetActiveStation(index);
                            SceneSingleton<CombatHUD>.i.ShowWeaponStation(SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation);
                        }
                        else
                            Plugin.Log("[BD] Station index out of range !");
                    }
                    catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
                }
            }

        }

        public class TargetList {
            private static readonly TraverseCache<CombatHUD, List<Unit>> _targetListCache = new("targetList");
            private static readonly TraverseCache<CombatHUD, Dictionary<Unit, HUDUnitMarker>> _markerLookupCache = new("markerLookup");
            private static readonly TraverseCache<CombatHUD, AudioClip> _selectSoundCache = new("selectSound");
            
            public static void AddTargets(List<Unit> units, bool muteSound = false) {
                try {
                    CombatHUD currentCombatHUD = UIBindings.Game.GetCombatHUDComponent();
                    Dictionary<Unit, HUDUnitMarker> markerLookup = _markerLookupCache.GetValue(currentCombatHUD);
                    AudioClip selectSound = _selectSoundCache.GetValue(currentCombatHUD);
                    List<Unit> currentTargets = [.. units];
                    currentTargets.Reverse();
                    foreach (Unit t_unit in currentTargets) {
                        if (markerLookup.ContainsKey(t_unit)) {
                            markerLookup[t_unit].SelectMarker();
                            GameBindings.Player.Aircraft.GetAircraft().weaponManager.AddTargetList(t_unit);
                        }
                    }
                    if (!muteSound)
                        SoundManager.PlayInterfaceOneShot(selectSound);
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
            }

            public static void DeselectAll() {
                try {
                    SceneSingleton<CombatHUD>.i.DeselectAll(false);
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
            }

            public static void DeselectUnit(Unit unit) {
                try {
                    SceneSingleton<CombatHUD>.i.DeSelectUnit(unit);
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); }
            }

            public static List<Unit> GetTargets() {
                try {
                    CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                    List<Unit> targetList = _targetListCache.GetValue(currentCombatHUD);
                    return [.. targetList];
                }
                catch (NullReferenceException e) { Plugin.Log(e.ToString()); return []; }
            }
        }
    }
}

