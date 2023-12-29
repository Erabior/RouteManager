using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Model;
using System.IO;
using RouteManager.v1.helpers;
using Logger = RouteManager.v1.helpers.Logger;
using System.Collections.Generic;
using static Game.Messages.PropertyChange;

namespace RouteManager
{
    public class RouteAI : MonoBehaviour
    {

        //Initialize Route AI.
        void Awake()
        {
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("subscribing to unload event");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapDidNunloadEvenForRouteMode);
        }
        void Update()
        {

            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                //Logger.LogToDebug("There is data in locomotiveCoroutines");
                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {
                    //Logger.LogToDebug($"Loco {keys[i].id} has values Coroutine: {LocoTelem.locomotiveCoroutines[keys[i]]} and Route Mode bool: {LocoTelem.RouteMode[keys[i]]}");
                    if (!LocoTelem.locomotiveCoroutines[keys[i]] && LocoTelem.RouteMode[keys[i]])
                    {




                        Logger.LogToDebug($"loco {keys[i].DisplayName} currently has not called a coroutine - Calling the Coroutine with {keys[i].DisplayName} as an arguement");
                        LocoTelem.DriveForward[keys[i]] = true;
                        LocoTelem.LineDirectionEastWest[keys[i]] = true;

                        LocoTelem.TransitMode[keys[i]] = true;
                        LocoTelem.RMMaxSpeed[keys[i]] = 0;
                        LocoTelem.locomotiveCoroutines[keys[i]] = true;

                        if (!LocoTelem.LineDirectionEastWest.ContainsKey(keys[i]))
                        {
                            LocoTelem.LineDirectionEastWest[keys[i]] = true;
                        }

                        StartCoroutine(AutoEngineerControlRoutine(keys[i]));


                    }
                    else if (LocoTelem.locomotiveCoroutines[keys[i]] && !LocoTelem.RouteMode[keys[i]])
                    {
                        Logger.LogToDebug($"loco {keys[i].DisplayName} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {keys[i].DisplayName}");
                        LocoTelem.LocomotivePrevDestination.Remove(keys[i]);
                        //LocoTelem.LocomotiveDestination.Remove(keys[i]);
                        LocoTelem.locomotiveCoroutines.Remove(keys[i]);
                        LocoTelem.DriveForward.Remove(keys[i]);
                        //LocoTelem.LineDirectionEastWest.Remove(keys[i]);
                        LocoTelem.TransitMode.Remove(keys[i]);
                        LocoTelem.RMMaxSpeed.Remove(keys[i]);
                        StopCoroutine(AutoEngineerControlRoutine(keys[i]));
                    }
                }
            }
            else
            {
                //Logger.LogToDebug("No key in locomotiveCoroutines: there are no locomotives that require the extended logic");
            }
        }
        public IEnumerator AutoEngineerControlRoutine(Car locomotive)
        {

            Logger.LogToDebug($"Entered Coroutine for {locomotive.DisplayName} - is Route Mode Enabled? {LocoTelem.RouteMode[locomotive]}");


            LocoTelem.CenterCar[locomotive] = ManagedTrains.GetCenterCoach(locomotive);
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                ManagedTrains.GetNextDestination(locomotive);
            }
            bool lowcoalwarngiven       = false;
            bool lowwaterwarngiven      = false;
            bool lowfuelwarngiven       = false;
            float RMmaxSpeed            = 0;
            float distanceToStation     = 0;
            float olddist               = float.MaxValue;


            //Define the minimum quantities for fuel as variables for better programability. 
            //Future Use:       Could be made dynamic allowing the user to define the minimums 
            //Future Tuning:    In a tuning scenario updating a quantity once is easier than multiple times. 
            float minDieselQuantity     = 100;
            float minWaterQuantity      = 500;
            float minCoalQuantity       = 0.5f;
            float trainVelocity = 0;



            while (LocoTelem.RouteMode[locomotive])
            {

                float? coallevel = ManagedTrains.GetLoadInfoForLoco(locomotive, "coal") / 2000;
                float? waterlevel = ManagedTrains.GetLoadInfoForLoco(locomotive, "water");
                float? diesellevel = ManagedTrains.GetLoadInfoForLoco(locomotive, "diesel-fuel");

                //If steam locomotive Check the water and coal levels
                if(locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
                {
                    //If coal level is not null
                    if (coallevel != null)
                    {
                        //If coal is below minimums
                        if (coallevel < minCoalQuantity)
                        {
                            //Check if warning was given
                            if (!lowcoalwarngiven)
                            {
                                //Give Warning
                                lowcoalwarngiven = true;
                                Logger.LogToConsole(String.Format("Locomotive {0} has less than {1}T of coal remaining", Hyperlink.To(locomotive), minCoalQuantity));
                            }
                        }
                        else
                        {
                            //Reset Warning
                            lowcoalwarngiven = false;
                        }
                    }
                    //If water level is not null
                    if (waterlevel != null)
                    {
                        //If water is below minimums
                        if (waterlevel < minWaterQuantity)
                        {
                            //Check if warning was given
                            if (!lowwaterwarngiven)
                            {
                                //Give Warning
                                lowwaterwarngiven = true;
                                Logger.LogToConsole(String.Format("Locomotive {0} has less than {1}Gallons of Water remaining", Hyperlink.To(locomotive), minWaterQuantity));
                            }
                        }
                        else
                        {
                            //Reset Warning
                            lowwaterwarngiven = false;
                        }
                    }
                }
                //If Diesel locomotive diesel levels
                else if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel) 
                {
                    //If fuel level is not null
                    if (diesellevel != null)
                    {
                        //If diesel level is below defined minimums
                        if (diesellevel < minDieselQuantity)
                        {
                            //Check if warning was given
                            if (!lowfuelwarngiven)
                            {
                                //Give warning
                                lowfuelwarngiven = true;
                                Logger.LogToConsole(String.Format("Locomotive {0} has less than {1}Gallons of Diesel-Fuel remaining", Hyperlink.To(locomotive), minDieselQuantity));
                            }
                        }
                        else
                        {
                            //Reset warning
                            lowfuelwarngiven = false;
                        }
                    }
                }

                if (LocoTelem.TransitMode[locomotive])
                {
                    Logger.LogToDebug("starting transit mode");
                    olddist = float.MaxValue;
                    bool YieldRequired = false;
                    bool approachBlastrequired = false;
                    while (LocoTelem.TransitMode[locomotive])
                    {


                        olddist = distanceToStation;
                        if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                        {
                            Logger.LogToDebug($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                            //clearDictsForLoco(locomotive);
                            ManagedTrains.SetRouteModeEnabled(false, locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));

                            yield break;
                        }
                        if (!LocoTelem.RouteMode[locomotive])
                        {

                            Logger.LogToDebug($"loco {locomotive.DisplayName} - route mode was disabled - Stopping Coroutine for {locomotive.DisplayName}");
                            //clearDictsForLoco(locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                            break;

                        }
                        if (!ManagedTrains.IsCurrentDestinationSelected(locomotive))
                        {
                            LocoTelem.LocomotiveDestination.Remove(locomotive);
                            ManagedTrains.GetNextDestination(locomotive);
                        }




                        try
                        {
                            distanceToStation = ManagedTrains.GetDistanceToDest(locomotive);
                            YieldRequired = false;
                        }
                        catch
                        {
                            if (YieldRequired)
                            {
                                Logger.LogToDebug($"distance to station not able to be calculated after yielding once. stopping coroutine");
                                yield break;
                            }
                            Logger.LogToDebug($"distance to station could not be calculated. Yielding for 5s");
                            YieldRequired = true;
                        }
                        if (distanceToStation <= -6969f)
                        {
                            YieldRequired = true;
                        }
                        if (YieldRequired)
                        {
                            yield return new WaitForSeconds(5);
                        }

                        trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);

                        if (distanceToStation > 350)
                        {

                            if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 10f))

                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Logger.LogToDebug("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 100;
                                Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }

                            RMmaxSpeed = 100;
                            Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                            if (distanceToStation < 500f)
                            {
                                Logger.LogToDebug("checking if blast required");
                                if (approachBlastrequired)
                                {
                                    Logger.LogToDebug("attempting to perform approach blast");
                                    yield return ManagedTrains.RMblow(locomotive, 0.25f, 1.5f );
                                    yield return ManagedTrains.RMblow(locomotive, 1f, 2.5f);
                                    yield return ManagedTrains.RMblow(locomotive, 1f, 1.75f, 0.25f);
                                    yield return ManagedTrains.RMblow(locomotive, 1f, 0.25f);
                                    yield return ManagedTrains.RMblow(locomotive, 0f);
                                    approachBlastrequired = false;
                                }

                            }
                            else
                            {
                                approachBlastrequired = true;
                            }
                            yield return new WaitForSeconds(5);

                        }
                        else if (distanceToStation <= 350 && distanceToStation > 10)
                        {
                            if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 15f))
                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Logger.LogToDebug("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 100;
                                Logger.LogToDebug($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }
                            RMmaxSpeed = distanceToStation / 8f;
                            if (RMmaxSpeed < 5f)
                            {
                                RMmaxSpeed = 5f;
                            }
                            ManagedTrains.RMbell(locomotive, true);

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
                if (!LocoTelem.TransitMode[locomotive])
                {
                    if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                    {
                        Logger.LogToDebug($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                        //clearDictsForLoco(locomotive);
                        ManagedTrains.SetRouteModeEnabled(false, locomotive);
                        StopCoroutine(AutoEngineerControlRoutine(locomotive));
                        break;
                    }


                    //Feature Enahncement: Issue #24
                    //Write to console the arrival of the train consist at station X
                    string currentStation = LocoTelem.LocomotiveDestination[locomotive];
                    Logger.LogToConsole(String.Format("{0} has arrived at {1} station", Hyperlink.To(locomotive), currentStation.ToUpper()));
                    ManagedTrains.RMbell(locomotive, false);

                    Logger.LogToDebug($"about to set new destination, curent destination {LocoTelem.LocomotiveDestination[locomotive].ToUpper()}");
                    ManagedTrains.GetNextDestination(locomotive);
                    Logger.LogToDebug($"New destination was set, destination: {LocoTelem.LocomotiveDestination[locomotive].ToUpper()}");

                    Logger.LogToDebug("Starting loading mode");
                    ManagedTrains.CopyStationsFromLocoToCoaches(locomotive);
                    
                    List<int> numPassInTrain = new List<int> { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue };
                    

                    LocoTelem.CenterCar[locomotive] = ManagedTrains.GetCenterCoach(locomotive);
                    

                    while (!LocoTelem.TransitMode[locomotive])
                    {

                        if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                        {
                            Logger.LogToDebug($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                            //clearDictsForLoco(locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                            break;
                        }
                        if (!LocoTelem.RouteMode[locomotive])
                        {

                            Logger.LogToDebug($"loco {locomotive} - route mode was disabled - Stopping Coroutine for {locomotive}");
                            //clearDictsForLoco(locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                            break;

                        }
                        trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);
                        while (trainVelocity > 0.1f)
                        {
                            trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);
                            if (trainVelocity > 0.1)
                            {
                                yield return new WaitForSeconds(1);
                            }
                            else
                            {
                                ManagedTrains.RMbell(locomotive, false);
                                yield return new WaitForSeconds(3);
                            }
                            
                        }

                        for (int i = 0; i<=4; i++)
                        {
                            numPassInTrain[i] = ManagedTrains.GetNumPassInTrain(locomotive);
                            //Logger.LogToDebug($"{locomotive.DisplayName} Has {numPassInTrain} onboard.  Was {oldNumPassInTrain} 2 seconds ago");
                            yield return new WaitForSeconds(1);

                        }

                        bool paxLoadNoChange = false;
                        Logger.LogToDebug($"Passenger loading history for {locomotive.DisplayName} over the past 5 seconds {String.Join(",",numPassInTrain)}");
                        paxLoadNoChange = numPassInTrain.All(numPass => numPass != int.MaxValue && numPass == numPassInTrain.First());
                        if (!paxLoadNoChange)
                        {
                            Logger.LogToDebug($"{locomotive.DisplayName} Passenger count still changing - disembarkation/embarkation in progress");
                            yield return new WaitForSeconds(1);
                        }
                        else
                        {
                            bool clearedForDeparture = true;

                            //Check the fuel levels before clearning the train for departure

                            //Check Steam fuel levels
                            if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
                            {
                                //Null Check
                                if (waterlevel != null)
                                {
                                    //Erronious debug statement?
                                    //Logger.LogToDebug($"loaded or disembarked {Math.Abs(oldNumPassInTrain - numPassInTrain)} passengers disembarkation/embarkation finished");

                                    //Check if below minimums
                                    if (waterlevel < minWaterQuantity)
                                    {
                                        //Below minimums notate as such and hold loco
                                        Logger.LogToConsole(String.Format("Locomotive {0} is low on {1} and is holding at {2}", Hyperlink.To(locomotive), "water" , LocoTelem.LocomotivePrevDestination[locomotive] ));
                                        clearedForDeparture = false;

                                        //Check again in 30 seconds. 
                                        yield return new WaitForSeconds(30);
                                    }
                                }

                                //Null Check
                                if (coallevel != null)
                                {
                                    //Check if below minimums
                                    if (coallevel < minCoalQuantity)
                                    {
                                        Logger.LogToConsole(String.Format("Locomotive {0} is low on {1} and is holding at {2}", Hyperlink.To(locomotive), "coal", LocoTelem.LocomotivePrevDestination[locomotive]));
                                        clearedForDeparture = false;
                                        yield return new WaitForSeconds(30);
                                    }

                                }
                            }
                            else if(locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel)
                            {
                                //Null Check
                                if (diesellevel != null)
                                {
                                    //Check if below minimums
                                    if (diesellevel < minDieselQuantity)
                                    {
                                        Logger.LogToConsole(String.Format("Locomotive {0} is low on {1} and is holding at {2}", Hyperlink.To(locomotive), "diesel-fuel", LocoTelem.LocomotivePrevDestination[locomotive]));
                                        clearedForDeparture = false;
                                        yield return new WaitForSeconds(30);
                                    }

                                }
                            }

                            if (clearedForDeparture)
                            {
                                LocoTelem.TransitMode[locomotive] = true;

                                //Feature Enahncement: Issue #24
                                //Write to console the departure of the train consist at station X
                                //Bugfix: Fixed Erronious departure messages if not cleared for departure.
                                Logger.LogToConsole(String.Format("{0} has departed {1} for {2}", Hyperlink.To(locomotive), currentStation.ToUpper(), LocoTelem.LocomotiveDestination[locomotive].ToUpper()));

                                yield return new WaitForSeconds(1);
                            }
                        }
                    }
                }
            }
            if (!LocoTelem.RouteMode[locomotive])
            {

                Logger.LogToDebug($"loco {locomotive} - route mode was disabled - Stopping Coroutine for {locomotive}");
                //clearDictsForLoco(locomotive);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));

            }
            if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                Logger.LogToDebug($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                //clearDictsForLoco(locomotive);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));
                ;
            }
        }

        private void clearDictsForLoco(Car locomotive)
        {
            LocoTelem.LocomotivePrevDestination.Remove(locomotive);
            LocoTelem.LocomotiveDestination.Remove(locomotive);
            LocoTelem.locomotiveCoroutines.Remove(locomotive);
            LocoTelem.DriveForward.Remove(locomotive);
            LocoTelem.LineDirectionEastWest.Remove(locomotive);
            LocoTelem.TransitMode.Remove(locomotive);
            LocoTelem.RMMaxSpeed.Remove(locomotive);
        }
        private void clearDicts()
        {
            LocoTelem.LocomotivePrevDestination.Clear();
            //LocoTelem.LocomotiveDestination.Clear();
            LocoTelem.locomotiveCoroutines.Clear();
            LocoTelem.DriveForward.Clear();
            LocoTelem.LineDirectionEastWest.Clear();
            LocoTelem.TransitMode.Clear();
            LocoTelem.RMMaxSpeed.Clear();
        }

        void OnMapDidNunloadEvenForRouteMode(MapDidUnloadEvent mapDidUnloadEvent)
        {
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("OnMapDidNunloadEvenForRouteMode called");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
            Logger.LogToDebug("--------------------------------------------------------------------------------------------------");

            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
                Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
                Logger.LogToDebug("Stopping All Route AI coroutine instances");
                Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
                Logger.LogToDebug("--------------------------------------------------------------------------------------------------");
                //Logger.LogToDebug("There is data in locomotiveCoroutines");
                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {

                    StopCoroutine(AutoEngineerControlRoutine(keys[i]));

                }
                clearDicts();
            }
        }
    }
}
