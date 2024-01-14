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
        public static readonly List<string> orderedStations = new List<string>
        {
            "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
            "almond", "nantahala", "topton", "rhodo", "andrews"
        };

        //Update the list of stations to stop at.
        public static void SetSelectedStations(Car car, List<PassengerStop> selectedStops)
        {
            //Trace Logging
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetSelectedStations", LogLevel.Trace);

            //Something went wrong
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            //Uppdate consists's station list
            LocoTelem.SelectedStations[car] = selectedStops;

            //Trace Logging
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetSelectedStations", LogLevel.Trace);
        }

        public static float GetDistanceToStation(Car locomotive, PassengerStop station)
        {
            RouteManager.logger.LogToDebug(String.Format("Loco: {0} getting distance to destination", locomotive.DisplayName));

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
        public static bool IsStationSelected(PassengerStop stop, Car locomotive)
        {
            //Trace Function
            //RouteManager.logger.LogToDebug("ENTERED FUNCTION: IsStationSelected", LogLevel.Trace);

            bool result = LocoTelem.UIStationSelections[locomotive].TryGetValue(stop.identifier, out bool isSelected) && isSelected;

            //Trace Function
            //RouteManager.logger.LogToDebug("EXITING FUNCTION: IsStationSelected", LogLevel.Trace);
            return result;
        }

        //Update station selection
        public static void SetStationSelected(PassengerStop stop, Car locomotive, bool isSelected)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: SetStationSelected", LogLevel.Trace);

            LocoTelem.UIStationSelections[locomotive][stop.identifier] = isSelected;

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: SetStationSelected", LogLevel.Trace);
        }

        //Check if Consist has any stations enabled
        public static bool IsAnyStationSelectedForLocomotive(Car locomotive)
        {
            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: IsAnyStationSelectedForLocomotive", LogLevel.Trace);

            // Check if the locomotive exists in the SelectedStations dictionary
            if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations))
            {
                // Return true if there is at least one selected station
                return selectedStations.Any();
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

            if (!LocoTelem.UIStationSelections.ContainsKey(locomotive))
            {
                var stationSelectionsForLocomotive = new Dictionary<string, bool>();
                var allStops = PassengerStop.FindAll();

                foreach (var stop in allStops)
                {
                    stationSelectionsForLocomotive[stop.identifier] = false;
                }

                LocoTelem.UIStationSelections[locomotive] = stationSelectionsForLocomotive;
            }

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: InitializeStationSelectionForLocomotive", LogLevel.Trace);
        }
    }
}