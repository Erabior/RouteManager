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

    [Header("Debugging & Development")]
    [Draw("New Interface", Tooltip = "Enable the New Interface")]
    public bool newInterface = false;
   
    [Draw("Logging Level", Tooltip = "Sets verbosity of event logging")]
    public LogLevel logLevel = LogLevel.Debug;

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
        RouteManagerUMM.logger.LogToDebug("OnChange() called");

        //Push settings back to RouteManager
        RouteManagerUMM.settingsData.minWaterQuantity = minWater;
        RouteManagerUMM.settingsData.minCoalQuantity = minCoal;
        RouteManagerUMM.settingsData.minDieselQuantity = minDiesel;

        RouteManagerUMM.settingsData.currentLogLevel = logLevel;
        RouteManagerUMM.settingsData.experimentalUI = newInterface;
    }

}
