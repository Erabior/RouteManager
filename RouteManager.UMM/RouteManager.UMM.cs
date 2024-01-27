using System;
using UnityModManagerNet;
using RouteManager.UMM.Util;
using JetBrains.Annotations;
using RouteManager.v2.dataStructures;
using RouteManager.v2.core;

namespace RouteManager.UMM
{
    public class RMUMM
    {
        public static UnityModManager.ModEntry ModEntry;
        public static RouteManager RMInstance { get; private set; }
        public static UMMLogger logger = new UMMLogger();
        public static UMMSettingsManager settingsManager=new UMMSettingsManager();

        public static UMMSettings settings;
        public static SettingsData settingsData;     

        [UsedImplicitly]
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            settings = UMMSettings.Load<UMMSettings>(modEntry);
            ModEntry.OnGUI = settings.Draw;
            ModEntry.OnSaveGUI = settings.Save;

            try
            {
                //Create a new instance of RouteManager and pass in a BepInExLogger instance
                RMInstance = new RouteManager(logger,settingsManager,"Unity Mod Manager");

                //Get the settings object so we can change settings at runtime with the UMM GUI
                settingsData = RouteManager.Settings;

                //Activate Route Manager
                RMInstance.Start();

                return true;
            }
            catch (Exception ex)
            {
                RouteManager.logger.LogToDebug(ex.Message);
                return false;
            }
        }

    }
}
