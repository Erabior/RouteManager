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
            if (float.TryParse(IniFile.Read("WaterWarningLevel", "Warnings"), out outValueFloat))
                SettingsData.minWaterQuantity = outValueFloat == 0 ? 500f : outValueFloat;

            //Set Min Coal Level
            if (float.TryParse(IniFile.Read("CoalWarningLevel", "Warnings"), out outValueFloat))
                SettingsData.minCoalQuantity = outValueFloat == 0 ? 0.5f : outValueFloat;

            //Set Min Diesel Level
            if (float.TryParse(IniFile.Read("CoalDieselLevel", "Warnings"), out outValueFloat))
                SettingsData.minCoalQuantity = outValueFloat == 0 ? 100f : outValueFloat;

            if(bool.TryParse(IniFile.Read("enableNewInterface", "Experimental"), out outValueBool))
                SettingsData.experimentalUI = outValueBool;

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
