using BepInEx.Logging;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
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

        AutoEngineer Engineer;


        //Default unity hook.
        void Awake()
        {

            //Log status
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("Dispatcher Initializing");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            //Hook the map unload event to gracefully stop all instances prior to map unload. 
            Messenger.Default.Register<MapDidUnloadEvent>(this, GameMapUnloaded);

            Engineer = new AutoEngineer();

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
                foreach (Car currentLoco in keys) 
                {
                    //Disable LogMessage for Update Thread unless REALLY NEEDED. 
                    //Logger.LogToDebug(String.Format("Loco {0} has not called coroutine. Calling Coroutine for {1}",currentLoco,currentLoco.DisplayName),Logger.logLevel.Verbose);

                    //Logger.LogToDebug($"Coroutine LocoTelem.locomotiveCoroutines[currentLoco] value was {LocoTelem.locomotiveCoroutines[currentLoco]}");
                    //Logger.LogToDebug($"Coroutine LocoTelem.RouteMode[currentLoco] value was {LocoTelem.RouteMode[currentLoco]}");

                    if (!LocoTelem.locomotiveCoroutines[currentLoco] && LocoTelem.RouteMode[currentLoco])
                    {
                        Logger.LogToDebug($"loco {currentLoco.DisplayName} currently has not called a coroutine - Calling the Coroutine with {currentLoco.DisplayName} as an arguement");

                        LocoTelem.locomotiveCoroutines[currentLoco] = true;

                        prepareDataStructures(currentLoco);

                        StartCoroutine(Engineer.AutoEngineerControlRoutine(currentLoco));

                    }
                    else if (LocoTelem.locomotiveCoroutines.ContainsKey(currentLoco))
                    {
                        if (LocoTelem.locomotiveCoroutines[currentLoco] && !LocoTelem.RouteMode[currentLoco])
                        {
                            Logger.LogToDebug($"loco {currentLoco.DisplayName} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {currentLoco.DisplayName}");

                            StopCoroutine(Engineer.AutoEngineerControlRoutine(currentLoco));

                            LocoTelem.locomotiveCoroutines[currentLoco] = false;

                            Logger.LogToDebug($"Stopped Coroutine for {currentLoco.DisplayName}");
                            //cleanDataStructures(currentLoco);
                        }
                    }
                }
            }

            //Trace Logging
            //Disable LogMessage for Update Thread unless REALLY NEEDED. 
            //Logger.LogToDebug("EXITING FUNCTION: Update", Logger.logLevel.Trace);
        }


        private void prepareDataStructures(Car currentLoco)
        {
            if (!LocoTelem.TransitMode.ContainsKey(currentLoco))
                LocoTelem.TransitMode[currentLoco] = true;

            if (!LocoTelem.RMMaxSpeed.ContainsKey(currentLoco))
                LocoTelem.RMMaxSpeed[currentLoco] = 45;

            if (!LocoTelem.approachWhistleSounded.ContainsKey(currentLoco))
                LocoTelem.approachWhistleSounded[currentLoco] = false;

            if (!LocoTelem.lowFuelQuantities.ContainsKey(currentLoco))
                LocoTelem.lowFuelQuantities[currentLoco] = new Dictionary<string, float>();

            if (!LocoTelem.closestStation.ContainsKey(currentLoco))
                LocoTelem.closestStation[currentLoco] = (null, 0);

            if (!LocoTelem.currentDestination.ContainsKey(currentLoco))
                LocoTelem.currentDestination[currentLoco] = default(PassengerStop);

            if (!LocoTelem.clearedForDeparture.ContainsKey(currentLoco))
                LocoTelem.clearedForDeparture[currentLoco] = false;

            if (!LocoTelem.CenterCar.ContainsKey(currentLoco))
                LocoTelem.CenterCar[currentLoco] = currentLoco;

            if (!LocoTelem.locoTravelingEastWard.ContainsKey(currentLoco))
                LocoTelem.locoTravelingEastWard[currentLoco] = true;

            if (!LocoTelem.needToUpdatePassengerCoaches.ContainsKey(currentLoco))
                LocoTelem.needToUpdatePassengerCoaches[currentLoco] = false;

        }

        private void cleanDataStructures(Car currentLoco)
        {
            if (LocoTelem.TransitMode.ContainsKey(currentLoco))
                LocoTelem.TransitMode.Remove(currentLoco);

            if (LocoTelem.RMMaxSpeed.ContainsKey(currentLoco))
                LocoTelem.RMMaxSpeed.Remove(currentLoco);

            if (LocoTelem.approachWhistleSounded.ContainsKey(currentLoco))
                LocoTelem.approachWhistleSounded.Remove(currentLoco);

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

            if (LocoTelem.needToUpdatePassengerCoaches.ContainsKey(currentLoco))
                LocoTelem.needToUpdatePassengerCoaches.Remove(currentLoco);

        }


        private void clearDicts()
        {
            LocoTelem.TransitMode.Clear();
            LocoTelem.RMMaxSpeed.Clear();
            LocoTelem.approachWhistleSounded.Clear();
            LocoTelem.LocomotivePrevDestination.Clear();
            LocoTelem.locomotiveCoroutines.Clear();
            LocoTelem.lowFuelQuantities.Clear();
            LocoTelem.closestStation.Clear();
            LocoTelem.currentDestination.Clear();
            LocoTelem.clearedForDeparture.Clear();
            LocoTelem.CenterCar.Clear();
            LocoTelem.needToUpdatePassengerCoaches.Clear();
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
                    StopCoroutine(Engineer.AutoEngineerControlRoutine(keys[i]));

                    //Attempt to prevent trains from taking off before route manager can be re-configured
                    try
                    {
                        StateManager.ApplyLocal(new AutoEngineerCommand(keys[i].id, AutoEngineerMode.Road, LocoTelem.locoTravelingEastWard[keys[i]], 0, null));
                    }
                    catch { }

                }

                clearDicts();
            }
        }
    }
}
