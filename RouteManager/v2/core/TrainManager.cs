using Game.Messages;
using Game.State;
using Model;
using Model.Definition;
using Model.OpsNew;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Game.Messages.PropertyChange;
using UnityEngine;
using Track;
using RouteManager.v2.dataStructures;
using Model.Definition.Data;
using RouteManager.v2.Logging;
using RollingStock;

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
                    RouteManager.logger.LogToDebug($"{car.DisplayName} No Diesel load information found for {loadIdent}.");
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
                            RouteManager.logger.LogToDebug($"{car.DisplayName} No Steam load information found for {loadIdent}.");
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
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: RMblow", LogLevel.Trace);

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} Whistling! Intensity: {1} Duration: {2} quillFinal: {3}", locomotive.DisplayName, intensity, duration, quillFinal), LogLevel.Verbose);

            duration = Mathf.Max(duration, 0.1f);

            float finalIntensity = quillFinal < 0 ? intensity : quillFinal;

            if (intensity == 0 && quillFinal == -1f)
            {
                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} Now Whistling at Intensity: {1} with finalquill -1f for", locomotive.DisplayName, intensity), LogLevel.Verbose);
                StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, intensity));
                yield break;
            }

            // Time interval for updates
            float timeDelta = 0.05f;

            // Total number of intervals
            int intervals = (int)(duration / timeDelta);

            // Change in intensity per interval
            float intensityChangePerInterval = (finalIntensity - intensity) / intervals;

            RouteManager.logger.LogToDebug(String.Format("Locomotive {0} Whistl Calculated Values timeDelta: {1} intervals: {2} intensityChangePerInterval: {3}", locomotive.DisplayName, timeDelta, intervals, intensityChangePerInterval), LogLevel.Verbose);

            //if final intensity is not specified then set a flat intensity with duration
            if (quillFinal == -1f) 
            {
                RouteManager.logger.LogToDebug(String.Format("Locomotive {0} Now Whistling at Intensity: {1} for {2} seconds", locomotive.DisplayName, intensity, duration), LogLevel.Verbose);
                StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, intensity));
                yield return new WaitForSeconds(duration);
            }
            else
            {
                for (int i = 0; i <= intervals; i++)
                {
                    float currentIntensity = intensity + intensityChangePerInterval * i;
                    RouteManager.logger.LogToDebug(String.Format("Locomotive {0} Now Whistling at Intensity: {1} for {2} intervals over {3} seconds", locomotive.DisplayName, currentIntensity, intervals , timeDelta), LogLevel.Verbose);
                    StateManager.ApplyLocal(new PropertyChange(locomotive.id, Control.Horn, currentIntensity));
                    yield return new WaitForSeconds(timeDelta);
                }
            }

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: RMblow", LogLevel.Trace);
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

        public static int GetPassengerCount(Car coach)
        {
            return coach.GetPassengerMarker()?.TotalPassengers ?? 0;
        }

        public static bool isTrainFull(Car locomotive)
        {
            int currentPaxCount = 0;

            foreach (Car currentCar in locomotive.EnumerateCoupled().ToList())
            {
                if (currentCar.Archetype == CarArchetype.Coach)
                {
                    currentPaxCount = GetPassengerCount(currentCar);

                    foreach(LoadSlot load in currentCar.Definition.LoadSlots) 
                    {
                        RouteManager.logger.LogToDebug(load.RequiredLoadIdentifier + " maximum capacity is " + load.MaximumCapacity, LogLevel.Verbose);
                    }

                    //If a single car is empty, then we have room and dont need to keep looking.
                    if(currentPaxCount != currentCar.Definition.LoadSlots.First().MaximumCapacity)
                    {
                        return false;
                    }
                }
            }

            //Went through the entire train. Seems like no room.
            return true;
        }

        public static void CopyStationsFromLocoToCoaches(Car locomotive)
        {
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} update coach station selection", locomotive.DisplayName), LogLevel.Verbose);

            string currentStation = LocoTelem.currentDestination[locomotive].identifier;
            int currentStationIndex = DestinationManager.orderedStations.IndexOf(currentStation);
            bool isTravelingEastWard = LocoTelem.locoTravelingEastWard[locomotive]; // true if traveling East

            // Determine the range of stations to include based on travel direction
            IEnumerable<string> relevantStations;

            if (StationManager.currentlyAtLastStation(locomotive))
            {
                relevantStations = DestinationManager.orderedStations.Except(new List<String>() { LocoTelem.closestStation[locomotive].Item1.identifier });
            }
            else if (isTravelingEastWard)
            {
                relevantStations = DestinationManager.orderedStations.Take(currentStationIndex + 1).Reverse();
            }
            else
            {
                relevantStations = DestinationManager.orderedStations.Skip(currentStationIndex);
            }

            foreach (string identifier in relevantStations)
            {
                RouteManager.logger.LogToDebug(String.Format("relevantStations contains {0}", identifier), LogLevel.Verbose);
            }

            //Filter to include only selected stations
            HashSet<string> selectedStationIdentifiers = LocoTelem.stopStations[locomotive]
                .Select(stop => stop.identifier)
                .ToHashSet();

            foreach (string identifier in selectedStationIdentifiers)
            {
                RouteManager.logger.LogToDebug(String.Format("selectedStationIdentifiers contains {0}", identifier), LogLevel.Verbose);
            }

            HashSet<string> filteredStations = relevantStations
                .Where(station => selectedStationIdentifiers.Contains(station))
                .ToHashSet();

            foreach (string identifier in filteredStations)
            {
                RouteManager.logger.LogToDebug(String.Format("filteredStations contains {0}", identifier), LogLevel.Verbose);
            }

            RouteManager.logger.LogToDebug(String.Format("Loco: {0} updating car station selection", locomotive.DisplayName), LogLevel.Debug);

            // Apply the filtered stations to each coach
            foreach (Car coach in locomotive.EnumerateCoupled().Where(car => car.Archetype == CarArchetype.Coach))
            {

                foreach (string identifier in filteredStations)
                {
                    RouteManager.logger.LogToDebug(String.Format("    Applying {0} to car {1}", identifier, coach.DisplayName), LogLevel.Verbose);
                }

                StateManager.ApplyLocal(new SetPassengerDestinations(coach.id, filteredStations.ToList()));
            }

            LocoTelem.needToUpdatePassengerCoaches[locomotive] = false;
        }

        public static void CopyStationsFromLocoToCoaches_dev(Car locomotive)
        {
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} update coach station selection", locomotive.DisplayName),LogLevel.Verbose);

            string currentStation = LocoTelem.currentDestination[locomotive].identifier;
            int currentStationIndex = DestinationManager.orderedStations.IndexOf(currentStation);
            bool isTravelingEastWard = LocoTelem.locoTravelingEastWard[locomotive]; // true if traveling East

            // Determine the range of stations to include based on travel direction
            // using CTC logical direction
            IEnumerable<string> stationsLeftOfMe;
            IEnumerable<string> stationsRightOfMe;
            IEnumerable<string> stopsLeftOfMe;
            IEnumerable<string> stopsRightOfMe;
            IEnumerable<string> transfersLeftOfMe;
            IEnumerable<string> transfersRightOfMe;
            IEnumerable<string> relevantRightStations;
            IEnumerable<string> relevantLeftStations;

            //Get all Pickups
            HashSet<string> pickupStationIdentifiers = LocoTelem.pickupStations[locomotive]
                .Select(stop => stop.identifier)
                .ToHashSet();

            foreach (string identifier in pickupStationIdentifiers)
            {
                RouteManager.logger.LogToDebug(String.Format("pickupStationIdentifiers contains {0}", identifier), LogLevel.Verbose);
            }

            //Get all Stops
            HashSet<string> stopStationIdentifiers = LocoTelem.stopStations[locomotive]
                .Select(stop => stop.identifier)
                .ToHashSet();

            foreach (string identifier in stopStationIdentifiers)
            {
                RouteManager.logger.LogToDebug(String.Format("stopStationIdentifiers contains {0}", identifier), LogLevel.Verbose);
            }

            //Get all transfers
            HashSet<string> transferStationIdentifiers = LocoTelem.transferStations[locomotive]
                .Select(stop => stop.Value.identifier)
                .Distinct()
                .ToHashSet();

            foreach (string identifier in transferStationIdentifiers)
            {
                RouteManager.logger.LogToDebug(String.Format("transferStationIdentifiers contains {0}", identifier), LogLevel.Verbose);
            }

            //get stations to the left of current position that are pickup points
            stationsLeftOfMe = DestinationManager.orderedStations.Skip(currentStationIndex)
                                                                 .Where(station => pickupStationIdentifiers.Contains(station));

            foreach (string identifier in stationsLeftOfMe)
            {
                RouteManager.logger.LogToDebug(String.Format("stationsLeftOfMe contains {0}", identifier), LogLevel.Verbose);
            }

            //get stations to the right of current position that are pickup points
            stationsRightOfMe = DestinationManager.orderedStations.Take(currentStationIndex-1)
                                                                  .Where(station => pickupStationIdentifiers.Contains(station));

            foreach (string identifier in stationsRightOfMe)
            {
                RouteManager.logger.LogToDebug(String.Format("stationsRightOfMe contains {0}", identifier), LogLevel.Verbose);
            }


            if (isTravelingEastWard) //travelling right (using CTC logical direction)
            {

                //get stations to the left of current position that are pickup points
                stopsLeftOfMe = DestinationManager.orderedStations.Skip(currentStationIndex)
                                                                  .Where(station => stopStationIdentifiers.Contains(station));

                foreach (string identifier in stopsLeftOfMe)
                {
                    RouteManager.logger.LogToDebug(String.Format("stopsLeftOfMe contains {0}", identifier), LogLevel.Verbose);
                }

                //get stations to the left of current position that are transfer drop off points
                transfersLeftOfMe = DestinationManager.orderedStations.Skip(currentStationIndex)
                                                                      .Where(station => transferStationIdentifiers.Contains(station));

                foreach (string identifier in transfersLeftOfMe)
                {
                    RouteManager.logger.LogToDebug(String.Format("stopsLeftOfMe contains {0}", identifier), LogLevel.Verbose);
                }


                //Keep the stations to the right that don't have a stop to the right
                relevantRightStations = stationsRightOfMe.Where(station =>
                                                                    !stopsLeftOfMe.Contains(station) &&
                                                                    !transfersLeftOfMe.Contains(station)
                                                                );

                foreach (string identifier in relevantRightStations)
                {
                    RouteManager.logger.LogToDebug(String.Format("relevantRightStations contains {0}", identifier), LogLevel.Verbose);
                }


                //Keep the stations to the left that don't have a stop to the right
                relevantLeftStations = stationsLeftOfMe.Where(station =>
                                                                    !stopsLeftOfMe.Contains(station) &&
                                                                    !transfersLeftOfMe.Contains(station)
                                                               );

                foreach (string identifier in relevantLeftStations)
                {
                    RouteManager.logger.LogToDebug(String.Format("relevantLeftStations contains {0}", identifier), LogLevel.Verbose);
                }
            }
            else //travelling left (using CTC logical direction)
            {
                //get stations to the right of current position that are pickup points
                stopsRightOfMe = DestinationManager.orderedStations.Take(currentStationIndex - 1)
                                                                      .Where(station => stopStationIdentifiers.Contains(station));

                foreach (string identifier in stopsRightOfMe)
                {
                    RouteManager.logger.LogToDebug(String.Format("stopsRightOfMe contains {0}", identifier), LogLevel.Verbose);
                }

                //get stations to the right of current position that are transfer drop off points
                transfersRightOfMe = DestinationManager.orderedStations.Take(currentStationIndex - 1)
                                                                      .Where(station => transferStationIdentifiers.Contains(station));
                
                foreach (string identifier in transfersRightOfMe)
                {
                    RouteManager.logger.LogToDebug(String.Format("transfersRightOfMe contains {0}", identifier), LogLevel.Verbose);
                }

                //Keep the stations to the right that don't have a stop to the right
                relevantRightStations = stationsRightOfMe.Where(station =>
                                                                    !stopsRightOfMe.Contains(station) &&
                                                                    !transfersRightOfMe.Contains(station)
                                                                );
                foreach (string identifier in relevantRightStations)
                {
                    RouteManager.logger.LogToDebug(String.Format("relevantRightStations contains {0}", identifier), LogLevel.Verbose);
                }

                //Keep the stations to the left that don't have a stop to the right
                relevantLeftStations = stationsLeftOfMe.Where(station =>
                                                                !stopsRightOfMe.Contains(station) &&
                                                                !transfersRightOfMe.Contains(station)
                                                               );
                
                foreach (string identifier in relevantLeftStations)
                {
                    RouteManager.logger.LogToDebug(String.Format("relevantLeftStations contains {0}", identifier), LogLevel.Verbose);
                }
            }

            HashSet<string> filteredStations = relevantRightStations.Union(relevantLeftStations).ToHashSet();

            foreach (string identifier in filteredStations)
            {
                RouteManager.logger.LogToDebug(String.Format("filteredStations contains {0}", identifier), LogLevel.Verbose);
            }

            RouteManager.logger.LogToDebug(String.Format("Loco: {0} updating car station selection", locomotive.DisplayName), LogLevel.Debug);

            // Apply the filtered stations to each coach
            foreach (Car coach in locomotive.EnumerateCoupled().Where(car => car.Archetype == CarArchetype.Coach))
            {

                foreach (string identifier in filteredStations)
                {
                    RouteManager.logger.LogToDebug(String.Format("    Applying {0} to car {1}", identifier, coach.DisplayName), LogLevel.Verbose);
                }

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
                RouteManager.logger.LogToError($"TransitMode dictionary does not contain key: {locomotive}");
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
                    RouteManager.logger.LogToDebug($" LocoTelem.RouteMode does not contain {locomotive.id} creating bool for {locomotive.id}");
                    LocoTelem.RouteMode[locomotive] = false;
                    LocoTelem.RouteModePaused[locomotive] = false;
                }
                RouteManager.logger.LogToDebug($"changing LocoTelem.Route Mode from {!IsOn} to {IsOn}");
                LocoTelem.RouteMode[locomotive] = IsOn;
                LocoTelem.RouteModePaused[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);

                if (!LocoTelem.locomotiveCoroutines.ContainsKey(locomotive))
                {
                    RouteManager.logger.LogToDebug($" LocoTelem.locomotiveCoroutines does not contain {locomotive.id} creating bool for {locomotive.DisplayName}");
                    LocoTelem.locomotiveCoroutines[locomotive] = false;
                    LocoTelem.RouteModePaused[locomotive] = false;
                }
            }
            else if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && IsOn)
            {
                RouteManager.logger.LogToConsole($"There are no stations selected for {locomotive.DisplayName}. Please select at least 1 station before enabling Route Mode");
            }
            else if (DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && !IsOn)
            {
                LocoTelem.RouteMode[locomotive] = false;
                LocoTelem.RouteModePaused[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);
            }
            else if (!DestinationManager.IsAnyStationSelectedForLocomotive(locomotive) && !IsOn)
            {
                LocoTelem.RouteMode[locomotive] = false;
                LocoTelem.RouteModePaused[locomotive] = false;
                OnRouteModeChanged?.Invoke(locomotive);
            }
            else
            {
                RouteManager.logger.LogToDebug($"Route Mode ({LocoTelem.RouteMode[locomotive]}) and IsAnyStationSelectedForLocomotive ({DestinationManager.IsAnyStationSelectedForLocomotive(locomotive)}) are no combination of false or true ");
            }
            return;
        }

        public static bool IsRouteModePaused(Car locomotive)
        {
            // Check if the locomotive exists in the TransitMode dictionary
            if (LocoTelem.RouteModePaused.ContainsKey(locomotive))
            {
                return LocoTelem.RouteModePaused[locomotive];
            }
            else
            {
                // Handle the case where the key does not exist, for example, by logging an error or initializing the key
                RouteManager.logger.LogToDebug($"Paused Mode dictionary does not contain key: {locomotive}. Setting it now.",LogLevel.Warning);

                // Optionally initialize the key with a default value
                LocoTelem.RouteModePaused[locomotive] = false; // Default value

                return false;
            }
        }

        public static event Action<Car> OnRouteModePaused;
        public static void PauseRouteMode(bool IsOn, Car locomotive)
        {
            if(LocoTelem.RouteMode.ContainsKey(locomotive)) 
            {
                if (IsOn && LocoTelem.RouteMode[locomotive])
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} route mode is now paused!", locomotive.DisplayName));
                    LocoTelem.RouteModePaused[locomotive] = true;
                }
                else
                {

                    //Restore data if possible
                    StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.locoTravelingEastWard[locomotive], (int)LocoTelem.RMMaxSpeed[locomotive], null));

                    RouteManager.logger.LogToDebug(String.Format("Loco {0} route mode is now unpaused!", locomotive.DisplayName));
                    LocoTelem.RouteModePaused[locomotive] = false;
                }
            }
            else
            {
                RouteManager.logger.LogToDebug(String.Format("Loco {0} route mode not enabled. Will not pause!", locomotive.DisplayName));
                LocoTelem.RouteModePaused[locomotive] = false;
            }

            OnRouteModeChanged?.Invoke(locomotive);
        }


        public static float GetTrainVelocity(Car locomotive)
        {
            return Math.Abs(locomotive.velocity * 2.23694f);
        }


        //Separate out the core fuel check logic
        public static void locoLowFuelCheck(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: locoLowFuelCheck", LogLevel.Trace);

            Dictionary <string, float> fuelCheckResults = new Dictionary<string, float>();

            //If steam locomotive Check the water and coal levels
            if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveSteam)
            {

                //If coal is below minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "coal") / 2000, RouteManager.Settings.minCoalQuantity))
                {
                    fuelCheckResults.Add("coal", GetLoadInfoForLoco(locomotive, "coal") / 2000);
                }

                //If water is below minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "water"), RouteManager.Settings.minWaterQuantity))
                {
                    fuelCheckResults.Add("water", GetLoadInfoForLoco(locomotive, "water"));
                }

            }
            //If Diesel locomotive diesel levels
            else if (locomotive.Archetype == Model.Definition.CarArchetype.LocomotiveDiesel)
            {
                //If diesel level is below defined minimums
                if (compareAgainstMinVal(TrainManager.GetLoadInfoForLoco(locomotive, "diesel-fuel"), RouteManager.Settings.minDieselQuantity))
                {
                    fuelCheckResults.Add("diesel-fuel", GetLoadInfoForLoco(locomotive, "diesel-fuel"));
                }
            }

            LocoTelem.lowFuelQuantities[locomotive] = fuelCheckResults;

            //Trace Method
            RouteManager.logger.LogToDebug("EXITING FUNCTION: locoLowFuelCheck", LogLevel.Trace);
        }

        //Methodize repeated code of fuel check. 
        //Method could be re-integrated into calling method now that additional checks have been rendered null from further code improvements.
        private static bool compareAgainstMinVal(float inputValue, float minimumValue)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: compareAgainstMinVal", LogLevel.Trace);

            //Compare to minimums
            if (inputValue < minimumValue)
                return true;

            //Trace Method
            RouteManager.logger.LogToDebug("EXITING FUNCTION: compareAgainstMinVal", LogLevel.Trace);

            //Something unexpected happened or fuel is above minimums.
            //Either way return false here as there is nothing further we can do. 
            return false;
        }
    }

}