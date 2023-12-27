using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Model;

namespace RouteManager
{
    public class RouteAI : MonoBehaviour
    {

        //Initialize Route AI.
        void Awake()
        {
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Debug.Log("subscribing to unload event");
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Messenger.Default.Register<MapDidUnloadEvent>(this, OnMapDidNunloadEvenForRouteMode);
        }
        void Update()
        {

            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                //Debug.Log("There is data in locomotiveCoroutines");
                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {
                    //Debug.Log($"Loco {keys[i].id} has values Coroutine: {LocoTelem.locomotiveCoroutines[keys[i]]} and Route Mode bool: {LocoTelem.RouteMode[keys[i]]}");
                    if (!LocoTelem.locomotiveCoroutines[keys[i]] && LocoTelem.RouteMode[keys[i]])
                    {




                        Debug.Log($"loco {keys[i].DisplayName} currently has not called a coroutine - Calling the Coroutine with {keys[i].DisplayName} as an arguement");
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
                        Debug.Log($"loco {keys[i].DisplayName} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {keys[i].DisplayName}");
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
                //Debug.Log("No key in locomotiveCoroutines: there are no locomotives that require the extended logic");
            }
        }
        public IEnumerator AutoEngineerControlRoutine(Car locomotive)
        {

            Debug.Log($"Entered Coroutine for {locomotive.DisplayName} - is Route Mode Enabled? {LocoTelem.RouteMode[locomotive]}");


            LocoTelem.CenterCar[locomotive] = ManagedTrains.GetCenterCoach(locomotive);
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                ManagedTrains.GetNextDestination(locomotive);
            }
            bool lowcoalwarngiven = false;
            bool lowwaterwarngiven = false;
            bool lowfuelwarngiven = false;
            float RMmaxSpeed = 0;
            float distanceToStation = 0;
            float olddist = float.MaxValue;
            while (LocoTelem.RouteMode[locomotive])
            {

                float? coallevel = ManagedTrains.GetLoadInfoForLoco(locomotive, "coal") / 2000;
                float? waterlevel = ManagedTrains.GetLoadInfoForLoco(locomotive, "water");
                float? diesellevel = ManagedTrains.GetLoadInfoForLoco(locomotive, "diesel-fuel");

                if(locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
                {
                    if (coallevel != null)
                    {
                        if (coallevel < 0.5)
                        {
                            if (!lowcoalwarngiven)
                            {
                                lowcoalwarngiven = true;
                                Console.Log($"Locomotive {locomotive.DisplayName} has less than 0.5T of coal remaining");
                            }
                        }
                        else
                        {
                            lowcoalwarngiven = false;
                        }
                    }

                    if (waterlevel != null)
                    {
                        if (waterlevel < 500)
                        {
                            if (!lowwaterwarngiven)
                            {
                                lowwaterwarngiven = true;
                                Console.Log($"Locomotive {locomotive.DisplayName} has less than 500 Gallons of Water remaining");
                            }
                        }
                        else
                        {
                            lowwaterwarngiven = false;
                        }
                    }
                }
                
                if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel) {

                    if (diesellevel != null)
                    {
                        if (diesellevel < 100)
                        {
                            if (!lowfuelwarngiven)
                            {
                                lowfuelwarngiven = true;
                                Console.Log($"Locomotive {locomotive.DisplayName} has less than 100 Gallons of diesel-fuel remaining");
                            }
                        }
                        else
                        {
                            lowfuelwarngiven = false;
                        }
                    }
                }

                if (LocoTelem.TransitMode[locomotive])
                {
                    Debug.Log("starting transit mode");
                    olddist = float.MaxValue;
                    bool YieldRequired = false;
                    while (LocoTelem.TransitMode[locomotive])
                    {


                        olddist = distanceToStation;
                        if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                        {
                            Debug.Log($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                            //clearDictsForLoco(locomotive);
                            ManagedTrains.SetRouteModeEnabled(false, locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));

                            yield break;
                        }
                        if (!LocoTelem.RouteMode[locomotive])
                        {

                            Debug.Log($"loco {locomotive.DisplayName} - route mode was disabled - Stopping Coroutine for {locomotive.DisplayName}");
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
                                Debug.Log($"distance to station not able to be calculated after yielding once. stopping coroutine");
                                yield break;
                            }
                            Debug.Log($"distance to station could not be calculated. Yielding for 5s");
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

                        var trainVelocity = Math.Abs(locomotive.velocity * 2.23694f);

                        if (distanceToStation > 350)
                        {

                            if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 10f))

                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Debug.Log("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 100;
                                Debug.Log($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }

                            RMmaxSpeed = 100;
                            Debug.Log($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                            yield return new WaitForSeconds(5);

                        }
                        else if (distanceToStation <= 350 && distanceToStation > 10)
                        {
                            if (distanceToStation > olddist && (trainVelocity > 1f && trainVelocity < 15f))
                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Debug.Log("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 100;
                                Debug.Log($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }
                            RMmaxSpeed = distanceToStation / 8f;
                            if (RMmaxSpeed < 5f)
                            {
                                RMmaxSpeed = 5f;
                            }


                            Debug.Log($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                            yield return new WaitForSeconds(1);

                        }
                        else if (distanceToStation <= 10 && distanceToStation > 0)
                        {
                            RMmaxSpeed = 0f;
                            Debug.Log($"{locomotive.DisplayName} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
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
                        Debug.Log($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                        //clearDictsForLoco(locomotive);
                        ManagedTrains.SetRouteModeEnabled(false, locomotive);
                        StopCoroutine(AutoEngineerControlRoutine(locomotive));
                        break;
                    }
                    ManagedTrains.GetNextDestination(locomotive);
                    Debug.Log("Starting loading mode");
                    ManagedTrains.CopyStationsFromLocoToCoaches(locomotive);
                    int numPassInTrain = 0;
                    int oldNumPassInTrain = int.MaxValue;
                    bool firstIter = true;

                    LocoTelem.CenterCar[locomotive] = ManagedTrains.GetCenterCoach(locomotive);
                    Debug.Log($"about to set new destination, curent destination{LocoTelem.LocomotiveDestination[locomotive]}");

                    Debug.Log($"New destination was set, destination: {LocoTelem.LocomotiveDestination[locomotive]}");

                    while (!LocoTelem.TransitMode[locomotive])
                    {

                        if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                        {
                            Debug.Log($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                            //clearDictsForLoco(locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                            break;
                        }
                        if (!LocoTelem.RouteMode[locomotive])
                        {

                            Debug.Log($"loco {locomotive} - route mode was disabled - Stopping Coroutine for {locomotive}");
                            //clearDictsForLoco(locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                            break;

                        }

                        if (firstIter)
                        {
                            yield return new WaitForSeconds(10);
                            firstIter = false;
                        }

                        numPassInTrain = ManagedTrains.GetNumPassInTrain(locomotive);
                        Debug.Log($"{locomotive} Has {numPassInTrain} onboard \t Was {oldNumPassInTrain} 5 seconds ago");

                        if (oldNumPassInTrain != numPassInTrain)
                        {
                            Debug.Log($"loaded or disembarked {Math.Abs(oldNumPassInTrain - numPassInTrain)} passengers disembarkation/embarkation in progress");
                            oldNumPassInTrain = numPassInTrain;
                            yield return new WaitForSeconds(10);
                        }
                        else
                        {
                            bool clearedForDeparture = true;

                            if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
                            {
                                if (waterlevel != null)
                                {
                                    Debug.Log($"loaded or disembarked {Math.Abs(oldNumPassInTrain - numPassInTrain)} passengers disembarkation/embarkation finished");
                                    if (waterlevel < 500)
                                    {
                                        Console.Log($"Locomotive {locomotive.DisplayName} is low on water and is holding at {LocoTelem.LocomotivePrevDestination[locomotive]} ");
                                        clearedForDeparture = false;
                                        yield return new WaitForSeconds(30);

                                    }
                                }

                                if (coallevel != null)
                                {
                                    if (coallevel < .5)
                                    {
                                        Console.Log($"Locomotive {locomotive.DisplayName} is low on coal and is holding at {LocoTelem.LocomotivePrevDestination[locomotive]} ");
                                        clearedForDeparture = false;
                                        yield return new WaitForSeconds(30);
                                    }

                                }
                            }

                            if(locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel)
                            {
                                if (diesellevel != null)
                                {
                                    if (diesellevel < 100)
                                    {
                                        Console.Log($"Locomotive {locomotive.DisplayName} is low on diesel-fuel and is holding at {LocoTelem.LocomotivePrevDestination[locomotive]} ");
                                        clearedForDeparture = false;
                                        yield return new WaitForSeconds(30);
                                    }

                                }
                            }

                            if (clearedForDeparture)
                            {
                                LocoTelem.TransitMode[locomotive] = true;
                                yield return new WaitForSeconds(1);
                            }


                        }
                    }
                }
            }
            if (!LocoTelem.RouteMode[locomotive])
            {

                Debug.Log($"loco {locomotive} - route mode was disabled - Stopping Coroutine for {locomotive}");
                //clearDictsForLoco(locomotive);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));

            }
            if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                Debug.Log($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
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
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Debug.Log("OnMapDidNunloadEvenForRouteMode called");
            Debug.Log("--------------------------------------------------------------------------------------------------");
            Debug.Log("--------------------------------------------------------------------------------------------------");

            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                Debug.Log("--------------------------------------------------------------------------------------------------");
                Debug.Log("--------------------------------------------------------------------------------------------------");
                Debug.Log("Stopping All Route AI coroutine instances");
                Debug.Log("--------------------------------------------------------------------------------------------------");
                Debug.Log("--------------------------------------------------------------------------------------------------");
                //Debug.Log("There is data in locomotiveCoroutines");
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
