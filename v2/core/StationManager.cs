using JetBrains.Annotations;
using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Track;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = RouteManager.v2.Logging.Logger;

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

        public static (PassengerStop,float) GetClosestStation(Car currentCar)
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: GetClosestStation", Logger.logLevel.Trace);

            //Debugging Output
            Logger.LogToDebug(String.Format("Car {0} calculating closest station...", currentCar.DisplayName), Logger.logLevel.Debug);

            // Initialize variables;
            PassengerStop closestStation = null;
            float closestDistance = float.MaxValue;
            Graph graph = Graph.Shared;


            //Get Locomotive Center on the map
            Vector3? locoMotivePosition = currentCar.GetCenterPosition(graph);

            // If centerpoint is null then bail
            if (locoMotivePosition == null)
            {
                Logger.LogToError("Could not obtain locomotive's center position.");
                return (null,0);
            }

            //Debugging Output
            Logger.LogToDebug(String.Format("Car {0} centerpoint {1} has value {2})", currentCar, locoMotivePosition, locoMotivePosition.Value), Logger.logLevel.Verbose);

            // Iterate over each selected station using a for loop
            foreach ( PassengerStop station in UnityEngine.Object.FindObjectsOfType<PassengerStop>())
            {
                Logger.LogToDebug($"Station center was: {station.CenterPoint}",Logger.logLevel.Verbose);
                Logger.LogToDebug($"Car center was: {locoMotivePosition.Value}", Logger.logLevel.Verbose);

                // Calculate the distance between the locomotive and the station's center point
                float distance = Vector3.Distance(locoMotivePosition.Value, station.CenterPoint);

                // Keep track of the closest station
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStation = station;
                }
            }

            //Debug output
            Logger.LogToDebug(String.Format("Car {0} Closest Station was: {1}", currentCar.DisplayName, closestStation.identifier), Logger.logLevel.Debug);

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: GetClosestStation", Logger.logLevel.Trace);

            return (closestStation, closestDistance);
        }


        //Attempt to determine midroute station better when starting the coroutine.
        public static PassengerStop getInitialDestination(Car locomotive)
        {
            PassengerStop nextStation = getNextStation(locomotive);

            Logger.LogToDebug(String.Format("Loco {0} determining initial destination",locomotive.DisplayName),Logger.logLevel.Debug);

            //Make sure a previous destination is set
            if (LocoTelem.previousDestinations.ContainsKey(locomotive))
            {
                Logger.LogToDebug(String.Format("Loco {0} has previous destinations", locomotive.DisplayName), Logger.logLevel.Verbose);
                //Compare Previous Destination
                //If we have not visited the closest station
                if (!LocoTelem.previousDestinations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                {
                    Logger.LogToDebug(String.Format("Loco {0} has previous destinations not containing closeset station", locomotive.DisplayName), Logger.logLevel.Verbose);
                    //If the closest station is selected....
                    if (LocoTelem.SelectedStations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                    {
                        Logger.LogToDebug(String.Format("Loco {0} Initial destintion is the closest: {1}", locomotive.DisplayName , LocoTelem.closestStation[locomotive].Item1));
                        return LocoTelem.closestStation[locomotive].Item1;
                    }
                }
            }
            else
            {
                //If the closest station is selected....
                if (LocoTelem.SelectedStations[locomotive].Contains(LocoTelem.closestStation[locomotive].Item1))
                {
                    Logger.LogToDebug(String.Format("Loco {0} Initial destintion is the closest: {1}", locomotive.DisplayName, LocoTelem.closestStation[locomotive].Item1));
                    return LocoTelem.closestStation[locomotive].Item1;
                }
                else
                {
                    Logger.LogToDebug(String.Format("Loco {0} Initial destintion is not the closest: {1}", locomotive.DisplayName, nextStation));
                    return nextStation;
                }
            }

            //Worst case, Just default to the next station
            Logger.LogToDebug(String.Format("Loco {0} getInitialDestination reached default case! Station was: {1}", locomotive.DisplayName, nextStation),Logger.logLevel.Error);
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
                Logger.LogToDebug(String.Format("Loco {0} does not have a destination. Defaulting to closest station {1}", locomotive.DisplayName,currentStation.identifier),Logger.logLevel.Debug);
            }
            else
            {
                currentStation = LocoTelem.currentDestination[locomotive];
            }

            //Get Selected menu items
            List<string> selectedStationIdentifiers = LocoTelem.SelectedStations
                .SelectMany(pair => pair.Value)
                .Select(passengerStop => passengerStop.identifier)
                .Distinct()
                .ToList();

            //Convert selected menu items into an ordered list of station stops
            List<string> orderedSelectedStations = DestinationManager.orderedStations.Where(item => selectedStationIdentifiers.Contains(item)).ToList();

            PassengerStop nextStop = calculateNextStation(orderedSelectedStations, LocoTelem.SelectedStations[locomotive], currentStation, locomotive);

            Logger.LogToDebug(String.Format("Loco {0} next stop determined to be: {1}", locomotive.DisplayName , nextStop.identifier), Logger.logLevel.Debug);

            return nextStop;

        }


        //Brand new station logic.
        private static PassengerStop calculateNextStation(List<string> orderedSelectedStations, List<PassengerStop> selectedPassengerStops, PassengerStop currentStation, Car locomotive)
        {
            //Debug
            Logger.LogToDebug(String.Format("Loco {0} calculating next station", locomotive.DisplayName), Logger.logLevel.Verbose);
            PassengerStop station;

            //Make sure we have at least 2 stations
            if (selectedPassengerStops != null && selectedPassengerStops.Count > 1)
            {
                //Current station index
                int currentIndex = orderedSelectedStations.IndexOf(currentStation.identifier);
                
                for (int i = 0; i < orderedSelectedStations.Count; i++)
                {
                    Logger.LogToDebug(String.Format("Selected Station: {0} has position {1}", orderedSelectedStations[i],i), Logger.logLevel.Verbose);
                }

                Logger.LogToDebug(String.Format("Current Index was calculated as: {0}", currentIndex), Logger.logLevel.Debug);

                //At first station go West
                if (currentIndex == 0)
                {
                    Logger.LogToDebug(String.Format("Loco {0} at end of line, returning west", locomotive.DisplayName), Logger.logLevel.Debug);

                    //Update telemetry
                    updateLocoTelemEndOfLine(locomotive, false);

                    //Bounds Checking
                    if (currentIndex + 1 <= orderedSelectedStations.Count)
                        station = stringIdentToStation(orderedSelectedStations[currentIndex + 1]);
                    else
                        station = stringIdentToStation(orderedSelectedStations[currentIndex]);

                    return station != null ? station : selectedPassengerStops.Last();
                }
                //At last station go East
                else if (currentIndex == orderedSelectedStations.Count - 1)
                {
                    Logger.LogToDebug(String.Format("Loco {0} at end of line, returning east", locomotive.DisplayName), Logger.logLevel.Debug);

                    //Update telemetry
                    updateLocoTelemEndOfLine(locomotive, true);

                    //Bounds Checking
                    if (currentIndex - 1 >= 0)
                        station = stringIdentToStation(orderedSelectedStations[currentIndex - 1]);
                    else
                        station = stringIdentToStation(orderedSelectedStations[currentIndex]);

                    return station != null ? station : selectedPassengerStops.Last();
                }
                //Keep going in the direction previously travelled...
                else
                {
                    //If we are traveling torward Silva from Anderson
                    if (LocoTelem.locoTravelingEastWard[locomotive])
                    {
                        Logger.LogToDebug(String.Format("Loco {0} next station is to the east", locomotive.DisplayName), Logger.logLevel.Debug);
                        //Bounds Checking
                        if (currentIndex - 1 >= 0)
                            station = stringIdentToStation(orderedSelectedStations[currentIndex - 1]);
                        else
                            station = stringIdentToStation(orderedSelectedStations[currentIndex]);

                        return station != null ? station : selectedPassengerStops.First();
                    }
                    else
                    {
                        Logger.LogToDebug(String.Format("Loco {0} next station is to the west", locomotive.DisplayName), Logger.logLevel.Debug);

                        //Bounds Checking
                        if (currentIndex + 1 <= orderedSelectedStations.Count)
                            station = stringIdentToStation(orderedSelectedStations[currentIndex + 1]);
                        else
                            station = stringIdentToStation(orderedSelectedStations[currentIndex]);

                        return station != null ? station : selectedPassengerStops.First();
                    }
                }
            }

            //Worst case, start from the beginning...
            Logger.LogToDebug(String.Format("Loco {0} faild to find next station ... Defaulting to first stop", locomotive.DisplayName), Logger.logLevel.Verbose);
            return selectedPassengerStops.First();
        }

        private static void updateLocoTelemEndOfLine(Car locomotive, bool direction)
        {
            LocoTelem.locoTravelingEastWard[locomotive] = direction;
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

    }
}
