using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using Microsoft.SqlServer.Server;
using Model;
using Model.OpsNew;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Track;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2.core
{
    public class AutoEngineer : MonoBehaviour
    {
        public IEnumerator AutoEngineerControlRoutine(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: AutoEngineerControlRoutine", Logger.logLevel.Trace);

            //Debug
            Logger.LogToDebug(String.Format("Coroutine Triggered!", locomotive.DisplayName, LocoTelem.RouteMode[locomotive]), Logger.logLevel.Verbose);
            Logger.LogToDebug(String.Format("Loco: {0} \t Route Mode: {1}", locomotive.DisplayName, LocoTelem.RouteMode[locomotive]), Logger.logLevel.Debug);

            //Setup departure clearances
            LocoTelem.clearedForDeparture[locomotive] = false;

            Logger.LogToDebug(String.Format("Loco: {0} \t has ID: {1}", locomotive.DisplayName, locomotive.id), Logger.logLevel.Debug);
            LocoTelem.locoTravelingWestward[locomotive] = true;

            //Set some initial values
            LocoTelem.currentDestination[locomotive]            = StationManager.getNextStation(locomotive);
            LocoTelem.closestStation[locomotive]                = StationManager.GetClosestStation(locomotive);
            LocoTelem.closestStationNeedsUpdated[locomotive]    = false;
            LocoTelem.CenterCar[locomotive]                     = TrainManager.GetCenterCoach(locomotive);

            //Route Mode is enabled!
            while (LocoTelem.RouteMode[locomotive])
            {
                //Update passenger markers as needed.
                if (LocoTelem.needToUpdatePassengerCoaches[locomotive])
                    TrainManager.CopyStationsFromLocoToCoaches(locomotive);

                Logger.LogToDebug(String.Format("Locomotive {0} center of train is car {1}", locomotive.DisplayName, LocoTelem.CenterCar[locomotive].DisplayName), Logger.logLevel.Verbose);

                if (LocoTelem.TransitMode[locomotive])
                {
                    Logger.LogToDebug(String.Format("Locomotive {0} is entering into transit mode", locomotive.DisplayName),Logger.logLevel.Verbose);
                    yield return locomotiveTransitControl(locomotive);
                }
                else
                {
                    Logger.LogToDebug(String.Format("Locomotive {0} is entering into Station Stop mode", locomotive.DisplayName), Logger.logLevel.Verbose);
                    yield return locomotiveStationStopControl(locomotive);
                }
            }

            //Locomotive is no longer in Route Mode
            Logger.LogToDebug(String.Format("Loco: {0} \t Route mode was disabled! Stopping Coroutine.", locomotive.DisplayName, Logger.logLevel.Debug));
            StopCoroutine(AutoEngineerControlRoutine(locomotive));

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: AutoEngineerControlRoutine", Logger.logLevel.Trace);
            yield return null;
        }

        //Locomotive Enroute to Destination
        public static IEnumerator locomotiveTransitControl(Car locomotive)
        {

            //Are we in a station?
            if (StationManager.isTrainInStation(locomotive))
            {
                //Is that staion the same as our current destination?
                if (LocoTelem.currentDestination[locomotive].identifier == LocoTelem.closestStation[locomotive].Item1.identifier)
                {
                    LocoTelem.TransitMode[locomotive] = false;
                    yield return new WaitForSeconds(1);
                }
            }

            //Determine direction to move
            Logger.LogToDebug(String.Format("Loco {0} has an orientation of {1}", locomotive.DisplayName, locomotive.Orientation), Logger.logLevel.Verbose);

            //Move in that direction

            //TEMP LOGIC
            float distanceToStation     = float.MaxValue;
            bool  delayExecution        = false;
            float olddist               = float.MaxValue;
            float trainVelocity         = 0;

            //Loop through transit logic
            while (LocoTelem.TransitMode[locomotive])
            {

                //Potential fix for edge case where loco reverses directions multiple times due to a race condition
                if (Math.Abs(olddist - distanceToStation) > 5)
                {
                    olddist = distanceToStation;
                }

                //Getting close to a station update some values...
                //Cheeky optimization to reduce excessive logging...
                if (distanceToStation != float.MaxValue)
                {
                    if (Math.Abs(distanceToStation) <= 1000 && LocoTelem.closestStationNeedsUpdated[locomotive])
                    {
                        //Update Center & closest station
                        LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);
                        LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation(locomotive);
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
                    distanceToStation = DestinationManager.GetDistanceToDest(locomotive);
                    delayExecution = false;
                }
                catch (Exception e)
                {
                    Logger.LogToDebug(e.Message,Logger.logLevel.Error);
                    Logger.LogToDebug(e.StackTrace, Logger.logLevel.Error);
                    //If after delaying execution for 5 seconds, stop coroutine for locomotive
                    if (delayExecution)
                    {
                        Logger.LogToConsole("Unable to determine distance to station. Disabling Dispatcher control of locomotive: " + locomotive.DisplayName);
                        yield break;
                    }

                    //Try again in 5 seconds
                    Logger.LogToDebug(String.Format("Distance to station could not be calculated for {0}. Yielding for 5s", locomotive.DisplayName), Logger.logLevel.Debug);
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
                Logger.LogToDebug(String.Format("Locomotive: {0} Distance to Station: {1} Prev Distance: {2}", locomotive.DisplayName, distanceToStation, olddist), Logger.logLevel.Verbose);

                if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 10f))
                {

                    LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                    Logger.LogToDebug("Was driving in the wrong direction! Changing direction");
                    Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}", Logger.logLevel.Debug);
                    StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

                    yield return new WaitForSeconds(10);
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
                    Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}");
                    generalTransit(locomotive);

                    yield return new WaitForSeconds(5);
                }
                //Entering Destination Boundary
                else if (distanceToStation <= 500 && distanceToStation > 400)
                {
                    onApproachLongDist(locomotive);

                    if (!LocoTelem.approachWhistleSounded[locomotive])
                    {
                        Logger.LogToDebug(String.Format("Locomotive {0} activating Appproach Whistle", locomotive.DisplayName), Logger.logLevel.Verbose);
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
                else if (distanceToStation <= 400 && distanceToStation > 100)
                {
                    onApproachMediumDist(locomotive, distanceToStation);
                    yield return new WaitForSeconds(1);
                }
                //Entering Platform
                else if (distanceToStation <= 100 && distanceToStation > 10)
                {
                    onApproachShortDist(locomotive, distanceToStation);
                    yield return new WaitForSeconds(1);
                }
                //Train in platform
                else if (distanceToStation <= 10 && distanceToStation > 0)
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
        }



        //Stopped at station
        private static IEnumerator locomotiveStationStopControl(Car locomotive)
        {
            float currentTrainVelocity = 100f;

            //Loop through station logic while loco is not in transit mode...
            while (!LocoTelem.TransitMode[locomotive])
            {
                //Ensure the train is at a complete stop. Else wait for it to stop...
                while ((currentTrainVelocity = TrainManager.GetTrainVelocity(locomotive)) > .1f)
                {
                    if (currentTrainVelocity > 0.1)
                    {
                        yield return new WaitForSeconds(1);
                    }
                    else
                    {
                        yield return new WaitForSeconds(3);
                    }
                }

                //Now that train is stopped, perform station ops and check fuel quantities before departure.
                if(wasCurrentStopServed(locomotive) && checkFuelQuantities(locomotive))
                    LocoTelem.clearedForDeparture[locomotive] = true;

                //Loco now clear for station departure. 
                if (LocoTelem.clearedForDeparture[locomotive])
                {
                    string previousDestination = LocoTelem.currentDestination[locomotive].identifier;
                    Logger.LogToDebug(String.Format("Locomotive {0} is cleared for departure.", locomotive.DisplayName));

                    //Update Destination
                    LocoTelem.currentDestination[locomotive] = StationManager.getNextStation(locomotive);
                    Logger.LogToDebug(String.Format("Locomotive {0} currentDestination is now {1}", locomotive.DisplayName, LocoTelem.currentDestination[locomotive].identifier),Logger.logLevel.Debug);

                    LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation(locomotive);
                    Logger.LogToDebug(String.Format("Locomotive {0} closestStation is now {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.identifier), Logger.logLevel.Debug);

                    //Transition to transit mode
                    LocoTelem.TransitMode[locomotive] = true;

                    //Feature Enahncement: Issue #24
                    //Write to console the departure of the train consist at station X
                    //Bugfix: message would previously be generated even when departure was not cleared. 
                    Logger.LogToConsole(String.Format("{0} has departed {1} for {2}", Hyperlink.To(locomotive), previousDestination.ToUpper(), LocoTelem.currentDestination[locomotive].DisplayName.ToUpper()));
                }
                else
                {
                    if (LocoTelem.lowFuelQuantities[locomotive].Count != 0)
                    {
                        //Generate warning for each type of low fuel.
                        foreach (KeyValuePair<string, float> type in LocoTelem.lowFuelQuantities[locomotive])
                        {
                            if (type.Key == "coal")
                            {
                                Logger.LogToConsole(String.Format("Locomotive {0} is low on coal and is holding at {1}", Hyperlink.To(locomotive), LocoTelem.LocomotivePrevDestination[locomotive]));
                            }
                            if (type.Key == "water")
                            {
                                Logger.LogToConsole(String.Format("Locomotive {0} is low on water and is holding at {1}", Hyperlink.To(locomotive), LocoTelem.LocomotivePrevDestination[locomotive]));
                            }

                            if (type.Key == "diesel-fuel")
                            {
                                Logger.LogToConsole(String.Format("Locomotive {0} is low on diesel and is holding at {1}", Hyperlink.To(locomotive), LocoTelem.LocomotivePrevDestination[locomotive]));
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
            Logger.LogToDebug("ENTERED FUNCTION: generalTransit", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} triggered General Transit.", locomotive.DisplayName), Logger.logLevel.Verbose);

            //Set AI Maximum speed
            //Track max speed takes precedence. 
            LocoTelem.RMMaxSpeed[locomotive] = 100f;

            //Apply Updated Max Speed
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: generalTransit", Logger.logLevel.Trace);
        }



        //Train is approaching location
        private static void onApproachLongDist(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: onApproachLongDist", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} triggered Long Approach.", locomotive.DisplayName), Logger.logLevel.Verbose);

            // Appears that this will not work through abstraction outside of the autoengineer enumerator.
            ////If yet to whistle on approach, then whistle
            //if (!LocoTelem.approachWhistleSounded[locomotive])
            //{
            //    Logger.LogToDebug(String.Format("Locomotive {0} activating Appproach Whistle", locomotive.DisplayName), Logger.logLevel.Verbose);
            //    TrainManager.standardWhistle(locomotive);
            //    LocoTelem.approachWhistleSounded[locomotive] = true;
            //}

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: onApproachLongDist", Logger.logLevel.Trace);
        }



        //Train is approaching platform
        private static void onApproachMediumDist(Car locomotive, float distanceToStation)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: onApproachMediumDist", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} triggered Medium Approach.", locomotive.DisplayName), Logger.logLevel.Verbose);

            //Gradually reduce maximum speed the closer to the platform we get. 
            float calculatedSpeed = distanceToStation / 12f;

            //Minimum speed should not be less than 15Mph
            if (calculatedSpeed < 15f)
            {
                LocoTelem.RMMaxSpeed[locomotive] = 15f;
            }
            else
            {
                LocoTelem.RMMaxSpeed[locomotive] = calculatedSpeed;
            }

            Logger.LogToDebug(String.Format("Locomotive {0} on Medium Approach: Speed limited to {1}", locomotive.DisplayName, LocoTelem.RMMaxSpeed[locomotive]), Logger.logLevel.Debug);

            //Apply Updated Max Speed
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: onApproachMediumDist", Logger.logLevel.Trace);
        }



        //Train is entering platform
        private static void onApproachShortDist(Car locomotive, float distanceToStation)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: onApproachShortDist", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} triggered Short Approach.", locomotive.DisplayName), Logger.logLevel.Verbose);

            //Gradually reduce maximum speed the closer to the platform we get. 
            float calculatedSpeed = distanceToStation / 8f;

            //Minimum speed should not be less than 15Mph
            if (calculatedSpeed < 5f)
            {
                //Set max speed to 5 mph for now.
                LocoTelem.RMMaxSpeed[locomotive] = 5f;

                //Apply Bell
                Logger.LogToDebug(String.Format("Locomotive {0} activating Approach Bell", locomotive.DisplayName), Logger.logLevel.Verbose);
                TrainManager.RMbell(locomotive, true);
            }
            else if(distanceToStation < 30)
            {
                //Apply Bell
                Logger.LogToDebug(String.Format("Locomotive {0} activating Approach Bell", locomotive.DisplayName), Logger.logLevel.Verbose);
                TrainManager.RMbell(locomotive, true);
            }
            else
            {
                LocoTelem.RMMaxSpeed[locomotive] = calculatedSpeed;
            }

            Logger.LogToDebug(String.Format("Locomotive {0} on Short Approach! Speed limited to {1}", locomotive.DisplayName, LocoTelem.RMMaxSpeed[locomotive]), Logger.logLevel.Debug);

            //Appply updated maxSpeed
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: onApproachShortDist", Logger.logLevel.Trace);
        }



        //Train Arrived at station
        private static void onArrival(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: onArrival", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} triggered on Arrival.", locomotive.DisplayName), Logger.logLevel.Verbose);

            //Train Arrived
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], 0, null));

            //Wait for loco to crawl to a stop. 
            if (Math.Abs(locomotive.velocity * 2.23694f) < .1f)
            {
                //Disable bell
                Logger.LogToDebug(String.Format("Locomotive {0} deactivating Approach Bell", locomotive.DisplayName), Logger.logLevel.Verbose);
                TrainManager.RMbell(locomotive, false);

                //Reset Approach whistle
                LocoTelem.approachWhistleSounded[locomotive] = false;

                //Disable transit mode.
                LocoTelem.TransitMode[locomotive] = false;
            }



            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: onArrival", Logger.logLevel.Trace);
        }



        //Check to see if passengers are unloaded
        private static bool wasCurrentStopServed(Car locomotive)
        {
            bool carsLoaded     = true;
            bool passWaiting    = true;

            //Initialize Variable
            PassengerMarker? marker = default(PassengerMarker);

            carsLoaded = carsStillLoaded(locomotive, marker);

            passWaiting = passStillWaiting (locomotive, marker);

            //if both cars have no pendding peope and station has no pending passengers
            if(!carsLoaded && !passWaiting)
            {
                Logger.LogToDebug(String.Format("Locomotive {0} has finished loading and unloading at {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName), Logger.logLevel.Verbose);
                return true;
            }

            Logger.LogToDebug(String.Format("Locomotive {0} has not finished loading and unloading at {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1.DisplayName), Logger.logLevel.Verbose);

            //Always assume stop has not been served unless determined otherwise. 
            return false;
        }



        private static bool carsStillLoaded (Car locomotive, PassengerMarker? marker)
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
                            //Logger.LogToDebug(String.Format("Passenger Car {0} has {1} passengers for the stop at {2}", currentCar.DisplayName, stop.Count, stop.Destination), Logger.logLevel.Verbose);
                            if (stop.Destination == LocoTelem.currentDestination[locomotive].identifier)
                            {
                                loadCheck = true;
                            }
                        }
                        //StationManager.getNumberPassengersWaitingForDestination()
                        //Logger.LogToDebug(String.Format("Passenger Car {0} has {1} passengers for the current stop at {2}", currentCar.DisplayName, marker.Value.CountPassengersForStop(LocoTelem.currentDestination[locomotive].identifier), LocoTelem.currentDestination[locomotive].identifier), Logger.logLevel.Verbose);
                    }
                }
            }

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
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }



        //Initial checks to determine if we can continue with the coroutine
        private bool cancelTransitModeIfNeeded(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: cancelTransitModeIfNeeded", Logger.logLevel.Trace);
            //If no stations are selected for the locmotive, end the coroutine
            if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                Logger.LogToConsole("No stations selected. Stopping Coroutine for: " + locomotive.DisplayName);
                TrainManager.SetRouteModeEnabled(false, locomotive);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));
                return true;
            }

            //Engineer mode was changed and is no longer route mode
            if (!LocoTelem.RouteMode[locomotive])
            {
                Logger.LogToDebug("Locomotive no longer in Route Mode. Stopping Coroutine for: " + locomotive.DisplayName, Logger.logLevel.Debug);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));
                return true;
            }

            //Trace Method
            Logger.LogToDebug("EXITING FUNCTION: cancelTransitModeIfNeeded", Logger.logLevel.Trace);
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
    }
}