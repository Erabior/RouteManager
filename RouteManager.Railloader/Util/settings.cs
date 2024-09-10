using RouteManager.v2.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;

namespace RouteManager.Railloader.Util
{
    public class settings
    {
        public LogLevel currentLogLevel { get; set;} = LogLevel.Info;

        public float minDieselQuantity { get; set; } = 100;

        public float minWaterQuantity { get; set; } = 500;

        public float minCoalQuantity { get; set; } = 0.5f;

        public bool experimentalUI { get; set; } = false;

        public bool showTimestamp { get; set; } = false;

        public bool showDaystamp { get; set; } = false;

        public bool showArrivalMessage { get; set; } = true;

        public bool showDepartureMessage { get; set; } = true;

        public bool waitUntilFull { get; set; } = false;

        public void Update()
        {
            RMRailloader.settingsData.currentLogLevel = currentLogLevel;
            RMRailloader.settingsData.minDieselQuantity = minDieselQuantity;
            RMRailloader.settingsData.minWaterQuantity = minWaterQuantity;
            RMRailloader.settingsData.minCoalQuantity = minCoalQuantity;
            RMRailloader.settingsData.experimentalUI = experimentalUI;
            RMRailloader.settingsData.showTimestamp = showTimestamp;
            RMRailloader.settingsData.showDaystamp = showDaystamp;
            RMRailloader.settingsData.showArrivalMessage = showArrivalMessage;
            RMRailloader.settingsData.showDepartureMessage = showDepartureMessage;
            RMRailloader.settingsData.waitUntilFull = waitUntilFull;
        }
    }
}
