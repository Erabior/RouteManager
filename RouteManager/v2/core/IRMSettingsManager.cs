using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using RouteManager.v2.Logging;
using System.IO;
using System.Reflection;


namespace RouteManager.v2.core
{
    public interface IRMSettingsManager
    {
        public bool Load();

        public bool Apply();

    }
}
