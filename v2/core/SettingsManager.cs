using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using RouteManager.v2.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RouteManager.v2.core
{
    public class SettingsManager
    {
        public static bool Load()
        {
            //Stub
            return LoadRouteManagerSettings();
        }

        public static bool Apply()
        {   
            //Stub
            return ApplyRouteManagerSettings();
        }


        //Load Settings from config file
        private static bool LoadRouteManagerSettings()
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: LoadRouteManagerSettings", Logger.logLevel.Trace);

            Logger.LogToDebug("Loading Settings", Logger.logLevel.Verbose);

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
            SettingsData.currentLogLevel = Utilities.ParseEnum<Logger.logLevel>(IniFile.Read("LogLevel", "Core"));

            if (bool.TryParse(IniFile.Read("WaitUntilFull", "Core"), out outValueBool))
            {
                Logger.LogToDebug("WaitUntilFull parsed as: " + outValueBool, Logger.logLevel.Verbose);
                SettingsData.waitUntilFull = outValueBool;
            }

            //Set Min Water Level
            if (float.TryParse(IniFile.Read("WaterLevel", "Alerts"), out outValueFloat))
            {
                Logger.LogToDebug("WaterLevel parsed as: " + outValueFloat, Logger.logLevel.Verbose);
                SettingsData.minWaterQuantity = outValueFloat >= 0 ? outValueFloat : 500f;
            }

            //Set Min Coal Level
            if (float.TryParse(IniFile.Read("CoalLevel", "Alerts"), out outValueFloat))
            {
                Logger.LogToDebug("CoalLevel parsed as: " + outValueFloat, Logger.logLevel.Verbose);
                SettingsData.minCoalQuantity = outValueFloat >= 0 ? outValueFloat : 0.5f;
            }

            //Set Min Diesel Level
            if (float.TryParse(IniFile.Read("DieselLevel", "Alerts"), out outValueFloat))
            {
                Logger.LogToDebug("DieselLevel parsed as: " + outValueFloat, Logger.logLevel.Verbose);
                SettingsData.minDieselQuantity = outValueFloat >= 0 ? outValueFloat : 100f;
            }

            if (bool.TryParse(IniFile.Read("ShowTimestamp", "Alerts"), out outValueBool))
            {
                Logger.LogToDebug("ShowTimestamp parsed as: " + outValueBool, Logger.logLevel.Verbose);
                SettingsData.showTimestamp = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("ShowDaystamp", "Alerts"), out outValueBool))
            {
                Logger.LogToDebug("ShowDaystamp parsed as: " + outValueBool, Logger.logLevel.Verbose);
                SettingsData.showDaystamp = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("ShowArrivalMessage", "Alerts"), out outValueBool))
            {
                Logger.LogToDebug("ShowArrivalMessage parsed as: " + outValueBool, Logger.logLevel.Verbose);
                SettingsData.showArrivalMessage = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("ShowDepartureMessage", "Alerts"), out outValueBool))
            {
                Logger.LogToDebug("ShowDepartureMessage parsed as: " + outValueBool, Logger.logLevel.Verbose);
                SettingsData.showDepartureMessage = outValueBool;
            }

            if (bool.TryParse(IniFile.Read("NewInterface", "Dev"), out outValueBool))
            {
                Logger.LogToDebug("NewInterface parsed as: " + outValueBool, Logger.logLevel.Verbose);
                SettingsData.experimentalUI = outValueBool;
            }

            //Log the loaded parameters to the log file.
            logLoadedValues();

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: LoadRouteManagerSettings", Logger.logLevel.Trace);
            return true;
        }

        private static bool ApplyRouteManagerSettings()
        {

            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: ApplyRouteManagerSettings", Logger.logLevel.Trace);

            Logger.LogToDebug("Applying Settings", Logger.logLevel.Verbose);

            //Apply Settings
            Logger.currentLogLevel = SettingsData.currentLogLevel;

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: ApplyRouteManagerSettings", Logger.logLevel.Trace);
            return true;
        }

        private static void logLoadedValues()
        {
            int defPadding = 30;
            Logger.LogToDebug("--------------------------------------------------------------------------");
            Logger.LogToDebug("Current Configured Settings:");
            Logger.LogToDebug("    LogLevel".PadRight(defPadding)             + Logger.currentLogLevel);
            Logger.LogToDebug("    WaitUntilFull".PadRight(defPadding)        + SettingsData.waitUntilFull);
            Logger.LogToDebug("    WaterLevel".PadRight(defPadding)           + SettingsData.minWaterQuantity);
            Logger.LogToDebug("    CoalLevel".PadRight(defPadding)            + SettingsData.minCoalQuantity);
            Logger.LogToDebug("    DieselLevel".PadRight(defPadding)          + SettingsData.minDieselQuantity);
            Logger.LogToDebug("    ShowTimestamp".PadRight(defPadding)        + SettingsData.showTimestamp);
            Logger.LogToDebug("    ShowDaystamp".PadRight(defPadding)         + SettingsData.showDaystamp);
            Logger.LogToDebug("    ShowArrivalMessage".PadRight(defPadding)   + SettingsData.showArrivalMessage);
            Logger.LogToDebug("    ShowDepartureMessage".PadRight(defPadding) + SettingsData.showDepartureMessage);
            Logger.LogToDebug("    NewInterface".PadRight(defPadding)         + SettingsData.experimentalUI);
            Logger.LogToDebug("--------------------------------------------------------------------------");
        }
    }
}
