using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using Model.OpsNew;
using RouteManager.v2.dataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RouteManager.v2.Logging;
using Network;
using RollingStock;
using Track;
using static Game.Reputation.PassengerReputationCalculator;

namespace RouteManager.v2.core
{
    public class AutoEngineer : MonoBehaviour
    {
        public IEnumerator AutoEngineerControlRoutine(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: AutoEngineerControlRoutine", LogLevel.Trace);

            //Debug
            RouteManager.logger.LogToDebug(String.Format("Coroutine Triggered!", locomotive.DisplayName, LocoTelem.RouteMode[locomotive]), LogLevel.Verbose);
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} \t Route Mode: {1}", locomotive.DisplayName, LocoTelem.RouteMode[locomotive]), LogLevel.Debug);

            //Setup departure clearances
            LocoTelem.clearedForDeparture[locomotive] = false;

            RouteManager.logger.LogToDebug(String.Format("Loco: {0} \t has ID: {1}", locomotive.DisplayName, locomotive.id), LogLevel.Debug);
            LocoTelem.locoTravelingEastWard[locomotive] = true;
            LocoTelem.locoTravelingForward[locomotive] = true;

            //Set some initial values
            LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation(locomotive);
            LocoTelem.currentDestination[locomotive] = StationManager.getInitialDestination(locomotive);
            LocoTelem.closestStationNeedsUpdated[locomotive] = false;
            LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);
            LocoTelem.needToUpdatePassengerCoaches[locomotive] = true;

            //Feature Ehancement #30
            LocoTelem.initialSpeedSliderSet[locomotive] = false;

            //Route Mode is enabled!
            while (LocoTelem.RouteMode[locomotive])
            {
                if (needToExitCoroutine(locomotive))
                {
                    yield break;
                }

                //Update passenger markers as needed.
                if (LocoTelem.needToUpdatePassengerCoaches[locomotive])
                    TrainManager.CopyStationsFromLocoToCoaches(locomotive);

                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} center of train is car {1}", locomotive.DisplayName, LocoTelem.CenterCar[locomotive].DisplayName), LogLevel.Verbose);

                if (LocoTelem.TransitMode[locomotive])
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is entering into transit mode", locomotive.DisplayName), LogLevel.Verbose);
                    yield return locomotiveTransitControl(locomotive);
                }
                else
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is entering into Station Stop mode", locomotive.DisplayName), LogLevel.Verbose);
                    yield return locomotiveStationStopControl(locomotive);
                }

                yield return null;
            }

            //Locomotive is no longer in Route Mode
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} \t Route mode was disabled! Stopping Coroutine.", locomotive.DisplayName, LogLevel.Debug));

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: AutoEngineerControlRoutine", LogLevel.Trace);
            yield break;
        }

        //Locomotive Enroute to Destination
        public IEnumerator locomotiveTransitControl(Car locomotive)
        {

            //Are we in a station?
            if (StationManager.isTrainInStation(locomotive))
            {
                //Is that station the same as our current destination?
                if (LocoTelem.currentDestination[locomotive].identifier == LocoTelem.closestStation[locomotive].Item1.identifier)
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} already at first station", locomotive.DisplayName, locomotive.Orientation), LogLevel.Verbose);
                    LocoTelem.TransitMode[locomotive] = false;
                    yield return new WaitForSeconds(1);
                }
            }

            //Determine direction to move
            RouteManager.logger.LogToDebug(String.Format("Loco {0} has an orientation of {1}", locomotive.DisplayName, locomotive.Orientation), LogLevel.Verbose);

            //Move in that direction

            //TEMP LOGIC
            float distanceToStation     = float.MaxValue;
            bool  delayExecution        = false;
            float olddist               = float.MaxValue;
            float trainVelocity         = 0;
            int stationPadding          = 10;


            //Loop through transit logic
            while (LocoTelem.TransitMode[locomotive])
            {
                if (needToExitCoroutine(locomotive))
                {
                    yield break;
                }

                //Refueling
                while (LocoTelem.RouteModePaused[locomotive])
                {
                    yield return null;
                }


                olddist = distanceToStation;


                //Getting close to a station update some values...
                //Cheeky optimization to reduce excessive logging...
                if (distanceToStation != float.MaxValue)
                {
                    if (Math.Abs(distanceToStation) <= 1000 && LocoTelem.closestStationNeedsUpdated[locomotive])
                    {
                        //Update Center & closest station
                        LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);
                        LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation_dev(locomotive);
                        LocoTelem.closestStationNeedsUpdated[locomotive] = false;
                    }
                    else if (Math.Abs(distanceToStation) > 1000)
                    {
                        //Reset closestStationNeedsUpdated
                        LocoTelem.closestStationNeedsUpdated[locomotive] = true;
                    }
                }

                /*****************************************************************
                 * 
                 * Distance To Station Check
                 * 
                 *****************************************************************/

                try
                {
                    distanceToStation = DestinationManager.GetDistanceToStation(locomotive, LocoTelem.currentDestination[locomotive]);
                    delayExecution = false;
                }
                catch (Exception e)
                {
                    RouteManager.logger.LogToDebug(e.Message, LogLevel.Error);
                    RouteManager.logger.LogToDebug(e.StackTrace, LogLevel.Error);
                    //If after delaying execution for 5 seconds, stop coroutine for locomotive
                    if (delayExecution)
                    {
                        RouteManager.logger.LogToConsole("Unable to determine distance to station. Disabling Dispatcher control of locomotive: " + locomotive.DisplayName);
                        if (RouteManager.Settings.experimentalUI)
                        {
                            StopCoroutine(AutoEngineerControlRoutine_dev(locomotive));
                        }
                        else
                        {
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                        }
                    }

                    //Try again in 5 seconds
                    RouteManager.logger.LogToDebug(String.Format("Distance to station could not be calculated for {0}. Yielding for 5s", locomotive.DisplayName), LogLevel.Debug);
                    delayExecution = true;
                }

                //Distance to station was not in acceptable range... try again later
                if (distanceToStation <= -6969f)
                {
                    delayExecution = true;
                }

                //Try again in 5 seconds
                if (delayExecution)
                {
                    yield return new WaitForSeconds(5);
                }

                /*****************************************************************
                 * 
                 * END Distance To Station Check
                 * 
                 *****************************************************************/

                /*****************************************************************
                 * 
                 * Start Locomotive Direction Check
                 * 
                 *****************************************************************/

                //We may be able to avoid this with better logic elsewhere...
                RouteManager.logger.LogToDebug(String.Format("Locomotive: {0} Distance to Station: {1} Prev Distance: {2}", locomotive.DisplayName, distanceToStation, olddist), LogLevel.Verbose);

                //Brute force Bug Fix
                //In certain instances timing can cause some stops such as Alarka -> Hemmingway to have moments where the delta is less than 1 or 2 and 
                //the value momentarily goes negative. So lets only run this if the delta is greater than 5
                if (Math.Abs(olddist - distanceToStation) > 5)
                {
                    if (distanceToStation > olddist)
                    {
                        LocoTelem.locoTravelingForward[locomotive] = !LocoTelem.locoTravelingForward[locomotive];

                        RouteManager.logger.LogToDebug("Was driving in the wrong direction! Changing direction");
                        RouteManager.logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}", LogLevel.Debug);

                        StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

                        //Wait until loco has started going in the correct direction
                        while (distanceToStation > olddist)
                        {
                            yield return new WaitForSeconds(1);

                            RouteManager.logger.LogToDebug("Was driving in the wrong direction! Waiting until turned around.");

                            olddist = distanceToStation;
                            distanceToStation = DestinationManager.GetDistanceToStation(locomotive, LocoTelem.currentDestination[locomotive]);
                        }
                    }
                }

                /*****************************************************************
                 * 
                 * END Locomotive Direction Check
                 * 
                 *****************************************************************/

                /*****************************************************************
                 * 
                 * START Locomotive Movements
                 * 
                 *****************************************************************/

                //Get Current train speed.
                trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);

                //Enroute to Destination
                if (distanceToStation > 500)
                {
                    RouteManager.logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}");
                    generalTransit(locomotive);

                    yield return new WaitForSeconds(5);
                }
                //Entering Destination Boundary
                else if (distanceToStation <= 400 && distanceToStation > 300)
                {
                    onApproachLongDist(locomotive);

                    //Hack to work around the new auto engineer crossing detection to prevent double blow / weird horn blow behavior. 
                    //This can be done better but further research is required. In the mean time this crude hack hopefully will reduce the occurances. 
                    if (!LocoTelem.approachWhistleSounded[locomotive] &&

                        ((locomotive.KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Horn)].FloatValue) <= 0f &&
                        (locomotive.KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Bell)].BoolValue) != true))
                    {
                        RouteManager.logger.LogToDebug(String.Format("Locomotive {0} activating Appproach Whistle", locomotive.DisplayName), LogLevel.Verbose);
                        yield return TrainManager.RMblow(locomotive, 0.25f, 1.5f);
                        yield return TrainManager.RMblow(locomotive, 1f, 2.5f);
                        yield return TrainManager.RMblow(locomotive, 1f, 1.75f, 0.25f);
                        yield return TrainManager.RMblow(locomotive, 1f, 0.25f);
                        yield return TrainManager.RMblow(locomotive, 0f);
                        LocoTelem.approachWhistleSounded[locomotive] = true;
                    }

                    yield return new WaitForSeconds(1);
                }
                //Approaching platform
                else if (distanceToStation <= 300 && distanceToStation > 100)
                {
                    onApproachMediumDist(locomotive, distanceToStation);
                    yield return new WaitForSeconds(1);
                }
                //Entering Platform
                else if (distanceToStation <= 100 && distanceToStation > stationPadding)
                {
                    onApproachShortDist(locomotive, distanceToStation);
                    yield return new WaitForSeconds(1);
                }
                //Train in platform
                else if (distanceToStation <= stationPadding && distanceToStation >= 0)
                {
                    onArrival(locomotive);
                    yield return new WaitForSeconds(1);
                }

                /*****************************************************************
                 * 
                 * END Locomotive Movements
                 * 
                 *****************************************************************/

                yield return null;
            }

            yield return null;
        }

        //Stopped at station
        private IEnumerator locomotiveStationStopControl(Car locomotive)
        {
            float currentTrainVelocity = 100f;

            //Loop through station logic while loco is not in transit mode...
            while (!LocoTelem.TransitMode[locomotive])
            {

                if (needToExitCoroutine(locomotive))
                {
                    yield break;
                }

                //Refueling
                while (LocoTelem.RouteModePaused[locomotive])
                {
                    yield return null;
                }

                AutoEngineerPersistence persistence = new AutoEngineerPersistence(locomotive.KeyValueObject);
                if (persistence.Orders.MaxSpeedMph > 0)
                {
                    LocoTelem.TransitMode[locomotive] = true;
                    yield break;
                }

                //Ensure the train is at a complete stop. Else wait for it to stop...
                while ((currentTrainVelocity = TrainManager.GetTrainVelocity(locomotive)) > .1f)
                {
                    if (currentTrainVelocity > 0.1)
                    {
                        yield return new WaitForSeconds(1);
                    }
                    else
                    {
                        if (!LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.currentDestination[locomotive]))
                            LocoTelem.previousDestinations[locomotive].Add(LocoTelem.currentDestination[locomotive]);
                        yield return new WaitForSeconds(3);
                    }
                }

                //Prior to loading, if CURRENT DESTINATION is the end of the line, then lets reset the coaches
                if (StationManager.currentlyAtLastStation(locomotive))
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is at last station stop. Copy stations to cars for return trip",locomotive.DisplayName));

                    TrainManager.CopyStationsFromLocoToCoaches(locomotive);
                }

                //Now that train is stopped, perform station ops and check fuel quantities before departure.
                if (wasCurrentStopServed(locomotive) && checkFuelQuantities(locomotive))
                    LocoTelem.clearedForDeparture[locomotive] = true;

                //Loco now clear for station departure. 
                if (LocoTelem.clearedForDeparture[locomotive])
                {
                    //Store previous station
                    if (!LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.currentDestination[locomotive]))
                        LocoTelem.previousDestinations[locomotive].Add(LocoTelem.currentDestination[locomotive]);


                    //Notate we are cleared for departure
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is cleared for departure. Determining next station", locomotive.DisplayName));

                    //Update Destination
                    LocoTelem.currentDestination[locomotive] = StationManager.getNextStation(locomotive);
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} currentDestination is now {1}", locomotive.DisplayName, LocoTelem.currentDestination[locomotive].identifier), LogLevel.Debug);

                    LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation(locomotive);
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} closestStation is now {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.identifier), LogLevel.Debug);

                    //Only do these things if we really should depart the station.
                    if (LocoTelem.currentDestination[locomotive] != LocoTelem.previousDestinations[locomotive].LastOrDefault())
                    {
                        //Transition to transit mode
                        LocoTelem.TransitMode[locomotive] = true;
                        LocoTelem.clearedForDeparture[locomotive] = false;

                        //Feature Enahncement: Issue #24
                        //Write to console the departure of the train consist at station X
                        //Bugfix: message would previously be generated even when departure was not cleared. 


                        if (RouteManager.Settings.showDepartureMessage)
                            RouteManager.logger.LogToConsole(String.Format("{0} has departed {1} for {2}", Hyperlink.To(locomotive), LocoTelem.previousDestinations[locomotive].Last().DisplayName.ToUpper(), LocoTelem.currentDestination[locomotive].DisplayName.ToUpper()));
                    }
                }
                else
                {
                    if (LocoTelem.lowFuelQuantities[locomotive].Count != 0)
                    {
                        string holdLocationName = LocoTelem.currentDestination[locomotive].DisplayName;

                        //Generate warning for each type of low fuel.
                        foreach (KeyValuePair<string, float> type in LocoTelem.lowFuelQuantities[locomotive])
                        {
                            if (type.Key == "coal")
                            {
                                RouteManager.logger.LogToConsole(String.Format("Locomotive {0} is low on coal and is holding at {1}", Hyperlink.To(locomotive), holdLocationName));
                            }
                            if (type.Key == "water")
                            {
                                RouteManager.logger.LogToConsole(String.Format("Locomotive {0} is low on water and is holding at {1}", Hyperlink.To(locomotive), holdLocationName));
                            }

                            if (type.Key == "diesel-fuel")
                            {
                                RouteManager.logger.LogToConsole(String.Format("Locomotive {0} is low on diesel and is holding at {1}", Hyperlink.To(locomotive), holdLocationName));
                            }
                        }
                        yield return new WaitForSeconds(30);
                    }
                    yield return new WaitForSeconds(5);
                }
            }

            yield return null;
        }

        //Train is enroute to destination
        private static void generalTransit(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: generalTransit", LogLevel.Trace);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} triggered General Transit.", locomotive.DisplayName), LogLevel.Verbose);

            //Set AI Maximum speed
            //Track max speed takes precedence. 
            //ImplementFeature enhancement #30
            //LocoTelem.RMMaxSpeed[locomotive] = 100f;

            //Optimization of code. No need to repetitively spam the statemanager with data that is not changing.
            //Apply Updated Max Speed

            if (LocoTelem.initialSpeedSliderSet[locomotive] == false)
            {
                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], 45, null));
                LocoTelem.initialSpeedSliderSet[locomotive] = true;
            }
            else
            {
                AutoEngineerPersistence persistence = new AutoEngineerPersistence(locomotive.KeyValueObject);

                if (LocoTelem.locoTravelingForward[locomotive] != persistence.Orders.Forward || (int)LocoTelem.RMMaxSpeed[locomotive] != (int)persistence.Orders.MaxSpeedMph)
                    StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));
            }

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: generalTransit", LogLevel.Trace);
        }

        //Train is approaching location
        private static void onApproachLongDist(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: onApproachLongDist", LogLevel.Trace);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} triggered Long Approach.", locomotive.DisplayName), LogLevel.Verbose);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} on Long Approach: Speed limited to {1}", locomotive.DisplayName, LocoTelem.RMMaxSpeed[locomotive]), LogLevel.Debug);

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: onApproachLongDist", LogLevel.Trace);
        }

        //Train is approaching platform
        private static void onApproachMediumDist(Car locomotive, float distanceToStation, float divisor = 16f)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: onApproachMediumDist", LogLevel.Trace);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} triggered Medium Approach.", locomotive.DisplayName), LogLevel.Verbose);

            //Gradually reduce maximum speed the closer to the platform we get. 
            float calculatedSpeed = distanceToStation / divisor; //prev: 10f - brakes more aggressive since update 2024.1.0 we need to slow down a little sooner

            //Prevent overspeed.
            if (calculatedSpeed > LocoTelem.RMMaxSpeed[locomotive])
                calculatedSpeed = LocoTelem.RMMaxSpeed[locomotive];

            //Minimum speed should not be less than 15Mph - testing 10Mph due to undershoot/excessive breaking
            if (calculatedSpeed < 10f)
            {
                calculatedSpeed = 10f;
            }

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} on Medium Approach: Speed limited to {1}", locomotive.DisplayName, (int)calculatedSpeed), LogLevel.Debug);

            //Apply Updated Max Speed
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], (int)calculatedSpeed, null));

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: onApproachMediumDist", LogLevel.Trace);
        }

        //Train is entering platform
        private static void onApproachShortDist(Car locomotive, float distanceToStation, float divisor = 10f)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: onApproachShortDist", LogLevel.Trace);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} triggered Short Approach.", locomotive.DisplayName), LogLevel.Verbose);

            //Gradually reduce maximum speed the closer to the platform we get. 
            float calculatedSpeed = distanceToStation / divisor; //prev 6f.  100/6 = 16; 16 > min long distance approach of 15 so we could speed up when getting closer

            //Prevent overspeed.
            if (calculatedSpeed > LocoTelem.RMMaxSpeed[locomotive])
                calculatedSpeed = LocoTelem.RMMaxSpeed[locomotive];

            //Minimum speed should not be less than 5Mph
            if (calculatedSpeed < 5f)
            {
                //Set max speed to 5 mph for now.
                calculatedSpeed = 5f;

                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} activating Approach Bell at speed <=5", locomotive.DisplayName), LogLevel.Verbose);
                TrainManager.RMbell(locomotive, true);
            }

            if (distanceToStation < 50)
            {
                //Apply Bell
                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} activating Approach Bell at distance 50", locomotive.DisplayName), LogLevel.Verbose);
                TrainManager.RMbell(locomotive, true);
            }

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} on Short Approach! Speed limited to {1}", locomotive.DisplayName, (int)calculatedSpeed), LogLevel.Debug);

            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], (int) calculatedSpeed, null));

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: onApproachShortDist", LogLevel.Trace);
        }

        //Train Arrived at station
        private static void onArrival(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: onArrival", LogLevel.Trace);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} triggered on Arrival.", locomotive.DisplayName), LogLevel.Verbose);

            //Train Arrived
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], 0, null));

            //Wait for loco to crawl to a stop. 
            if (Math.Abs(locomotive.velocity * 2.23694f) < .1f)
            {
                //Disable bell
                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} deactivating Approach Bell", locomotive.DisplayName), LogLevel.Verbose);
                TrainManager.RMbell(locomotive, false);

                //Reset Approach whistle
                LocoTelem.approachWhistleSounded[locomotive] = false;

                //Disable transit mode.
                LocoTelem.TransitMode[locomotive] = false;

                if (RouteManager.Settings.showArrivalMessage)
                    RouteManager.logger.LogToConsole(String.Format("{0} has arrived at {1}", Hyperlink.To(locomotive), LocoTelem.currentDestination[locomotive].DisplayName.ToUpper()));
            }



            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: onArrival", LogLevel.Trace);
        }

        //Check to see if passengers are unloaded
        private static bool wasCurrentStopServed(Car locomotive)
        {
            bool carsLoaded = true;
            bool passWaiting = true;
            bool trainFull = false;

            //Initialize Variable
            PassengerMarker? marker = default(PassengerMarker);

            carsLoaded = carsStillLoaded(locomotive, marker);

            passWaiting = passStillWaiting(locomotive, marker);

            trainFull = TrainManager.isTrainFull(locomotive);

            //Must unload all passengers for the current station
            if (!carsLoaded)
            {
                //No passengers to unload, however the train is full...
                //Looks like those waiting passengers are going to have to find a train with room.
                if (trainFull)
                {
                    //Only notify if not configured to wait until full

                    if (!RouteManager.Settings.waitUntilFull)
                        RouteManager.logger.LogToConsole(String.Format("Locomotive {0} consist is full. No room for additional passengers!", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName));

                    return true;
                }

                //Station has passengers destined for a scheduled station
                if (!passWaiting && !RouteManager.Settings.waitUntilFull)
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} consist has finished loading and unloading at {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName), LogLevel.Verbose);
                    return true;
                }
            }

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} consist has not finished loading and unloading at {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName), LogLevel.Verbose);

            //Always assume stop has not been served unless determined otherwise. 
            return false;
        } 

        private static bool carsStillLoaded(Car locomotive, PassengerMarker? marker)
        {
            bool loadCheck = false;

            foreach (Car currentCar in locomotive.EnumerateCoupled())
            {
                //If the current car is a passenger car lets try to get the passenger data.
                if (currentCar.Archetype == Model.Definition.CarArchetype.Coach)
                {
                    //Get data for the current car
                    marker = currentCar.GetPassengerMarker();

                    //Since this type is nullable, make sure its not null...
                    if (marker != null && marker.HasValue)
                    {
                        //Loop through the list of passenger data for the current coach.
                        foreach (var stop in marker.Value.Groups)
                        {
                            //RouteManager.logger.LogToDebug(String.Format("Passenger Car {0} has {1} passengers for the stop at {2}", currentCar.DisplayName, stop.Count, stop.Destination), LogLevel.Verbose);
                            if (stop.Destination == LocoTelem.currentDestination[locomotive].identifier)
                            {
                                loadCheck = true;
                            }
                        }
                        //StationManager.getNumberPassengersWaitingForDestination()
                        //RouteManager.logger.LogToDebug(String.Format("Passenger Car {0} has {1} passengers for the current stop at {2}", currentCar.DisplayName, marker.Value.CountPassengersForStop(LocoTelem.currentDestination[locomotive].identifier), LocoTelem.currentDestination[locomotive].identifier), LogLevel.Verbose);
                    }
                }
            }

            if (loadCheck)
                RouteManager.logger.LogToDebug(String.Format("Loco {0} consist contains passengers for current stop", locomotive.DisplayName), LogLevel.Debug);
            else
                RouteManager.logger.LogToDebug(String.Format("Loco {0} consist empty for current stop", locomotive.DisplayName), LogLevel.Debug);

            return loadCheck;
        }

        private static bool passStillWaiting(Car locomotive, PassengerMarker? marker)
        {

            foreach (Car currentCar in locomotive.EnumerateCoupled())
            {
                //If the current car is a passenger car lets try to get the passenger data.
                if (currentCar.Archetype == Model.Definition.CarArchetype.Coach)
                {
                    //Get data for the current car
                    marker = currentCar.GetPassengerMarker();

                    //Since this type is nullable, make sure its not null...
                    if (marker != null && marker.HasValue)
                    {
                        //Loop through the list of selected stations for the current coach.
                        foreach (var selectedStop in marker.Value.Destinations)
                        {
                            //If any destinations have a passsenger still in the platform bail out and return true. 
                            if (StationManager.getNumberPassengersWaitingForDestination(LocoTelem.closestStation[locomotive].Item1, selectedStop) > 0)
                            {
                                RouteManager.logger.LogToDebug(String.Format("Loco {0} still boarding for destination: {1}", locomotive.DisplayName, selectedStop), LogLevel.Debug);
                                return true;
                            }
                        }
                    }
                }
            }

            RouteManager.logger.LogToDebug(String.Format("Loco {0} all passengers boarded", locomotive.DisplayName), LogLevel.Debug);

            return false;
        }


        //Initial checks to determine if we can continue with the coroutine
        private bool needToExitCoroutine(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: needToExitCoroutine", LogLevel.Trace);

            try
            {
                //If no stations are selected for the locmotive, end the coroutine
                if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive))
                {
                    RouteManager.logger.LogToConsole("No stations selected. Stopping Coroutine for: " + locomotive.DisplayName);
                    TrainManager.SetRouteModeEnabled(false, locomotive);
                    return true;
                }

                //Engineer mode was changed and is no longer route mode
                if (!LocoTelem.RouteMode[locomotive])
                {
                    RouteManager.logger.LogToDebug("Locomotive no longer in Route Mode. Stopping Coroutine for: " + locomotive.DisplayName, LogLevel.Debug);
                    return true;
                }

                //If we have selected stations
                if (LocoTelem.stopStations[locomotive].Count <= 1)
                {
                    //If our previous destination key exists
                    if (LocoTelem.previousDestinations.ContainsKey(locomotive) && LocoTelem.currentDestination.ContainsKey(locomotive))
                    {
                        //If there is data for the loco in prev dest.
                        if (LocoTelem.previousDestinations[locomotive].Count >= 1)
                        {
                            //Only if our current dest = our last visited, consider the coroutine terminatable.
                            if (LocoTelem.currentDestination[locomotive] == LocoTelem.previousDestinations[locomotive].Last())
                            {
                                RouteManager.logger.LogToConsole(String.Format("{0} has no more stations. Halting Control.", Hyperlink.To(locomotive)));
                                TrainManager.SetRouteModeEnabled(false, locomotive);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RouteManager.logger.LogToConsole("{0} Internal Error Occurred Halting control!" + locomotive.DisplayName);
                RouteManager.logger.LogToError("{0} error in needToExitCoroutine" + locomotive.DisplayName);
                RouteManager.logger.LogToError(ex.ToString());
                RouteManager.logger.LogToError(ex.StackTrace);
                return true;
            }

            //Trace Method
            RouteManager.logger.LogToDebug("EXITING FUNCTION: needToExitCoroutine", LogLevel.Trace);
            return false;
        }

        private static bool checkFuelQuantities(Car locomotive)
        {
            //Update Fuel quantities
            TrainManager.locoLowFuelCheck(locomotive);

            if (LocoTelem.lowFuelQuantities[locomotive].Count == 0)
                return true;

            return false;
        }

        private static bool GetDirection(Car locomotive, PassengerStop stop)
        {
            bool stationIsToOurRight = StationManager.isStationRight(locomotive, stop);

            RouteManager.logger.LogToDebug($"Loco: {locomotive.DisplayName}, Facing right: {locomotive.Orientation > 0}, Station is to our right: {stationIsToOurRight}", LogLevel.Debug);

            //determine if we need to travel in forward or reverse mode based on destination and loco orientation
            if (stationIsToOurRight && locomotive.Orientation >= 0 ||
                !stationIsToOurRight && locomotive.Orientation < 0)
            {
                return true;
            }
            else if (stationIsToOurRight && locomotive.Orientation < 0 ||
                    !stationIsToOurRight && locomotive.Orientation >= 0)
            {
                return false;
            }
            else
            {
                return false;
            }
        }




        /************************************************************************************************************
         * 
         * 
         * 
         *                  Experimental / Developement / Preview features. 
         * 
         * 
         * 
         ************************************************************************************************************/


        public IEnumerator AutoEngineerControlRoutine_dev(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: AutoEngineerControlRoutine_dev", LogLevel.Trace);

            //Debug
            RouteManager.logger.LogToDebug("Dev Coroutine Triggered!", LogLevel.Verbose);
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} \t Route Mode: {1}", locomotive.DisplayName, LocoTelem.RouteMode[locomotive]), LogLevel.Debug);

            //Setup departure clearances
            LocoTelem.clearedForDeparture[locomotive] = false;

            RouteManager.logger.LogToDebug(String.Format("Loco: {0} \t has ID: {1}", locomotive.DisplayName, locomotive.id), LogLevel.Debug);

            //Set some initial values
            //closest station will only return a station marked as a stop
            LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation_dev(locomotive);
            LocoTelem.currentDestination[locomotive] = StationManager.getNextStation_dev(locomotive);

            //set locomotive drive direction
            LocoTelem.locoTravelingForward[locomotive] = GetDirection(locomotive, LocoTelem.currentDestination[locomotive]);

            //get route info - what are the switches on our route and what state do they need to be in?
            List<RouteSwitchData> switchRequirements;
            PassengerStop alarka = PassengerStop.FindAll().Where(stop => stop.identifier == "alarka").First();

            if (DestinationManager.GetRouteSwitches(locomotive.LocationF, (Track.Location)alarka.TrackSpans.First().lower, out switchRequirements))
            {
                //we have the total route to end of line/branch
                // Check if the next station has multiple platforms and find the last common switch
                DestinationManager.PlanNextRoute(locomotive.LocationF, LocoTelem.currentDestination[locomotive], ref switchRequirements);
            }
            else
            {
                //oh-oh we're flying blind!
            }

            LocoTelem.routeSwitchRequirements[locomotive] = switchRequirements;

            LocoTelem.closestStationNeedsUpdated[locomotive] = false;
            LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);

            //set initial passenger loading
            TrainManager.CopyStationsFromLocoToCoaches_dev(locomotive);

            RouteManager.logger.LogToDebug($"Copy complete", LogLevel.Debug);

            RouteManager.logger.LogToDebug($"Closest station: {LocoTelem.closestStation.ContainsKey(locomotive)} Center Car: {LocoTelem.CenterCar.ContainsKey(locomotive)}", LogLevel.Debug);

            //Give time for passenger loading/unloading if already at the station
            if (Vector3.Distance(LocoTelem.closestStation[locomotive].Item1.CenterPoint, LocoTelem.CenterCar[locomotive].GetCenterPosition(Graph.Shared)) <= 15f) //StationManager.isTrainInStation(LocoTelem.CenterCar[locomotive]))
            {
                RouteManager.logger.LogToDebug($"We're close", LogLevel.Debug);

                while (!wasCurrentStopServed_dev(locomotive))
                {
                    yield return new WaitForSeconds(1);
                }

                RouteManager.logger.LogToDebug($"Stop Served", LogLevel.Debug);
            }


            if (RouteManager.Settings.showDepartureMessage)
                RouteManager.logger.LogToConsole(String.Format("{0} has departed for {1}", Hyperlink.To(locomotive), LocoTelem.currentDestination[locomotive].DisplayName.ToUpper()));

            RouteManager.logger.LogToDebug($"Clearing", LogLevel.Debug);
            LocoTelem.clearedForDeparture[locomotive] = true;
            RouteManager.logger.LogToDebug($"Cleared", LogLevel.Debug);



            /**************************************************
             * 
             * Initialisation complete, start main routines
             * 
            ***************************************************/

            //Feature Ehancement #30
            LocoTelem.initialSpeedSliderSet[locomotive] = false;

            //Route Mode is enabled!
            while (LocoTelem.RouteMode[locomotive])
            {
                if (needToExitCoroutine_dev(locomotive))
                {
                    yield break;
                }

                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} center of train is car {1}", locomotive.DisplayName, LocoTelem.CenterCar[locomotive].DisplayName), LogLevel.Verbose);

                if (LocoTelem.TransitMode[locomotive])
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is entering into transit mode", locomotive.DisplayName), LogLevel.Verbose);
                    yield return locomotiveTransitControl_dev(locomotive);
                }
                else
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is entering into Station Stop mode", locomotive.DisplayName), LogLevel.Verbose);
                    yield return locomotiveStationStopControl_dev(locomotive);
                }

                yield return null;
            }

            //Locomotive is no longer in Route Mode
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} \t Route mode was disabled! Stopping Coroutine.", locomotive.DisplayName, LogLevel.Debug));

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: AutoEngineerControlRoutine", LogLevel.Trace);
            yield break;
        }


        //Locomotive Enroute to Destination
        public IEnumerator locomotiveTransitControl_dev(Car locomotive)
        {

            //Are we in a station?
            if (StationManager.isTrainInStation(locomotive))
            {
                //Is that station the same as our current destination?
                if (LocoTelem.currentDestination[locomotive].identifier == LocoTelem.closestStation[locomotive].Item1.identifier)
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} already at first station", locomotive.DisplayName, locomotive.Orientation), LogLevel.Verbose);
                    LocoTelem.TransitMode[locomotive] = false;
                    yield return new WaitForSeconds(1);
                }
            }

            //TEMP LOGIC
            float distanceToStation = float.MaxValue;
            bool delayExecution = false;
            float trainVelocity;
            int stationPadding = 10;

            //get initial data
            RouteSwitchData nextSwitch;
            float distanceToSwitch = float.MinValue;
            bool stopForSwitch = false;
            bool checkNextSwitch = true;
            int nextSwitchIndex;


            //Loop through transit logic
            while (LocoTelem.TransitMode[locomotive])
            {
                if (needToExitCoroutine_dev(locomotive))
                {
                    yield break;
                }

                //Refueling
                while (LocoTelem.RouteModePaused[locomotive])
                {
                    yield return null;
                }

                /*
                 * Check all switches that are up to 400 metres away
                 */

                //get the first switch in the list
                nextSwitch = LocoTelem.routeSwitchRequirements[locomotive].FirstOrDefault();
                nextSwitchIndex = 0;

                if (nextSwitch != null)
                {
                    distanceToSwitch = DestinationManager.GetDistanceToSwitch(locomotive, nextSwitch);
                    checkNextSwitch = true;
                }
                    
                while (checkNextSwitch)
                {
                    //remove switche if it's beneath/behind us
                    if(distanceToSwitch <= 0 && nextSwitch != null)
                    {
                        //remove the switch
                        RouteManager.logger.LogToDebug($"Removing switch: {nextSwitch.trackSwitch.id}, Distance: {distanceToSwitch}", LogLevel.Debug);
                        LocoTelem.routeSwitchRequirements[locomotive].Remove(nextSwitch);

                        //find the next switch and calculate the distance
                        nextSwitch = LocoTelem.routeSwitchRequirements[locomotive].FirstOrDefault();
                        nextSwitchIndex = 0;
                        
                        //no more switches
                        if (nextSwitch == null)
                            break;
                    }
                    else
                    {
                        break;
                    }

                    //check our next switch distance
                    distanceToSwitch = DestinationManager.GetDistanceToSwitch(locomotive, nextSwitch);
                    RouteManager.logger.LogToDebug($"Approaching: {nextSwitch.trackSwitch.id}, Distance: {distanceToSwitch}", LogLevel.Debug);

                    if (distanceToSwitch <= 400)
                    {
                        //Check switch state vs requirements: Need normal and is reverse || need reverse and is normal
                        if (nextSwitch.requiredStateNormal && nextSwitch.trackSwitch.isThrown ||
                            !nextSwitch.requiredStateNormal && !nextSwitch.trackSwitch.isThrown)
                        {
                            RouteManager.logger.LogToDebug($"Switch {nextSwitch.trackSwitch.id} state incorrect req normal: {nextSwitch.requiredStateNormal}, is reversed: {nextSwitch.trackSwitch.isThrown}", LogLevel.Debug);
                            //switch is not in the required position, is it marked as able to be routed around?
                            //Currently we are ony looking at passenger platforms but in the future, we might want to look at track segments for complex switch yards
                            if (nextSwitch.isRoutable)
                            {
                                RouteManager.logger.LogToDebug($"Switch is routable", LogLevel.Debug);
                                //check if we can leave on an alternate platform
                                TrackSpan[] ts = LocoTelem.currentDestination[locomotive].TrackSpans.ToArray();
                                
                                /*
                                 **** work in progress - check all platforms ***
                                if (LocoTelem.nextPassengerPlatform[locomotive] == null)
                                {
                                    LocoTelem.nextPassengerPlatform[locomotive] = 0;
                                }

                                while (LocoTelem.nextPassengerPlatform[locomotive] < ts.Length -1 )
                                {
                                    //Try the next plaform
                                    LocoTelem.nextPassengerPlatform[locomotive]++;
                                    Location pNext = (Location)ts[(int)LocoTelem.nextPassengerPlatform[locomotive]].lower;

                                    //can a route be found?
                                    List<RouteSwitchData> mainRoute = LocoTelem.routeSwitchRequirements[locomotive];
                                }
                                */

                                Location pNext = (Location)ts[1].lower;
                                List<RouteSwitchData> mainRoute = LocoTelem.routeSwitchRequirements[locomotive];

                                if (pNext == null || !DestinationManager.PlanRouteDeviation(ref mainRoute, nextSwitch, locomotive.LocationF, pNext))
                                {
                                    //we can't enter this platform come to a stop
                                    stopForSwitch = true;
                                    RouteManager.logger.LogToDebug($"Deviation unsuccessful", LogLevel.Debug);
                                }
                                else
                                {
                                    RouteManager.logger.LogToDebug($"Locomotive {locomotive.DisplayName}: route updated for switch");
                                }
                            }
                            else
                            {
                                stopForSwitch = true;
                                RouteManager.logger.LogToDebug($"Switch is unroutable", LogLevel.Debug);
                            }
                        }

                        if (stopForSwitch)
                        {
                            RouteManager.logger.LogToConsole(String.Format("{0} is holding at a switch; required state: {1}", Hyperlink.To(locomotive), nextSwitch.requiredStateNormal ? "NORMAL" : "REVERSED"));
                            
                            //wait for the switch to clear
                            while (nextSwitch.requiredStateNormal == nextSwitch.trackSwitch.isThrown)
                            {
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], (int)0, null));
                                yield return new WaitForSeconds(1);
                            }

                            RouteManager.logger.LogToDebug($"Switch {nextSwitch.trackSwitch.id} cleared", LogLevel.Debug);
                            stopForSwitch = false;
                        }
                    }
                    else
                    {
                        RouteManager.logger.LogToDebug($"No switches within 400m", LogLevel.Debug);
                        checkNextSwitch = false;
                        break;
                    }

                    //Get the switch after the current one
                    RouteManager.logger.LogToDebug($"Getting subsequent switch, index: {nextSwitchIndex}, count: {LocoTelem.routeSwitchRequirements[locomotive].Count() - 1}", LogLevel.Debug);
                    if (nextSwitchIndex < LocoTelem.routeSwitchRequirements[locomotive].Count() - 1)
                    {
                        nextSwitchIndex++;

                        nextSwitch = LocoTelem.routeSwitchRequirements[locomotive][nextSwitchIndex + 1];
                        distanceToSwitch = DestinationManager.GetDistanceToSwitch(locomotive, nextSwitch);
                    }
                    else
                    {
                        checkNextSwitch = false;
                    }  
                }

                //Getting close to a station update some values...
                //Cheeky optimization to reduce excessive logging...
                if (distanceToStation != float.MaxValue)
                {
                    if (Math.Abs(distanceToStation) <= 1000 && LocoTelem.closestStationNeedsUpdated[locomotive])
                    {
                        //Update Center & closest station
                        LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);
                        LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation_dev(locomotive);
                        LocoTelem.closestStationNeedsUpdated[locomotive] = false;
                    }
                    else if (Math.Abs(distanceToStation) > 1000)
                    {
                        //Reset closestStationNeedsUpdated
                        LocoTelem.closestStationNeedsUpdated[locomotive] = true;
                    }
                }

                /*****************************************************************
                 * 
                 * Distance To Station Check
                 * 
                 *****************************************************************/

                try
                {
                    distanceToStation = DestinationManager.GetDistanceToStation(locomotive, LocoTelem.currentDestination[locomotive]);
                    delayExecution = false;
                }
                catch (Exception e)
                {
                    RouteManager.logger.LogToDebug(e.Message, LogLevel.Error);
                    RouteManager.logger.LogToDebug(e.StackTrace, LogLevel.Error);
                    //If after delaying execution for 5 seconds, stop coroutine for locomotive
                    if (delayExecution)
                    {
                        RouteManager.logger.LogToConsole("Unable to determine distance to station. Disabling Dispatcher control of locomotive: " + locomotive.DisplayName);
                        StopCoroutine(AutoEngineerControlRoutine_dev(locomotive));
                    }

                    //Try again in 5 seconds
                    RouteManager.logger.LogToDebug(String.Format("Distance to station could not be calculated for {0}. Yielding for 5s", locomotive.DisplayName), LogLevel.Debug);
                    delayExecution = true;
                }

                //Distance to station was not in acceptable range... try again later
                if (distanceToStation <= -6969f)
                {
                    delayExecution = true;
                }

                //Try again in 5 seconds
                if (delayExecution)
                {
                    yield return new WaitForSeconds(1);//5);
                }

                /*****************************************************************
                 * 
                 * END Distance To Station Check
                 * 
                 *****************************************************************/

                /*****************************************************************
                 * 
                 * START Locomotive Movements
                 * 
                 *****************************************************************/

                //We may be able to avoid this with better logic elsewhere...
                RouteManager.logger.LogToDebug(String.Format("Locomotive: {0} Distance to Station: {1}", locomotive.DisplayName, distanceToStation), LogLevel.Verbose);

                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

                //Get Current train speed.
                trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);

                //Enroute to Destination
                if (distanceToStation > 500)
                {
                    RouteManager.logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}");
                    generalTransit(locomotive);

                    yield return new WaitForSeconds(1);// 5);
                }
                //Entering Destination Boundary
                else if (distanceToStation <= 400 && distanceToStation > 300)
                {
                    onApproachLongDist(locomotive);

                    //make sure our passenger loading/unloading is set as we arrive at the station
                    //TrainManager.CopyStationsFromLocoToCoaches_dev(locomotive);

                    //Hack to work around the new auto engineer crossing detection to prevent double blow / weird horn blow behavior. 
                    //This can be done better but further research is required. In the mean time this crude hack hopefully will reduce the occurances. 
                    if (!LocoTelem.approachWhistleSounded[locomotive] &&

                        ((locomotive.KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Horn)].FloatValue) <= 0f &&
                        (locomotive.KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Bell)].BoolValue) != true))
                    {
                        RouteManager.logger.LogToDebug(String.Format("Locomotive {0} activating Appproach Whistle", locomotive.DisplayName), LogLevel.Verbose);
                        yield return TrainManager.RMblow(locomotive, 0.25f, 1.5f);
                        yield return TrainManager.RMblow(locomotive, 1f, 2.5f);
                        yield return TrainManager.RMblow(locomotive, 1f, 1.75f, 0.25f);
                        yield return TrainManager.RMblow(locomotive, 1f, 0.25f);
                        yield return TrainManager.RMblow(locomotive, 0f);
                        LocoTelem.approachWhistleSounded[locomotive] = true;
                    }

                    yield return new WaitForSeconds(1);
                }
                //Approaching platform
                else if (distanceToStation <= 300 && distanceToStation > 100)
                {
                    onApproachMediumDist(locomotive, distanceToStation, 10f);
                    yield return new WaitForSeconds(1);
                }
                //Entering Platform
                else if (distanceToStation <= 100 && distanceToStation > stationPadding)
                {
                    onApproachShortDist(locomotive, distanceToStation);
                    yield return new WaitForSeconds(1);
                }
                //Train in platform
                else if (distanceToStation <= stationPadding && distanceToStation >= 0)
                {
                    onArrival(locomotive);
                    yield return new WaitForSeconds(1);
                }

                /*****************************************************************
                 * 
                 * END Locomotive Movements
                 * 
                 *****************************************************************/

                yield return null;
            }

            yield return null;
        }

        private IEnumerator locomotiveStationStopControl_dev(Car locomotive)
        {
            float currentTrainVelocity = 100f;

            RouteManager.logger.LogToDebug($"locomotiveStationStopControl_dev {LocoTelem.TransitMode[locomotive]}");

            //Loop through station logic while loco is not in transit mode...
            while (!LocoTelem.TransitMode[locomotive])
            {

                if (needToExitCoroutine_dev(locomotive))
                {
                    RouteManager.logger.LogToDebug("Exiting StationStopControl Dev");
                    yield break;
                }

                RouteManager.logger.LogToDebug("Continuing StationStopControl");
                //Refueling
                while (LocoTelem.RouteModePaused[locomotive])
                {
                    yield return null;
                }

                AutoEngineerPersistence persistence = new AutoEngineerPersistence(locomotive.KeyValueObject);
                if (persistence.Orders.MaxSpeedMph > 0)
                {
                    LocoTelem.TransitMode[locomotive] = true;
                    yield break;
                }

                //Ensure the train is at a complete stop. Else wait for it to stop...
                while ((currentTrainVelocity = TrainManager.GetTrainVelocity(locomotive)) > .1f)
                {
                    if (currentTrainVelocity > 0.1)
                    {
                        yield return new WaitForSeconds(1);
                    }
                }

                /*
                    Setup next station and passenger loading/unloading    
                */

                //Store previous station - we only really care where we've come from 
                LocoTelem.previousDestination[locomotive] = LocoTelem.currentDestination[locomotive];

                //Update Destination - review logic
                LocoTelem.currentDestination[locomotive] = StationManager.getNextStation_dev(locomotive);
                LocoTelem.locoTravelingForward[locomotive] = GetDirection(locomotive, LocoTelem.currentDestination[locomotive]);

                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} currentDestination is now {1}", locomotive.DisplayName, LocoTelem.currentDestination[locomotive].identifier), LogLevel.Debug);

                //update the stations at each stop otherwise transfer stations will not work
                TrainManager.CopyStationsFromLocoToCoaches_dev(locomotive);

                //Wait for passengers to load/unload
                while (!wasCurrentStopServed_dev(locomotive))
                {
                    yield return new WaitForSeconds(1f);
                }

                //Now that train is stopped, perform station ops and check fuel quantities before departure.
                if (checkFuelQuantities(locomotive))
                    LocoTelem.clearedForDeparture[locomotive] = true;


                //Loco now clear for station departure. 
                if (LocoTelem.clearedForDeparture[locomotive])
                {

                    //Notate we are cleared for departure
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} is cleared for departure.", locomotive.DisplayName));

                    //Only do these things if we really should depart the station.
                    if (LocoTelem.currentDestination[locomotive] != LocoTelem.previousDestination[locomotive])
                    {
                        //Transition to transit mode
                        LocoTelem.TransitMode[locomotive] = true;
                        LocoTelem.clearedForDeparture[locomotive] = false;

                        //Feature Enahncement: Issue #24
                        //Write to console the departure of the train consist at station X
                        //Bugfix: message would previously be generated even when departure was not cleared. 

                        if (RouteManager.Settings.showDepartureMessage)
                            RouteManager.logger.LogToConsole(String.Format("{0} has departed {1} for {2}", Hyperlink.To(locomotive), LocoTelem.previousDestination[locomotive].DisplayName.ToUpper(), LocoTelem.currentDestination[locomotive].DisplayName.ToUpper()));
                    }
                }
                else
                {
                    if (LocoTelem.lowFuelQuantities[locomotive].Count != 0)
                    {
                        string holdLocationName = LocoTelem.currentDestination[locomotive].DisplayName;

                        //Generate warning for each type of low fuel.
                        foreach (KeyValuePair<string, float> type in LocoTelem.lowFuelQuantities[locomotive])
                        {
                            if (type.Key == "coal")
                            {
                                RouteManager.logger.LogToConsole(String.Format("Locomotive {0} is low on coal and is holding at {1}", Hyperlink.To(locomotive), holdLocationName));
                            }
                            if (type.Key == "water")
                            {
                                RouteManager.logger.LogToConsole(String.Format("Locomotive {0} is low on water and is holding at {1}", Hyperlink.To(locomotive), holdLocationName));
                            }

                            if (type.Key == "diesel-fuel")
                            {
                                RouteManager.logger.LogToConsole(String.Format("Locomotive {0} is low on diesel and is holding at {1}", Hyperlink.To(locomotive), holdLocationName));
                            }
                        }
                        yield return new WaitForSeconds(30);
                    }
                    yield return new WaitForSeconds(5);
                }
            }

            RouteManager.logger.LogToDebug("EXITING FUNCTION: locomotiveStationStopControl_dev", LogLevel.Trace);
            yield return null;
        }

        private static bool wasCurrentStopServed_dev(Car locomotive)
        {
            bool carsLoaded = true;
            bool passWaiting = true;
            bool trainFull = false;

            //Initialize Variable
            PassengerMarker? marker = default(PassengerMarker);

            carsLoaded = carsStillLoaded_dev(locomotive, marker);

            passWaiting = passStillWaiting(locomotive, marker);

            trainFull = TrainManager.isTrainFull(locomotive);

            //Must unload all passengers for the current station
            if (!carsLoaded)
            {
                //No passengers to unload, however the train is full...
                //Looks like those waiting passengers are going to have to find a train with room.
                if (trainFull)
                {
                    //Only notify if not configured to wait until full

                    if (!RouteManager.Settings.waitUntilFull)
                        RouteManager.logger.LogToConsole(String.Format("Locomotive {0} consist is full. No room for additional passengers!", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName));

                    return true;
                }

                //Station has passengers destined for a scheduled station
                if (!passWaiting && !RouteManager.Settings.waitUntilFull)
                {
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} consist has finished loading and unloading at {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName), LogLevel.Verbose);
                    return true;
                }
            }

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} consist has not finished loading and unloading at {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName), LogLevel.Verbose);

            //Always assume stop has not been served unless determined otherwise. 
            return false;
        }

        private static bool carsStillLoaded_dev(Car locomotive, PassengerMarker? marker)
        {

            foreach (Car currentCar in locomotive.EnumerateCoupled().Where(car => car.Archetype == Model.Definition.CarArchetype.Coach))
            {
                //Get data for the current car
                marker = currentCar.GetPassengerMarker();

                //Since this type is nullable, make sure its not null...
                if (marker != null && marker.HasValue)
                {
                    //Loop through the list of passenger data for the current coach.
                    foreach (var stop in marker.Value.Groups)
                    {
                        RouteManager.logger.LogToDebug($"Checking cars still loaded, Destination: {stop.Destination} Count: {stop.Count}");
                        RouteManager.logger.LogToDebug(String.Format("Passenger Car {0} has {1} passengers for the stop at {2}, Loco Destination {3}", currentCar.DisplayName, stop.Count, stop.Destination, LocoTelem.currentDestination[locomotive].identifier), LogLevel.Verbose);


                        if (stop.Count > 0 && !LocoTelem.relevantPassengers[locomotive].Contains(stop.Destination))
                        {
                            //there are passengers that need to be unloaded
                            RouteManager.logger.LogToDebug(String.Format("Loco {0} consist contains passengers for current stop", locomotive.DisplayName), LogLevel.Debug);
                            return true;
                        }
                    }
                    //StationManager.getNumberPassengersWaitingForDestination()
                    //RouteManager.logger.LogToDebug(String.Format("Passenger Car {0} has {1} passengers for the current stop at {2}", currentCar.DisplayName, marker.Value.CountPassengersForStop(LocoTelem.currentDestination[locomotive].identifier), LocoTelem.currentDestination[locomotive].identifier), LogLevel.Verbose);
                }

            }

            RouteManager.logger.LogToDebug(String.Format("Loco {0} consist empty for current stop", locomotive.DisplayName), LogLevel.Debug);

            return false;
        }

        //Initial checks to determine if we can continue with the coroutine
        private bool needToExitCoroutine_dev(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: needToExitCoroutine Dev", LogLevel.Trace);

            try
            {
                //If no stations are selected for the locmotive, end the coroutine
                if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive))
                {
                    RouteManager.logger.LogToConsole("No stations selected. Stopping Coroutine for: " + locomotive.DisplayName);
                    TrainManager.SetRouteModeEnabled(false, locomotive);
                    return true;
                }

                //Engineer mode was changed and is no longer route mode
                if (!LocoTelem.RouteMode[locomotive])
                {
                    RouteManager.logger.LogToDebug("Locomotive no longer in Route Mode. Stopping Coroutine for: " + locomotive.DisplayName, LogLevel.Debug);
                    return true;
                }

                //If we have selected stations
                if (LocoTelem.stopStations[locomotive].Count <= 1)
                {
                    //If our previous destination key exists
                    if (LocoTelem.previousDestination.ContainsKey(locomotive) && LocoTelem.currentDestination.ContainsKey(locomotive))
                    {
                        //If there is data for the loco in prev dest.
                        if (LocoTelem.previousDestination[locomotive] != null && LocoTelem.previousDestination[locomotive] != default(PassengerStop))
                        {
                            //Only if our current dest = our last visited, consider the coroutine terminatable.
                            if (LocoTelem.currentDestination[locomotive] == LocoTelem.previousDestination[locomotive])
                            {
                                RouteManager.logger.LogToConsole(String.Format("{0} has no more stations. Halting Control.", Hyperlink.To(locomotive)));
                                TrainManager.SetRouteModeEnabled(false, locomotive);
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RouteManager.logger.LogToConsole("{0} Internal Error Occurred Halting control!" + locomotive.DisplayName);
                RouteManager.logger.LogToError("{0} error in needToExitCoroutine" + locomotive.DisplayName);
                RouteManager.logger.LogToError(ex.ToString());
                RouteManager.logger.LogToError(ex.StackTrace);
                return true;
            }

            //Trace Method
            RouteManager.logger.LogToDebug("EXITING FUNCTION: needToExitCoroutine Dev", LogLevel.Trace);
            return false;
        }

    }
}