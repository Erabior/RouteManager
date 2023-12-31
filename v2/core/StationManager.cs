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
            Logger.LogToDebug(String.Format("Car {0} Closest Station was: {1}", currentCar.DisplayName, closestStation.DisplayName), Logger.logLevel.Debug);

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: GetClosestStation", Logger.logLevel.Trace);

            return (closestStation, closestDistance);
        }

        public static PassengerStop getNextStation(Car locomotive)
        {
            PassengerStop currentStation = default(PassengerStop);

            //Set a current destination if it does not exist, else use the current destination.
            if (!LocoTelem.currentDestination.ContainsKey(locomotive) || LocoTelem.currentDestination[locomotive] == default(PassengerStop))
            {
                //No Destination set so for now, assume closest station.
                currentStation = GetClosestStation(locomotive).Item1;
                Logger.LogToDebug("Loco {0} does not have destination. Defaulting to closest station {1}",Logger.logLevel.Debug);
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

            //Parse orderedSelected stops to PassengerStops
            //LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedPassengerStops);

            PassengerStop nextStop = calculateNextStation(orderedSelectedStations, LocoTelem.SelectedStations[locomotive], currentStation, locomotive);

            Logger.LogToDebug(nextStop.identifier, Logger.logLevel.Debug);

            return nextStop;

        }




        private static PassengerStop calculateNextStation(List<string> orderedSelectedStations, List<PassengerStop> selectedPassengerStops, PassengerStop currentStation, Car locomotive)
        {

            Logger.LogToDebug(String.Format("Loco {0} calculating next station",locomotive.DisplayName), Logger.logLevel.Verbose);

            //Make sure we have stations after all that parsing was done...
            if (selectedPassengerStops != null && selectedPassengerStops.Count > 1)
            {
                //Current station index
                int currentIndex = orderedSelectedStations.IndexOf(currentStation.identifier);

                //Current station is is not a valid selected station...
                if (currentIndex! < 0)
                {
                    Logger.LogToDebug(String.Format("Loco {0} Current station is not selected. Defaulting to First Selected station", locomotive.DisplayName), Logger.logLevel.Verbose);
                    return selectedPassengerStops.First();
                }

                Logger.LogToDebug(String.Format("Loco {0} determining direction of travel for station selection", locomotive.DisplayName), Logger.logLevel.Verbose);

                //If we are traveling torward Anderson from Silva
                if (LocoTelem.locoTravelingWestward[locomotive])
                {
                    Logger.LogToDebug(String.Format("Loco {0} Traveleing West", locomotive.DisplayName), Logger.logLevel.Debug);
                    if (currentIndex == selectedPassengerStops.Count - 1) 
                    {
                        Logger.LogToDebug(String.Format("Loco {0} at end of line", locomotive.DisplayName), Logger.logLevel.Debug);
                        LocoTelem.locoTravelingWestward[locomotive] = false;
                        LocoTelem.needToUpdatePassengerCoaches[locomotive] = false;

                        return LocoTelem.currentDestination[locomotive] = selectedPassengerStops[currentIndex - 1];
                    }
                    else
                    {
                       return selectedPassengerStops[currentIndex + 1];
                    }
                }
                else
                {
                    Logger.LogToDebug(String.Format("Loco {0} Traveleing East", locomotive.DisplayName), Logger.logLevel.Debug);
                    if (currentIndex == 0)
                    {
                        Logger.LogToDebug(String.Format("Loco {0} at end of line", locomotive.DisplayName), Logger.logLevel.Debug);
                        LocoTelem.locoTravelingWestward[locomotive] = true;
                        LocoTelem.needToUpdatePassengerCoaches[locomotive] = false;

                        return LocoTelem.currentDestination[locomotive] = selectedPassengerStops[currentIndex + 1];
                    }
                    else
                    {
                        return selectedPassengerStops[currentIndex - 1];
                    }
                }
            }

            Logger.LogToDebug(String.Format("Loco {0} faild to find next station ... Defaulting to first stop", locomotive.DisplayName), Logger.logLevel.Verbose);

            //Worst case, start from the beginning...
            return selectedPassengerStops.First();
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
