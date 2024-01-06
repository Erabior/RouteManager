using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UI.Menu;
using RouteManager.v2;
using RouteManager.v2.UI;
using RouteManager.v2.core;
using RMLogger = RouteManager.v2.Logging.Logger;
using RouteManager.v2.dataStructures;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Device;


namespace RouteManager
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class RouteManagerLoader : BaseUnityPlugin

    {
        private const string modGUID = "Erabior.Dispatcher";
        private const string modName = "Dispatcher";
        private const string modVersion = "2.0.0.3";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;

        //Default unity hook.
        void Awake()
        {
            harmony.PatchAll();
            mls = Logger;
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

                    //Bind the Route AI component to the Game Object instance
                    erabiorDispatcher.AddComponent<Dispatcher>();

                    //Enable Experimental UI
                    if (SettingsData.experimentalUI)
                        erabiorDispatcher.AddComponent<ModInterface>();
                }
            }

            private static void loadSettings()
            {
                //Load Route Manager Configuration
                if (!SettingsManager.Load())
                    RMLogger.LogToError("FAILED TO LOAD SETTINGS!");
                else
                    RMLogger.LogToDebug("Loaded Settings.", RMLogger.logLevel.Debug);

                //Attempt to apply settings
                if (!SettingsManager.Apply())
                    RMLogger.LogToError("FAILED TO APPLY SETTINGS!");
                else
                    RMLogger.LogToDebug("Applied Settings.", RMLogger.logLevel.Debug);

                RMLogger.LogToDebug("Log Level is now: " + RMLogger.currentLogLevel.ToString());
            }
        }
    }
}