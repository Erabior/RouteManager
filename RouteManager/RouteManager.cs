using HarmonyLib;
using UnityEngine;
using UI.Menu;
using RouteManager.v2;
using RouteManager.v2.UI;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.Logging;

namespace RouteManager
{
    public class RouteManager

    {
        public const string modGUID = "Erabior.Dispatcher";
        public const string modName = "Dispatcher";
        public const string modVersion = AppVersion.Version;

        public static IRMLogger logger;
        public static IRMSettingsManager settingsManager;
        public static SettingsData Settings = new SettingsData();

        private readonly Harmony harmony = new Harmony(modGUID);

        public void Start()
        {
            harmony.PatchAll();
        }
        public RouteManager(IRMLogger loggerInterface, IRMSettingsManager settingsInterface)
        {
            logger = loggerInterface;
            settingsManager = settingsInterface;
        }


        //Accessor for getting current mod information
        public static string getModGuid()
        {
            return modGUID;
        }

        //Accessor for getting current mod information
        public static string getModName()
        {
            return modName;
        }

        //Accessor for getting current mod information
        public static string getModVersion()
        {
            return modVersion;
        }
    }

    public class ModInjector : MonoBehaviour
    {
        [HarmonyPatch(typeof(PersistentLoader), nameof(PersistentLoader.ShowLoadingScreen))]
        public static class ShowLoadingScreen
        {
            public static void Postfix(bool show)
            {

                //Load Mod Settings
                loadSettings();

                //I am sure that there is probably a better way of ensuring that only one GameObject is generated.
                //Multiple instances can and will cause issues with multiple actions triggering and more.
                if (GameObject.FindObjectsOfType(typeof(Dispatcher)).Length == 0)
                {
                    //Create Parent Game object to bind the instance of Route AI To.
                    GameObject erabiorDispatcher = new GameObject("Erabior.Dispatcher");

                    RouteManager.logger.LogToDebug($"Adding Dispatcher", LogLevel.Info);
                    //Bind the Route AI component to the Game Object instance
                    erabiorDispatcher.AddComponent<Dispatcher>();
                    RouteManager.logger.LogToDebug($"Dispatcher Added", LogLevel.Info);
                    //Enable Experimental UI
                    if (RouteManager.Settings.experimentalUI)
                        erabiorDispatcher.AddComponent<ModInterface>();
                }
            }

            private static void loadSettings()
            {
                //Load Route Manager Configuration
                RouteManager.logger.LogToDebug($"Coal minQty: {RouteManager.Settings.minCoalQuantity}",LogLevel.Info);
                if (!RouteManager.settingsManager.Load())
                    RouteManager.logger.LogToError("FAILED TO LOAD SETTINGS!");
                else
                    RouteManager.logger.LogToDebug("Loaded Settings.", LogLevel.Debug);

                RouteManager.logger.LogToDebug($"Coal minQty: {RouteManager.Settings.minCoalQuantity}", LogLevel.Info);

                //Attempt to apply settings
                if (!RouteManager.settingsManager.Apply())
                    RouteManager.logger.LogToError("FAILED TO APPLY SETTINGS!");
                else
                    RouteManager.logger.LogToDebug("Applied Settings.", LogLevel.Debug);

                RouteManager.logger.LogToDebug("Log Level is now: " + RouteManager.logger.currentLogLevel.ToString());
            }
        }
    }
}