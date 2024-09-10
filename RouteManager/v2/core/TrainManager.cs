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
            int currentStationIndex = StationInformation.OrderedStations.IndexOf(currentStation);
            bool isTravelingEastWard = LocoTelem.locoTravelingEastWard[locomotive]; // true if traveling East

            // Determine the range of stations to include based on travel direction
            IEnumerable<string> relevantStations;

            if (StationManager.currentlyAtLastStation(locomotive))
            {
                relevantStations = StationInformation.OrderedStations.Except(new List<String>() { LocoTelem.closestStation[locomotive].Item1.identifier });
            }
            else if (isTravelingEastWard)
            {
                relevantStations = StationInformation.OrderedStations.Take(currentStationIndex + 1).Reverse();
            }
            else
            {
                relevantStations = StationInformation.OrderedStations.Skip(currentStationIndex);
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



        /************************************************************************************************************
         * 
         * 
         * 
         *                  Experimental / Developement / Preview features. 
         * 
         * 
         * 
         ************************************************************************************************************/



        public static void CopyStationsFromLocoToCoaches_dev(Car locomotive)
        {
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} update coach station selection", locomotive.DisplayName), LogLevel.Verbose);

            string currentStation = StationManager.GetClosestStation_dev(locomotive).Item1.identifier; //LocoTelem.currentDestination[locomotive].identifier;
            int currentStationIndex = StationInformation.OrderedStations.IndexOf(currentStation);
            bool isTravelingEastWard = LocoTelem.locoTravelingEastWard[locomotive]; // true if traveling East
            IEnumerable<string> filteredStations;

            RouteManager.logger.LogToDebug($"CopyStationsFromLocoToCoaches_dev() Current Station {currentStation}, Current Station Index: {currentStationIndex}, Is Travelling East: {isTravelingEastWard}", LogLevel.Verbose);

            var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
            RouteManager.logger.LogToDebug($"CopyStationsFromLocoToCoaches_dev() stopsLookup Complete");
            List<PassengerStop> orderedStops = StationInformation.OrderedStations.Select(id => stopsLookup[id])
                                                                                 .ToList();
            RouteManager.logger.LogToDebug($"CopyStationsFromLocoToCoaches_dev() orderedStops Complete");
            List<PassengerStop> stationsLeftOfMe = orderedStops.Skip(currentStationIndex + 1).ToList();
            List<PassengerStop> stopsLeftOfMe = LocoTelem.stopStations[locomotive].Where(stop => stationsLeftOfMe.Contains(stop)).ToList();
            List<PassengerStop> stationsRightOfMe = orderedStops.Take(currentStationIndex).ToList(); //+0 
            List<PassengerStop> stopsRightOfMe = LocoTelem.stopStations[locomotive].Where(stop => stationsRightOfMe.Contains(stop)).ToList();
            RouteManager.logger.LogToDebug($"CopyStationsFromLocoToCoaches_dev() stations left & right Complete");

            Dictionary<PassengerStop, PassengerStop> transferStations;
            LocoTelem.transferStations.TryGetValue(locomotive, out transferStations);
            if (transferStations == null)
                transferStations = new Dictionary<PassengerStop, PassengerStop>();

            RouteManager.logger.LogToDebug($"CopyStationsFromLocoToCoaches_dev() transfer stations Complete");

            RouteManager.logger.LogToDebug($"stationsLeftOfMe: {string.Join(", ", stationsLeftOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);
            RouteManager.logger.LogToDebug($"stationsRightOfMe: {string.Join(", ", stationsRightOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);
            RouteManager.logger.LogToDebug($"stopsLeftOfMe: {string.Join(", ", stopsLeftOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);
            RouteManager.logger.LogToDebug($"stopsRightOfMe: {string.Join(", ", stopsRightOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

            //Filter to stations marked as a pickup
            List<PassengerStop> pickupsLeftOfMe = stationsLeftOfMe.Where(station => LocoTelem.pickupStations[locomotive].Contains(station)).ToList();
            List<PassengerStop> pickupsRightOfMe = stationsRightOfMe.Where(station => LocoTelem.pickupStations[locomotive].Contains(station)).ToList();

            RouteManager.logger.LogToDebug($"Pickup stationsLeftOfMe: {string.Join(", ", pickupsLeftOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);
            RouteManager.logger.LogToDebug($"Pickup stationsRightOfMe: {string.Join(", ", pickupsRightOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

            if (isTravelingEastWard) //Travelling CTC logical right
            {
                //If a pickup station on my left is also a stop station we want to remove it
                List<PassengerStop> filteredPickupsLeftOfMe = pickupsLeftOfMe.Where(station => !LocoTelem.stopStations[locomotive].Contains(station)).ToList();
                RouteManager.logger.LogToDebug($"Pickups left of me with no stop: {string.Join(", ", filteredPickupsLeftOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Find where the remaining pickups left of me will be dropped off, only keep the pickups if drop off point is a stop to the right
                filteredPickupsLeftOfMe =
                    transferStations.Where(station =>
                    {
                        RouteManager.logger.LogToDebug($"assessing: {station.Key}, {station.Value}, Contains Key: {filteredPickupsLeftOfMe.Contains(station.Key)}, Contains Value: {stopsRightOfMe.Contains(station.Value)}, Matches Current Station: {station.Value.identifier == currentStation}");
                        return filteredPickupsLeftOfMe.Contains(station.Key) && (stopsRightOfMe.Contains(station.Value));
                    })
                                    .Select(station => station.Key).ToList();
                RouteManager.logger.LogToDebug($"Pickups left of me with a transfer to my right: {string.Join(", ", filteredPickupsLeftOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Find the pickups right of me without a stop
                List<PassengerStop> pickupsRightOfMeNoStop = pickupsRightOfMe.Where(station => !LocoTelem.stopStations[locomotive].Contains(station)).ToList();
                RouteManager.logger.LogToDebug($"Pickups right of me with no stop: {string.Join(", ", pickupsRightOfMeNoStop.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Pickups right of me where the passengers are transferred/dropped off at a station to my left
                pickupsRightOfMeNoStop = transferStations.Where(station =>
                {
                    RouteManager.logger.LogToDebug($"assessing: {station.Key}, {station.Value}, Contains Key: {pickupsRightOfMeNoStop.Contains(station.Key)}, Contains Value: {stopsLeftOfMe.Contains(station.Value)}, Matches Current Station: {station.Value.identifier == currentStation}");
                    return pickupsRightOfMeNoStop.Contains(station.Key) && ((stopsLeftOfMe.Contains(station.Value) || station.Value.identifier == currentStation));
                })
                    .Select(station => station.Key).ToList();
                RouteManager.logger.LogToDebug($"Pickups right of me with no stop and the transfer to my left: {string.Join(", ", pickupsRightOfMeNoStop.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Remove stations with no stop and a transfer to my left from the list of pickups to my right
                List<PassengerStop> filteredPickupsRightOfMe = pickupsRightOfMe.Where(station => !pickupsRightOfMeNoStop.Contains(station)).ToList();
                RouteManager.logger.LogToDebug($"Pickups right of me with a stop or transfer to my right: {string.Join(", ", filteredPickupsRightOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

                filteredStations = filteredPickupsRightOfMe.Union(filteredPickupsLeftOfMe).Select(station => station.identifier);

            }
            else  //Travelling CTC logical left
            {
                //If a pickup station on my right is also a stop station we want to remove it
                List<PassengerStop> filteredPickupsRightOfMe = pickupsRightOfMe.Where(station => !LocoTelem.stopStations[locomotive].Contains(station)).ToList();
                RouteManager.logger.LogToDebug($"Pickups right of me with no stop: {string.Join(", ", filteredPickupsRightOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Find where the remaining pickups right of me will be dropped off, only keep the pickups if drop off point is a stop to the left
                filteredPickupsRightOfMe =
                    transferStations.Where(station =>
                    {
                        RouteManager.logger.LogToDebug($"assessing: {station.Key}, {station.Value}, Contains Key: {filteredPickupsRightOfMe.Contains(station.Key)}, Contains Value: {stopsLeftOfMe.Contains(station.Value)}, Matches Current Station: {station.Value.identifier == currentStation}", LogLevel.Trace);
                        return filteredPickupsRightOfMe.Contains(station.Key) && (stopsLeftOfMe.Contains(station.Value));
                    })
                    .Select(station => station.Key).ToList();

                RouteManager.logger.LogToDebug($"Pickups right of me with a transfer to my left: {string.Join(", ", filteredPickupsRightOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

                // Find the pickups left of me without a stop
                List<PassengerStop> pickupsLeftOfMeNoStop = pickupsLeftOfMe.Where(station => !LocoTelem.stopStations[locomotive].Contains(station)).ToList();
                RouteManager.logger.LogToDebug($"Pickups left of me with no stop: {string.Join(", ", pickupsLeftOfMeNoStop.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Pickups left of me where the passengers are transferred/dropped off at a station to my right
                pickupsLeftOfMeNoStop = transferStations.Where(station =>
                {
                    RouteManager.logger.LogToDebug($"assessing: {station.Key}, {station.Value}, Contains Key: {pickupsLeftOfMeNoStop.Contains(station.Key)}, Contains Value: {stopsRightOfMe.Contains(station.Value)}, Matches Current Station: {station.Value.identifier == currentStation}", LogLevel.Trace);
                    return pickupsLeftOfMeNoStop.Contains(station.Key) && ((stopsRightOfMe.Contains(station.Value) || station.Value.identifier == currentStation));
                })
                    .Select(station => station.Key).ToList();
                RouteManager.logger.LogToDebug($"Pickups left of me with no stop and the transfer to my right: {string.Join(", ", pickupsLeftOfMeNoStop.Select(stop => stop.identifier))}", LogLevel.Verbose);

                //Remove stations with no stop and a transfer to my right from the list of pickups to my left
                List<PassengerStop> filteredPickupsLeftOfMe = pickupsLeftOfMe.Where(station => !pickupsLeftOfMeNoStop.Contains(station)).ToList();
                RouteManager.logger.LogToDebug($"Pickups left of me with a stop or transfer to my left: {string.Join(", ", filteredPickupsLeftOfMe.Select(stop => stop.identifier))}", LogLevel.Verbose);

                filteredStations = filteredPickupsRightOfMe.Union(filteredPickupsLeftOfMe).Select(station => station.identifier);

            }

            RouteManager.logger.LogToDebug($"Filtered stations: {string.Join(", ", filteredStations)}", LogLevel.Verbose);

            LocoTelem.relevantPassengers[locomotive] = filteredStations.ToList();

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
    }

}