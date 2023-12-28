using BepInEx.Logging;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2
{
    public class Dispatcher : MonoBehaviour
    {

        //Initialize Route AI.
        void Awake()
        {
            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Mod Initialized");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            //Load Route Manager Configuration
            SettingsManager.Load();
            SettingsManager.Apply();

            //Hook the map unload event to gracefully stop all instances prior to map unload. 
            Messenger.Default.Register<MapDidUnloadEvent>(this, GameMapUnloaded);
        }
        void Update()
        {

            //Only do stuff if we have an actively controlled consist
            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                //Logger.LogToDebug("There is data in locomotiveCoroutines");
                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

            }
        }

        //Map is about to unload so lets cleanup all the instances of the mod.
        //Needed to Prevent crash on exit. 
        private void GameMapUnloaded(MapDidUnloadEvent mapDidUnloadEvent)
        {
            Logger.LogToDebug("GAME MAP UNLOAD TRIGGERED");

            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                Logger.LogToDebug("Stopping all Dispatcher AI Instances");

                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {
                    //StopCoroutine(AutoEngineerControlRoutine(keys[i]));
                }
                //clearDicts();
            }
        }
    }
}
