using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Collections;
using System.Reflection;

namespace NO_Tactitools.Core;

public class Bindings {
    public class Player{
        public class Aircraft{
            public class Countermeasures{
                public static bool HasJammer(){
                    try {
                        // Reflection to access private field 'countermeasureStations'
                        var mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        var f = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = f.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);

                        var stations = stationsObj as IList;
                        int count = stations.Count;

                        return count > 1;
                    }
                    catch (NullReferenceException) {Plugin.Log("[CC] No aircraft found !"); return false; }
                }

                public static void SetIRFlare(){
                    try {
                        // No need for other checks here, the player always has flares
                        SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
                    }
                    catch (NullReferenceException) { Plugin.Log("[CC] No aircraft found !"); }
                }

                public static void SetJammer(){
                    try {
                        // Reflection to access private field 'countermeasureStations'
                        var mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        var f = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = f.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);

                        var stations = stationsObj as IList;
                        int count = stations.Count;

                        if (count > 1) {
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 1;
                        }
                    }
                    catch (NullReferenceException) {Plugin.Log("[CC] No aircraft found !"); }
                }
            }
        }

        public class Weapons{
        }

        public class TargetList{
            public static void AddTargets(List<Unit> units){
                try {
                    foreach (Unit t_unit in units) {
                        SceneSingleton<CombatHUD>.i.SelectUnit(t_unit);
                    }
                }
                catch (NullReferenceException) { Plugin.Log("[TR] No CombatHUD found !"); }
            }

            public static void DeselectAll(){
                try {
                    SceneSingleton<CombatHUD>.i.DeselectAll(false);
                }
                catch (NullReferenceException) { Plugin.Log("[TR] No CombatHUD found !"); }
            }

            public static List<Unit> GetTargets(){
                try {
                    return [.. (List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()];
                }
                catch (NullReferenceException) { Plugin.Log("[TR] No CombatHUD found !"); return []; }
            }
        }
    }
}