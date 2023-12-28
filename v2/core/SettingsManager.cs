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

            //Get INI File Location
            string RouteManagerCFG = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RouteManager.ini");
            if (!File.Exists(RouteManagerCFG))
            {
                return false;
            }

            //Load Ini File
            IniFile.Path = new FileInfo(RouteManagerCFG).FullName;

            //Read INI File Properties
            Settings.currentLogLevel = Utilities.ParseEnum<Logger.logLevel>(IniFile.Read("LogLevel", "Core"));

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: LoadRouteManagerSettings", Logger.logLevel.Trace);
            return true;
        }

        private static bool ApplyRouteManagerSettings()
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: ApplyRouteManagerSettings", Logger.logLevel.Trace);

            //Appply Settings
            Logger.currentLogLevel = Settings.currentLogLevel;

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: ApplyRouteManagerSettings", Logger.logLevel.Trace);
            return true;
        }
    }
}
