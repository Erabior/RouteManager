using RouteManager.BepInEx;
using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using RouteManager.v2.Logging;

using System.IO;
using System.Reflection;

namespace RouteManager.v2.core
{
    public class BepInExSettingsManager : IRMSettingsManager
    {
        public bool Load()
        {
            //Trace Logging
            RMBepInEx.logger.LogToDebug("ENTERED FUNCTION: LoadRouteManagerSettings", LogLevel.Trace);

            RMBepInEx.logger.LogToDebug("Loading Settings", LogLevel.Verbose);

            //Get INI File Location
            string RouteManagerCFG = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RouteManager.ini");
            if (!File.Exists(RouteManagerCFG))
            {
                return false;
            }

            //Load Ini File
            IniFile.Path = new FileInfo(RouteManagerCFG).FullName;

            /********************************************************************************
            *********************************************************************************
            *
            *
            *                       Read INI File Properties
            *                       
            *
            *********************************************************************************
            ********************************************************************************/

            float outValueFloat = 0;
            bool outValueBool = false;

            //Set Log Level
            RMBepInEx.logger.currentLogLevel = Utilities.ParseEnum<LogLevel>(IniFile.Read("LogLevel", "Core"));

            if (bool.TryParse(IniFile.Read("WaitUntilFull", "Core"), out outValueBool))
            {
                RMBepInEx.logger.LogToDebug("WaitUntilFull parsed as: " + outValueBool, LogLevel.Verbose);
                RMBepInEx.settingsData.waitUntilFull = outValueBool;
            }

            //Set Min Water Level
            if (float.TryParse(IniFile.Read("WaterLevel", "Alerts"), out outValueFloat))
            {
                RMBepInEx.logger.LogToDebug("WaterLevel parsed as: " + outValueFloat, LogLevel.Verbose);
                RMBepInEx.settingsData.minWaterQuantity = outValueFloat >= 0 ? outValueFloat : 500f;
            }

            //Set Min Coal Level
            if (float.TryParse(IniFile.Read("CoalLevel", "Alerts"), out outValueFloat))
            {
                RMBepInEx.logger.LogToDebug("CoalLevel parsed as: " + outValueFloat, LogLevel.Verbose);
                RMBepInEx.settingsData.minCoalQuantity = outValueFloat >= 0 ? outValueFloat : 0.5f;
            }

            //Set Min Diesel Level
            if (float.TryParse(IniFile.Read("DieselLevel", "Alerts"), out outValueFloat))
            {
                RMBepInEx.logger.LogToDebug("DieselLevel parsed as: " + outValueFloat, LogLevel.Verbose);
                RMBepInEx.settingsData.minDieselQuantity = outValueFloat >= 0 ? outValueFloat : 100f;
            }

            if (bool.TryParse(IniFile.Read("ShowTimestamp", "Alerts"), out outValueBool))
            {
                RMBepInEx.logger.LogToDebug("ShowTimestamp parsed as: " + outValueBool, LogLevel.Verbose);
                RMBepInEx.settingsData.showTimestamp = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("ShowDaystamp", "Alerts"), out outValueBool))
            {
                RMBepInEx.logger.LogToDebug("ShowDaystamp parsed as: " + outValueBool, LogLevel.Verbose);
                RMBepInEx.settingsData.showDaystamp = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("ShowArrivalMessage", "Alerts"), out outValueBool))
            {
                RMBepInEx.logger.LogToDebug("ShowArrivalMessage parsed as: " + outValueBool, LogLevel.Verbose);
                RMBepInEx.settingsData.showArrivalMessage = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("ShowDepartureMessage", "Alerts"), out outValueBool))
            {
                RMBepInEx.logger.LogToDebug("ShowDepartureMessage parsed as: " + outValueBool, LogLevel.Verbose);
                RMBepInEx.settingsData.showDepartureMessage = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("NewInterface", "Dev"), out outValueBool))
            {
                RMBepInEx.logger.LogToDebug("NewInterface parsed as: " + outValueBool, LogLevel.Verbose);
                RMBepInEx.settingsData.experimentalUI = outValueBool;
            }

            //Log the loaded parameters to the log file.
            logLoadedValues();

            //Trace Logging
            RMBepInEx.logger.LogToDebug("EXITING FUNCTION: LoadRouteManagerSettings", LogLevel.Trace);
            return true;
        }

        public bool Apply()
        {   
            //Trace Logging
            RMBepInEx.logger.LogToDebug("ENTERED FUNCTION: ApplyRouteManagerSettings", LogLevel.Trace);

            RMBepInEx.logger.LogToDebug("Applying Settings", LogLevel.Verbose);

            //Apply Settings
            RMBepInEx.logger.currentLogLevel = RouteManager.Settings.currentLogLevel;

            //Trace Logging
            RMBepInEx.logger.LogToDebug("EXITING FUNCTION: ApplyRouteManagerSettings", LogLevel.Trace);
            return true;
        }

        private static void logLoadedValues()
        {
            int defPadding = 30;
            RMBepInEx.logger.LogToDebug("--------------------------------------------------------------------------");
            RMBepInEx.logger.LogToDebug("Current Configured Settings:");
            RMBepInEx.logger.LogToDebug("    LogLevel".PadRight(defPadding) + RMBepInEx.logger.currentLogLevel);
            RMBepInEx.logger.LogToDebug("    WaitUntilFull".PadRight(defPadding) + RMBepInEx.settingsData.waitUntilFull);
            RMBepInEx.logger.LogToDebug("    WaterLevel".PadRight(defPadding) + RMBepInEx.settingsData.minWaterQuantity);
            RMBepInEx.logger.LogToDebug("    CoalLevel".PadRight(defPadding) + RMBepInEx.settingsData.minCoalQuantity);
            RMBepInEx.logger.LogToDebug("    DieselLevel".PadRight(defPadding) + RMBepInEx.settingsData.minDieselQuantity);
            RMBepInEx.logger.LogToDebug("    ShowTimestamp".PadRight(defPadding) + RMBepInEx.settingsData.showTimestamp);
            RMBepInEx.logger.LogToDebug("    ShowDaystamp".PadRight(defPadding) + RMBepInEx.settingsData.showDaystamp);
            RMBepInEx.logger.LogToDebug("    ShowArrivalMessage".PadRight(defPadding) + RMBepInEx.settingsData.showArrivalMessage);
            RMBepInEx.logger.LogToDebug("    ShowDepartureMessage".PadRight(defPadding) + RMBepInEx.settingsData.showDepartureMessage);
            RMBepInEx.logger.LogToDebug("    NewInterface".PadRight(defPadding) + RMBepInEx.settingsData.experimentalUI);
            RMBepInEx.logger.LogToDebug("--------------------------------------------------------------------------");
        }
    }
}
