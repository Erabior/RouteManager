using BepInEx;
using RouteManager.BepInEx.Util;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;

namespace RouteManager.BepInEx
{
    [BepInPlugin(RouteManager.modGUID, RouteManager.modName, RouteManager.modVersion)]
    public class RMBepInEx : BaseUnityPlugin

    {
        public static RouteManager RMInstance { get; private set; }
        public static BepInExLogger logger = new BepInExLogger();
        public static BepInExSettingsManager settingsManager = new BepInExSettingsManager();

        public static SettingsData settingsData;
       
        //Default unity hook.
        void Awake()
        {
            //Create a new instance of RouteManager and pass in a BepInExLogger instance
            RMInstance = new RouteManager(logger,settingsManager, "Bepis Injector Extensible");

            //Get the settings object so we can change settings
            settingsData = RouteManager.Settings;

            //Activate Route Manager
            RMInstance.Start();
        }
    }
}