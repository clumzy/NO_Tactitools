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
                public static void SetIRFlare(){
                    try {
                        SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
                    }
                    catch (NullReferenceException) { }
                }

                public static void SetJammer(){
                    try {
                        // Reflection to access private field 'countermeasureStations'
                        var mgr = SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager;
                        var mgrType = mgr.GetType();

                        object stationsObj = null;
                        var f = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (f != null) stationsObj = f.GetValue(mgr);

                        var stations = stationsObj as IList;
                        int count = stations.Count;

                        if (count > 1) {
                            mgr.activeIndex = 1;
                        }
                    }
                    catch (NullReferenceException) { }
                }
            }
        }
    }
}