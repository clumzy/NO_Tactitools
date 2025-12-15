using System;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;



namespace NO_Tactitools.Core;

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
            public static global::Aircraft GetAircraft(bool nullIsOkay = false) {
                try {
                    return SceneSingleton<CombatHUD>.i.aircraft;
                }
                catch (NullReferenceException) {
                    if (!nullIsOkay) {
                        Plugin.Log("[Bindings.Player.Aircraft.GetAircraft] NullReferenceException: CombatHUD or aircraft was null; returning null.");
                    }
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
            public class Countermeasures {

                public static int GetCurrentIndex() {
                    try {
                        return SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager.activeIndex;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetCurrentIndex] NullReferenceException: countermeasureManager or CombatHUD/aircraft was null; returning -1."); return -1; }
                }

                public static int GetIRFlareAmmo() {
                    try {
                        Type mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        FieldInfo stationsField = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = stationsField.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);
                        var IRStation = (stationsObj as IList)[HasECMPod() ? 1 : 0];
                        int count = (int)IRStation.GetType().GetField("ammo", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(IRStation);
                        return count;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetIRAmmo] NullReferenceException: countermeasure manager or IR station unavailable; returning 0 IR flares."); return 0; }
                }

                public static int GetIRFlareMaxAmmo() {
                    try {
                        Type mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        FieldInfo stationsField = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = stationsField.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);
                        var IRStation = (stationsObj as IList)[HasECMPod() ? 1 : 0];
                        Type stationType = IRStation.GetType();
                        FieldInfo counterField = stationType.GetField("countermeasures", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        var ejectorStationObj = counterField.GetValue(IRStation);
                        FlareEjector ejectorStation = (FlareEjector)(ejectorStationObj as IList)[0];
                        int maxCount = ejectorStation.GetMaxAmmo();
                        return maxCount;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetIRMaxAmmo] NullReferenceException: countermeasure manager or IR station unavailable; returning 0 as max IR flares."); return 0; }
                }

                public static int GetJammerAmmo() {
                    try {
                        Type mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        FieldInfo stationsField = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = stationsField.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);
                        var JammerStation = (stationsObj as IList)[HasECMPod() ? 0 : 1];
                        Type stationType = JammerStation.GetType();
                        FieldInfo counterField = stationType.GetField("countermeasures", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        var jammerStationObj = counterField.GetValue(JammerStation);
                        RadarJammer jammerStation = (RadarJammer)(jammerStationObj as IList)[0];
                        Type powerSupplyType = jammerStation.GetType();
                        FieldInfo powerSupplyField = powerSupplyType.GetField("powerSupply", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        PowerSupply supply = (PowerSupply)powerSupplyField.GetValue(jammerStation);
                        int charge = (int)(supply.GetCharge() * 100f);
                        return charge;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.GetJammerAmmo] NullReferenceException: countermeasure manager or jammer station/power supply unavailable; returning 0% jammer charge."); return 0; }
                }

                public static bool HasIRFlare() {
                    try {
                        // Reflection to access private field 'countermeasureStations'
                        Type mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        FieldInfo stationsField = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = stationsField.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);

                        return (stationsObj as IList).Count > 0;
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.HasIRFlare] NullReferenceException: countermeasure manager or stations list unavailable; assuming no IR flares (false)."); return false; }
                }

                public static bool HasECMPod() {
                    try {
                        return (
                            HasJammer() &&
                                ((GetPlatformName() == "UH-80 Ibis") ||
                                (GetPlatformName() == "SAH-46 Chicane") ||
                                (GetPlatformName() == "VL-49 Tarantula") ||
                                (GetPlatformName() == "CI-22 Cricket")));
                    }
                    catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Aircraft.Countermeasures.HasECMPod] NullReferenceException: countermeasure manager or stations list unavailable; assuming no ECM pod (false)."); return false; }
                }

                public static bool HasJammer() {
                    try {
                        // Reflection to access private field 'countermeasureStations'
                        Type mgrType = (SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager).GetType();
                        FieldInfo stationsField = mgrType.GetField("countermeasureStations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        object stationsObj = stationsField.GetValue(SceneSingleton<CombatHUD>.i.aircraft.countermeasureManager);

                        return (stationsObj as IList).Count > 1;
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
        }

        public class Weapons {

            public static string GetActiveStationName() {
                try {
                    string name = SceneSingleton<CombatHUD>.i.aircraft.weaponManager.currentWeaponStation.WeaponInfo.shortName;
                    if (name ==""){
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
                    Type weaponIndicatorType = (Bindings.UI.Game.GetWeaponStatus()).GetType();
                    FieldInfo weaponImageField = weaponIndicatorType.GetField("weaponImage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    Image weaponImage = (Image)weaponImageField.GetValue(Bindings.UI.Game.GetWeaponStatus());
                    return weaponImage;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.Weapons.GetActiveStationImage] NullReferenceException: CombatHUD or weapon image unavailable; returning null."); return null; }
            }

            public static string GetStationNameByIndex(int index) {
                try {
                    if (index < GetStationCount()) {
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

        public class TargetList {
            public static void AddTargets(List<Unit> units) {
                try {
                    foreach (Unit t_unit in units) {
                        SceneSingleton<CombatHUD>.i.SelectUnit(t_unit);
                    }
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.TargetList.AddTargets] NullReferenceException: CombatHUD target selection unavailable; cannot add targets."); }
            }

            public static void DeselectAll() {
                try {
                    SceneSingleton<CombatHUD>.i.DeselectAll(false);
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.Player.TargetList.DeselectAll] NullReferenceException: CombatHUD not available; cannot deselect targets."); }
            }

            public static List<Unit> GetTargets() {
                try {
                    return [.. (List<Unit>)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("targetList").GetValue()];
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
                    var textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    var textComp = textObj.AddComponent<Text>();
                    textComp.font = Bindings.UI.Draw.GetDefaultFont();
                    textComp.fontSize = fontSize;
                    textComp.fontStyle = fontStyle;
                    textComp.color = color ?? Color.white;
                    this.textOpacity = textComp.color.a;
                    textComp.alignment = TextAnchor.MiddleCenter;
                    textComp.text = "";
                    textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                    textComp.verticalOverflow = VerticalWrapMode.Overflow;
                    rectTransform.sizeDelta = new Vector2(textComp.preferredWidth, textComp.preferredHeight);
                    var textTransform = gameObject.transform.Find("LabelText");
                    textComponent = textTransform.GetComponent<Text>();
                    return;
                }

                public void SetText(string text) {
                    textComponent.text = text;
                    rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth, textComponent.preferredHeight);
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

                    // Inward borders
                    // Top Border: Full width, thickness t, at top edge
                    Vector2 topLeft_Top = new Vector2(-halfSize.x, halfSize.y);
                    Vector2 bottomRight_Top = new Vector2(halfSize.x, halfSize.y - t);

                    // Bottom Border: Full width, thickness t, at bottom edge
                    Vector2 topLeft_Bottom = new Vector2(-halfSize.x, -halfSize.y + t);
                    Vector2 bottomRight_Bottom = new Vector2(halfSize.x, -halfSize.y);

                    // Left Border: Height - 2t, thickness t, at left edge (between top and bottom borders)
                    Vector2 topLeft_Left = new Vector2(-halfSize.x, halfSize.y - t);
                    Vector2 bottomRight_Left = new Vector2(-halfSize.x + t, -halfSize.y + t);

                    // Right Border: Height - 2t, thickness t, at right edge (between top and bottom borders)
                    Vector2 topLeft_Right = new Vector2(halfSize.x - t, halfSize.y - t);
                    Vector2 bottomRight_Right = new Vector2(halfSize.x, -halfSize.y + t);

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
                Text weaponText = Bindings.UI.Game.GetFlightHUD().GetComponentInChildren<Text>();
                return weaponText.font;
            }
        }

        public class Game {
            public static void DisplayToast(string message, float duration = 2f) {
                SceneSingleton<AircraftActionsReport>.i?.ReportText(message, duration);
            }

            public static Transform GetCombatHUD() { // HMD
                try {
                    return SceneSingleton<CombatHUD>.i.transform;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetCombatHUD] NullReferenceException: CombatHUD singleton not available; returning null."); return null; }
            }

            public static Transform GetFlightHUD() { // HUD
                try {
                    return SceneSingleton<FlightHud>.i.transform;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetFlightHUD] NullReferenceException: FlightHud singleton not available; returning null."); return null; }
            }

            public static Transform GetTargetScreen(bool nullIsOkay = false) {
                try {
                    Type targetCamType = (SceneSingleton<CombatHUD>.i.aircraft.targetCam).GetType();
                    FieldInfo targetScreenField = targetCamType.GetField("targetScreenUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    TargetScreenUI targetScreenUIObject = (TargetScreenUI)targetScreenField.GetValue(SceneSingleton<CombatHUD>.i.aircraft.targetCam);
                    return targetScreenUIObject.transform;
                }
                catch (NullReferenceException) {
                    if (!nullIsOkay)
                        Plugin.Log("[Bindings.UI.Game.GetTargetScreen] NullReferenceException: targetCam or TargetScreenUI not available; returning null.");
                    return null;
                }
            }

            public static Transform GetTacScreen() {
                try {
                    foreach (Cockpit child in UnityEngine.Object.FindObjectsOfType<Cockpit>()) {
                        Type cockpitType = child.GetType();
                        FieldInfo tacScreenField = cockpitType.GetField("tacScreen", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        TacScreen tacScreenObject = (TacScreen)tacScreenField.GetValue(child);
                        if (tacScreenObject != null) {
                            return tacScreenObject.transform.Find("Canvas").transform;
                        }
                    }
                    Plugin.Log("[BD] No Cockpit with TacScreen found !");
                    return null;
                }
                catch (NullReferenceException) { Plugin.Log("[Bindings.UI.Game.GetTacScreen] NullReferenceException: TacScreen or cockpit reference was null; returning null."); return null; }
            }

            public static TacScreen GetTacScreenComponent() {
                try {
                    foreach (Cockpit child in UnityEngine.Object.FindObjectsOfType<Cockpit>()) {
                        Type cockpitType = child.GetType();
                        FieldInfo tacScreenField = cockpitType.GetField("tacScreen", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        TacScreen tacScreenObject = (TacScreen)tacScreenField.GetValue(child);
                        if (tacScreenObject != null) {
                            return tacScreenObject;
                        }
                    }
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

            public static WeaponStatus GetWeaponStatus() {
                try {
                    GameObject topRightPanel = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("topRightPanel").GetValue();
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
                GameObject topRightPanel = (GameObject)Traverse.Create(SceneSingleton<CombatHUD>.i).Field("topRightPanel").GetValue();
                CanvasGroup cg = topRightPanel.GetComponent<CanvasGroup>() ?? topRightPanel.AddComponent<CanvasGroup>();
                if (cg != null) {
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }
        }

        public class Generic {
            public static void KillLayout(Transform target) {
                var layoutGroup = target.GetComponent<UnityEngine.UI.LayoutGroup>();
                if (layoutGroup != null) layoutGroup.enabled = false;
                var contentFitter = target.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (contentFitter != null) contentFitter.enabled = false;
            }

            public static void HideChildren(Transform target) {
                foreach (Transform child in target) {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public class Sound {
            private static AudioSource audioSource;

            public static void PlaySound(string soundFileName) {
                static IEnumerator<UnityWebRequestAsyncOperation> LoadAndPlayAudio(string path) {
                    using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG);
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
                        Plugin.Logger.LogError("[UIUtils] Error loading audio: " + www.error);
                    }
                    else {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        audioSource.PlayOneShot(clip);
                    }
                }

                if (audioSource == null) {
                    GameObject audioGO = new("NOTT_AudioSource");
                    audioSource = audioGO.AddComponent<AudioSource>();
                    UnityEngine.Object.DontDestroyOnLoad(audioGO);
                }

                string soundPath = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "assets", "sounds", soundFileName);
                if (!File.Exists(soundPath)) {
                    Plugin.Logger.LogError($"[UIUtils] Sound file not found at: {soundPath}");
                    return;
                }

                SceneSingleton<CombatHUD>.i.StartCoroutine(LoadAndPlayAudio(soundPath));
            }
        }
    }
}