using Game.Messages;
using Game.State;
using Model;
using Model.Definition;
using Model.OpsNew;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game.Messages.PropertyChange;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;
using Track;
using RouteManager.v2.dataStructures;
using UnityEngine.InputSystem.EnhancedTouch;

namespace RouteManager.v2.core
{
    internal class TrainManager
    {


        //Get Fuel Load information for the Requested locomotive 
        public static float GetLoadInfoForLoco(Car car, String loadIdent)
        {
            int slotIndex;
            //Check for diesel first as its cheaper computationally
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
                }
            }
            //Only enumerate and iterate through the cars in the train if/when we need to. 
            else
            {
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
            }

            //Something went wrong so assume 0 fuel
            return 0f;
        }

        public static IEnumerator standardWhistle(Car locomotive)
        {
            yield return TrainManager.RMblow(locomotive, 0.25f, 1.5f);
            yield return TrainManager.RMblow(locomotive, 1f, 2.5f);
            yield return TrainManager.RMblow(locomotive, 1f, 1.75f, 0.25f);
            yield return TrainManager.RMblow(locomotive, 1f, 0.25f);
            yield return TrainManager.RMblow(locomotive, 0f);
        }

        public static IEnumerator RMblow(Car locomotive, float intensity, float duration = 1f, float quillFinal = -1f)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: RMblow", Logger.logLevel.Trace);

            Logger.LogToDebug(String.Format("Locomotive {0} Whistling! Intensity: {1} Duration: {2} quillFinal: {3}", locomotive.DisplayName, intensity, duration, quillFinal), Logger.logLevel.Verbose);

            duration = Mathf.Max(duration, 0.1f);

            float finalIntensity = quillFinal < 0 ? intensity : quillFinal;

            if (intensity == 0 && quillFinal == -1f)
            {
                Logger.LogToDebug(String.Format("Locomotive {0} Now Whistling at Intensity: {1} with finalquill -1f for", locomotive.DisplayName, intensity), Logger.logLevel.Verbose);
                StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, intensity));
                yield break;
            }

            // Time interval for updates
            float timeDelta = 0.05f;

            // Total number of intervals
            int intervals = (int)(duration / timeDelta);

            // Change in intensity per interval
            float intensityChangePerInterval = (finalIntensity - intensity) / intervals;

            Logger.LogToDebug(String.Format("Locomotive {0} Whistl Calculated Values timeDelta: {1} intervals: {2} intensityChangePerInterval: {3}", locomotive.DisplayName, timeDelta, intervals, intensityChangePerInterval), Logger.logLevel.Verbose);

            //if final intensity is not specified then set a flat intensity with duration
            if (quillFinal == -1f) 
            {
                Logger.LogToDebug(String.Format("Locomotive {0} Now Whistling at Intensity: {1} for {2} seconds", locomotive.DisplayName, intensity, duration), Logger.logLevel.Verbose);
                StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, intensity));
                yield return new WaitForSeconds(duration);
            }
            else
            {
                for (int i = 0; i <= intervals; i++)
                {
                    float currentIntensity = intensity + intensityChangePerInterval * i;
                    Logger.LogToDebug(String.Format("Locomotive {0} Now Whistling at Intensity: {1} for {2} intervals over {3} seconds", locomotive.DisplayName, currentIntensity, intervals , timeDelta), Logger.logLevel.Verbose);
                    StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, currentIntensity));
                    yield return new WaitForSeconds(timeDelta);
                }
            }

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: RMblow", Logger.logLevel.Trace);
        }
        public static void RMbell(Car locomotive, bool IsBell)
        {
            StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Bell, IsBell));
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

        public static void CopyStationsFromLocoToCoaches(Car locomotive)
        {
            Logger.LogToDebug(String.Format("Loco: {0} update coach station selection", locomotive.DisplayName),Logger.logLevel.Verbose);

            string currentStation = LocoTelem.currentDestination[locomotive].identifier;
            Logger.LogToDebug(String.Format("currentStation", currentStation), Logger.logLevel.Verbose);

            int currentStationIndex = DestinationManager.orderedStations.IndexOf(currentStation);

            Logger.LogToDebug(String.Format("currentStationIndex", currentStationIndex), Logger.logLevel.Verbose);

            bool isEastWest = LocoTelem.locoTravelingWestward[locomotive]; // true if traveling West

            Logger.LogToDebug(String.Format("isEastWest", isEastWest), Logger.logLevel.Verbose);

            Logger.LogToDebug(String.Format("Loco: {0} calculating stations to apply", locomotive.DisplayName), Logger.logLevel.Verbose);

            // Determine the range of stations to include based on travel direction
            IEnumerable<string> relevantStations = isEastWest ?
                DestinationManager.orderedStations.Skip(currentStationIndex) :
                DestinationManager.orderedStations.Take(currentStationIndex + 1).Reverse();

            // Filter to include only selected stations
            HashSet<string> selectedStationIdentifiers = LocoTelem.SelectedStations[locomotive]
                .Select(stop => stop.identifier)
                .ToHashSet();

            HashSet<string> filteredStations = relevantStations
                .Where(station => selectedStationIdentifiers.Contains(station))
                .ToHashSet();

            Logger.LogToDebug(String.Format("Loco: {0} updating car station selection", locomotive.DisplayName), Logger.logLevel.Debug);
            // Apply the filtered stations to each coach
            foreach (Car coach in locomotive.EnumerateCoupled().Where(car => car.Archetype == CarArchetype.Coach))
            {
                Logger.LogToDebug(String.Format("Applying station selection to car", coach.DisplayName), Logger.logLevel.Verbose);
                StateManager.ApplyLocal(new SetPassengerDestinations(coach.id, filteredStations.ToList()));
            }

            LocoTelem.needToUpdatePassengerCoaches[locomotive] = false;
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


            if (DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && IsOn)
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
                    Logger.LogToDebug($" LocoTelem.locomotiveCoroutines does not contain {locomotive.id} creating bool for {locomotive.DisplayName}");
                    LocoTelem.locomotiveCoroutines[locomotive] = false;
                }
            }
            else if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && IsOn)
            {
                Logger.LogToConsole($"There are no stations selected for {locomotive.DisplayName}. Please select at least 1 station before enabling Route Mode");
            }
            else if (DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && !IsOn)
            {
                LocoTelem.RouteMode[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);
            }
            else if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && !IsOn)
            {
                LocoTelem.RouteMode[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);
            }
            else
            {
                Logger.LogToDebug($"Route Mode ({LocoTelem.RouteMode[locomotive]}) and IsAnyStationSelectedForLocomotive ({DestinationManager.IsAnyStationSelectedForLocomotive(locomotive)}) are no combination of false or true ");
            }
            return;
        }


        public static float GetTrainVelocity(Car locomotive)
        {
            return Math.Abs(locomotive.velocity * 2.23694f);
        }


        //Separate out the core fuel check logic
        public static void locoLowFuelCheck(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: locoLowFuelCheck", Logger.logLevel.Trace);

            Dictionary <string, float> fuelCheckResults = new Dictionary<string, float>();

            //If steam locomotive Check the water and coal levels
            if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
            {

                //If coal is below minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "coal") / 2000, SettingsData.minCoalQuantity))
                {
                    fuelCheckResults.Add("coal", GetLoadInfoForLoco(locomotive, "coal") / 2000);
                }

                //If water is below minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "water"), SettingsData.minWaterQuantity))
                {
                    fuelCheckResults.Add("water", GetLoadInfoForLoco(locomotive, "water"));
                }

            }
            //If Diesel locomotive diesel levels
            else if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel)
            {
                //If diesel level is below defined minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "diesel-fuel"), SettingsData.minDieselQuantity))
                {
                    fuelCheckResults.Add("diesel-fuel", GetLoadInfoForLoco(locomotive, "diesel-fuel"));
                }
            }

            LocoTelem.lowFuelQuantities[locomotive] = fuelCheckResults;

            //Trace Method
            Logger.LogToDebug("EXITING FUNCTION: locoLowFuelCheck", Logger.logLevel.Trace);
        }

        //Methodize repeated code of fuel check. 
        //Method could be re-integrated into calling method now that additional checks have been rendered null from further code improvements.
        private static bool compareAgainstMinVal(float inputValue, float minimumValue)
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