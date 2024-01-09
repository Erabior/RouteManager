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

            //Set Min Water Level
            if (float.TryParse(IniFile.Read("WaterLevel", "Alerts"), out outValueFloat))
            {
                Logger.LogToDebug("WaterLevel is read as: " + outValueFloat, Logger.logLevel.Debug);
                SettingsData.minWaterQuantity = outValueFloat >= 0 ? outValueFloat : 500f;
                Logger.LogToDebug("WaterLevel is now: " + SettingsData.minWaterQuantity);
            }

            //Set Min Coal Level
            if (float.TryParse(IniFile.Read("CoalLevel", "Alerts"), out outValueFloat))
            {
                Logger.LogToDebug("CoalLevel is read as: " + outValueFloat, Logger.logLevel.Debug);
                SettingsData.minCoalQuantity = outValueFloat >= 0 ? outValueFloat : 0.5f;
                Logger.LogToDebug("CoalLevel is now: " + SettingsData.minCoalQuantity);
            }

            //Set Min Diesel Level
            if (float.TryParse(IniFile.Read("DieselLevel", "Alerts"), out outValueFloat))
            {
                Logger.LogToDebug("DieselLevel is read as: " + outValueFloat, Logger.logLevel.Debug);
                SettingsData.minDieselQuantity = outValueFloat >= 0 ? outValueFloat : 100f;
                Logger.LogToDebug("DieselLevel is now: " + SettingsData.minDieselQuantity);
            }

            if (bool.TryParse(IniFile.Read("ShowTimestamp", "Alerts"), out outValueBool))
            {
                Logger.LogToDebug("ShowTimestamp is read as: " + outValueBool, Logger.logLevel.Debug);
                SettingsData.showTimestamp = outValueBool;
                Logger.LogToDebug("ShowTimestamp is now: " + SettingsData.showTimestamp);
            }

            if (bool.TryParse(IniFile.Read("ShowDaystamp", "Alerts"), out outValueBool))
            {
                Logger.LogToDebug("ShowDaystamp is read as: " + outValueBool, Logger.logLevel.Debug);
                SettingsData.showDaystamp = outValueBool;
                Logger.LogToDebug("ShowDaystamp is now: " + SettingsData.showDaystamp);
            }

            if (bool.TryParse(IniFile.Read("NewInterface", "Dev"), out outValueBool))
            {
                Logger.LogToDebug("NewInterface is read as: " + outValueBool, Logger.logLevel.Debug);
                SettingsData.experimentalUI = outValueBool;
                Logger.LogToDebug("NewInterface is now: " + SettingsData.experimentalUI);
            }

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
    }
}
