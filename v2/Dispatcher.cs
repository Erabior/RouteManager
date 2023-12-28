﻿using BepInEx.Logging;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Model;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2
{
    public class Dispatcher : MonoBehaviour
    {




        //Default unity hook.
        void Awake()
        {
            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Mod Initializing");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            //Load Route Manager Configuration
            if (!SettingsManager.Load())
                Logger.LogToError("FAILED TO LOAD SETTINGS!");
            else
                Logger.LogToDebug("Loaded Settings.", Logger.logLevel.Debug);

            //Attempt to apply settings
            if (!SettingsManager.Apply())
                Logger.LogToError("FAILED TO APPLY SETTINGS!");
            else
                Logger.LogToDebug("Applied Settings.", Logger.logLevel.Debug);

            //Hook the map unload event to gracefully stop all instances prior to map unload. 
            Messenger.Default.Register<MapDidUnloadEvent>(this, GameMapUnloaded);

            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Mod Ready!");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
        }



        //Default unity hook.
        void Update()
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: Update", Logger.logLevel.Trace);

            //Only do stuff if we have an actively controlled consist
            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                List<Car> keys = LocoTelem.locomotiveCoroutines.Keys.ToList();

                //For each locomotive in the Coroutine list
                foreach(Car currentLoco in keys) 
                {
                    Logger.LogToDebug(String.Format("Loco {0} has not called coroutine. Calling Coroutine for {1}",currentLoco,currentLoco.DisplayName),Logger.logLevel.Verbose);


                }
            }

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: Update", Logger.logLevel.Trace);
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