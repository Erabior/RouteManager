using RouteManager.v2.Logging;
using System;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace RouteManager.UMM.Util;

[Serializable]
[DrawFields(DrawFieldMask.OnlyDrawAttr)]
public class UMMSettings : ModSettings, IDrawable
{
    public static Action<UMMSettings> OnSettingsUpdated;

    [Header("Fuel and Water Alerts")]
    [Draw("Water Qty", Tooltip = "This is the water alert level")]
    public float minWater = 500f;
    [Draw("Coal Qty", Tooltip = "This is the coal alert level")]
    public float minCoal = 0.5f;
    [Draw("Diesel Qty", Tooltip = "This is the diesel alert level")]
    public float minDiesel = 100f;

    [Space(10)]

    [Header("Behaviour")]
    [Draw("Wait Until Full", Tooltip = "Wait at the station until all passenger carriages are full")]
    public static bool waitUntilFull = false;
    [Draw("Alert on Arrival", Tooltip = "Show an alert when the train arrives at a station")]
    public static bool showArrivalMessage = true;
    [Draw("Alert on Departure", Tooltip = "Show an alert when the train departs a station")]
    public static bool showDepartureMessage = true;

    [Space(10)]

    [Header("Debugging & Development")]
    [Draw("New Interface", Tooltip = "Enable the New Interface")]
    public bool newInterface = false;
   
    [Draw("Logging Level", Tooltip = "Sets verbosity of event logging")]
    public LogLevel logLevel = LogLevel.Debug;

    public bool showTimestamp = false;

    public bool showDaystamp = false;

    public void Draw(ModEntry modEntry)
    {
        UMMSettings self = this;
        UnityModManager.UI.DrawFields(ref self, modEntry, DrawFieldMask.OnlyDrawAttr, OnChange);

    }

    public override void Save(ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {
        RMUMM.logger.LogToDebug("OnChange() called");

        //Push settings back to RouteManager
        //Alert levels
        RMUMM.settingsData.minWaterQuantity = minWater;
        RMUMM.settingsData.minCoalQuantity = minCoal;
        RMUMM.settingsData.minDieselQuantity = minDiesel;

        //Behaviours
        RMUMM.settingsData.showArrivalMessage = showArrivalMessage;
        RMUMM.settingsData.showDepartureMessage = showDepartureMessage;
        RMUMM.settingsData.waitUntilFull = waitUntilFull;

        //Debugging and Development
        RMUMM.settingsData.currentLogLevel = logLevel;
        RMUMM.settingsData.showTimestamp = showTimestamp;
        RMUMM.settingsData.showDaystamp = showDaystamp;
        RMUMM.settingsData.experimentalUI = newInterface;
    }

}
