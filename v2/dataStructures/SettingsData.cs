using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RouteManager.v2.Logging.Logger;

namespace RouteManager.v2.dataStructures
{
    public class SettingsData
    {
        public static logLevel currentLogLevel  = logLevel.Info;

        public static float minDieselQuantity   = 100;

        public static float minWaterQuantity    = 500;

        public static float minCoalQuantity     = 0.5f;

        public static bool experimentalUI       = false;

        public static bool showTimestamp        = false;

        public static bool showDaystamp         = false;

        public static bool showArrivalMessage   = true;

        public static bool showDepartureMessage = true;
    }
}
