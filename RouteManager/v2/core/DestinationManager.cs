using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;
using RouteManager.v2.Logging;

namespace RouteManager.v2.core
{
    public static class DestinationManager
    {

        //Update the list of stations to stop at.
        public static void SetStopStations(Car car, List<PassengerStop> selectedStops)
        {
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetStopStations", LogLevel.Trace);

            //Something went wrong
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            //Update consists's station list
            LocoTelem.stopStations[car] = selectedStops;

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetStopStations", LogLevel.Trace);
        }

        //Update the list of passengers to board.
        public static void SetPickupStations(Car car, List<PassengerStop> selectedPickups)
        {
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetPickupStations", LogLevel.Trace);

            //Something went wrong
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            //Update consists's station list
            LocoTelem.pickupStations[car] = selectedPickups;

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetPickupStations", LogLevel.Trace);
        }

        
        //Update the list of passengers to board.
        public static void SetTransferStations(Car car,Dictionary<PassengerStop, PassengerStop> selectedTransfers)
        {
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetTransferStations", LogLevel.Trace);

            //Something went wrong
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            //Update consists's station list
            RouteManager.logger.LogToDebug("Updating SetTransferStations", LogLevel.Trace);
            LocoTelem.transferStations[car] = selectedTransfers;
            RouteManager.logger.LogToDebug("Updated SetTransferStations", LogLevel.Trace);

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetTransferStations", LogLevel.Trace);
        }

        public static float GetDistanceToStation(Car locomotive, PassengerStop station)
        {
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} getting distance to destination", locomotive.DisplayName),LogLevel.Trace);

            Graph trackGraph = Graph.Shared;
            float shortestDistance = float.MaxValue;
            Car centerCar = LocoTelem.CenterCar[locomotive];
            centerCar.GetCenterPosition(trackGraph);

            //Check all tracks associated with a station
            foreach (TrackSpan trackSpan in station.TrackSpans)
            {
                if (trackSpan.lower.HasValue)
                {
                    Location trackSpanLocation = (Location)trackSpan.lower;
                    float trackHalfLength = trackSpan.Length / 2;

                    float distanceA = trackGraph.FindDistance(centerCar.LocationA, trackSpanLocation) + trackHalfLength;
                    float distanceB = trackGraph.FindDistance(centerCar.LocationB, trackSpanLocation) + trackHalfLength;

                    float furthestCarEdgeDistance = Math.Max(distanceA, distanceB);
                    float straightLineDistance = Vector3.Distance(centerCar.GetCenterPosition(trackGraph), trackSpan.GetCenterPoint());

                    //Once close enough, swap to using straight line distance since TrackSpan distance only goes from the edge of the car to the edge of the span
                    if (straightLineDistance < trackSpan.Length)
                    {
                        shortestDistance = Math.Min(shortestDistance, straightLineDistance);
                    }
                    else
                    {
                        shortestDistance = Math.Min(shortestDistance, furthestCarEdgeDistance);
                    }
                }
                else
                {
                    throw new ArgumentNullException($"Unable to calculate distance to {station.identifier} due to a null track span");
                }
            }

            return shortestDistance;
        }


        /**************************************************************************************************************************
         * 
         * 
         * 
         * 
         *                                              UI HELPER METHODS BELOW THIS POINT
         * 
         * 
         * 
         * 
         * 
         ***************************************************************************************************************************/


        //Determine if station is selected
        public static bool IsStopStationSelected(PassengerStop stop, Car locomotive)
        {
            //Trace Function
            //RouteManager.logger.LogToDebug("ENTERED FUNCTION: IsStopStationSelected", LogLevel.Trace);

            bool result = LocoTelem.UIStopStationSelections[locomotive].TryGetValue(stop.identifier, out bool isSelected) && isSelected;

            //Trace Function
            //RouteManager.logger.LogToDebug("EXITING FUNCTION: IsStopStationSelected", LogLevel.Trace);
            return result;
        }

        //Update station selection
        public static void SetStopStationSelected(PassengerStop stop, Car locomotive, bool isSelected)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetStopStationSelected", LogLevel.Trace);

            LocoTelem.UIStopStationSelections[locomotive][stop.identifier] = isSelected;

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetStopStationSelected", LogLevel.Trace);
        }

        //Determine if station is selected
        public static bool IsPickupStationSelected(PassengerStop stop, Car locomotive)
        {
            //Trace Function
            //RouteManager.logger.LogToDebug("ENTERED FUNCTION: IsPickupStationSelected", LogLevel.Trace);

            bool result = LocoTelem.UIPickupStationSelections[locomotive].TryGetValue(stop.identifier, out bool isSelected) && isSelected;

            //Trace Function
            //RouteManager.logger.LogToDebug("EXITING FUNCTION: IsPickupStationSelected", LogLevel.Trace);
            return result;
        }

        //Update station selection
        public static void SetPickupStationSelected(PassengerStop stop, Car locomotive, bool isSelected)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetPickupStationSelected", LogLevel.Trace);

            LocoTelem.UIPickupStationSelections[locomotive][stop.identifier] = isSelected;

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetPickupStationSelected", LogLevel.Trace);
        }

        //Determine if station is selected
        public static PassengerStop IsTransferStationSelected(PassengerStop transferFrom, Car locomotive)
        {
            //Trace Function
            //RouteManager.logger.LogToDebug("ENTERED FUNCTION: IsPickupStationSelected", LogLevel.Trace);
            PassengerStop transferTo;

            bool result = LocoTelem.UITransferStationSelections[locomotive].TryGetValue(transferFrom.identifier, out transferTo);

            //Trace Function
            //RouteManager.logger.LogToDebug("EXITING FUNCTION: IsPickupStationSelected", LogLevel.Trace);
            return transferTo;
        }

        //Update station selection
        public static void SetTransferStationSelected(PassengerStop transferFrom, Car locomotive, PassengerStop transferTo)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetPickupStationSelected", LogLevel.Trace);

            LocoTelem.UITransferStationSelections[locomotive][transferFrom.identifier] = transferTo;

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetPickupStationSelected", LogLevel.Trace);
        }






        //Check if Consist has any stations enabled
        public static bool IsAnyStationSelectedForLocomotive(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: IsAnyStationSelectedForLocomotive", LogLevel.Trace);

            // Check if the locomotive exists in the stopStations dictionary
            if (LocoTelem.stopStations.TryGetValue(locomotive, out List<PassengerStop> stopStations))
            {
                // Return true if there is at least one selected station
                return stopStations.Any();
            }

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: IsAnyStationSelectedForLocomotive", LogLevel.Trace);

            // Return false if the locomotive is not found or no stations are selected
            return false;
        }

        //Setup create station selection list of current consist.
        public static void InitializeStationSelectionForLocomotive(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: InitializeStationSelectionForLocomotive", LogLevel.Trace);

            //TODO: Check if this is a stop or pickup station
            if (!LocoTelem.UIStopStationSelections.ContainsKey(locomotive))
            {
                var stationSelectionsForLocomotive = new Dictionary<string, bool>();
                var allStops = PassengerStop.FindAll();

                foreach (var stop in allStops)
                {
                    stationSelectionsForLocomotive[stop.identifier] = false;
                }

                //TODO: Check if this is a stop or pickup station
                LocoTelem.UIStopStationSelections[locomotive] = stationSelectionsForLocomotive;
                LocoTelem.UIPickupStationSelections[locomotive] = new Dictionary<string, bool>();//stationSelectionsForLocomotive;
                LocoTelem.UITransferStationSelections[locomotive] = new Dictionary<string, PassengerStop>();
            }

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: InitializeStationSelectionForLocomotive", LogLevel.Trace);
        }
    }
}