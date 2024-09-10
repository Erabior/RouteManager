using RouteManager.v2.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteManager.Railloader.Util
{
    public class RailloaderSettingsManager : IRMSettingsManager
    {
        public bool Apply()
        {
            RMRailloader.logger.currentLogLevel = RouteManager.Settings.currentLogLevel;
            return true;
        }

        public bool Load()
        {
            RMRailloader.logger.LogToDebug("Load() called");

            logLoadedValues();

            //Trigger the settings manager to push the settings through
            RMRailloader.settings.Update();
            return true;
        }

        private static void logLoadedValues()
        {
            int defPadding = 30;
            RMRailloader.logger.LogToDebug("--------------------------------------------------------------------------");
            RMRailloader.logger.LogToDebug("Current Configured Settings:");
            RMRailloader.logger.LogToDebug("    LogLevel".PadRight(defPadding) + RMRailloader.logger.currentLogLevel);
            RMRailloader.logger.LogToDebug("    WaitUntilFull".PadRight(defPadding) + RMRailloader.settingsData.waitUntilFull);
            RMRailloader.logger.LogToDebug("    WaterLevel".PadRight(defPadding) + RMRailloader.settingsData.minWaterQuantity);
            RMRailloader.logger.LogToDebug("    CoalLevel".PadRight(defPadding) + RMRailloader.settingsData.minCoalQuantity);
            RMRailloader.logger.LogToDebug("    DieselLevel".PadRight(defPadding) + RMRailloader.settingsData.minDieselQuantity);
            RMRailloader.logger.LogToDebug("    ShowTimestamp".PadRight(defPadding) + RMRailloader.settingsData.showTimestamp);
            RMRailloader.logger.LogToDebug("    ShowDaystamp".PadRight(defPadding) + RMRailloader.settingsData.showDaystamp);
            RMRailloader.logger.LogToDebug("    ShowArrivalMessage".PadRight(defPadding) + RMRailloader.settingsData.showArrivalMessage);
            RMRailloader.logger.LogToDebug("    ShowDepartureMessage".PadRight(defPadding) + RMRailloader.settingsData.showDepartureMessage);
            RMRailloader.logger.LogToDebug("    NewInterface".PadRight(defPadding) + RMRailloader.settingsData.experimentalUI);
            RMRailloader.logger.LogToDebug("--------------------------------------------------------------------------");
        }
    }
}
