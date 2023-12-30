using Game.Messages;
using Game.State;
using Microsoft.SqlServer.Server;
using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
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

            //Get Initial Closest Station
            LocoTelem.closestStation[locomotive] = StationManager.GetClosestStation(locomotive);

            //Route Mode is enabled!
            while (LocoTelem.RouteMode[locomotive])
            {

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

            //Check to make sure we are not already at our desired station
            if (StationManager.isLocomotiveInStation(locomotive))
            {
                LocoTelem.TransitMode[locomotive] = false;
                yield return new WaitForSeconds(1);
            }

            //Determine direction to move
            Logger.LogToDebug(String.Format("Loco {0} has an orientation of {1}", locomotive.DisplayName, locomotive.Orientation), Logger.logLevel.Verbose);

            //Move in that direction

            //TEMP LOGIC
            float distanceToStation     = float.MaxValue;
            bool delayExecution         = false;
            float olddist               = float.MaxValue;
            float trainVelocity = 0;

            //Loop through transit logic
            while (LocoTelem.TransitMode[locomotive])
            {

                olddist = distanceToStation;

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
                catch
                {
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
                    bool brakeApplied = locomotive.air.handbrakeApplied || locomotive.air.BrakeCylinder.Pressure > 2f;

                    if (!brakeApplied)
                    {
                        LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                        Logger.LogToDebug("Was driving in the wrong direction! Changing direction");
                        Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}", Logger.logLevel.Debug);
                        StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));
                    }
                    else
                    {
                        Logger.LogToDebug(String.Format("Locomotive {0} appears to be stopping or stopped", locomotive.DisplayName), Logger.logLevel.Debug);
                    }
                    yield return new WaitForSeconds(5);
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

                //Train Confirmed to be stopped...

                //Passenger Load / Unload Logic Here
                //Temporarily wait 10 seconds before clearing loco for departure...
                if(!LocoTelem.clearedForDeparture[locomotive])
                    yield return new WaitForSeconds(10);

                checkFuelQuantities(locomotive);

                //Loco now clear for station departure. 
                if (LocoTelem.clearedForDeparture[locomotive])
                {
                    //Transition to transit mode
                    LocoTelem.TransitMode[locomotive] = true;

                    //Feature Enahncement: Issue #24
                    //Write to console the departure of the train consist at station X
                    //Bugfix: message would previously be generated even when departure was not cleared. 
                    Logger.LogToConsole(String.Format("{0} has departed {1} for {2}", Hyperlink.To(locomotive), LocoTelem.currentDestination[locomotive].DisplayName.ToUpper(), LocoTelem.LocomotiveDestination[locomotive].ToUpper()));
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
                }
            }
            //yield return null;
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
            float calculatedSpeed = distanceToStation / 8f;

            //Minimum speed should not be less than 15Mph
            if (calculatedSpeed < 15f)
            {
                LocoTelem.RMMaxSpeed[locomotive] = 15f;
            }
            else
            {
                LocoTelem.RMMaxSpeed[locomotive] = calculatedSpeed;
            }

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
                LocoTelem.RMMaxSpeed[locomotive] = 5f;
            }
            else
            {
                LocoTelem.RMMaxSpeed[locomotive] = calculatedSpeed;
            }

            //Apply Bell
            Logger.LogToDebug(String.Format("Locomotive {0} activating Approach Bell", locomotive.DisplayName), Logger.logLevel.Verbose);
            TrainManager.RMbell(locomotive, true);

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

            //Disable bell
            Logger.LogToDebug(String.Format("Locomotive {0} deactivating Approach Bell", locomotive.DisplayName), Logger.logLevel.Verbose);
            TrainManager.RMbell(locomotive, false);

            //Reset Approach whistle
            LocoTelem.approachWhistleSounded[locomotive] = false;

            //Disable transit mode.
            LocoTelem.TransitMode[locomotive] = false;

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: onArrival", Logger.logLevel.Trace);
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

        private static void checkFuelQuantities(Car locomotive)
        {
            //Update Fuel quantities
            TrainManager.locoLowFuelCheck(locomotive);

            if (LocoTelem.lowFuelQuantities[locomotive].Count == 0)
                return;

            LocoTelem.clearedForDeparture[locomotive] = false;
        }
    }
}