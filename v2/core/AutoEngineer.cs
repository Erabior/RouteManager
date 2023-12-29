using Game.Messages;
using Game.State;
using Microsoft.SqlServer.Server;
using Model;
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
            Logger.LogToDebug(String.Format("Coroutine Triggered! Loco: {0} \t Route Mode: {1}",locomotive.DisplayName, LocoTelem.RouteMode[locomotive]), Logger.logLevel.Debug);

            //Calculate the center of the train for target positioning when stopping at station.
            LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);

            //Review Later 
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                DestinationManager.GetNextDestination(locomotive);
            }


            //Probably could move a lot of this to the loco telem class...
            bool lowCoalWarningGiven    = false;
            bool lowWaterWarningGiven   = false;
            bool lowFuelWarningGiven    = false;
            float distanceToStation     = 0;
            float olddist               = float.MaxValue;
            bool delayExecution         = false;
            float trainVelocity         = 0;


            while (LocoTelem.RouteMode[locomotive])
            {
                /********************************************************************************************************************
                 * 
                 * 
                 *                                  START Locomotive TRANSIT Logic
                 * 
                 * 
                 *********************************************************************************************************************/
                if (LocoTelem.TransitMode[locomotive])
                {
                    Logger.LogToDebug(String.Format("Locomotive {0} is entering into transit mode",locomotive.DisplayName));

                    olddist = distanceToStation;

                    //Delay script execution
                    delayExecution = false;

                    //Loop through transit logic
                    while (LocoTelem.TransitMode[locomotive])
                    {
                        //Keep Track of old distance for use with direction check.
                        olddist = distanceToStation;

                        //Determine if we should continue with the coroutine. If not break out...
                        if (cancelTransitModeIfNeeded(locomotive))
                        {
                            break;
                        }

                        //If current destination is not selected, then remove the destination and proceed to the next.
                        //Unsure of the edge case need for this yet.
                        //Review Later
                        if (!DestinationManager.IsCurrentDestinationSelected(locomotive))
                        {
                            LocoTelem.LocomotiveDestination.Remove(locomotive);
                            DestinationManager.GetNextDestination(locomotive);
                        }


                        /*****************************************************************
                         * 
                         * Fuel Check
                         * 
                         *****************************************************************/
                        List<KeyValuePair<string, float>> lowFuelData = locoLowFuelCheck(locomotive);
                        if (lowFuelData.Count != 0)
                        {
                            //Generate warning for each type of low fuel.
                            foreach (KeyValuePair<string, float> type in lowFuelData)
                            {
                                if (type.Key == "coal" && lowCoalWarningGiven == false)
                                {
                                    lowCoalWarningGiven = true;
                                    Logger.LogToConsole(String.Format("Locomotive {0} has less than {1} tons of coal remaining", locomotive.DisplayName, type.Value));
                                }
                                if (type.Key == "water" && lowWaterWarningGiven == false)
                                {
                                    lowWaterWarningGiven = true;
                                    Logger.LogToConsole(String.Format("Locomotive {0} has less than {1}G of water remaining", locomotive.DisplayName, type.Value));
                                }

                                if (type.Key == "diesel-fuel" && lowFuelWarningGiven == false)
                                {
                                    lowFuelWarningGiven = true;
                                    Logger.LogToConsole(String.Format("Locomotive {0} has less than {1}G of diesel remaining", locomotive.DisplayName, type.Value));
                                }
                            }
                        }
                        /*****************************************************************
                         * 
                         * END Fuel Check
                         * 
                         *****************************************************************/

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
                                Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {(int)LocoTelem.RMMaxSpeed[locomotive]}",Logger.logLevel.Debug);
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));
                            }
                            else
                            {
                                Logger.LogToDebug(String.Format("Locomotive {0} appears to be stopping or stopped",locomotive.DisplayName),Logger.logLevel.Debug);
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
                        else if(distanceToStation <= 10 && distanceToStation > 0)
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
                /********************************************************************************************************************
                 * 
                 * 
                 *                                  END Locomotive TRANSIT Logic
                 * 
                 * 
                 *********************************************************************************************************************/



                /********************************************************************************************************************
                 * 
                 * 
                 *                                  START Locomotive STATIONARY Logic
                 * 
                 * 
                 *********************************************************************************************************************/

                if (!LocoTelem.TransitMode[locomotive])
                {
                    //Determine if we should continue with the coroutine. If not break out...
                    if (cancelTransitModeIfNeeded(locomotive))
                    {
                        break;
                    }


                    //Feature Enahncement: Issue #24
                    //Write to console the arrival of the train consist at station X
                    string currentStation = LocoTelem.LocomotiveDestination[locomotive];
                    Logger.LogToConsole(String.Format("{0} has arrived at {1} station", locomotive.DisplayName, currentStation.ToUpper()));

                    //Deactivate Bell
                    TrainManager.RMbell(locomotive, false);

                    //Update Route Destination
                    Logger.LogToDebug(String.Format("Locomotive {0} - Current destination: {1}",locomotive.DisplayName, LocoTelem.LocomotiveDestination[locomotive].ToUpper()), Logger.logLevel.Debug);
                    DestinationManager.GetNextDestination(locomotive);
                    Logger.LogToDebug(String.Format("Locomotive {0} - New destination: {1}", locomotive.DisplayName, LocoTelem.LocomotiveDestination[locomotive].ToUpper()), Logger.logLevel.Debug);


                    //Set Passenger car station lists
                    Logger.LogToDebug(String.Format("Locomotive {0} - Loading Initiated", locomotive.DisplayName));
                    TrainManager.CopyStationsFromLocoToCoaches(locomotive);

                    //Passenger Loading 
                    List<int> numPassInTrain = new List<int> { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue };

                    //Redundant at this point?
                    //Technically train length could change as cars are removed or addded, but unlikely in route mode even while stopped. 
                    LocoTelem.CenterCar[locomotive] = TrainManager.GetCenterCoach(locomotive);

                    //Loop through stationary logic
                    while (!LocoTelem.TransitMode[locomotive])
                    {
                        //Determine if we should continue with the coroutine. If not break out...
                        if (cancelTransitModeIfNeeded(locomotive))
                        {
                            break;
                        }

                        /******************************************************************
                         ****************************************************************** 
                         * 
                         * 
                         *  REVIEW PASSENGER LOGIC UPDATE
                         * 
                         * 
                         ****************************************************************** 
                         *****************************************************************/

                        //Wait for train to halt movement
                        trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);
                        while (trainVelocity > 0.1f)
                        {
                            //Update velocity
                            trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);
                            if (trainVelocity > 0.1)
                            {
                                yield return new WaitForSeconds(1);
                            }
                            else
                            {
                                yield return new WaitForSeconds(3);
                            }
                        }

                        //
                        for (int i = 0; i <= 4; i++)
                        {
                            numPassInTrain[i] = TrainManager.GetNumPassInTrain(locomotive);
                            yield return new WaitForSeconds(1);

                        }

                        bool paxLoadNoChange = false;
                        Logger.LogToDebug($"Passenger loading history for {locomotive.DisplayName} over the past 5 seconds {String.Join(",", numPassInTrain)}");
                        paxLoadNoChange = numPassInTrain.All(numPass => numPass != int.MaxValue && numPass == numPassInTrain.First());
                        if (!paxLoadNoChange)
                        {
                            Logger.LogToDebug($"{locomotive.DisplayName} Passenger count still changing - disembarkation/embarkation in progress");
                            yield return new WaitForSeconds(1);
                        }
                        /******************************************************************
                         ******************************************************************
                         * 
                         * 
                         *  END REVIEW PASSENGER LOGIC UPDATE
                         * 
                         * 
                         ******************************************************************
                         ******************************************************************/
                        else
                        {
                            bool clearedForDeparture = true;

                            /*****************************************************************
                             * 
                             * Fuel Check
                             * 
                             *****************************************************************/
                            List<KeyValuePair<string, float>> lowFuelData = locoLowFuelCheck(locomotive);
                            if (lowFuelData.Count != 0)
                            {
                                //Generate warning for each type of low fuel.
                                foreach (KeyValuePair<string, float> type in lowFuelData)
                                {
                                    if (type.Key == "coal" && lowCoalWarningGiven == false)
                                    {
                                        clearedForDeparture = false;
                                        Logger.LogToConsole(String.Format("Locomotive {0} is low on coal and is holding at {1}", locomotive.DisplayName, LocoTelem.LocomotivePrevDestination[locomotive]));
                                    }
                                    if (type.Key == "water" && lowWaterWarningGiven == false)
                                    {
                                        clearedForDeparture = false;
                                        Logger.LogToConsole(String.Format("Locomotive {0} is low on water and is holding at {1}", locomotive.DisplayName, LocoTelem.LocomotivePrevDestination[locomotive]));
                                    }

                                    if (type.Key == "diesel-fuel" && lowFuelWarningGiven == false)
                                    {
                                        clearedForDeparture = false;
                                        Logger.LogToConsole(String.Format("Locomotive {0} is low on diesel and is holding at {1}", locomotive.DisplayName, LocoTelem.LocomotivePrevDestination[locomotive]));
                                    }
                                }
                            }
                            /*****************************************************************
                             * 
                             * END Fuel Check
                             * 
                             *****************************************************************/

                            //Determine if Cleared for Departure. 
                            if (clearedForDeparture)
                            {
                                LocoTelem.TransitMode[locomotive] = true;
                                
                                //Feature Enahncement: Issue #24
                                //Write to console the departure of the train consist at station X
                                //Bugfix: message would previously be generated even when departure was not cleared. 
                                Logger.LogToConsole(String.Format("{0} has departed {1} for {2}", locomotive.DisplayName, currentStation.ToUpper(), LocoTelem.LocomotiveDestination[locomotive].ToUpper()));

                                yield return new WaitForSeconds(30);
                            }
                        }
                    }
                }

                /********************************************************************************************************************
                 * 
                 * 
                 *                                  END Locomotive STATIONARY Logic
                 * 
                 * 
                 *********************************************************************************************************************/

            }
            if (!LocoTelem.RouteMode[locomotive])
            {

                Logger.LogToDebug($"loco {locomotive} - route mode was disabled - Stopping Coroutine for {locomotive}", Logger.logLevel.Debug);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));

            }
            if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                Logger.LogToDebug($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}", Logger.logLevel.Debug);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));
            }

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: AutoEngineerControlRoutine", Logger.logLevel.Trace);
        }



        //Train is enroute to destination
        private void generalTransit(Car locomotive)
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
        private void onApproachLongDist(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: onApproachLongDist", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} triggered Long Approach.",locomotive.DisplayName), Logger.logLevel.Verbose);

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
        private void onApproachMediumDist(Car locomotive, float distanceToStation)
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
        private void onApproachShortDist(Car locomotive, float distanceToStation)
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
            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int) LocoTelem.RMMaxSpeed[locomotive], null));

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: onApproachShortDist", Logger.logLevel.Trace);
        }



        //Train Arrived at station
        private void onArrival(Car locomotive)
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


        //Separate out the core fuel check logic
        private List<KeyValuePair<string, float>> locoLowFuelCheck(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: locoLowFuelCheck", Logger.logLevel.Trace);

            List<KeyValuePair<string, float>> fuelCheckResults = new List<KeyValuePair<string, float>>();

            //If steam locomotive Check the water and coal levels
            if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
            {

                //If coal is below minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "coal") / 2000, Settings.minCoalQuantity))
                {
                    fuelCheckResults.Add(new KeyValuePair<string, float>("coal", TrainManager.GetLoadInfoForLoco(locomotive, "coal") / 2000));
                }

                //If water is below minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "water"), Settings.minWaterQuantity))
                {
                    fuelCheckResults.Add(new KeyValuePair<string, float>("coal", TrainManager.GetLoadInfoForLoco(locomotive, "water")));
                }

            }
            //If Diesel locomotive diesel levels
            else if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel)
            {
                //If diesel level is below defined minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "diesel-fuel"),Settings.minDieselQuantity))
                {
                    fuelCheckResults.Add(new KeyValuePair<string, float>("coal", TrainManager.GetLoadInfoForLoco(locomotive, "diesel-fuel")));
                }
            }

            //Trace Method
            Logger.LogToDebug("EXITING FUNCTION: locoLowFuelCheck", Logger.logLevel.Trace);
            return fuelCheckResults;
        }

        //Methodize repeated code of fuel check. 
        //Method could be re-integrated into calling method now that additional checks have been rendered null from further code improvements.
        private bool compareAgainstMinVal(float inputValue, float minimumValue)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: compareAgainstMinVal", Logger.logLevel.Trace);

            //Compare to minimums
            if (inputValue < minimumValue)
                return true;

            //Trace Method
            Logger.LogToDebug("EXITING FUNCTION: compareAgainstMinVal", Logger.logLevel.Trace);

            //Something unexpected happened or fuel is above minimums.
            //Either way return false here as there is nothing further we can do. 
            return false;
        }
    }
}
