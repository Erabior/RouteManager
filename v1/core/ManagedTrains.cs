using Game.Messages;
using Game.State;
using Model;
using Model.Definition;
using Model.OpsNew;
using RollingStock;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using Track;
using UnityEngine;
using static Game.Messages.PropertyChange;
using Logger = RouteManager.v1.helpers.Logger;

namespace RouteManager
{
    public class ManagedTrains : MonoBehaviour
    {
        // Rest of your ManagedTrains code...

        public static IEnumerator RMblow(Car locomotive, float intensity, float duration = 1f, float quillFinal = -1f)
        {
            duration = Mathf.Max(duration, 0.1f);

            float finalIntensity = quillFinal < 0 ? intensity : quillFinal;
            if (intensity == 0 && quillFinal == -1f)
            {
                StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, intensity));
                yield break;
            }

            float timeDelta = 0.05f; // Time interval for updates
            int intervals = (int)(duration / timeDelta); // Total number of intervals
            float intensityChangePerInterval = (finalIntensity - intensity) / intervals; // Change in intensity per interval

            if (quillFinal == -1f) //if final intensity is not specified then set a flat intensity with duration
            {
                StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, intensity));
                yield return new WaitForSeconds(duration);
            }
            else
            {
                for (int i = 0; i <= intervals; i++)
                {
                    float currentIntensity = intensity + intensityChangePerInterval * i;
                    StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, currentIntensity));
                    yield return new WaitForSeconds(timeDelta);
                }
            }
            
        }
        public static void RMbell(Car locomotive, bool IsBell)
        {
            StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Bell, IsBell));
        }

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
                    //Debugging
                    Logger.LogToDebug($"{car.DisplayName} No Diesel load information found for {loadIdent}.");
                    //With the updated logic based on archetype checking i propose that this return is no longer needed. 
                    //return null;
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
                    else
                    {
                        //Debugging
                        Logger.LogToDebug($"{car.DisplayName} No Steam load information found for {loadIdent}.");
                    }
                }
            }

            //Something went wrong so assume 0 fuel
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
                    Logger.LogToDebug($"Load Identifier: {loadIdentifier}");
                    Logger.LogToDebug($"Slot Index: {slotIndex}");
                    Logger.LogToDebug($"Value: {loadInfo.Value}");
                    Logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}");
                    // Add more details you wish to log
                    return;
                }
                else
                {
                    Logger.LogToDebug($"No load information found for {loadIdentifier}.");
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
                        Logger.LogToDebug($"Load Identifier: {loadIdentifier}");
                        Logger.LogToDebug($"Slot Index: {slotIndex}");
                        Logger.LogToDebug($"Value: {loadInfo.Value}");
                        Logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}");
                        // Add more details you wish to log
                    }
                    else
                    {
                        Logger.LogToDebug($"No load information found for {loadIdentifier}.");
                    }
                }
                else
                {
                    Logger.LogToDebug($"No Tender found for {loadIdentifier}.");
                }
            }


        }
        public static void PrintCarInfo(Car car)
        {
            var graph = Graph.Shared;
            if (car == null)
            {
                Logger.LogToDebug("Car is null");
                return;
            }

            // Retrieve saved stations for this car from ManagedTrains
            if (LocoTelem.SelectedStations.TryGetValue(car, out List<PassengerStop> selectedStations))
            {
                string stationNames = string.Join(", ", selectedStations.Select(s => s.name));
                Vector3? centerPoint = car.GetCenterPosition(graph); // Assuming GetCenterPosition exists

                Logger.LogToDebug($"Car ID: {car.id}, Selected Stations: {stationNames}, Center Position: {centerPoint}");
            }
            else
            {
                Logger.LogToDebug("No stations selected for this car.");
            }


            if (LocoTelem.LocomotiveDestination.TryGetValue(car, out string dest))
            {

                Logger.LogToDebug($"destination: {dest}");
            }
            else
            {
                Logger.LogToDebug("No destination for this car.");
            }

            if (graph == null)
            {
                Logger.LogToError("Graph object is null");
                return; // or handle this case as needed
            }

            if (car == null)
            {
                Logger.LogToError("Car object is null");
                return; // or handle this case as needed
            }

            var locationF = car.LocationF;
            var locationR = car.LocationR;
            var direction = car.GetCenterRotation(graph);
            Logger.LogToDebug($"LocationF {locationF} LocationR {locationR} Rotation: {direction}");

            if (LocoTelem.LocomotivePrevDestination.TryGetValue(car, out string prevDest))
            {
                Logger.LogToDebug($"Previous destination: {prevDest}");
            }
            else
            {
                Logger.LogToDebug("No previous destination for this car.");
            }
            if (LocoTelem.TransitMode.TryGetValue(car, out bool inTransitMode))
            {
                Logger.LogToDebug($"Transit Mode: {inTransitMode}");
            }
            else
            {
                Logger.LogToDebug("No Transit Mode recorded for this car.");
            }
            if (LocoTelem.LineDirectionEastWest.TryGetValue(car, out bool isEastWest))
            {
                Logger.LogToDebug($"Line Direction East/West: {isEastWest}");
            }
            else
            {
                Logger.LogToDebug("No Line Direction East/West recorded for this car.");
            }
            if (LocoTelem.DriveForward.TryGetValue(car, out bool driveForward))
            {
                Logger.LogToDebug($"Drive Forward: {driveForward}");
            }
            else
            {
                Logger.LogToDebug("No Drive Forward recorded for this car.");
            }
            if (LocoTelem.locomotiveCoroutines.TryGetValue(car, out bool coroutineExists))
            {
                Logger.LogToDebug($"Locomotive Coroutine Exists: {coroutineExists}");
            }
            else
            {
                Logger.LogToDebug("No Locomotive Coroutine recorded for this car.");
            }
            if (LocoTelem.CenterCar.TryGetValue(car, out Car centerCar))
            {
                Logger.LogToDebug($"Center Car: {centerCar}");
            }
            else
            {
                Logger.LogToDebug("No Center Car recorded for this car.");
            }
            try
            {
                LocoTelem.CenterCar[car] = GetCenterCoach(car);
                Logger.LogToDebug($"center car for {car}: {LocoTelem.CenterCar[car]}");
            }
            catch (Exception ex)
            {
                Logger.LogToDebug($"could not get center car: {ex}");
            }
            var Locovelocity = car.velocity;
            Logger.LogToDebug($"Current Speed: {Locovelocity}");

            var cars = car.EnumerateCoupled().ToList();

            foreach (var trainCar in cars)
            {
                Logger.LogToDebug($"{trainCar.Archetype}");
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
            Logger.LogToDebug($"Position of the loco {centerPoint} also centerpoint.value {centerPoint.Value}");
            // Check if centerPoint is null
            if (centerPoint == null)
            {
                Logger.LogToError("Could not obtain locomotive's center position.");
                return null;
            }

            if (!LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations) || selectedStations.Count == 0)
            {
                Logger.LogToDebug("No stations selected for this locomotive.");
                return null;
            }

            // Initialize variables to track the closest station
            string closestStationName = null;
            float closestDistance = float.MaxValue;

            // Iterate over each selected station using a for loop
            for (int i = 0; i < selectedStations.Count; i++)
            {
                PassengerStop station = selectedStations[i];
                Logger.LogToDebug($"Station that was retrived from selectedStation: {station} and this should be the Indentifier for the station {station.identifier}");
                if (StationManager.Stations.TryGetValue(station.identifier, out StationData stationData))
                {
                    // Calculate the distance between the locomotive and the station's center point
                    // Unwrap the nullable Vector3 using the Value property
                    Logger.LogToDebug($"Station center: {stationData.Center}");
                    Logger.LogToDebug($"loco center: {centerPoint.Value}");

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
                    Logger.LogToError($"Station data not found for identifier: {station.identifier}");
                }
            }
            Logger.LogToDebug($"returning {closestStationName}");
            return closestStationName;
        }

        public static void GetNextDestination(Car locomotive)
        {
            bool isSelectedInSelectedStations = true;
            bool isSelectedInUISelectedStations = true;


            Logger.LogToDebug($"Getting next station for {locomotive.id}");
            string currentStation = null;
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                Logger.LogToDebug($"LocomotiveDestination does not contain key: {locomotive} getting the closest station");
                currentStation = GetClosestSelectedStation(locomotive);
                Logger.LogToDebug($"Locomotive {locomotive} is closest to {currentStation} ");
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
            Logger.LogToDebug($"current station is {currentStation}");
            List<string> selectedStationIdentifiers = LocoTelem.SelectedStations
                .SelectMany(pair => pair.Value)
                .Select(passengerStop => passengerStop.identifier)
                .Distinct()
                .ToList();

            var orderedSelectedStations = orderedStations.Where(item => selectedStationIdentifiers.Contains(item)).ToList();

            Logger.LogToDebug($"try to get value from SelectedStations and checking number of selected stops");
            if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStops) && selectedStops.Count > 1)
            {
                Logger.LogToDebug($"got value and there were {selectedStops.Count} stops");

                int currentIndex = orderedSelectedStations.IndexOf(currentStation);

                Logger.LogToDebug($"The index of the current station in the list of selected stations is {currentIndex}");
                if (currentIndex == -1)
                {
                    LocoTelem.LocomotiveDestination[locomotive] = selectedStops.First().identifier;
                    Logger.LogToDebug($" setting the next station to the first station because there was no current station");
                    return;  // If no current station, return the first selected station
                }



                if (EastWest)
                {
                    Logger.LogToDebug($"Going East to West");

                    if (currentIndex == orderedSelectedStations.Count - 1)
                    {
                        Logger.LogToDebug($"Reached {currentStation} and is the West most end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex - 1]}");
                        CopyStationsFromLocoToCoaches(locomotive);

                        LocoTelem.LineDirectionEastWest[locomotive] = false;
                        LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                        LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex - 1];
                    }
                    else
                    {
                        Logger.LogToDebug($"Reached {currentStation} next station is {orderedSelectedStations[currentIndex + 1]}");
                        LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex + 1];

                    }

                }
                else
                {
                    Logger.LogToDebug($"Going West to East");

                    if (currentIndex == 0)
                    {
                        Logger.LogToDebug($"Reached {currentStation} and is the East most end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex + 1]}");
                        CopyStationsFromLocoToCoaches(locomotive);
                        LocoTelem.LineDirectionEastWest[locomotive] = true;
                        LocoTelem.DriveForward[locomotive] = !LocoTelem.DriveForward[locomotive];
                        LocoTelem.LocomotiveDestination[locomotive] = orderedSelectedStations[currentIndex + 1];
                    }
                    else
                    {
                        Logger.LogToDebug($"Reached {currentStation} next station is {orderedSelectedStations[currentIndex - 1]}");
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
            Logger.LogToDebug("There was no next destination");
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
                    Logger.LogToDebug($"failed to get the number of passengers from GetPassengerCount(coach): {ex}");
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

                Logger.LogToError("Locomotive is null in GetDistanceToDest.");
                return -6969; // Return a default value or handle this case as needed
            }

            // Check if the locomotive key exists in the LocomotiveDestination dictionary
            if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
            {
                Logger.LogToError($"LocomotiveDestination does not contain key: {locomotive}");
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
                Logger.LogToError("Destination is null for locomotive.");
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
                Logger.LogToError($"Station not found for destination: {destination}");
                return -6969f; // Handle missing station
            }

            Vector3 destCenter = StationManager.Stations[destination].Center;
            Vector3 destCentern = StationManager.Stations["alarkajctn"].Center;

            if (destination == "alarkajct")
            {
                Logger.LogToDebug($"Going to AlarkaJct checking which platform is closest south dist: {Vector3.Distance(locomotivePosition, destCenter)} | north dist: {Vector3.Distance(locomotivePosition, destCentern)}");
                if (Vector3.Distance(locomotivePosition, destCenter) > Vector3.Distance(locomotivePosition, destCentern))
                {
                    Logger.LogToDebug($"North is closest");
                    return Vector3.Distance(locomotivePosition, destCentern);
                }
                else
                {
                    Logger.LogToDebug($"South is closest");
                }
            }
            return Vector3.Distance(locomotivePosition, destCenter);
        }

        public static void CopyStationsFromLocoToCoaches(Car locomotive)
        {
            Logger.LogToDebug($"Copying Stations from loco: {locomotive.DisplayName} to coupled coaches");
            string currentStation = LocoTelem.LocomotiveDestination[locomotive];
            int currentStationIndex = orderedStations.IndexOf(currentStation);
            //bool isEastWest = LocoTelem.LineDirectionEastWest[locomotive]; // true if traveling West

            //// Determine the range of stations to include based on travel direction
            //IEnumerable<string> relevantStations = isEastWest ?
            //    orderedStations.Skip(currentStationIndex) :
            //    orderedStations.Take(currentStationIndex + 1).Reverse();

            // Filter to include only selected stations
            HashSet<string> selectedStationIdentifiers = LocoTelem.SelectedStations[locomotive]
                .Select(stop => stop.identifier)
                .ToHashSet();

            //HashSet<string> filteredStations = relevantStations
            //    .Where(station => selectedStationIdentifiers.Contains(station))
            //    .ToHashSet();

            // Apply the filtered stations to each coach
            foreach (Car coach in locomotive.EnumerateCoupled().Where(car => car.Archetype == CarArchetype.Coach))
            {
                StateManager.ApplyLocal(new SetPassengerDestinations(coach.id, selectedStationIdentifiers.ToList()));
            }
        }

        // Method to display a message about the update (can be customized based on your UI implementation)
        private void DisplayUpdatedPassengerCarsMessage(int count)
        {
            // Implement the logic to display a message to the user
            Logger.LogToDebug($"Selected stations copied to {count} passenger cars.");
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
                Logger.LogToError($"TransitMode dictionary does not contain key: {locomotive}");
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
                    Logger.LogToDebug($" LocoTelem.RouteMode does not contain {locomotive.id} creating bool for {locomotive.id}");
                    LocoTelem.RouteMode[locomotive] = false;
                }
                Logger.LogToDebug($"changing LocoTelem.Route Mode from {!IsOn} to {IsOn}");
                LocoTelem.RouteMode[locomotive] = IsOn;
                OnRouteModeChanged?.Invoke(locomotive);

                if (!LocoTelem.locomotiveCoroutines.ContainsKey(locomotive))
                {
                    Logger.LogToDebug($" LocoTelem.locomotiveCoroutines does not contain {locomotive.id} creating bool for {locomotive.id}");
                    LocoTelem.locomotiveCoroutines[locomotive] = false;
                }
            }
            else if (!StationManager.IsAnyStationSelectedForLocomotive(locomotive) && IsOn)
            {
                Logger.LogToConsole($"There are no stations selected for {locomotive.DisplayName}. Please select at least 1 station before enabling Route Mode");
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
                Logger.LogToDebug($"Route Mode ({LocoTelem.RouteMode[locomotive]}) and IsAnyStationSelectedForLocomotive ({StationManager.IsAnyStationSelectedForLocomotive(locomotive)}) are no combination of false or true ");
            }
            return;
        }
    }

}
