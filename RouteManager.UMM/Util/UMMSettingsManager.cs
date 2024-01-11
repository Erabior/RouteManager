using RouteManager.UMM;
using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using RouteManager.v2.Logging;

using System.IO;
using System.Reflection;

namespace RouteManager.v2.core
{
    public class UMMSettingsManager : IRMSettingsManager
    {
        public bool Load()
        {
            RMUMM.logger.LogToDebug("Load() called");

            logLoadedValues();

            //Trigger the settings manager to push the settings through
            RMUMM.settings.OnChange();
            return true;
        }

        public bool Apply()
        {
            RMUMM.logger.currentLogLevel = RMUMM.settingsData.currentLogLevel;
            return true;
        }

        private static void logLoadedValues()
        {
            int defPadding = 30;
            RMUMM.logger.LogToDebug("--------------------------------------------------------------------------");
            RMUMM.logger.LogToDebug("Current Configured Settings:");
            RMUMM.logger.LogToDebug("    LogLevel".PadRight(defPadding) + RMUMM.logger.currentLogLevel);
            RMUMM.logger.LogToDebug("    WaitUntilFull".PadRight(defPadding) + RMUMM.settingsData.waitUntilFull);
            RMUMM.logger.LogToDebug("    WaterLevel".PadRight(defPadding) + RMUMM.settingsData.minWaterQuantity);
            RMUMM.logger.LogToDebug("    CoalLevel".PadRight(defPadding) + RMUMM.settingsData.minCoalQuantity);
            RMUMM.logger.LogToDebug("    DieselLevel".PadRight(defPadding) + RMUMM.settingsData.minDieselQuantity);
            RMUMM.logger.LogToDebug("    ShowTimestamp".PadRight(defPadding) + RMUMM.settingsData.showTimestamp);
            RMUMM.logger.LogToDebug("    ShowDaystamp".PadRight(defPadding) + RMUMM.settingsData.showDaystamp);
            RMUMM.logger.LogToDebug("    ShowArrivalMessage".PadRight(defPadding) + RMUMM.settingsData.showArrivalMessage);
            RMUMM.logger.LogToDebug("    ShowDepartureMessage".PadRight(defPadding) + RMUMM.settingsData.showDepartureMessage);
            RMUMM.logger.LogToDebug("    NewInterface".PadRight(defPadding) + RMUMM.settingsData.experimentalUI);
            RMUMM.logger.LogToDebug("--------------------------------------------------------------------------");
        }
    }
}
