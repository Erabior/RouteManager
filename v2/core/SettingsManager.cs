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
            return LoadRouteManagerSettings();
        }

        public static bool Apply()
        {
            return ApplyRouteManagerSettings();
        }


        //Load Settings from config file
        private static bool LoadRouteManagerSettings()
        {
            try
            {
                Logger.LogToDebug("ENTERED FUNCTION: LoadRouteManagerSettings", Logger.logLevel.Trace);

                //Get DLL File Location
                string RouteManagerCFG = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RouteManager.cfg");

                if (!File.Exists(RouteManagerCFG))
                {
                    return false;
                }
                //Settings.currentLogLevel = Utilities.ParseEnum<Logger.logLevel>(File.ReadLines(RouteManagerCFG).First());

                IniFile.SetIniFile(RouteManagerCFG);
                string result = IniFile.Read("LogLevel", "Core");

                Logger.LogToDebug(result);

            }
            catch { return false; }


            Logger.LogToDebug("EXITING FUNCTION: LoadRouteManagerSettings", Logger.logLevel.Trace);
            return true;
        }

        private static bool ApplyRouteManagerSettings()
        {
            Logger.LogToDebug("ENTERED FUNCTION: ApplyRouteManagerSettings", Logger.logLevel.Trace);

            //Appply Settings
            Logger.currentLogLevel = Settings.currentLogLevel;

            Logger.LogToDebug("EXITING FUNCTION: ApplyRouteManagerSettings", Logger.logLevel.Trace);
            return true;
        }
    }
}
