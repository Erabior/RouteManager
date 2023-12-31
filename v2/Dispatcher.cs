using BepInEx.Logging;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Model;
using RollingStock;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Track;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2
{
    public class Dispatcher : MonoBehaviour
    {

        AutoEngineer engineerAi;


        //Default unity hook.
        void Awake()
        {

            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Dispatcher Initializing");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            //Hook the map unload event to gracefully stop all instances prior to map unload. 
            Messenger.Default.Register<MapDidUnloadEvent>(this, GameMapUnloaded);

            engineerAi = new AutoEngineer();

            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Dispatcher Ready!");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
        }



        //Default unity hook.
        void Update()
        {
            //Trace Logging
            //Disable LogMessage for Update Thread unless REALLY NEEDED. 
            //Logger.LogToDebug("ENTERED FUNCTION: Update", Logger.logLevel.Trace);

            //Only do stuff if we have an actively controlled consist
            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                List<Car> keys = LocoTelem.locomotiveCoroutines.Keys.ToList();

                //For each locomotive in the Coroutine list
                foreach(Car currentLoco in keys) 
                {
                    //Disable LogMessage for Update Thread unless REALLY NEEDED. 
                    //Logger.LogToDebug(String.Format("Loco {0} has not called coroutine. Calling Coroutine for {1}",currentLoco,currentLoco.DisplayName),Logger.logLevel.Verbose);

                    if (!LocoTelem.locomotiveCoroutines[currentLoco] && LocoTelem.RouteMode[currentLoco])
                    {
                        Logger.LogToDebug($"loco {currentLoco.DisplayName} currently has not called a coroutine - Calling the Coroutine with {currentLoco.DisplayName} as an arguement");

                        prepareDataStructures(currentLoco);

                        StartCoroutine(engineerAi.AutoEngineerControlRoutine(currentLoco));

                    }
                    else if (LocoTelem.locomotiveCoroutines[currentLoco] && !LocoTelem.RouteMode[currentLoco])
                    {
                        Logger.LogToDebug($"loco {currentLoco.DisplayName} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {currentLoco.DisplayName}");

                        cleanDataStructures(currentLoco);

                        StopCoroutine(engineerAi.AutoEngineerControlRoutine(currentLoco));
                    }
                }
            }

            //Trace Logging
            //Disable LogMessage for Update Thread unless REALLY NEEDED. 
            //Logger.LogToDebug("EXITING FUNCTION: Update", Logger.logLevel.Trace);
        }


        private void prepareDataStructures(Car currentLoco)
        {
            LocoTelem.DriveForward[currentLoco] = true;
            LocoTelem.LineDirectionEastWest[currentLoco] = true;
            LocoTelem.TransitMode[currentLoco] = true;
            LocoTelem.RMMaxSpeed[currentLoco] = 0;
            LocoTelem.locomotiveCoroutines[currentLoco] = true;
            LocoTelem.approachWhistleSounded[currentLoco] = false;
            LocoTelem.lowFuelQuantities[currentLoco] = new Dictionary<string, float>();
            LocoTelem.closestStation[currentLoco] = (null,0);
            LocoTelem.currentDestination[currentLoco] = default(PassengerStop);
            LocoTelem.clearedForDeparture[currentLoco] = false;
            LocoTelem.CenterCar[currentLoco] = currentLoco;
            LocoTelem.locoTravelingWestward[currentLoco] = true;

            if (!LocoTelem.LineDirectionEastWest.ContainsKey(currentLoco))
            {
                LocoTelem.LineDirectionEastWest[currentLoco] = true;
            }
        }

        private void cleanDataStructures(Car currentLoco)
        {
            if(LocoTelem.DriveForward.ContainsKey(currentLoco))
                LocoTelem.DriveForward.Remove(currentLoco) ;

            if (LocoTelem.LineDirectionEastWest.ContainsKey(currentLoco))
                LocoTelem.LineDirectionEastWest.Remove(currentLoco);

            if (LocoTelem.TransitMode.ContainsKey(currentLoco))
                LocoTelem.TransitMode.Remove(currentLoco);

            if (LocoTelem.RMMaxSpeed.ContainsKey(currentLoco))
                LocoTelem.RMMaxSpeed.Remove(currentLoco);

            if (LocoTelem.approachWhistleSounded.ContainsKey(currentLoco))
                LocoTelem.approachWhistleSounded.Remove(currentLoco);

            if (LocoTelem.LineDirectionEastWest.ContainsKey(currentLoco))
                LocoTelem.LineDirectionEastWest.Remove(currentLoco);

            if (LocoTelem.LocomotivePrevDestination.ContainsKey(currentLoco))
                LocoTelem.LocomotivePrevDestination.Remove(currentLoco);

            if (LocoTelem.locomotiveCoroutines.ContainsKey(currentLoco))
                LocoTelem.locomotiveCoroutines.Remove(currentLoco);

            if (LocoTelem.lowFuelQuantities.ContainsKey(currentLoco))
                LocoTelem.lowFuelQuantities.Remove(currentLoco);

            if (LocoTelem.closestStation.ContainsKey(currentLoco))
                LocoTelem.closestStation.Remove(currentLoco);

            if (LocoTelem.currentDestination.ContainsKey(currentLoco))
                LocoTelem.currentDestination.Remove(currentLoco);

            if (LocoTelem.clearedForDeparture.ContainsKey(currentLoco))
                LocoTelem.clearedForDeparture.Remove(currentLoco);
            
            if (LocoTelem.CenterCar.ContainsKey(currentLoco))
                    LocoTelem.CenterCar.Remove(currentLoco);

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
