using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Track;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2.core
{
    public static class StationManager
    {
        private static readonly List<string> orderedStations = new List<string>
        {
            "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
            "almond", "nantahala", "topton", "rhodo", "andrews"
        };

        //Update the list of stations to stop at.
        public static void SetSelectedStations(Car car, List<PassengerStop> selectedStops)
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: SetSelectedStations", Logger.logLevel.Trace);

            //Something went wrong
            if (car == null)
            {
                throw new ArgumentNullException(nameof(car));
            }

            //Uppdate consists's station list
            LocoTelem.SelectedStations[car] = selectedStops;

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: SetSelectedStations", Logger.logLevel.Trace);
        }

        //Determine if the current destination is a selected station.
        public static bool IsCurrentDestinationSelected(Car locomotive)
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: IsCurrentDestinationSelected", Logger.logLevel.Trace);

            if (LocoTelem.LocomotiveDestination.TryGetValue(locomotive, out string currentDestination))
            {
                if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations))
                {
                    return selectedStations.Any(station => station.identifier == currentDestination);
                }
            }

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: IsCurrentDestinationSelected", Logger.logLevel.Trace);

            return false;
        }

        public static string GetClosestSelectedStation(Car locomotive)
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: GetClosestSelectedStation", Logger.logLevel.Trace);

            Graph graph = Graph.Shared;

            //Get Locomotive Center on the map
            Vector3? centerPoint = locomotive.GetCenterPosition(graph);
            Logger.LogToDebug(String.Format("Loco {0} centerpoint {1} has value {2})",locomotive, centerPoint, centerPoint.Value),Logger.logLevel.Debug);

            // If centerpoint is null then bail
            if (centerPoint == null)
            {
                Logger.LogToError("Could not obtain locomotive's center position.");
                return null;
            }

            //IF no stations are selected, bail
            if (!LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations) || selectedStations.Count == 0)
            {
                Logger.LogToDebug("No stations selected for this locomotive.", Logger.logLevel.Debug);
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
                if (StationInformation.Stations.TryGetValue(station.identifier, out StationMapData stationData))
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

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: GetClosestSelectedStation", Logger.logLevel.Trace);

            return closestStationName;
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
            Logger.LogToDebug("ENTERED FUNCTION: IsStationSelected", Logger.logLevel.Trace);

            bool result =  LocoTelem.UIStationSelections[locomotive].TryGetValue(stop.identifier, out bool isSelected) && isSelected;

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: IsStationSelected", Logger.logLevel.Trace);
            return result; 
        }

        //Update station selection
        public static void SetStationSelected(PassengerStop stop, Car locomotive, bool isSelected)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: SetStationSelected", Logger.logLevel.Trace);

            LocoTelem.UIStationSelections[locomotive][stop.identifier] = isSelected;

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: SetStationSelected", Logger.logLevel.Trace);
        }

        //Check if Consist has any stations enabled
        public static bool IsAnyStationSelectedForLocomotive(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: IsAnyStationSelectedForLocomotive", Logger.logLevel.Trace);

            // Check if the locomotive exists in the SelectedStations dictionary
            if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations))
            {
                // Return true if there is at least one selected station
                return selectedStations.Any();
            }

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: IsAnyStationSelectedForLocomotive", Logger.logLevel.Trace);

            // Return false if the locomotive is not found or no stations are selected
            return false;
        }

        //Setup create station selection list of current consist.
        public static void InitializeStationSelectionForLocomotive(Car locomotive)
        {
            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: InitializeStationSelectionForLocomotive", Logger.logLevel.Trace);

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
            Logger.LogToDebug("EXITING FUNCTION: InitializeStationSelectionForLocomotive", Logger.logLevel.Trace);
        }
    }
}
