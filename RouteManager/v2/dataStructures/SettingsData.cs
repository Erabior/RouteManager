using RouteManager.v2.Logging;

namespace RouteManager.v2.dataStructures
{
    public class SettingsData
    {
        public LogLevel currentLogLevel  = LogLevel.Info;

        public float minDieselQuantity   = 100;

        public float minWaterQuantity    = 500;

        public float minCoalQuantity     = 0.5f;

        public bool experimentalUI       = false;

        public bool showTimestamp        = false;

        public bool showDaystamp         = false;

        public bool showArrivalMessage   = true;

        public bool showDepartureMessage = true;

        public bool waitUntilFull        = false;
    }
}
