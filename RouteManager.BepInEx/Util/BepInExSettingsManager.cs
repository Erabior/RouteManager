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
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: LoadRouteManagerSettings", LogLevel.Trace);

            RouteManager.logger.LogToDebug("Loading Settings", LogLevel.Verbose);

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

            //Set Min Water Level
            if (float.TryParse(IniFile.Read("WaterLevel", "Alerts"), out outValueFloat))
                RMBepInEx.settingsData.minWaterQuantity = outValueFloat <= 0 ? 500f : outValueFloat;

            //Set Min Coal Level
            if (float.TryParse(IniFile.Read("CoalLevel", "Alerts"), out outValueFloat))
                RMBepInEx.settingsData.minCoalQuantity = outValueFloat <= 0 ? 0.5f : outValueFloat;

            //Set Min Diesel Level
            if (float.TryParse(IniFile.Read("DieselLevel", "Alerts"), out outValueFloat))
                RMBepInEx.settingsData.minCoalQuantity = outValueFloat <= 0 ? 100f : outValueFloat;

            if (bool.TryParse(IniFile.Read("NewInterface", "Dev"), out outValueBool))
                RMBepInEx.settingsData.experimentalUI = outValueBool;

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: LoadRouteManagerSettings", LogLevel.Trace);
            return true;
        }

        public bool Apply()
        {   
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: ApplyRouteManagerSettings", LogLevel.Trace);

            RouteManager.logger.LogToDebug("Applying Settings", LogLevel.Verbose);

            //Apply Settings
            RouteManager.logger.currentLogLevel = RouteManager.Settings.currentLogLevel;

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: ApplyRouteManagerSettings", LogLevel.Trace);
            return true;
        }
    }
}
