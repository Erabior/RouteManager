using Game.Messages;
using Game.State;
using Model;
using Model.Definition;
using Model.OpsNew;
using RollingStock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Track;
using UnityEngine;

namespace RouteManager
{
    public class ManagedTrains : MonoBehaviour
    {
        // Rest of your ManagedTrains code...

        //Update the list of stations to stop at.
        public static void UpdateSelectedStations(Car car, List<PassengerStop> selectedStops)
        {
            //Something went wrong
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            //Uppdate consists's station list
            LocoTelem.SelectedStations[car] = selectedStops;
        }


        public static bool IsCurrentDestinationSelected(Car locomotive)
        {
            if (LocoTelem.LocomotiveDestination.TryGetValue(locomotive, out string currentDestination))
            {
                if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations))
                {
                    return selectedStations.Any(station => station.identifier == currentDestination);
                }
            }

            return false;
        }

        public static float? GetLoadInfoForLoco(Car car, String loadIdent)
        {
            int slotIndex;


            if (loadIdent == "diesel-fuel")
            {

                CarLoadInfo? loadInfo = car.GetLoadInfo(loadIdent, out slotIndex);

                if (loadInfo.HasValue)
                {

                    return loadInfo.Value.Quantity;
                }
                else
                {
                    Debug.Log($"{car.DisplayName} No load information found for {loadIdent}.");
                    return null;
                }

            }


            var cars = car.EnumerateCoupled().ToList();

            foreach (var trainCar in cars)
            {
                if (trainCar.Archetype == CarArchetype.Tender)
                {
                    Car Tender = trainCar;
                    CarLoadInfo? loadInfo = Tender.GetLoadInfo(loadIdent, out slotIndex);

                    if (loadInfo.HasValue)
                    {
                        return loadInfo.Value.Quantity;
                    }
                }
            }



            return 0f;
        }
        public static void TestLoadInfo(Car locomotive, string loadIdentifier)
        {
            int slotIndex;


            if (loadIdentifier == "diesel-fuel")
            {

                CarLoadInfo? loadInfo = locomotive.GetLoadInfo(loadIdentifier, out slotIndex);

                if (loadInfo.HasValue)
                {
                    Debug.Log($"Load Identifier: {loadIdentifier}");
                    Debug.Log($"Slot Index: {slotIndex}");
                    Debug.Log($"Value: {loadInfo.Value}");
                    Debug.Log($"Quantity: {loadInfo.Value.Quantity}");
                    // Add more details you wish to log
                    return;
                }
                else
                {
                    Debug.Log($"No load information found for {loadIdentifier}.");
                    return;
                }

            }

            var cars = locomotive.EnumerateCoupled().ToList();
            foreach (var trainCar in cars)
            {
                if (trainCar.Archetype == CarArchetype.Tender)
                {
                    Car Tender = trainCar;
                    CarLoadInfo? loadInfo = Tender.GetLoadInfo(loadIdentifier, out slotIndex);

                    if (loadInfo.HasValue)
                    {
                        Debug.Log($"Load Identifier: {loadIdentifier}");
                        Debug.Log($"Slot Index: {slotIndex}");
                        Debug.Log($"Value: {loadInfo.Value}");
                        Debug.Log($"Quantity: {loadInfo.Value.Quantity}");
                        // Add more details you wish to log
                    }
                    else
                    {
                        Debug.Log($"No load information found for {loadIdentifier}.");
                    }
                }
                else
                {
                    Debug.Log($"No Tender found for {loadIdentifier}.");
                }
            }


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
            if (LocoTelem.SelectedStations.TryGetValue(car, out List<PassengerStop> selectedStations))
            {
                string stationNames = string.Join(", ", selectedStations.Select(s => s.name));
                Vector3? centerPoint = car.GetCenterPosition(graph); // Assuming GetCenterPosition exists

                Debug.Log($"Car ID: {car.id}, Selected Stations: {stationNames}, Center Position: {centerPoint}");
            }
            else
            {
                Debug.Log("No stations selected for this car.");
            }


            if (LocoTelem.LocomotiveDestination.TryGetValue(car, out string dest))
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

            if (LocoTelem.LocomotivePrevDestination.TryGetValue(car, out string prevDest))
            {
                Debug.Log($"Previous destination: {prevDest}");
            }
            else
            {
                Debug.Log("No previous destination for this car.");
            }
            if (LocoTelem.TransitMode.TryGetValue(car, out bool inTransitMode))
            {
                Debug.Log($"Transit Mode: {inTransitMode}");
            }
            else
            {
                Debug.Log("No Transit Mode recorded for this car.");
            }
            if (LocoTelem.LineDirectionEastWest.TryGetValue(car, out bool isEastWest))
            {
                Debug.Log($"Line Direction East/West: {isEastWest}");
            }
            else
            {
                Debug.Log("No Line Direction East/West recorded for this car.");
            }
            if (LocoTelem.DriveForward.TryGetValue(car, out bool driveForward))
            {
                Debug.Log($"Drive Forward: {driveForward}");
            }
            else
            {
                Debug.Log("No Drive Forward recorded for this car.");
            }
            if (LocoTelem.locomotiveCoroutines.TryGetValue(car, out bool coroutineExists))
            {
                Debug.Log($"Locomotive Coroutine Exists: {coroutineExists}");
            }
            else
            {
                Debug.Log("No Locomotive Coroutine recorded for this car.");
            }
            if (LocoTelem.CenterCar.TryGetValue(car, out Car centerCar))
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

            TestLoadInfo(car, "water");

            TestLoadInfo(car, "coal");

            TestLoadInfo(car, "diesel-fuel");

        }

        private static readonly List<string> orderedStations = new List<string>
    {
        "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
        "almond", "nantahala", "topton", "rhodo", "andrews"
    };

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
                Debug.Log($"Station that was retrived from selectedStation: {station} and this should be the Indentifier for the station {station.identifier}");
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
            bool isSelectedInSelectedStations = true;
            bool isSelectedInUISelectedStations = true;


            Debug.Log($"Getting next station for {locomotive.id}");
            string currentStation = null;
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                Debug.Log($"LocomotiveDestination does not contain key: {locomotive} getting the closest station");
                currentStation = GetClosestSelectedStation(locomotive);
                Debug.Log($"Locomotive {locomotive} is closest to {currentStation} ");
                LocoTelem.LocomotiveDestination[locomotive] = currentStation;
                return;
            }
            else
            {
                currentStation = LocoTelem.LocomotiveDestination[locomotive];
            }
            if (!LocoTelem.LineDirectionEastWest.ContainsKey(locomotive))
            {
                LocoTelem.LineDirectionEastWest[locomotive] = true;
            }

            LocoTelem.LocomotivePrevDestination[locomotive] = currentStation;
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
                        Debug.Log($"Reached {currentStation} and is the end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex - 1]}");
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

                }
                else
                {
                    Debug.Log($"Going West to East");

                    if (currentIndex == 0)
                    {
                        Debug.Log($"Reached {currentStation} and is the end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex + 1]}");

                        LocoTelem.LineDirectionEastWest[locomotive] = true;
                        LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                        LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex + 1];
                    }
                    else
                    {
                        Debug.Log($"Reached {currentStation} next station is {orderedSelectedStations[currentIndex - 1]}");
                        LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex - 1];
                    }

                }
                isSelectedInSelectedStations = selectedStops.Any(stop => stop.identifier == LocoTelem.LocomotiveDestination[locomotive]);
                isSelectedInUISelectedStations = LocoTelem.UIStationSelections[locomotive].TryGetValue(LocoTelem.LocomotiveDestination[locomotive], out bool uiSelected) && uiSelected;

                if (!isSelectedInSelectedStations || !isSelectedInUISelectedStations)
                {
                    // Update both dictionaries to indicate the station is not selected
                    if (isSelectedInSelectedStations)
                    {
                        selectedStops.RemoveAll(stop => stop.identifier == LocoTelem.LocomotiveDestination[locomotive]);
                    }
                    if (isSelectedInUISelectedStations)
                    {
                        LocoTelem.UIStationSelections[locomotive][LocoTelem.LocomotiveDestination[locomotive]] = false;
                    }

                    // Recursively call the method
                    GetNextDestination(locomotive);
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

        public static Car GetCenterCoach(Car locomotive)
        {
            var graph = Graph.Shared;

            // List of all coupled cars
            var cars = locomotive.EnumerateCoupled().ToList();
            var coaches = new List<Car>();

            foreach (var car in cars)
            {

                if (car.Archetype == CarArchetype.Coach)
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
                return -6969; // Return a default value or handle this case as needed
            }

            // Check if the locomotive key exists in the LocomotiveDestination dictionary
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                Debug.LogError($"LocomotiveDestination does not contain key: {locomotive}");
                LocoTelem.LocomotiveDestination[locomotive] = GetClosestSelectedStation(locomotive);

                if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
                {
                    return -6969f; // Or handle this scenario appropriately
                }

            }
            Vector3 locomotivePosition = new Vector3();
            string destination = LocoTelem.LocomotiveDestination[locomotive];

            if (destination == null)
            {
                Debug.LogError("Destination is null for locomotive.");
                return -6969f; // Handle null destination
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
                return -6969f; // Handle missing station
            }

            Vector3 destCenter = StationManager.Stations[destination].Center;
            Vector3 destCentern = StationManager.Stations["alarkajctn"].Center;

            if (destination == "alarkajct")
            {
                Debug.Log($"Going to AlarkaJct checking which platform is closest south dist: {Vector3.Distance(locomotivePosition, destCenter)} | north dist: {Vector3.Distance(locomotivePosition, destCentern)}");
                if (Vector3.Distance(locomotivePosition, destCenter) > Vector3.Distance(locomotivePosition, destCentern))
                {
                    Debug.Log($"North is closest");
                    return Vector3.Distance(locomotivePosition, destCentern);
                }
                else
                {
                    Debug.Log($"South is closest");
                }
            }
            return Vector3.Distance(locomotivePosition, destCenter);
        }

        public static void CopyStationsFromLocoToCoaches(Car locomotive)
        {
            Debug.Log($"Copying Stations from loco: {locomotive.DisplayName} to coupled coaches");
            string currentStation = LocoTelem.LocomotiveDestination[locomotive];
            int currentStationIndex = orderedStations.IndexOf(currentStation);
            bool isEastWest = LocoTelem.LineDirectionEastWest[locomotive]; // true if traveling West

            // Determine the range of stations to include based on travel direction
            IEnumerable<string> relevantStations = isEastWest ?
                orderedStations.Skip(currentStationIndex) :
                orderedStations.Take(currentStationIndex + 1).Reverse();

            // Filter to include only selected stations
            HashSet<string> selectedStationIdentifiers = LocoTelem.SelectedStations[locomotive]
                .Select(stop => stop.identifier)
                .ToHashSet();

            HashSet<string> filteredStations = relevantStations
                .Where(station => selectedStationIdentifiers.Contains(station))
                .ToHashSet();

            // Apply the filtered stations to each coach
            foreach (Car coach in locomotive.EnumerateCoupled().Where(car => car.Archetype == CarArchetype.Coach))
            {
                StateManager.ApplyLocal(new SetPassengerDestinations(coach.id, filteredStations.ToList()));
            }
        }

        // Method to display a message about the update (can be customized based on your UI implementation)
        private void DisplayUpdatedPassengerCarsMessage(int count)
        {
            // Implement the logic to display a message to the user
            Debug.Log($"Selected stations copied to {count} passenger cars.");
        }

        public static bool IsRouteModeEnabled(Car locomotive)
        {
            // Check if the locomotive exists in the TransitMode dictionary
            if (LocoTelem.RouteMode.ContainsKey(locomotive))
            {
                return LocoTelem.RouteMode[locomotive];
            }
            else
            {
                // Handle the case where the key does not exist, for example, by logging an error or initializing the key
                Debug.LogError($"TransitMode dictionary does not contain key: {locomotive}");
                // Optionally initialize the key with a default value
                LocoTelem.RouteMode[locomotive] = false; // Default value
                return false;
            }
        }


        public static event Action<Car> OnRouteModeChanged;
        public static void SetRouteModeEnabled(bool IsOn, Car locomotive)
        {


            if (StationManager.IsAnyStationSelectedForLocomotive(locomotive) && IsOn)
            {
                if (!LocoTelem.RouteMode.ContainsKey(locomotive))
                {
                    Debug.Log($" LocoTelem.RouteMode does not contain {locomotive.id} creating bool for {locomotive.id}");
                    LocoTelem.RouteMode[locomotive] = false;
                }
                Debug.Log($"changing LocoTelem.Route Mode from {!IsOn} to {IsOn}");
                LocoTelem.RouteMode[locomotive] = IsOn;
                OnRouteModeChanged?.Invoke(locomotive);

                if (!LocoTelem.locomotiveCoroutines.ContainsKey(locomotive))
                {
                    Debug.Log($" LocoTelem.locomotiveCoroutines does not contain {locomotive.id} creating bool for {locomotive.id}");
                    LocoTelem.locomotiveCoroutines[locomotive] = false;
                }
            }
            else if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive) && IsOn)
            {
                Console.Log($"There are no stations selected for {locomotive.DisplayName}. Please select at least 1 station before enabling Route Mode");
            }

            else if (StationManager.IsAnyStationSelectedForLocomotive(locomotive) && !IsOn)
            {
                LocoTelem.RouteMode[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);
            }
            else if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive) && !IsOn)
            {
                LocoTelem.RouteMode[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);
            }
            else
            {
                Debug.Log($"Route Mode ({LocoTelem.RouteMode[locomotive]}) and IsAnyStationSelectedForLocomotive ({StationManager.IsAnyStationSelectedForLocomotive(locomotive)}) are no combination of false or true ");
            }
            return;
        }
    }

}
