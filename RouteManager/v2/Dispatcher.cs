using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using RollingStock;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RouteManager.v2
{
    public class Dispatcher : MonoBehaviour
    {

        AutoEngineer Engineer;


        //Default unity hook.
        void Awake()
        {

            //Log status
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            RouteManager.logger.LogToDebug("Dispatcher Initializing");
            RouteManager.logger.LogToDebug("    Dispatcher Version".PadRight(30) + RouteManager.getModVersion());
            RouteManager.logger.LogToDebug("    Dispatcher Mod Manager".PadRight(30) + RouteManager.getModLoader());
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            //Hook the map load event to build route data at load. 
            Messenger.Default.Register<MapDidLoadEvent>(this, GameMapLoaded);
            
            //Hook the map unload event to gracefully stop all instances prior to map unload. 
            Messenger.Default.Register<MapDidUnloadEvent>(this, GameMapUnloaded);

            Engineer = new AutoEngineer();

            //Log status
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            RouteManager.logger.LogToDebug("Dispatcher Ready!");
            RouteManager.logger.LogToDebug("--------------------------------------------------------------------------------------------------");
        }



        //Default unity hook.
        void Update()
        {
            //Trace Logging
            //Disable LogMessage for Update Thread unless REALLY NEEDED. 
            //RouteManager.logger.LogToDebug("ENTERED FUNCTION: Update", LogLevel.Trace);

            //Only do stuff if we have an actively controlled consist
            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                List<Car> keys = LocoTelem.locomotiveCoroutines.Keys.ToList();

                //For each locomotive in the Coroutine list
                foreach (Car currentLoco in keys) 
                {
                    //Disable LogMessage for Update Thread unless REALLY NEEDED. 
                    //RouteManager.logger.LogToDebug(String.Format("Loco {0} has not called coroutine. Calling Coroutine for {1}",currentLoco,currentLoco.DisplayName),LogLevel.Verbose);

                    //RouteManager.logger.LogToDebug($"Coroutine LocoTelem.locomotiveCoroutines[currentLoco] value was {LocoTelem.locomotiveCoroutines[currentLoco]}");
                    //RouteManager.logger.LogToDebug($"Coroutine LocoTelem.RouteMode[currentLoco] value was {LocoTelem.RouteMode[currentLoco]}");

                    if (!LocoTelem.locomotiveCoroutines[currentLoco] && LocoTelem.RouteMode[currentLoco])
                    {
                        RouteManager.logger.LogToDebug($"loco {currentLoco.DisplayName} currently has not called a coroutine - Calling the Coroutine with {currentLoco.DisplayName} as an arguement");

                        LocoTelem.locomotiveCoroutines[currentLoco] = true;

                        prepareDataStructures(currentLoco);

                        if(RouteManager.Settings.experimentalUI)
                        {
                            StartCoroutine(Engineer.AutoEngineerControlRoutine_dev(currentLoco));
                        }
                        else
                        {
                            StartCoroutine(Engineer.AutoEngineerControlRoutine(currentLoco));
                        }

                    }
                    else if (LocoTelem.locomotiveCoroutines.ContainsKey(currentLoco))
                    {
                        if (LocoTelem.locomotiveCoroutines[currentLoco] && !LocoTelem.RouteMode[currentLoco])
                        {
                            RouteManager.logger.LogToDebug($"loco {currentLoco.DisplayName} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {currentLoco.DisplayName}");

                            if (RouteManager.Settings.experimentalUI)
                            {
                                StopCoroutine(Engineer.AutoEngineerControlRoutine_dev(currentLoco));
                            }
                            else
                            {
                                StopCoroutine(Engineer.AutoEngineerControlRoutine(currentLoco));
                            }
                            LocoTelem.locomotiveCoroutines[currentLoco] = false;

                            RouteManager.logger.LogToDebug($"Stopped Coroutine for {currentLoco.DisplayName}");
                            //cleanDataStructures(currentLoco);
                        }
                    }
                }
            }

            //Trace Logging
            //Disable LogMessage for Update Thread unless REALLY NEEDED. 
            //RouteManager.logger.LogToDebug("EXITING FUNCTION: Update", LogLevel.Trace);
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

            if (!LocoTelem.locoTravelingForward.ContainsKey(currentLoco))
                LocoTelem.locoTravelingForward[currentLoco] = true;

            if (!LocoTelem.needToUpdatePassengerCoaches.ContainsKey(currentLoco))
                LocoTelem.needToUpdatePassengerCoaches[currentLoco] = false;

            if (!LocoTelem.previousDestinations.ContainsKey(currentLoco))
                LocoTelem.previousDestinations[currentLoco] = new List<PassengerStop>();

            if (!LocoTelem.UIStationEntries.ContainsKey(currentLoco))
                LocoTelem.UIStationEntries[currentLoco] = new List<PassengerStop>();

        }

        private void cleanDataStructures(Car currentLoco)
        {
            if (LocoTelem.TransitMode.ContainsKey(currentLoco))
                LocoTelem.TransitMode.Remove(currentLoco);

            if (LocoTelem.RMMaxSpeed.ContainsKey(currentLoco))
                LocoTelem.RMMaxSpeed.Remove(currentLoco);

            if (LocoTelem.approachWhistleSounded.ContainsKey(currentLoco))
                LocoTelem.approachWhistleSounded.Remove(currentLoco);

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
            LocoTelem.locomotiveCoroutines.Clear();
            LocoTelem.RouteMode.Clear();
            LocoTelem.TransitMode.Clear();
            LocoTelem.CenterCar.Clear();
            LocoTelem.RMMaxSpeed.Clear();
            LocoTelem.initialSpeedSliderSet.Clear();
            LocoTelem.approachWhistleSounded.Clear();
            LocoTelem.clearedForDeparture.Clear();
            LocoTelem.locoTravelingEastWard.Clear();
            LocoTelem.locoTravelingForward.Clear();
            LocoTelem.needToUpdatePassengerCoaches.Clear();
            LocoTelem.closestStationNeedsUpdated.Clear();
            LocoTelem.closestStation.Clear();
            LocoTelem.currentDestination.Clear();
            LocoTelem.previousDestinations.Clear();
            LocoTelem.lowFuelQuantities.Clear();
            LocoTelem.UIStopStationSelections.Clear();
            LocoTelem.UIPickupStationSelections.Clear();
            LocoTelem.UITransferStationSelections.Clear();
            LocoTelem.stopStations.Clear();
            LocoTelem.pickupStations.Clear();
            LocoTelem.transferStations.Clear();
            LocoTelem.relevantPassengers.Clear();
        }


        //Map is about to unload so lets cleanup all the instances of the mod.
        //Needed to Prevent crash on exit. 
        private void GameMapUnloaded(MapDidUnloadEvent mapDidUnloadEvent)
        {
            RouteManager.logger.LogToDebug("GAME MAP UNLOAD TRIGGERED");

            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                RouteManager.logger.LogToDebug("Stopping all Dispatcher AI Instances");

                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {
                    if (RouteManager.Settings.experimentalUI)
                    {
                        StopCoroutine(Engineer.AutoEngineerControlRoutine_dev(keys[i]));
                    }
                    else
                    {
                        StopCoroutine(Engineer.AutoEngineerControlRoutine(keys[i]));
                    }
                    //Attempt to prevent trains from taking off before route manager can be re-configured
                    try
                    {
                        StateManager.ApplyLocal(new AutoEngineerCommand(keys[i].id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[keys[i]], 0, null));
                    }
                    catch { }

                }

                clearDicts();
            }
        }

        private void GameMapLoaded(MapDidLoadEvent mapDidLoadEvent)
        {
            RouteManager.logger.LogToDebug("GAME MAP LOAD TRIGGERED");

            StationInformation.BuildStationMap();
            StationInformation.BuildOrderedList();

            RouteManager.logger.LogToDebug("GAME MAP LOAD COMPLETE");
        }
    }
}
