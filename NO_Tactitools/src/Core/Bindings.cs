using System;
using HarmonyLib;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;



namespace NO_Tactitools.Core;

public class TraverseCache<TObject, TValue>(string fieldName) where TObject : class {
    // this post was made by George Gang
    private TObject _cachedObject;
    private Traverse _traverse;

    public TValue GetValue(TObject currentObject, bool silent = true) {
        if (_traverse == null || _cachedObject != currentObject) {
            _cachedObject = currentObject;
            _traverse = Traverse.Create(currentObject).Field(fieldName);
            if (!silent)
                Plugin.Log("[TraverseCache<" + typeof(TObject).Name.ToString() + ", " + typeof(TValue).Name.ToString() + ">] "
                +"Cached field '" + fieldName.ToString() 
                + "' for object of type '" + typeof(TObject).Name.ToString() + "'.");
        }
        return _traverse.GetValue<TValue>();
    }

    public void Reset() {
        _cachedObject = null;
        _traverse = null;
    }
}

public class Bindings {
    public class GameState {
        public static bool IsGamePaused() {
            try {
                return GameplayUI.GameIsPaused;
            }
            catch (NullReferenceException) { Plugin.Log("[Bindings.GameState.IsGamePaused] NullReferenceException: GameplayUI singleton not available; assuming game is not paused."); return false; }
        }
    }

    public class Player {
        public class Aircraft {
            private static readonly TraverseCache<global::Aircraft, Radar> _radarCache = new("radar");
            
            public static global::Aircraft GetAircraft(bool silent = false) {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft;
                }
                catch (NullReferenceException) {
                    if (!silent)
                        Plugin.Log("[Bindings.Player.Aircraft.GetAircraft] NullReferenceException: CombatHUD or aircraft was null; returning null.");
                    return null;
                }
            }

            public static string GetPlatformName() {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft.GetAircraftParameters().aircraftName;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.GetPlatformName] NullReferenceException: CombatHUD or aircraft was null; returning 'Unknown'."); return "Unknown"; }
            }

            public static void ToggleAutoControl() {
                try {
                    SceneSingleton<CombatHUD>.i.ToggleAutoControl();
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.ToggleAutoControl] NullReferenceException: CombatHUD or aircraft not available; unable to toggle auto control."); }
            }

            public static bool IsRadarJammed() {
                try {
                    global::Aircraft aircraft = GetAircraft();
                    if (aircraft == null) return false;
                    Radar radar = _radarCache.GetValue(aircraft);
                    return radar != null && radar.IsJammed();
                }
                catch (Exception e) {
                    Plugin.Log($"[Bindings.Player.Aircraft.IsRadarJammed] Exception: {e.Message}");
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
                    catch (NullReferenceException) {
                        Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetStationsList] NullReferenceException: countermeasureManager or CombatHUD/aircraft was null; returning null.");
                        return null;
                    }
                }

                public static int GetCurrentIndex() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetCurrentIndex] NullReferenceException: countermeasureManager or CombatHUD/aircraft was null; returning -1."); return -1; }
                }

                public static int GetIRFlareAmmo() {
                    try {
                        IList stationsList = GetStationsList();
                        object IRStation = stationsList[HasECMPod() ? 1 : 0];
                        int count = _irStationAmmoCache.GetValue(IRStation);
                        return count;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetIRAmmo] NullReferenceException: countermeasure manager or IR station unavailable; returning 0 IR flares."); return 0; }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetIRMaxAmmo] NullReferenceException: countermeasure manager or IR station unavailable; returning 0 as max IR flares."); return 0; }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetJammerAmmo] NullReferenceException: countermeasure manager or jammer station/power supply unavailable; returning 0% jammer charge."); return 0; }
                }

                public static bool HasIRFlare() {
                    try {
                        IList stationsList = GetStationsList();
                        return stationsList.Count > 0;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.HasIRFlare] NullReferenceException: countermeasure manager or stations list unavailable; assuming no IR flares (false)."); return false; }
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
                    catch (Exception) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.HasECMPod] Exception: countermeasure manager or stations list unavailable; assuming no ECM pod (false)."); return false; }
                }

                public static bool HasJammer() {
                    try {
                        IList stationsList = GetStationsList();
                        return stationsList.Count > 1;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.HasJammer] NullReferenceException: countermeasure manager or stations list unavailable; assuming no jammer (false)."); return false; }
                }

                public static bool IsFlareSelected() {
                    try {
                        if (HasECMPod())
                            return GetCurrentIndex() == 1;
                        else
                            return GetCurrentIndex() == 0;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.IsFlareSelected] NullReferenceException: countermeasure manager or CombatHUD/aircraft was null; returning false."); return false; }
                }

                public static void SetIRFlare() {
                    try {
                        if (HasECMPod())
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 1;
                        else
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.SetIRFlare] NullReferenceException: countermeasure manager unavailable; cannot select IR flare."); }
                }

                public static void SetJammer() {
                    try {
                        if (HasECMPod())
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 0;
                        else
                            SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex = 1;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.SetJammer] NullReferenceException: countermeasure manager unavailable; cannot select jammer."); }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetActiveStationName] NullReferenceException: CombatHUD or weapon name text unavailable; returning 'Unknown Weapon'."); return "Unknown Weapon"; }
                }

                public static int GetActiveStationAmmo() {
                    try {
                        if (GetStationCount() == 0)
                            return 0;
                        else
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.Ammo;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetActiveStationAmmo] NullReferenceException: CombatHUD or ammo count text unavailable; returning 0 ammo."); return 0; }
                }

                public static string GetActiveStationAmmoString() {
                    try {
                        if (GetStationCount() == 0)
                            return "0";
                        else
                            return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.GetAmmoReadout();
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetActiveStationAmmoString] NullReferenceException: CombatHUD or ammo count text unavailable; returning '0'."); return "0"; }
                }

                public static float GetActiveStationReloadProgress() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.GetReloadStatusMax();
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetActiveStationReloadProgress] NullReferenceException: CombatHUD or weapon station unavailable; returning 0f."); return 0f; }
                }

                public static Image GetActiveStationImage() {
                    try {
                        WeaponStatus currentWeaponStatus = Bindings.UI.Game.GetWeaponStatus();
                        Image weaponImage = _weaponImageCache.GetValue(currentWeaponStatus);
                        return weaponImage;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetActiveStationImage] NullReferenceException: CombatHUD or weapon image unavailable; returning null."); return null; }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetStationNameByIndex] NullReferenceException: CombatHUD or weapon station/name unavailable; returning 'Unknown Weapon'."); return "Unknown Weapon"; }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetStationAmmoByIndex] NullReferenceException: CombatHUD or weapon station/ammo unavailable; returning 0 ammo."); return 0; }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetStationMaxAmmoByIndex] NullReferenceException: CombatHUD or weapon station/max ammo unavailable; returning 0 max ammo."); return 0; }
                }
                public static int GetStationCount() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.weaponStations.Count;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.StationCount] NullReferenceException: CombatHUD or aircraft/weaponStations was null; returning fallback station count 5."); return 5; }
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
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.SetActiveStation] NullReferenceException: CombatHUD or aircraft/weaponManager was null; cannot set active station."); }
                }
            }

        }

        public class TargetList {
            private static readonly TraverseCache<CombatHUD, List<Unit>> _targetListCache = new("targetList");
            private static readonly TraverseCache<CombatHUD, Dictionary<Unit, HUDUnitMarker>> _markerLookupCache = new("markerLookup");
            private static readonly TraverseCache<CombatHUD, AudioClip> _selectSoundCache = new("selectSound");
            
            public static void AddTargets(List<Unit> units, bool muteSound = false) {
                try {
                    CombatHUD currentCombatHUD = Bindings.UI.Game.GetCombatHUDComponent();
                    Dictionary<Unit, HUDUnitMarker> markerLookup = _markerLookupCache.GetValue(currentCombatHUD);
                    AudioClip selectSound = _selectSoundCache.GetValue(currentCombatHUD);
                    List<Unit> currentTargets = [.. units];
                    currentTargets.Reverse();
                    foreach (Unit t_unit in currentTargets) {
                        if (markerLookup.ContainsKey(t_unit)) {
                            markerLookup[t_unit].SelectMarker();
                            Bindings.Player.Aircraft.GetAircraft().weaponManager.AddTargetList(t_unit);
                        }
                    }
                    if (!muteSound)
                        SoundManager.PlayInterfaceOneShot(selectSound);
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.TargetList.AddTargets] NullReferenceException: CombatHUD target selection unavailable; cannot add targets."); }
            }

            public static void DeselectAll() {
                try {
                    SceneSingleton<CombatHUD>.i.DeselectAll(false);
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.TargetList.DeselectAll] NullReferenceException: CombatHUD not available; cannot deselect targets."); }
            }

            public static void DeselectUnit(Unit unit) {
                try {
                    SceneSingleton<CombatHUD>.i.DeSelectUnit(unit);
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.TargetList.DeslectUnit] NullReferenceException: CombatHUD target deselection unavailable; cannot deselect target."); }
            }

            public static List<Unit> GetTargets() {
                try {
                    CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                    List<Unit> targetList = _targetListCache.GetValue(currentCombatHUD);
                    return [.. targetList];
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.TargetList.GetTargets] NullReferenceException: CombatHUD or targetList unavailable; returning empty list."); return []; }
            }
        }
    }
    public class UI {
        public class Draw {
            public abstract class UIElement {
                protected GameObject gameObject;
                protected RectTransform rectTransform;
                protected Image imageComponent;
                protected string mfdKey;

                protected UIElement(
                    string name,
                    Transform UIParent = null,
                    string mfdKey = null) {
                    this.mfdKey = mfdKey;
                    if (UIParent != null) {
                        foreach (Transform child in UIParent) {
                            if (child.name == name) {
                                gameObject = child.gameObject;
                                rectTransform = gameObject.GetComponent<RectTransform>();
                                imageComponent = gameObject.GetComponent<Image>();
                                return;
                            }
                        }
                    }
                    // Create a new GameObject for the element
                    gameObject = new GameObject(name);
                    gameObject.transform.SetParent(UIParent, false);
                    rectTransform = gameObject.AddComponent<RectTransform>();
                    imageComponent = gameObject.AddComponent<Image>();
                    return;
                }

                public virtual void SetPosition(Vector2 position) {
                    rectTransform.anchoredPosition = position;
                }

                public virtual Vector2 GetPosition() {
                    return rectTransform.anchoredPosition;
                }

                public virtual void SetColor(Color color) {
                    imageComponent.color = color;
                }

                public GameObject GetGameObject() => gameObject;
                public RectTransform GetRectTransform() => rectTransform;
                public Image GetImageComponent() => imageComponent;

                public void Destroy() {
                    UnityEngine.Object.Destroy(gameObject);
                }
            }

            public class UILabel : UIElement {
                private Text textComponent;
                private float backgroundOpacity;
                private float textOpacity;

                public UILabel(
                    string name,
                    Vector2 position,
                    Transform UIParent = null,
                    FontStyle fontStyle = FontStyle.Normal,
                    Color? color = null,
                    int fontSize = 24,
                    float backgroundOpacity = 0.8f) : base(name, UIParent) {
                    this.backgroundOpacity = backgroundOpacity;
                    rectTransform.anchoredPosition = position;
                    rectTransform.sizeDelta = new Vector2(200, 40);
                    imageComponent.color = new Color(0, 0, 0, this.backgroundOpacity);
                    GameObject textObj = new("LabelText");
                    textObj.transform.SetParent(gameObject.transform, false);
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    Text textComp = textObj.AddComponent<Text>();
                    textComp.font = Bindings.UI.Draw.GetDefaultFont();
                    textComp.material = Bindings.UI.Draw.GetDefaultTextMaterial();
                    textComp.fontSize = fontSize;
                    textComp.fontStyle = fontStyle;
                    textComp.color = color ?? Color.white;
                    this.textOpacity = textComp.color.a;
                    textComp.alignment = TextAnchor.MiddleCenter;
                    textComp.text = "";
                    textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textComp.verticalOverflow = VerticalWrapMode.Overflow;
                    rectTransform.sizeDelta = new Vector2(textComp.preferredWidth, textComp.fontSize);
                    Transform textTransform = gameObject.transform.Find("LabelText");
                    textComponent = textTransform.GetComponent<Text>();
                    return;
                }

                public void SetText(string text) {
                    textComponent.text = text;
                    rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.fontSize);
                }

                public override void SetColor(Color color) {
                    textComponent.color = color;
                }

                public void SetFontSize(int size) {
                    textComponent.fontSize = size;
                    rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
                }

                public void SetFontStyle(FontStyle style) {
                    textComponent.fontStyle = style;
                }

                public void SetOpacity(float opacity) {
                    opacity = Mathf.Clamp01(opacity);
                    Color textColor = textComponent.color;
                    textComponent.color = new Color(textColor.r, textColor.g, textColor.b, textOpacity * opacity);
                    Color bgColor = imageComponent.color;
                    imageComponent.color = new Color(bgColor.r, bgColor.g, bgColor.b, backgroundOpacity * opacity);
                }
                public string GetText() => textComponent.text;

                public Vector2 GetTextSize() {
                    return new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
                }
            }

            public class UILine : UIElement {
                private float thickness;
                private float baseOpacity;

                public UILine(
                    string name,
                    Vector2 start,
                    Vector2 end,
                    Transform UIParent = null,
                    Color? color = null,
                    float thickness = 2f) : base(name, UIParent) {
                    this.thickness = thickness;
                    imageComponent.color = color ?? Color.white;
                    this.baseOpacity = imageComponent.color.a;
                    Vector2 direction = end - start;
                    float length = direction.magnitude;
                    rectTransform.sizeDelta = new Vector2(length, thickness);
                    rectTransform.anchoredPosition = start + direction / 2f;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                    return;
                }

                public void SetCoordinates(Vector2 start, Vector2 end) {
                    if (rectTransform == null) return;
                    Vector2 direction = end - start;
                    float length = direction.magnitude;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(length, rectTransform.sizeDelta.y);
                    rectTransform.anchoredPosition = start + direction / 2f;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                }

                public void SetThickness(float thickness) {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
                }

                public void ResetThickness() {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, thickness);
                }

                public void SetOpacity(float opacity) {
                    opacity = Mathf.Clamp01(opacity);
                    Color color = imageComponent.color;
                    imageComponent.color = new Color(color.r, color.g, color.b, baseOpacity * opacity);
                }
            }

            public class UIRectangle : UIElement {
                private Vector2 cornerA;
                private Vector2 cornerB;
                private Color fillColor;

                public UIRectangle(
                    string name,
                    Vector2 cornerA,
                    Vector2 cornerB,
                    Transform UIParent = null,
                    Color? fillColor = null) : base(name, UIParent) {

                    this.cornerA = cornerA;
                    this.cornerB = cornerB;
                    this.fillColor = fillColor ?? new Color(1, 1, 1, 0.1f);
                    imageComponent.color = this.fillColor;

                    UpdateRect();
                    return;
                }

                private void UpdateRect() {
                    Vector2 min = Vector2.Min(cornerA, cornerB);
                    Vector2 max = Vector2.Max(cornerA, cornerB);
                    Vector2 size = max - min;
                    Vector2 center = (min + max) / 2f;

                    rectTransform.anchoredPosition = center;
                    rectTransform.sizeDelta = size;
                }

                public virtual void SetCorners(Vector2 a, Vector2 b) {
                    cornerA = a;
                    cornerB = b;
                    UpdateRect();
                }

                public virtual void SetFillColor(Color color) {
                    fillColor = color;
                    imageComponent.color = fillColor;
                }

                public virtual void SetCenter(Vector2 center) {
                    Vector2 size = rectTransform.sizeDelta;
                    Vector2 half = size / 2f;
                    cornerA = center - half;
                    cornerB = center + half;
                    UpdateRect();
                }

                public Vector2 GetSize() => rectTransform.sizeDelta;

                public Vector2 GetCornerA() => cornerA;
                public Vector2 GetCornerB() => cornerB;
                public Vector2 GetCenter() => rectTransform.anchoredPosition;
                public Color GetFillColor() => fillColor;
                public GameObject GetRectObject() => gameObject;
            }

            public class UIAdvancedRectangle : UIRectangle {
                private UIRectangle topBorder;
                private UIRectangle bottomBorder;
                private UIRectangle leftBorder;
                private UIRectangle rightBorder;
                private float borderThickness;
                private Color borderColor;

                public UIAdvancedRectangle(
                    string name,
                    Vector2 cornerA,
                    Vector2 cornerB,
                    Color borderColor,
                    float borderThickness,
                    Transform UIParent = null,
                    Color? fillColor = null) : base(name, cornerA, cornerB, UIParent, fillColor) {

                    this.borderColor = borderColor;
                    this.borderThickness = borderThickness;

                    topBorder = new UIRectangle(name + "_Top", Vector2.zero, Vector2.zero, gameObject.transform, borderColor);
                    bottomBorder = new UIRectangle(name + "_Bottom", Vector2.zero, Vector2.zero, gameObject.transform, borderColor);
                    leftBorder = new UIRectangle(name + "_Left", Vector2.zero, Vector2.zero, gameObject.transform, borderColor);
                    rightBorder = new UIRectangle(name + "_Right", Vector2.zero, Vector2.zero, gameObject.transform, borderColor);

                    UpdateBorders();
                }

                private void UpdateBorders() {
                    Vector2 size = GetSize();
                    Vector2 halfSize = size / 2f;
                    float t = borderThickness;

                    // Outward borders
                    // Top Border: Full width (including corners), thickness t, extending outward at top edge
                    Vector2 topLeft_Top = new(-halfSize.x - t, halfSize.y + t);
                    Vector2 bottomRight_Top = new(halfSize.x + t, halfSize.y);

                    // Bottom Border: Full width (including corners), thickness t, extending outward at bottom edge
                    Vector2 topLeft_Bottom = new(-halfSize.x - t, -halfSize.y);
                    Vector2 bottomRight_Bottom = new(halfSize.x + t, -halfSize.y - t);

                    // Left Border: Full height (between top and bottom borders), thickness t, extending outward at left edge
                    Vector2 topLeft_Left = new(-halfSize.x - t, halfSize.y);
                    Vector2 bottomRight_Left = new(-halfSize.x, -halfSize.y);

                    // Right Border: Full height (between top and bottom borders), thickness t, extending outward at right edge
                    Vector2 topLeft_Right = new(halfSize.x, halfSize.y);
                    Vector2 bottomRight_Right = new(halfSize.x + t, -halfSize.y);

                    topBorder.SetCorners(topLeft_Top, bottomRight_Top);
                    bottomBorder.SetCorners(topLeft_Bottom, bottomRight_Bottom);
                    leftBorder.SetCorners(topLeft_Left, bottomRight_Left);
                    rightBorder.SetCorners(topLeft_Right, bottomRight_Right);

                    topBorder.SetFillColor(borderColor);
                    bottomBorder.SetFillColor(borderColor);
                    leftBorder.SetFillColor(borderColor);
                    rightBorder.SetFillColor(borderColor);
                }

                public override void SetCorners(Vector2 a, Vector2 b) {
                    base.SetCorners(a, b);
                    UpdateBorders();
                }

                public void SetBorderColor(Color color) {
                    borderColor = color;
                    UpdateBorders();
                }

                public void SetBorderThickness(float thickness) {
                    borderThickness = thickness;
                    UpdateBorders();
                }
            }

            public static Font GetDefaultFont() {
                Text weaponText = Bindings.UI.Game.GetFlightHUDTransform().GetComponentInChildren<Text>();
                return weaponText.font;
            }

            public static Material GetDefaultTextMaterial() {
                Text weaponText = Bindings.UI.Game.GetCombatHUDTransform().GetComponentInChildren<Text>();
                return weaponText.material;
            }
        }

        public class Game {
            private static readonly TraverseCache<CombatHUD, GameObject> _topRightPanelCache = new("topRightPanel");
            private static readonly TraverseCache<TargetCam, TargetScreenUI> _targetScreenUICache = new("targetScreenUI");
            private static readonly TraverseCache<Cockpit, TacScreen> _tacScreenCache = new("tacScreen");
            private static TacScreen _cachedTacScreen;
            
            public static void DisplayToast(string message, float duration = 2f) {
                SceneSingleton<AircraftActionsReport>.i?.ReportText(message, duration);
            }

            public static Transform GetCombatHUDTransform() { // HMD
                try {
                    return SceneSingleton<CombatHUD>.i.transform;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetCombatHUD] NullReferenceException: CombatHUD singleton not available; returning null."); return null; }
            }

            public static CombatHUD GetCombatHUDComponent() {
                try {
                    return SceneSingleton<CombatHUD>.i;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetCombatHUDComponent] NullReferenceException: CombatHUD singleton not available; returning null."); return null; }
            }

            public static Transform GetFlightHUDTransform() { // HUD
                try {
                    return SceneSingleton<FlightHud>.i.transform;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetFlightHUD] NullReferenceException: FlightHud singleton not available; returning null."); return null; }
            }

            public static Transform GetTargetScreenTransform(bool silent = false) {
                try {
                    TargetCam currentTargetCam = SceneSingleton<CombatHUD>.i.aircraft.targetCam;
                    TargetScreenUI targetScreenUIObject = _targetScreenUICache.GetValue(currentTargetCam);
                    return targetScreenUIObject.transform;
                }
                catch (NullReferenceException) {
                    if (!silent)
                        Plugin.Log("[Bindings.UI.Game.GetTargetScreen] NullReferenceException: targetCam or TargetScreenUI not available; returning null.");
                    return null;
                }
            }

            public static Transform GetTacScreenTransform(bool silent = false) {
                try {
                    TacScreen tacScreenObject = GetTacScreenComponent(silent:true);
                    if (tacScreenObject != null) {
                        return tacScreenObject.transform.Find("Canvas").transform;
                    }
                    else {
                        if (!silent)
                            Plugin.Log("[Bindings.UI.Game.GetTacScreen] TacScreen component not found; returning null.");
                        return null;
                    }
                }
                catch (NullReferenceException) { 
                    if (!silent)
                        Plugin.Log("[Bindings.UI.Game.GetTacScreen] NullReferenceException: TacScreen or cockpit reference was null; returning null."); 
                    return null; }
            }

            public static TacScreen GetTacScreenComponent(bool silent = false) {
                try {
                    if (_cachedTacScreen != null) {
                        return _cachedTacScreen;
                    }
                    
                    foreach (Cockpit child in UnityEngine.Object.FindObjectsOfType<Cockpit>()) {
                        TacScreen tacScreenObject = _tacScreenCache.GetValue(child);
                        if (tacScreenObject != null) {
                            _cachedTacScreen = tacScreenObject;
                            return tacScreenObject;
                        }
                    }
                    if (!silent)
                        Plugin.Log("[BD] No Cockpit with TacScreen found !");
                    return null;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetTacScreenComponent] NullReferenceException: TacScreen or cockpit reference was null; returning null."); return null; }
            }

            public static TargetCam GetTargetCamComponent() {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft.targetCam;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetTargetCamComponent] NullReferenceException: TargetCam or CombatHUD/aircraft was null; returning null."); return null; }
            }

            public static TargetScreenUI GetTargetScreenUIComponent(bool silent = false) {
                try {
                    TargetCam currentTargetCam = SceneSingleton<CombatHUD>.i.aircraft.targetCam;
                    TargetScreenUI targetScreenUI = _targetScreenUICache.GetValue(currentTargetCam);
                    return targetScreenUI;
                }
                catch (NullReferenceException) {
                    if (!silent)
                        Plugin.Log("[Bindings.UI.Game.GetTargetScreenUIComponent] NullReferenceException: TargetScreenUI not available; returning null.");
                    return null;
                }
            }
            public static WeaponStatus GetWeaponStatus() {
                try {
                    CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                    GameObject topRightPanel = _topRightPanelCache.GetValue(currentCombatHUD);
                    WeaponStatus weaponStatus = topRightPanel.GetComponentInChildren<WeaponStatus>();
                    return weaponStatus;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetWeaponStatus] NullReferenceException: CombatHUD or weaponIndicator singleton not available; returning null."); return null; }
                catch (IndexOutOfRangeException) { return null; } // this means the game is paused and there is no weapon status displayed
            }

            public static CameraStateManager GetCameraStateManager() {
                try {
                    return SceneSingleton<CameraStateManager>.i;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetCameraStateManager] NullReferenceException: CameraStateManager singleton not available; returning null."); return null; }
            }

            public static void HideWeaponPanel() {
                CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                GameObject topRightPanel = _topRightPanelCache.GetValue(currentCombatHUD);
                topRightPanel.SetActive(false);
            }

            public static void ShowWeaponPanel() {
                CombatHUD currentCombatHUD = SceneSingleton<CombatHUD>.i;
                GameObject topRightPanel = _topRightPanelCache.GetValue(currentCombatHUD);
                topRightPanel.SetActive(true);
            }
        }

        public class Generic {
            public static void KillLayout(Transform target) {
                UnityEngine.UI.LayoutGroup layoutGroup = target.GetComponent<UnityEngine.UI.LayoutGroup>();
                if (layoutGroup != null) layoutGroup.enabled = false;
                UnityEngine.UI.ContentSizeFitter contentFitter = target.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (contentFitter != null) contentFitter.enabled = false;
            }

            public static void HideChildren(Transform target) {
                foreach (Transform child in target) {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public class Sound {
            public static Dictionary<string, AudioClip> loadedClips = [];
            public static void PlaySound(string fileName) {
                static IEnumerator PlayAudio(string fileName) {
                    AudioClip clip = loadedClips[fileName];
                    if (clip == null) {
                        Plugin.Log("[UIUtils] Loaded audio clip" + fileName + "is null.");
                        yield break;
                    }
                    SoundManager.PlayInterfaceOneShot(clip);
                }

                SceneSingleton<CombatHUD>.i.StartCoroutine(PlayAudio(fileName));
            }

            public static void LoadAllSounds() {
                string soundsDirectory = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "assets", "sounds");
                if (!Directory.Exists(soundsDirectory)) {
                    Plugin.Log($"[UIUtils] Sounds directory not found at: {soundsDirectory}");
                    return;
                }

                foreach (string filePath in Directory.GetFiles(soundsDirectory)) {
                    string fileName = Path.GetFileName(filePath).Split('.')[0];
                    if (!loadedClips.ContainsKey(fileName)) {
                        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
                        UnityEngine.Networking.UnityWebRequestAsyncOperation operation = www.SendWebRequest();
                        while (!operation.isDone) { } // Wait for completion

                        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
                            Plugin.Log("[UIUtils] Error loading audio: " + www.error);
                        }
                        else {
                            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                            if (clip != null) {
                                loadedClips[fileName] = clip;
                                Plugin.Log("[UIUtils] Loaded audio clip: " + fileName);
                            }
                            else {
                                Plugin.Log("[UIUtils] Loaded audio clip is null for file: " + fileName);
                            }
                        }
                    }
                }
            }
        }
    }
}