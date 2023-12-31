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
                        Logger.LogToDebug($"Reached {currentStation} and is the end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex - 1]}");
                        TrainManager.CopyStationsFromLocoToCoaches(locomotive);

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
                        Logger.LogToDebug($"Reached {currentStation} and is the end of the line. Reversing travel direction back to {orderedSelectedStations[currentIndex + 1]}");

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
                //if current station is not in selected stations
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
            if (!StationInformation.Stations.ContainsKey(destination))
            {
                Logger.LogToError($"Station not found for destination: {destination}");
                return -6969f; // Handle missing station
            }

            Vector3 destCenter = StationInformation.Stations[destination].Center;
            Vector3 destCentern = StationInformation.Stations["alarkajctn"].Center;

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
