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
            RouteManagerUMM.logger.LogToDebug("Load() called");

            //Trigger the settings manager to push the settings through
            RouteManagerUMM.settings.OnChange();
            return true;
        }

        public bool Apply()
        {
            RouteManagerUMM.logger.currentLogLevel = RouteManagerUMM.settingsData.currentLogLevel;
            return true;
        }
    }
}
