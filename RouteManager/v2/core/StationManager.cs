using Game.Events;
using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using RouteManager.v2.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;


namespace RouteManager.v2.core
{
    public class StationManager
    {
        public static bool doesStationHavePassengersWaiting(PassengerStop passengerStop)
        {
            return passengerStop.Waiting.Count > 0 ? true : false;
        }

        public static int getNumberPassengersWaitingForDestination(PassengerStop sourceStation, string destinationStationName)
        {
            //Get the passengers stop object
            PassengerStop destinationStop = UnityEngine.Object.FindObjectsOfType<PassengerStop>().Where(x => x.DisplayName == sourceStation.DisplayName).FirstOrDefault();

            //Only if the passenger stop is not null or default
            if(destinationStop != null && !destinationStop.Equals(default(PassengerStop)))
            {
                //Attempt to query for the destination
                KeyValuePair <String,int> destinationStationInfo =  destinationStop.Waiting.Where(x => x.Key == destinationStationName).FirstOrDefault();

                //If we have passenters waiting return the value
                if(!destinationStationInfo.Equals(default(KeyValuePair<String,int>)))
                {
                    return destinationStationInfo.Value;
                }
                //Apparently there were no passengers waiting for the destionation...
                else
                {
                    return 0;
                }
            }

            //Default return case if something went wrong.
            return -1;
        }

        public static PassengerStop[] getNeighboringStations(PassengerStop sourceStation)
        {

            //Only if the passenger stop is not null or default
            if (sourceStation != null && !sourceStation.Equals(default(PassengerStop)))
            {
                return sourceStation.neighbors.ToArray();
            }

            //Default return case if something went wrong.
            return null;
        }

        public static bool isStationUnlocked(PassengerStop sourceStation)
        {
            //Only if the passenger stop is not null or default
            if (sourceStation != null && !sourceStation.Equals(default(PassengerStop)))
            {
                return sourceStation.ProgressionDisabled;
            }

            //Default return case if something went wrong.
            return false;
        }

        public static bool currentlyAtLastStation(Car locomotive)
        {
            //Get Selected menu items
            List<string> selectedStationIdentifiers = LocoTelem.stopStations[locomotive]
                .Select(passengerStop => passengerStop.identifier)
                .Distinct()
                .ToList();

            List<string> orderedStopStations = StationInformation.OrderedStations.Where(item => selectedStationIdentifiers.Contains(item)).ToList();

            int currentIndex = orderedStopStations.IndexOf(LocoTelem.closestStation[locomotive].Item1.identifier);

            if (currentIndex == orderedStopStations.Count - 1 || currentIndex == 0)
                return true;

            return false;
        }

        public static (PassengerStop,float) GetClosestStation(Car currentCar)
        {
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: GetClosestStation", LogLevel.Trace);

            //Debugging Output
            RouteManager.logger.LogToDebug(String.Format("Car {0} calculating closest station...", currentCar.DisplayName), LogLevel.Debug);

            // Initialize variables;
            PassengerStop closestStation = null;
            float closestDistance = float.MaxValue;
            Graph graph = Graph.Shared;


            //Get Locomotive Center on the map
            Vector3? locoMotivePosition = currentCar.GetCenterPosition(graph);

            // If centerpoint is null then bail
            if (locoMotivePosition == null)
            {
                RouteManager.logger.LogToError("Could not obtain locomotive's center position.");
                return (null,0);
            }

            //Debugging Output
            RouteManager.logger.LogToDebug(String.Format("Car {0} centerpoint {1} has value {2})", currentCar, locoMotivePosition, locoMotivePosition.Value), LogLevel.Verbose);

            // Iterate over each selected station using a for loop
            foreach ( PassengerStop station in UnityEngine.Object.FindObjectsOfType<PassengerStop>())
            {
                float distance = 0;

                //Internal railroader bug linked to progression causes the game to fail to calculate the station center point. 
                //In the event that the game fails to calculate the station center point fall back to the predefined hardcoded values.
                //Work Around for issue #50
                try
                {
                    // Calculate the distance between the locomotive and the station's center point
                    //TODO: Convert distance from 'as the crow flies' to 'by track'
                    RouteManager.logger.LogToDebug($"Station center was: {station.CenterPoint}", LogLevel.Verbose);
                    distance = Vector3.Distance(locoMotivePosition.Value, station.CenterPoint);
                }
                catch 
                {
                    RouteManager.logger.LogToDebug($"Station center was: {StationInformation.Stations[station.identifier.ToLower()].Center}", LogLevel.Verbose);
                    //TODO: Convert distance from 'as the crow flies' to 'by track'
                    distance = Vector3.Distance(locoMotivePosition.Value, StationInformation.Stations[station.identifier.ToLower()].Center);
                }

                // Keep track of the closest station
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStation = station;
                }
            }

            //Debug output
            RouteManager.logger.LogToDebug(String.Format("Car {0} Closest Station was: {1}", currentCar.DisplayName, closestStation.identifier), LogLevel.Debug);

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: GetClosestStation", LogLevel.Trace);

            return (closestStation, closestDistance);
        }

        //Attempt to determine midroute station better when starting the coroutine.
        public static PassengerStop getInitialDestination(Car locomotive)
        {
            PassengerStop nextStation = getNextStation(locomotive);

            RouteManager.logger.LogToDebug(String.Format("Loco {0} determining initial destination",locomotive.DisplayName),LogLevel.Debug);

            //Make sure a previous destination is set
            if (LocoTelem.previousDestinations.ContainsKey(locomotive))
            {
                RouteManager.logger.LogToDebug(String.Format("Loco {0} has previous destinations", locomotive.DisplayName), LogLevel.Verbose);
                //Compare Previous Destination
                //If we have not visited the closest station
                if (!LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} has previous destinations not containing closeset station", locomotive.DisplayName), LogLevel.Verbose);
                    //If the closest station is selected....
                    if (LocoTelem.stopStations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                    {
                        RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is the closest: {1}", locomotive.DisplayName , LocoTelem.closestStation[locomotive].Item1));
                        return LocoTelem.closestStation[locomotive].Item1;
                    }
                }
            }
            else
            {
                //If the closest station is selected....
                if (LocoTelem.stopStations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is the closest: {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1));
                    return LocoTelem.closestStation[locomotive].Item1;
                }
                else if (LocoTelem.stopStations[locomotive].Contains(LocoTelem.currentDestination[locomotive]) && !LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.currentDestination[locomotive]))
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is the current: {1}", locomotive.DisplayName, LocoTelem.currentDestination[locomotive]));
                    return LocoTelem.currentDestination[locomotive];
                }
                else
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is not the closest: {1}", locomotive.DisplayName, nextStation));
                    return nextStation;
                }
            }

            //Worst case, Just default to the next station
            RouteManager.logger.LogToDebug(String.Format("Loco {0} getInitialDestination reached default case! Station was: {1}", locomotive.DisplayName, nextStation),LogLevel.Error);
            return nextStation;
        }

        public static PassengerStop getNextStation(Car locomotive)
        {
            PassengerStop currentStation = default(PassengerStop);

            //Set a current destination if it does not exist, else use the current destination.
            if (!LocoTelem.currentDestination.ContainsKey(locomotive) || LocoTelem.currentDestination[locomotive] == default(PassengerStop))
            {
                //No Destination set so for now, assume closest station.
                currentStation = GetClosestStation(locomotive).Item1;
                RouteManager.logger.LogToDebug(String.Format("Loco {0} does not have a destination. Defaulting to closest station {1}", locomotive.DisplayName,currentStation.identifier),LogLevel.Debug);
            }
            else
            {
                currentStation = LocoTelem.currentDestination[locomotive];
            }

            //Get Selected menu items
            List<string> selectedStationIdentifiers = LocoTelem.stopStations[locomotive]
                .Select(passengerStop => passengerStop.identifier)
                .Distinct()
                .ToList();

            //Convert selected menu items into an ordered list of station stops
            List<string> orderedstopStations = StationInformation.OrderedStations.Where(item => selectedStationIdentifiers.Contains(item)).ToList();

            PassengerStop nextStop = calculateNextStation(orderedstopStations, LocoTelem.stopStations[locomotive], currentStation, locomotive);

            RouteManager.logger.LogToDebug(String.Format("Loco {0} next stop determined to be: {1}", locomotive.DisplayName , nextStop.identifier), LogLevel.Debug);

            return nextStop;

        }

        //Brand new station logic.
        private static PassengerStop calculateNextStation(List<string> orderedstopStations, List<PassengerStop> selectedPassengerStops, PassengerStop currentStation, Car locomotive)
        {
            //Debug
            RouteManager.logger.LogToDebug(String.Format("Loco {0} calculating next station", locomotive.DisplayName), LogLevel.Verbose);
            PassengerStop station;

            //Make sure we have at least 2 stations
            if (selectedPassengerStops != null && selectedPassengerStops.Count > 1)
            {
                //Current station index
                int currentIndex = orderedstopStations.IndexOf(currentStation.identifier);
                
                for (int i = 0; i < orderedstopStations.Count; i++)
                {
                    RouteManager.logger.LogToDebug(String.Format("Selected Station: {0} has position {1}", orderedstopStations[i],i), LogLevel.Verbose);
                }

                RouteManager.logger.LogToDebug(String.Format("Current Index was calculated as: {0}", currentIndex), LogLevel.Debug);

                // Current station is not a selected station
                if (currentIndex < 0)
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} current station is not in the selected stations. Defaulting to closest stop!", locomotive.DisplayName), LogLevel.Verbose);
                    PassengerStop closestStation = null;
                    float closestStationDistance = float.MaxValue;

                    foreach (PassengerStop selectedPassengerStop in selectedPassengerStops)
                    {
                        float testDistance = DestinationManager.GetDistanceToStation(locomotive, selectedPassengerStop);

                        if (testDistance < closestStationDistance)
                        {
                            closestStationDistance = testDistance;
                            closestStation = selectedPassengerStop;
                        }
                    }

                    return closestStation;
                }

                //At first station go West
                if (currentIndex == 0)
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} at end of line, returning west", locomotive.DisplayName), LogLevel.Debug);

                    //Update telemetry
                    updateLocoTelemEndOfLine(locomotive, false);

                    //Bounds Checking
                    if (currentIndex + 1 <= orderedstopStations.Count)
                        station = stringIdentToStation(orderedstopStations[currentIndex + 1]);
                    else
                        station = stringIdentToStation(orderedstopStations[currentIndex]);

                    //Workaround for Cochran 
                    if (station != null)
                    {
                        if (station.identifier == "alarka" || LocoTelem.previousDestinations[locomotive].Contains(stringIdentToStation("alarka")))
                            station = alarkaJunctionWorkAround(locomotive, station);
                    }

                    return station != null ? station : selectedPassengerStops.Last();
                }
                //At last station go East
                else if (currentIndex == orderedstopStations.Count - 1)
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} at end of line, returning east", locomotive.DisplayName), LogLevel.Debug);

                    //Update telemetry
                    updateLocoTelemEndOfLine(locomotive, true);

                    //Bounds Checking
                    if (currentIndex - 1 >= 0)
                        station = stringIdentToStation(orderedstopStations[currentIndex - 1]);
                    else
                        station = stringIdentToStation(orderedstopStations[currentIndex]);

                    //Workaround for Cochran 
                    if (station != null)
                    {
                        if (station.identifier == "alarka" || LocoTelem.previousDestinations[locomotive].Contains(stringIdentToStation("alarka")))
                            station = alarkaJunctionWorkAround(locomotive, station);
                    }

                    return station != null ? station : selectedPassengerStops.Last();
                }
                //Keep going in the direction previously travelled...
                else
                {
                    //If we are traveling torward Sylva from Anderson
                    if (LocoTelem.locoTravelingEastWard[locomotive])
                    {
                        RouteManager.logger.LogToDebug(String.Format("Loco {0} next station is to the east", locomotive.DisplayName), LogLevel.Debug);
                        //Bounds Checking
                        if (currentIndex - 1 >= 0)
                            station = stringIdentToStation(orderedstopStations[currentIndex - 1]);
                        else
                            station = stringIdentToStation(orderedstopStations[currentIndex]);

                        //Workaround for Cochran 
                        if (station != null)
                        {
                            if (station.identifier == "alarka" || LocoTelem.previousDestinations[locomotive].Contains(stringIdentToStation("alarka")))
                                station = alarkaJunctionWorkAround(locomotive, station);
                        }

                        return station != null ? station : selectedPassengerStops.First();
                    }
                    else
                    {
                        RouteManager.logger.LogToDebug(String.Format("Loco {0} next station is to the west", locomotive.DisplayName), LogLevel.Debug);

                        //Bounds Checking
                        if (currentIndex + 1 <= orderedstopStations.Count)
                            station = stringIdentToStation(orderedstopStations[currentIndex + 1]);
                        else
                            station = stringIdentToStation(orderedstopStations[currentIndex]);

                        //Workaround for Cochran 
                        if (station != null)
                        {
                            if (station.identifier == "alarka" || LocoTelem.previousDestinations[locomotive].Contains(stringIdentToStation("alarka")))
                                station = alarkaJunctionWorkAround(locomotive, station);
                        }

                        return station != null ? station : selectedPassengerStops.First();
                    }
                }
            }

            //Worst case, start from the beginning...
            RouteManager.logger.LogToDebug(String.Format("Loco {0} faild to find next station ... Defaulting to first stop", locomotive.DisplayName), LogLevel.Verbose);
            return selectedPassengerStops.First();
        }

        //Overal logic needs adjusted to handle this better long term but to get V2 ready we will have to live
        //with a generic work around until we can revisit this after v2's launch.
        private static PassengerStop alarkaJunctionWorkAround(Car locomotive, PassengerStop nextStation)
        {
            List<string> selectedStationIdentifiers = LocoTelem.stopStations[locomotive]
                .Select(passengerStop => passengerStop.identifier)
                .Distinct()
                .ToList();

            if (selectedStationIdentifiers.Contains("cochran") && 
                selectedStationIdentifiers.Contains("alarka") &&
                !LocoTelem.previousDestinations[locomotive].Contains(stringIdentToStation("cochran")))
                return LocoTelem.stopStations[locomotive][LocoTelem.stopStations[locomotive].FindIndex(p => p.identifier == "cochran")];
            else
                return nextStation;
        }

        private static void updateLocoTelemEndOfLine(Car locomotive, bool direction)
        {
            LocoTelem.locoTravelingEastWard[locomotive] = direction;
            LocoTelem.locoTravelingForward[locomotive] = !LocoTelem.locoTravelingForward[locomotive];
            LocoTelem.needToUpdatePassengerCoaches[locomotive] = true;

            //Reset previous destinations but preserve the last destination for tracking
            if (LocoTelem.previousDestinations.ContainsKey(locomotive) && LocoTelem.previousDestinations[locomotive].Count>=1 )
            {
                PassengerStop lastStop = LocoTelem.previousDestinations[locomotive].Last<PassengerStop>();
                LocoTelem.previousDestinations[locomotive].Clear();
                LocoTelem.previousDestinations[locomotive].Add(lastStop);
            }
        }

        private static PassengerStop? stringIdentToStation(string stationIdentifier)
        {
            foreach (PassengerStop currenItem in UnityEngine.Object.FindObjectsOfType<PassengerStop>())
            {
                if (currenItem.identifier == stationIdentifier)
                    return currenItem;
            }
            return null;
        }

        //Will need adjusting
        public static bool isTrainInStation(Car currentCar)
        {
            if(Vector3.Distance(LocoTelem.closestStation[currentCar].Item1.CenterPoint, currentCar.GetCenterPosition(Graph.Shared)) <= 15f)
            {
                return true;
            }

            return false;
        }

        public static bool isStationRight(Car locomotive, PassengerStop station)
        {
            Graph graph = Graph.Shared;

            //Get front of the locomotive's location
            Location locoFront = locomotive.LocationA;
            Location locoRear = locomotive.LocationB;

            // Calculate the distance between the locomotive and the station's platform centre
            float distanceF = (graph.FindDistance(locoFront, (Location)station.TrackSpans.First().lower) + graph.FindDistance(locoFront, (Location)station.TrackSpans.First().upper)) / 2;
            float distanceR = (graph.FindDistance(locoRear, (Location)station.TrackSpans.First().lower) + graph.FindDistance(locoRear, (Location)station.TrackSpans.First().upper)) / 2;

            RouteManager.logger.LogToDebug($"Loco {locomotive.DisplayName} distance to station {station.identifier}: distanceF {distanceF} distanceR {distanceR}", LogLevel.Verbose);

            return (distanceR - distanceF) > 0;
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



        public static (PassengerStop, float) GetClosestStation_dev(Car currentCar)
        {
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: GetClosestStation_dev", LogLevel.Trace);

            //Debugging Output
            RouteManager.logger.LogToDebug(String.Format("Car {0} calculating closest station...", currentCar.DisplayName), LogLevel.Debug);

            // Initialize variables;
            PassengerStop closestStation = null;
            float closestDistance = float.MaxValue;
            Graph graph = Graph.Shared;

            //Get front of the locomotive's location
            Location locoFront = currentCar.LocationA;
            Location locoRear = currentCar.LocationB;

            // If locoFront is null then bail
            if (locoFront == null)
            {
                RouteManager.logger.LogToError("Could not obtain locomotive's front position.");
                return (null, 0);
            }

            //Debugging Output
            RouteManager.logger.LogToDebug(String.Format("Car {0} centerpoint {1} Facing right {2}", currentCar.DisplayName, locoFront.GetPosition(), currentCar.Orientation > 0), LogLevel.Verbose);

            /*
            RouteManager.logger.LogToDebug("List Platform Locations", LogLevel.Verbose);
            
            foreach (PassengerStop station in PassengerStop.FindAll().Where(ps => !ps.ProgressionDisabled))
            {
                RouteManager.logger.LogToDebug($"Station {station.name} neighbours: {string.Join(", ", station.neighbors.Select(ps => ps.identifier).ToArray())}");
                RouteManager.logger.LogToDebug($"Station {station.name} Lower: {((Location)station.TrackSpans.First().lower).GetPosition()}, Rot: {((Location)station.TrackSpans.First().lower).GetPositionRotation()}, Direction: {((Location)station.TrackSpans.First().lower).GetDirection()}");
                RouteManager.logger.LogToDebug($"Station {station.name} Upper: {((Location)station.TrackSpans.First().upper).GetPosition()}, Rot: {((Location)station.TrackSpans.First().upper).GetPositionRotation()}, Direction: {((Location)station.TrackSpans.First().upper).GetDirection()}");
            }*/


            // Iterate over each station that has been progress unlocked and station is a selected stop
            foreach (PassengerStop station in PassengerStop.FindAll().Where(ps => !ps.ProgressionDisabled && LocoTelem.stopStations[currentCar].Contains(ps)))
            {
                float distance = 0;

                RouteManager.logger.LogToDebug($"Station {station.name} neighbours: {string.Join(", ", station.neighbors.Select(ps => ps.identifier).ToArray())}");

                try
                {
                    // Calculate the distance between the locomotive and the station's platform centre
                    float distanceF = (graph.FindDistance(locoFront, (Location)station.TrackSpans.First().lower) + graph.FindDistance(locoFront, (Location)station.TrackSpans.First().upper)) / 2;
                    float distanceR = (graph.FindDistance(locoRear, (Location)station.TrackSpans.First().lower) + graph.FindDistance(locoRear, (Location)station.TrackSpans.First().upper)) / 2;

                    if (isStationRight(currentCar, station) && currentCar.Orientation > 0 ||
                        !isStationRight(currentCar, station) && currentCar.Orientation < 0)
                    {
                        distance = distanceF;
                    }
                    else if (isStationRight(currentCar, station) && currentCar.Orientation < 0 ||
                            !isStationRight(currentCar, station) && currentCar.Orientation > 0)
                    {
                        distance = distanceR;
                    }

                    RouteManager.logger.LogToDebug($"Graph distance from {currentCar.DisplayName} to {station.identifier}: Front: {distanceF}, Rear: {distanceR}", LogLevel.Verbose);

                }
                catch
                {
                    RouteManager.logger.LogToDebug($"Error calculating graph distance", LogLevel.Verbose);
                    return (null, float.MaxValue);
                }

                // Keep track of the closest station
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStation = station;
                }
            }

            //Debug output
            RouteManager.logger.LogToDebug(String.Format("Car {0} Closest Station was: {1}", currentCar.DisplayName, closestStation.identifier), LogLevel.Debug);

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: GetClosestStation_dev", LogLevel.Trace);

            return (closestStation, closestDistance);
        }

        public static PassengerStop getNextStation_dev(Car locomotive)
        {
            PassengerStop nextStop = null;
            PassengerStop currentStation = default(PassengerStop);
            int currentStationIndex = -1;

            //Set a current destination if it does not exist, else use the current destination.
            if (!LocoTelem.currentDestination.ContainsKey(locomotive) || LocoTelem.currentDestination[locomotive] == default(PassengerStop))
            {
                //No Destination set so for now, assume closest station.
                currentStation = GetClosestStation_dev(locomotive).Item1;
                RouteManager.logger.LogToDebug(String.Format("Loco {0} does not have a destination. Defaulting to closest station {1}", locomotive.DisplayName, currentStation.identifier), LogLevel.Debug);
            }
            else
            {
                currentStation = LocoTelem.currentDestination[locomotive];
            }

            Dictionary<string, PassengerStop> stationsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);

            //Get Selected menu items
            List<string> selectedStationIdentifiers = LocoTelem.stopStations[locomotive]
                .Select(passengerStop => passengerStop.identifier)
                .Distinct()
                .ToList();

            //Convert selected menu items into an ordered list of station stops
            List<string> orderedstopStations = StationInformation.OrderedStations.Where(item => selectedStationIdentifiers.Contains(item)).ToList();

            currentStationIndex = orderedstopStations.IndexOf(currentStation.identifier);

            RouteManager.logger.LogToDebug($"Current Index: {currentStationIndex} Travelling East: {LocoTelem.locoTravelingEastWard[locomotive]} osStations Count: {orderedstopStations.Count()}", LogLevel.Trace);

            if (currentStationIndex < 0)
            {
                //find the current station index
                int closestIndex = StationInformation.OrderedStations.FindIndex(stop => stop.Equals(currentStation.identifier));
                if (LocoTelem.locoTravelingEastWard[locomotive])
                {
                    //what are the stops after this point?
                    string nextStopStation = StationInformation.OrderedStations.Take(closestIndex - 1).Where(station => orderedstopStations.Contains(station)).Last();

                    if (nextStopStation != null)
                    {
                        nextStop = stationsLookup[nextStopStation];
                    }
                    else
                    {
                        //past the end of the line, get the last stop station
                        LocoTelem.locoTravelingEastWard[locomotive] = false;
                        nextStop = stationsLookup[orderedstopStations.Last()];
                    }

                }
                else
                {
                    //what are the stops after this point?
                    string nextStopStation = StationInformation.OrderedStations.Skip(closestIndex + 1).Where(station => orderedstopStations.Contains(station)).First();

                    if (nextStopStation != null)
                    {
                        nextStop = stationsLookup[nextStopStation];
                    }
                    else
                    {
                        //past the end of the line, get the last stop station
                        LocoTelem.locoTravelingEastWard[locomotive] = true;
                        nextStop = stationsLookup[orderedstopStations.First()];
                    }
                }
            }
            else
            {

                if (LocoTelem.locoTravelingEastWard[locomotive])
                {
                    if (currentStationIndex <= 0)
                    {
                        //we're at the end of the line
                        LocoTelem.locoTravelingEastWard[locomotive] = false;
                        nextStop = stationsLookup[orderedstopStations[currentStationIndex + 1]];
                    }
                    else
                    {
                        nextStop = stationsLookup[orderedstopStations[currentStationIndex - 1]];
                    }
                }
                else
                {
                    if (currentStationIndex >= orderedstopStations.Count() - 1)
                    {
                        //we're at the end of the line
                        LocoTelem.locoTravelingEastWard[locomotive] = true;
                        nextStop = stationsLookup[orderedstopStations[currentStationIndex - 1]];
                    }
                    else
                    {
                        nextStop = stationsLookup[orderedstopStations[currentStationIndex + 1]];
                    }
                }
            }

            RouteManager.logger.LogToDebug(String.Format("Loco {0} next stop determined to be: {1}", locomotive.DisplayName, nextStop.identifier), LogLevel.Debug);

            return nextStop;
        }

        /*
        public static PassengerStop getInitialDestination_dev(Car locomotive)
        {

            if (isTrainInStation(locomotive))
            {
                return LocoTelem.closestStation[locomotive];
            }
            else
            {

            }

            /*
            PassengerStop nextStation = getNextStation(locomotive);

            RouteManager.logger.LogToDebug(String.Format("Loco {0} determining initial destination", locomotive.DisplayName), LogLevel.Debug);

            //Make sure a previous destination is set
            if (LocoTelem.previousDestinations.ContainsKey(locomotive))
            {
                RouteManager.logger.LogToDebug(String.Format("Loco {0} has previous destinations", locomotive.DisplayName), LogLevel.Verbose);
                //Compare Previous Destination
                //If we have not visited the closest station
                if (!LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} has previous destinations not containing closeset station", locomotive.DisplayName), LogLevel.Verbose);
                    //If the closest station is selected....
                    if (LocoTelem.stopStations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                    {
                        RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is the closest: {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1));
                        return LocoTelem.closestStation[locomotive].Item1;
                    }
                }
            }
            else
            {
                //If the closest station is selected....
                if (LocoTelem.stopStations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is the closest: {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1));
                    return LocoTelem.closestStation[locomotive].Item1;
                }
                else if (LocoTelem.stopStations[locomotive].Contains(LocoTelem.currentDestination[locomotive]) && !LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.currentDestination[locomotive]))
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is the current: {1}", locomotive.DisplayName, LocoTelem.currentDestination[locomotive]));
                    return LocoTelem.currentDestination[locomotive];
                }
                else
                {
                    RouteManager.logger.LogToDebug(String.Format("Loco {0} Initial destintion is not the closest: {1}", locomotive.DisplayName, nextStation));
                    return nextStation;
                }
            }

            //Worst case, Just default to the next station
            RouteManager.logger.LogToDebug(String.Format("Loco {0} getInitialDestination reached default case! Station was: {1}", locomotive.DisplayName, nextStation), LogLevel.Error);
            return nextStation;
            
        }*/

    }
}
