using Game.Messages;
using Game.State;
using Model;
using RouteManager.v2.dataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            bool lowCoalWarningGiven    = false;
            bool lowWaterWarningGiven   = false;
            bool lowFuelWarningGiven    = false;
            float RMmaxSpeed            = 0;
            float distanceToStation     = 0;
            float olddist               = float.MaxValue;
            bool delayExecution         = false;
            bool aproachBlastrequired   = false;
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

                        //Determine if we should continue with the coroutine. If not break out...
                        if(cancelTransitModeIfNeeded(locomotive))
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



                        //Get Current train speed.
                        trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);


                        if (distanceToStation > 350)
                        {

                            /*We may be able to avoid this with better logic elsewhere...
                            
                            if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 10f))
                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Logger.LogToDebug("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 100;
                                Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }
                            */

                            RMmaxSpeed = 100;
                            Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));

                            //Blow Train Horn
                            if (distanceToStation < 500f)
                            {
                                Logger.LogToDebug("checking if blast required");
                                if (aproachBlastrequired)
                                {
                                    Logger.LogToDebug("attempting to perform approach blast");
                                    yield return TrainManager.RMblow(locomotive, 0.25f, 1.5f);
                                    yield return TrainManager.RMblow(locomotive, 1f, 2.5f);
                                    yield return TrainManager.RMblow(locomotive, 1f, 1.75f, 0.25f);
                                    yield return TrainManager.RMblow(locomotive, 1f, 0.25f);
                                    yield return TrainManager.RMblow(locomotive, 0f);
                                    aproachBlastrequired = false;
                                }

                            }
                            else
                            {
                                aproachBlastrequired = true;
                            }

                            yield return new WaitForSeconds(5);

                        }
                        else if (distanceToStation <= 350 && distanceToStation > 10)
                        {
                            /*We may be able to avoid this with better logic elsewhere...
                            if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 15f))
                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Logger.LogToDebug("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 100;
                                Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }
                            */

                            RMmaxSpeed = distanceToStation / 8f;
                            if (RMmaxSpeed < 5f)
                            {
                                RMmaxSpeed = 5f;
                            }

                            //Activate Bell
                            TrainManager.RMbell(locomotive, true);

                            Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                            yield return new WaitForSeconds(1);

                        }
                        else if (distanceToStation <= 10 && distanceToStation > 0)
                        {
                            RMmaxSpeed = 0f;
                            Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], 0, null));
                            LocoTelem.TransitMode[locomotive] = false;
                            yield return new WaitForSeconds(1);

                        }
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
