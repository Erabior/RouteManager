using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using RollingStock;
using System.Collections.Generic;
using System;
using UI.Builder;
using UI.CarInspector;
using Game.Messages;
using Game.State;
using Model.AI;
using System.Linq;
using Model;
using System.Reflection;
using Game;
using static Model.OpsNew.PassengerMarker;
using Helpers;
using Serilog;
using Model.Definition;
using Track;
using System.Collections;
using static ManagedTrains;
using UI.Menu;
using System.Runtime.CompilerServices;
using Model.OpsNew;
using UnityEngine.InputSystem.EnhancedTouch;


namespace RouteManager
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Dispatcher : BaseUnityPlugin
    {
        private const string modGUID = "Erabior.Dispatcher";
        private const string modName = "Dispatcher";
        private const string modVersion = "0.7.8.5";
        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;

        void Awake()
        {
            harmony.PatchAll();
            mls = Logger;
            mls.LogInfo("Dispatcher Mod Loaded - Awake method called.");
        }
    }

    public class RouteAIInjector : MonoBehaviour
    {
        [HarmonyPatch(typeof(PersistentLoader), nameof(PersistentLoader.ShowLoadingScreen))]
        public static class ShowLoadingScreen
        {
            public static void Postfix(bool show)
            {
                new GameObject().AddComponent<RouteAI>();
            }
        }
    }
    


    public class RouteAI : MonoBehaviour
    {
        void Update()
        {
            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                //Debug.Log("There is data in locomotiveCoroutines");
                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {
                    if (!LocoTelem.locomotiveCoroutines[keys[i]])
                    {
                        Debug.Log($"loco {keys[i]} currently has not called a coroutine - Calling the Coroutine with {keys[i]} as an arguement");
                        LocoTelem.locomotiveCoroutines[keys[i]] = true;


                        LocoTelem.DriveForward[keys[i]]= true;
                        LocoTelem.LineDirectionEastWest[keys[i]] = true;
                        LocoTelem.TransitMode[keys[i]] = true;
                        LocoTelem.RMMaxSpeed[keys[i]] = 0;

                        StartCoroutine(AutoEngineerControlRoutine(keys[i]));
                    }
                    else if (LocoTelem.locomotiveCoroutines[keys[i]] && LocoTelem.SelectedStations[keys[i]].Count < 0)
                    {
                        Debug.Log($"loco {keys[i]} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {keys[i]}");
                        LocoTelem.LocomotivePrevDestination.Remove(keys[i]);
                        LocoTelem.LocomotiveDestination.Remove(keys[i]);
                        LocoTelem.locomotiveCoroutines.Remove(keys[i]);
                        LocoTelem.DriveForward.Remove(keys[i]);
                        LocoTelem.LineDirectionEastWest.Remove(keys[i]);
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
        private IEnumerator AutoEngineerControlRoutine(Car locomotive)
        {
            Debug.Log($"Entered Coroutine for {locomotive.id} - is any station selected {StationManager.IsAnyStationSelectedForLocomotive(locomotive)}");

            ManagedTrains.GetNextDestination(locomotive);
            float RMmaxSpeed = 0;
            float distanceToStation=0;
            float olddist=float.MaxValue;
            LocoTelem.CenterCar[locomotive]=GetCenterCoach(locomotive);

            while (StationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                if (LocoTelem.TransitMode[locomotive])
                {
                    Debug.Log("starting transit mode");
                    olddist = float.MaxValue;
                    while (LocoTelem.TransitMode[locomotive])
                    {
                        
                        bool YieldRequired = false;
                        olddist = distanceToStation;
                        if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                        {
                            break;
                        }
                        try
                        {
                            distanceToStation = ManagedTrains.GetDistanceToDest(locomotive);
                            
                            
                        }
                        catch
                        {
                            Debug.Log($"distance to station could not be calculated. Yielding for 5s");
                            YieldRequired = true;
                        }
                        if (YieldRequired){
                            yield return new WaitForSeconds(5);
                        }

                        
                        distanceToStation = ManagedTrains.GetDistanceToDest(locomotive);
                        


                        
                        var trainVelocity = Math.Abs(locomotive.velocity* 2.23694f);
                        if (distanceToStation > 350)
                        {
                            
                            
                            if (distanceToStation > olddist && (trainVelocity > 5f && trainVelocity < 15f))
                            {
                                LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                                Debug.Log("Was driving in the wrong direction. Reversing Direction");
                                RMmaxSpeed = 45;
                                Debug.Log($"{locomotive.id} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                                StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                                yield return new WaitForSeconds(30);
                            }

                            RMmaxSpeed = 45;
                            Debug.Log($"{locomotive.id} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                            yield return new WaitForSeconds(5);
                            
                        }
                        else if (distanceToStation <= 350 && distanceToStation > 25)
                        {
                            RMmaxSpeed = distanceToStation / 7f;
                            Debug.Log($"{locomotive.id} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], (int)RMmaxSpeed, null));
                            yield return new WaitForSeconds(1);
                            
                        }
                        else if (distanceToStation <= 25 && distanceToStation > 10)
                        {
                            RMmaxSpeed = 5f;
                            Debug.Log($"{locomotive.id} distance to station: {distanceToStation} Speed: {trainVelocity} Max speed: {RMmaxSpeed}");
                            StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.DriveForward[locomotive], 0, null));
                            LocoTelem.TransitMode[locomotive] = false;
                            yield return new WaitForSeconds(1);
                            
                        }
                    }
                }
                if (!LocoTelem.TransitMode[locomotive])
                {
                    Debug.Log("Starting loading mode");
                    int numPassInTrain = 0;
                    int oldNumPassInTrain = int.MaxValue;
                    bool firstIter=true;
                    LocoTelem.CenterCar[locomotive] = GetCenterCoach(locomotive);
                    Debug.Log($"about to set new destination, curent destination{LocoTelem.LocomotiveDestination[locomotive]}");
                    ManagedTrains.GetNextDestination(locomotive);
                    Debug.Log($"New destination was set, destination: {LocoTelem.LocomotiveDestination[locomotive]}");
                    while (!LocoTelem.TransitMode[locomotive])
                    {
                        
                        if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
                        {
                            Debug.Log($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                            clearDicts(locomotive);
                            StopCoroutine(AutoEngineerControlRoutine(locomotive));
                            break;
                        }

                        if (firstIter)
                        {
                            yield return new WaitForSeconds(5);
                            firstIter=false;
                        }

                        numPassInTrain = GetNumPassInTrain(locomotive);
                        Debug.Log($"{locomotive} Has {numPassInTrain} onboard \t Was {oldNumPassInTrain} 5 seconds ago");

                        if (oldNumPassInTrain != numPassInTrain)
                        {
                            Debug.Log($"loaded or disembarked {Math.Abs(oldNumPassInTrain - numPassInTrain)} passengers disembarkation/embarkation in progress");
                            oldNumPassInTrain = numPassInTrain;
                            yield return new WaitForSeconds(5);
                        }
                        else
                        {
                            Debug.Log($"loaded or disembarked {Math.Abs(oldNumPassInTrain - numPassInTrain)} passengers disembarkation/embarkation finished");
                            LocoTelem.TransitMode[locomotive] = true;
                            yield return new WaitForSeconds(1);
                        }
                    }
                }   
            }
            
            if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                Debug.Log($"loco {locomotive} currently has called a coroutine but no longer has stations selected - Stopping Coroutine for {locomotive}");
                clearDicts(locomotive);
                StopCoroutine(AutoEngineerControlRoutine(locomotive));
                ;
            }
        }

        private void clearDicts(Car locomotive)
        {
            LocoTelem.LocomotivePrevDestination.Remove(locomotive);
            LocoTelem.LocomotiveDestination.Remove(locomotive);
            LocoTelem.locomotiveCoroutines.Remove(locomotive);
            LocoTelem.DriveForward.Remove(locomotive);
            LocoTelem.LineDirectionEastWest.Remove(locomotive);
            LocoTelem.TransitMode.Remove(locomotive);
            LocoTelem.RMMaxSpeed.Remove(locomotive);
        }
    }
}

public class ManagedTrains : MonoBehaviour
{
    // Rest of your ManagedTrains code...

    public class LocoTelem
    {
        public static Dictionary<Car, string> LocomotiveDestination { get; private set; } = new Dictionary<Car, string>();
        public static Dictionary<Car, string> LocomotivePrevDestination { get; private set; } = new Dictionary<Car, string>();
        public static Dictionary<Car, List<PassengerStop>> SelectedStations { get; private set; } = new Dictionary<Car, List<PassengerStop>>();
        public static Dictionary<Car, float> RMMaxSpeed { get; private set; } = new Dictionary<Car, float>();
        public static Dictionary<Car, bool> TransitMode { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool> LineDirectionEastWest { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool> DriveForward { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, bool> locomotiveCoroutines { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, Car> CenterCar { get; private set; } = new Dictionary<Car, Car>();

    }

   
    

    // A dictionary mapping cars to a list of selected stations.
   


    public static void InitializeLocomotive(Car locomotive)
    {
        if (!LocoTelem.LineDirectionEastWest.ContainsKey(locomotive))
        {
            // Default direction is true
            LocoTelem.LineDirectionEastWest[locomotive] = true;
        }

        if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
        {
            // Get the first selected station for this locomotive, if any
            if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStops) && selectedStops.Any())
            {
                LocoTelem.LocomotiveDestination[locomotive] = selectedStops.First().identifier;
            }
            else
            {
                LocoTelem.LocomotiveDestination[locomotive] = "whittier"; // No stations selected, set to whittier
            }
        }
    }


    public static void UpdateSelectedStations(Car car, List<PassengerStop> selectedStops)
    {
        if (car == null)
        {
            throw new ArgumentNullException(nameof(car));
        }

        LocoTelem.SelectedStations[car] = selectedStops;
    }


    public static void PrintCarInfo(Car car)
    {
        var graph = Graph.Shared;
        if (car == null)
        {
            Debug.Log("Car is null");
            return;
        }
        
        // Retrieve saved stations for this car from ManagedTrains
        if (ManagedTrains.LocoTelem.SelectedStations.TryGetValue(car, out List<PassengerStop> selectedStations))
        {
            string stationNames = string.Join(", ", selectedStations.Select(s => s.name));
            Vector3? centerPoint = car.GetCenterPosition(graph); // Assuming GetCenterPosition exists

            Debug.Log($"Car ID: {car.id}, Selected Stations: {stationNames}, Center Position: {centerPoint}");
        }
        else
        {
            Debug.Log("No stations selected for this car.");
        }
        

        if (ManagedTrains.LocoTelem.LocomotiveDestination.TryGetValue(car, out string dest))
        {

            Debug.Log($"destination: {dest}");
        }
        else
        {
            Debug.Log("No destination for this car.");
        }
        
        if (graph == null)
        {
            Debug.LogError("Graph object is null");
            return; // or handle this case as needed
        }

        if (car == null)
        {
            Debug.LogError("Car object is null");
            return; // or handle this case as needed
        }

        var locationF = car.LocationF;
        var locationR = car.LocationR;
        var direction = car.GetCenterRotation(graph);
        Debug.Log($"LocationF {locationF} LocationR {locationR} Rotation: {direction}");

        if (ManagedTrains.LocoTelem.LocomotivePrevDestination.TryGetValue(car, out string prevDest))
        {
            Debug.Log($"Previous destination: {prevDest}");
        }
        else
        {
            Debug.Log("No previous destination for this car.");
        }
        if (ManagedTrains.LocoTelem.TransitMode.TryGetValue(car, out bool inTransitMode))
        {
            Debug.Log($"Transit Mode: {inTransitMode}");
        }
        else
        {
            Debug.Log("No Transit Mode recorded for this car.");
        }
        if (ManagedTrains.LocoTelem.LineDirectionEastWest.TryGetValue(car, out bool isEastWest))
        {
            Debug.Log($"Line Direction East/West: {isEastWest}");
        }
        else
        {
            Debug.Log("No Line Direction East/West recorded for this car.");
        }
        if (ManagedTrains.LocoTelem.DriveForward.TryGetValue(car, out bool driveForward))
        {
            Debug.Log($"Drive Forward: {driveForward}");
        }
        else
        {
            Debug.Log("No Drive Forward recorded for this car.");
        }
        if (ManagedTrains.LocoTelem.locomotiveCoroutines.TryGetValue(car, out bool coroutineExists))
        {
            Debug.Log($"Locomotive Coroutine Exists: {coroutineExists}");
        }
        else
        {
            Debug.Log("No Locomotive Coroutine recorded for this car.");
        }
        if (ManagedTrains.LocoTelem.CenterCar.TryGetValue(car, out Car centerCar))
        {
            Debug.Log($"Center Car: {centerCar}");
        }
        else
        {
            Debug.Log("No Center Car recorded for this car.");
        }
        try
        {
            LocoTelem.CenterCar[car] = GetCenterCoach(car);
            Debug.Log($"center car for {car}: {LocoTelem.CenterCar[car]}");
        }
        catch (Exception ex)
        {
            Debug.Log($"could not get center car: {ex}");
        }
        var Locovelocity = car.velocity;
        Debug.Log($"Current Speed: {Locovelocity}");

        var cars = car.EnumerateCoupled().ToList();

        foreach (var trainCar in cars)
        {
            Debug.Log($"{trainCar.Archetype}");
        }
    }


    public static bool IsDirectionForward { get; set; } = true;

    private static readonly List<string> orderedStations = new List<string>
    {
        "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
        "almond", "nantahala", "topton", "rhodo", "andrews"
    };

    // Method to get the next destination station

    public static string GetClosestSelectedStation(Car locomotive)
    {
        var graph = Graph.Shared;
        Vector3? centerPoint = locomotive.GetCenterPosition(graph);
        Debug.Log($"Position of the loco {centerPoint} also centerpoint.value {centerPoint.Value}");
        // Check if centerPoint is null
        if (centerPoint == null)
        {
            Debug.LogError("Could not obtain locomotive's center position.");
            return null;
        }

        if (!LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations) || selectedStations.Count == 0)
        {
            Debug.Log("No stations selected for this locomotive.");
            return null;
        }

        // Initialize variables to track the closest station
        string closestStationName = null;
        float closestDistance = float.MaxValue;

        // Iterate over each selected station using a for loop
        for (int i = 0; i < selectedStations.Count; i++)
        {
            PassengerStop station = selectedStations[i];
            Debug.Log($"Station that was retrived from selectedStation: {station} and this shoudl be the Indentifier for the station {station.identifier}");
            if (StationManager.Stations.TryGetValue(station.identifier, out StationData stationData))
            {
                // Calculate the distance between the locomotive and the station's center point
                // Unwrap the nullable Vector3 using the Value property
                Debug.Log($"Station center: {stationData.Center}");
                Debug.Log($"loco center: {centerPoint.Value}");

                float distance = Vector3.Distance(centerPoint.Value, stationData.Center);

                // Update the closest station if this one is closer
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStationName = station.identifier;
                }
            }
            else
            {
                Debug.LogError($"Station data not found for identifier: {station.identifier}");
            }
        }
        Debug.Log($"returning {closestStationName}");
        return closestStationName;
    }

    public static void GetNextDestination(Car locomotive)
    {
        Debug.Log($"Getting next station for {locomotive.id}");
        string currentStation = null;
        if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
        {
            Debug.Log($"LocomotiveDestination does not contain key: {locomotive} getting the closes station");
            currentStation = GetClosestSelectedStation(locomotive);
            Debug.Log($"Locomotive {locomotive} is closest to {currentStation} ");
            LocoTelem.LocomotiveDestination[locomotive] = currentStation;
        }
        else
        {
            currentStation = LocoTelem.LocomotiveDestination[locomotive];
        }
        if (!LocoTelem.LineDirectionEastWest.ContainsKey(locomotive))
        {
            LocoTelem.LineDirectionEastWest[locomotive] = true;
        }
        bool EastWest = LocoTelem.LineDirectionEastWest[locomotive];
        Debug.Log($"current station is {currentStation}");
        List<string> selectedStationIdentifiers = LocoTelem.SelectedStations
            .SelectMany(pair => pair.Value)
            .Select(passengerStop => passengerStop.identifier)
            .Distinct()
            .ToList();

        var orderedSelectedStations = orderedStations.Where(item => selectedStationIdentifiers.Contains(item)).ToList(); 

        Debug.Log($"try to get value from SelectedStations and checking number of selected stops");
        if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStops) && selectedStops.Count > 1)
        {
            Debug.Log($"got value and there were {selectedStops.Count} stops");

            int currentIndex = orderedSelectedStations.IndexOf(currentStation);

            Debug.Log($"The index of the current station in the list of selected stations is {currentIndex}");
            if (currentIndex == -1)
            {
                LocoTelem.LocomotiveDestination[locomotive] = selectedStops.First().identifier;
                Debug.Log($" setting the next station to the first station because there was no current station");
                return;  // If no current station, return the first selected station
            }



            if (EastWest)
            {
                Debug.Log($"Going East to West");

                if (currentIndex == orderedSelectedStations.Count - 1)
                {
                    Debug.Log($"Reached {currentStation} and is the end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex-1]}");
                    CopyStationsFromLocoToCoaches(locomotive);
                    LocoTelem.LineDirectionEastWest[locomotive] = false;
                    LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                    LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex - 1];
                }
                else
                {
                    Debug.Log($"Reached {currentStation} next station is {orderedSelectedStations[currentIndex + 1]}");
                    LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex + 1];
                    
                }

                return;
            }
            else
            {
                Debug.Log($"Going West to East");

                if (currentIndex == 0)
                {
                    Debug.Log($"Reached {currentStation} and is the end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex + 1]}");
                    CopyStationsFromLocoToCoaches(locomotive);
                    LocoTelem.LineDirectionEastWest[locomotive] = true;
                    LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                    LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex + 1];
                }
                else
                {
                    Debug.Log($"Reached {currentStation} next station is {orderedSelectedStations[currentIndex - 1]}");
                    LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex - 1];
                }

                return;
            }
        }
        Debug.Log("There was no next destination");
        return; // No next destination
    }

    public static Vector3 GetTrainCenter(Car locomotive)
    {
        var graph = Graph.Shared;

        // List of all coupled cars
        var cars = locomotive.EnumerateCoupled().ToList();

        // List of cars with their center positions
        var carPositions = cars.Select(car => car.GetCenterPosition(graph)).ToList();

        // Calculate the average position (center) of all cars
        Vector3 center = Vector3.zero;
        foreach (var pos in carPositions)
        {
            center += pos;
        }
        center /= cars.Count;

        // Find the car closest to the center position
        float bestDist = float.PositiveInfinity;
        Car bestCar = null;
        foreach (var car in cars)
        {
            var dist = Vector3.SqrMagnitude(car.GetCenterPosition(graph) - center);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestCar = car;
            }
        }

        // Return the center position
        return center;
    }

    public static Car GetCenterCoach (Car locomotive)
    {
        var graph = Graph.Shared;

        // List of all coupled cars
        var cars = locomotive.EnumerateCoupled().ToList();
        var coaches = new List<Car>();

        foreach (var car in cars)
        {

            if(car.Archetype == CarArchetype.Coach)
            {
                coaches.Add(car);
            }

        }
        // List of cars with their center positions
        var carPositions = coaches.Select(coaches => coaches.GetCenterPosition(graph)).ToList();
        
        // Calculate the average position (center) of all cars
        Vector3 center = Vector3.zero;
        foreach (var pos in carPositions)
        {
            center += pos;
        }
        center /= coaches.Count;

        // Find the car closest to the center position
        float bestDist = float.PositiveInfinity;
        Car bestCar = null;
        foreach (var coach in coaches)
        {
            var dist = Vector3.SqrMagnitude(coach.GetCenterPosition(graph) - center);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestCar = coach;
            }
        }

        // Return the center position
        return bestCar;
    }

    public static int GetNumPassInTrain(Car locomotive) 
    { 
        int numPass = 0;

        var cars = locomotive.EnumerateCoupled().ToList();
        var coaches = new List<Car>();

        foreach (var car in cars)
        {

            if (car.Archetype == CarArchetype.Coach)
            {
                coaches.Add(car);
            }

        }

        foreach (Car coach in coaches)
        {

            try
            {
                numPass += GetPassengerCount(coach);
            }
            catch (Exception ex)
            {
                Debug.Log($"failed to get the number of passengers from GetPassengerCount(coach): {ex}");
            }
            

        }

        return numPass;
    }


    public static int GetPassengerCount(Car coach)
    {
        return coach.GetPassengerMarker()?.TotalPassengers ?? 0;
    }


    public static float GetDistanceToDest(Car locomotive)
    {
        // Check if the locomotive is null
        if (locomotive == null)
        {
            Debug.LogError("Locomotive is null in GetDistanceToDest.");
            return 0f; // Return a default value or handle this case as needed
        }
        
        // Check if the locomotive key exists in the LocomotiveDestination dictionary
        if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
        {
            Debug.LogError($"LocomotiveDestination does not contain key: {locomotive}");
            LocoTelem.LocomotiveDestination[locomotive] = GetClosestSelectedStation(locomotive);

            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                return 0f; // Or handle this scenario appropriately
            }

        }
        Vector3 locomotivePosition = new Vector3();
        string destination = LocoTelem.LocomotiveDestination[locomotive];
        if (destination == null)
        {
            Debug.LogError("Destination is null for locomotive.");
            return 0f; // Handle null destination
        }
        var graph = Graph.Shared;
        if (LocoTelem.CenterCar.ContainsKey(locomotive))
        {
            if (LocoTelem.CenterCar[locomotive] is Car)
            {
                locomotivePosition = LocoTelem.CenterCar[locomotive].GetCenterPosition(graph);
            }
            else
            {
                locomotivePosition = locomotive.GetCenterPosition(graph);
            }
        }
        else
        {
            locomotivePosition = locomotive.GetCenterPosition(graph);
        }
        if (!StationManager.Stations.ContainsKey(destination))
        {
            Debug.LogError($"Station not found for destination: {destination}");
            return 0f; // Handle missing station
        }

        Vector3 destCenter = StationManager.Stations[destination].Center;
        return Vector3.Distance(locomotivePosition, destCenter);
    }

    private static void CopyStationsFromLocoToCoaches(Car locomotive)
    {
        // Retrieve all selected stations for the locomotive
        HashSet<string> selectedStations = LocoTelem.SelectedStations[locomotive].Select(stop => stop.identifier).ToHashSet();

        // Loop through each coupled car
        foreach (Car item in locomotive.EnumerateCoupled())
        {
            if (item.Archetype == CarArchetype.Coach)
            {
                StateManager.ApplyLocal(new SetPassengerDestinations(item.id, selectedStations.ToList()));
                ;
            }
        }
    }

    // Method to display a message about the update (can be customized based on your UI implementation)
    private void DisplayUpdatedPassengerCarsMessage(int count)
    {
        // Implement the logic to display a message to the user
        Debug.Log($"Selected stations copied to {count} passenger cars.");
    }
}



public class StationData
{
    public Vector3 Pos0 { get; set; }
    public Vector3 Pos1 { get; set; }
    public Vector3 Center { get; set; }
    public float Length { get; set; }

    public StationData(float x0, float y0, float z0, float x1, float y1, float z1, float xc, float yc, float zc, float len)
    {
        Pos0 = new Vector3(x0, y0, z0);
        Pos1 = new Vector3(x1, y1, z1);
        Center = new Vector3(xc, yc, zc);
        Length = len;
    }
}

public static class StationManager
{
    private static Dictionary<string, bool> stationSelections = new Dictionary<string, bool>();

    public static Dictionary<string, StationData> Stations = new Dictionary<string, StationData>
    {
        { "sylva", new StationData(24634.5f, 620.57f, -941.23f, 24563.24f, 620.57f, -935.94f, 24598.87f, 620.57f, -938.585f, 71.45608232f) },
        { "dillsboro", new StationData(22379.87f, 603.17f, -1410.88f, 22326.76f, 603.17f, -1434.12f, 22353.315f, 603.17f, -1422.5f, 57.9721459f) },
        { "wilmot", new StationData(16511.31f, 569.97f, 2326.23f, 16493.52f, 569.97f, 2329.14f, 16502.415f, 569.97f, 2327.685f, 18.0264306f) },
        { "whittier", new StationData(12267.1f, 561.45f, 5864.33f, 12279.19f, 561.45f, 5893.68f, 12273.145f, 561.45f, 5879.005f, 31.74256763f) },
        { "ela", new StationData(9569.54f, 546.61f, 7404.1f, 9554.41f, 546.61f, 7409.92f, 9561.975f, 546.61f, 7407.01f, 16.21077728f) },
        { "bryson", new StationData(4530.43f, 528.97f, 5428.56f, 4473.52f, 528.97f, 5407.87f, 4501.975f, 528.97f, 5418.215f, 60.55430786f) },
        { "hemingway", new StationData(2820.64f, 578.52f, 3079.64f, 2815.72f, 578.54f, 3055.54f, 2818.18f, 578.53f, 3067.59f, 24.59708926f) },
        { "alarkajct", new StationData(1745.6f, 590.23f, 1503.32f, 1737.93f, 589.78f, 1425.91f, 1741.765f, 590.005f, 1464.615f, 77.79035609f) },
        { "cochran", new StationData(1996.88f, 591.62f, -205.13f, 2007.29f, 591.85f, -218.98f, 2002.085f, 591.735f, -212.055f, 17.32753589f) },
        { "alarka", new StationData(4170.52f, 644.81f, -3113.05f, 4201.17f, 645.24f, -3140.48f, 4185.845f, 645.025f, -3126.765f, 41.13407711f) },
        { "almond", new StationData(-6340.3f, 524.97f, -1291.01f, -6316.44f, 524.97f, -1347.1f, -6328.37f, 524.97f, -1319.055f, 60.95398018f) },
        { "nantahala", new StationData(-15594.29f, 595.2f, -10588.8f, -15642.63f, 595.51f, -10646.29f, -15618.46f, 595.355f, -10617.545f, 75.11292698f) },
        { "topton", new StationData(-18969.52f, 793.22f, -15217.75f, -18977.49f, 792.7f, -15231.27f, -18973.505f, 792.96f, -15224.51f, 15.70292011f) },
        { "rhodo", new StationData(-22993.12f, 653.53f, -18005.08f, -23014.11f, 653.15f, -18030.5f, -23003.615f, 653.34f, -18017.79f, 32.96818011f) },
        { "andrews", new StationData(-29923.78f, 538.97f, -20057.8f, -29990.74f, 538.97f, -20092.33f, -29957.26f, 538.97f, -20075.065f, 75.33898393f) }

    };

    
    public static void InitializeStationSelections()
    {
        var allStops = PassengerStop.FindAll();
        Debug.Log($"all stops {allStops}");
        foreach (var stop in allStops)
        {
            stationSelections[stop.identifier] = false;
        }
    }

    public static bool IsStationSelected(PassengerStop stop)
    {
        return stationSelections.TryGetValue(stop.identifier, out bool isSelected) && isSelected;
    }

    public static bool IsAnyStationSelected(List<PassengerStop> stations)
    {
        return stations.Any(stop => stationSelections.TryGetValue(stop.identifier, out bool isSelected) && isSelected);
    }

    public static void SetStationSelected(PassengerStop stop, bool isSelected)
    {
        stationSelections[stop.identifier] = isSelected;
    }
    public static bool IsAnyStationSelectedForLocomotive(Car locomotive)
    {
        // Check if the locomotive exists in the SelectedStations dictionary
        if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations))
        {
            // Return true if there is at least one selected station
            return selectedStations.Any();
        }

        // Return false if the locomotive is not found or no stations are selected
        return false;
    }
}

namespace RouteManagerUI
{
    
    [HarmonyPatch(typeof(CarInspector), "PopulateAIPanel")]
    public static class CarInspectorPopulateAIPanelPatch 
    {

        static bool Prefix(CarInspector __instance, UIPanelBuilder builder)
        {
            // Access the _car field using reflection
            var carField = typeof(CarInspector).GetField("_car", BindingFlags.NonPublic | BindingFlags.Instance);
            var car = carField.GetValue(__instance) as Car; // Assuming the type of _car is Car
            
            if (car == null)
            {
                // Handle the case where car is not found or is null
                return true; // You might want to let the original method run in this case
            }
            
            builder.FieldLabelWidth = 100f;
            builder.Spacing = 8f;
            AutoEngineerPersistence persistence = new AutoEngineerPersistence(car.KeyValueObject);
            AutoEngineerMode mode2 = Mode();
            builder.AddObserver(persistence.ObserveOrders(delegate
            {
                if (Mode() != mode2)
                {
                    builder.Rebuild();
                }
            }, callInitial: false));
            
            builder.AddField("Mode", builder.ButtonStrip(delegate (UIPanelBuilder builder)
            {
                builder.AddButtonSelectable("Manual", mode2 == AutoEngineerMode.Off, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Off, null, null, null);
                });
                builder.AddButtonSelectable("Road", mode2 == AutoEngineerMode.Road, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Road, null, null, null);
                });
                builder.AddButtonSelectable("Yard", mode2 == AutoEngineerMode.Yard, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Yard, null, null, null);
                });

            }));
            
            if (!persistence.Orders.Enabled)
            {
                builder.AddExpandingVerticalSpacer();
                return false;
            }
            
            if (!StationManager.IsAnyStationSelectedForLocomotive(car))
            {
                builder.AddField("Direction", builder.ButtonStrip(delegate (UIPanelBuilder builder)
                {
                    builder.AddObserver(persistence.ObserveOrders(delegate
                    {
                        builder.Rebuild();
                    }, callInitial: false));
                    builder.AddButtonSelectable("Reverse", !persistence.Orders.Forward, delegate
                    {
                        bool? forward3 = false;
                        if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                        {
                            SetOrdersValue(null, forward3, null, null);
                        }

                    });
                    builder.AddButtonSelectable("Forward", persistence.Orders.Forward, delegate
                    {
                        bool? forward2 = true;
                        if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                        {
                            SetOrdersValue(null, forward2, null, null);
                        }

                    });
                }));
            }
            
            if (mode2 == AutoEngineerMode.Road)
            {
                if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                {
                    int num = MaxSpeedMphForMode(mode2);
                    RectTransform control = builder.AddSlider(() => persistence.Orders.MaxSpeedMph / 5, delegate
                    {
                        int maxSpeedMph4 = persistence.Orders.MaxSpeedMph;
                        return maxSpeedMph4.ToString();
                    }, delegate (float value)
                    {
                        int? maxSpeedMph3 = (int)(value * 5f);
                        if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                        {
                            SetOrdersValue(null, null, maxSpeedMph3, null);
                        }


                    }, 0f, num / 5, wholeNumbers: true);
                    builder.AddField("Max Speed", control);
                }
                
                var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
                var orderedStops = new string[] { "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka", "almond", "nantahala", "topton", "rhodo", "andrews" }
                                   .Select(id => stopsLookup[id])
                                   .Where(ps => !ps.ProgressionDisabled)
                                   .ToList();

                // Create a scrollable view to list the stations
                builder.VScrollView(delegate (UIPanelBuilder builder)
                {
                    foreach (PassengerStop stop in orderedStops)
                    {
                        builder.HStack(delegate (UIPanelBuilder hstack)
                        {
                            // Add a checkbox for each station
                            hstack.AddToggle(() => StationManager.IsStationSelected(stop), isOn =>
                            {
                                StationManager.SetStationSelected(stop, isOn);
                                
                                UpdateManagedTrainsSelectedStations(car); // Update when checkbox state changes
                            });

                            // Add a label next to the checkbox
                            hstack.AddLabel(stop.name);
                        });
                    }
                });

                bool anyStationSelected = StationManager.IsAnyStationSelectedForLocomotive(car);

                 //If any station is selected, add a button to the UI
                if (anyStationSelected)
                {
                    builder.AddButton("Print Car Info", () => ManagedTrains.PrintCarInfo(car));
                }

            }
            

            if (mode2 == AutoEngineerMode.Yard)
            {
                RectTransform control2 = builder.ButtonStrip(delegate (UIPanelBuilder builder)
                {
                    builder.AddButton("Stop", delegate
                    {
                        float? distance8 = 0f;
                        SetOrdersValue(null, null, null, distance8);
                    });
                    builder.AddButton("½", delegate
                    {
                        float? distance7 = 6.1f;
                        SetOrdersValue(null, null, null, distance7);
                    });
                    builder.AddButton("1", delegate
                    {
                        float? distance6 = 12.2f;
                        SetOrdersValue(null, null, null, distance6);
                    });
                    builder.AddButton("2", delegate
                    {
                        float? distance5 = 24.4f;
                        SetOrdersValue(null, null, null, distance5);
                    });
                    builder.AddButton("5", delegate
                    {
                        float? distance4 = 61f;
                        SetOrdersValue(null, null, null, distance4);
                    });
                    builder.AddButton("10", delegate
                    {
                        float? distance3 = 122f;
                        SetOrdersValue(null, null, null, distance3);
                    });
                    builder.AddButton("20", delegate
                    {
                        float? distance2 = 244f;
                        SetOrdersValue(null, null, null, distance2);
                    });
                }, 4);
                builder.AddField("Car Lengths", control2);
            }
            
            builder.AddExpandingVerticalSpacer();
            builder.AddField("Status", () => persistence.PlannerStatus, UIPanelBuilder.Frequency.Periodic);
            static int MaxSpeedMphForMode(AutoEngineerMode mode)
            {
                return mode switch
                {
                    AutoEngineerMode.Off => 0,
                    AutoEngineerMode.Road => 45,
                    AutoEngineerMode.Yard => 15,
                    _ => throw new ArgumentOutOfRangeException("mode", mode, null),
                };
            }
            
            AutoEngineerMode Mode()
            {
                Orders orders2 = persistence.Orders;
                if (!orders2.Enabled)
                {
                    return AutoEngineerMode.Off;
                }
                if (!orders2.Yard)
                {
                    return AutoEngineerMode.Road;
                }
                return AutoEngineerMode.Yard;
            }
            
            void SendAutoEngineerCommand(AutoEngineerMode mode, bool forward, int maxSpeedMph, float? distance)
            {
                StateManager.ApplyLocal(new AutoEngineerCommand(car.id, mode, forward, maxSpeedMph, distance));
            }
            
            void SetOrdersValue(AutoEngineerMode? mode, bool? forward, int? maxSpeedMph, float? distance)
            {
                Orders orders = persistence.Orders;
                if (!orders.Enabled && mode.HasValue && mode.Value != 0 && !maxSpeedMph.HasValue)
                {
                    float num2 = car.velocity * 2.23694f;
                    float num3 = Mathf.Abs(num2);
                    maxSpeedMph = ((num2 > 0.1f) ? (Mathf.CeilToInt(num3 / 5f) * 5) : 0);
                    forward = num2 >= -0.1f;
                }
                if (mode == AutoEngineerMode.Yard)
                {
                    maxSpeedMph = MaxSpeedMphForMode(AutoEngineerMode.Yard);
                }
                AutoEngineerMode mode3 = mode ?? Mode();
                int maxSpeedMph2 = Mathf.Min(maxSpeedMph ?? orders.MaxSpeedMph, MaxSpeedMphForMode(mode3));
                SendAutoEngineerCommand(mode3, forward ?? orders.Forward, maxSpeedMph2, distance);
            }
            

            
            return false; // Prevent the original method from running
        }

        private static void UpdateManagedTrainsSelectedStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();
            var selectedStations = allStops.Where(StationManager.IsStationSelected).ToList();

            // Update the ManagedTrains with the selected stations for this car
            ManagedTrains.UpdateSelectedStations(car, selectedStations);
            if (!LocoTelem.locomotiveCoroutines.ContainsKey(car))
            {
                Debug.Log($" Loco.Telem.locomotiveCoroutines does not contain {car.id} creating bool for {car.id}");
                LocoTelem.locomotiveCoroutines[car] = false;
            }
            

            
        }

    }
}