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

        public static float GetDistanceToDest(Car locomotive)
        {
            Logger.LogToDebug(String.Format("Loco: {0} getting distance to destination", locomotive.DisplayName));

            // Check if the locomotive is null
            if (locomotive == null)
            {
                Logger.LogToError("Locomotive is null in GetDistanceToDest.");
                return -6969; // Return a default value or handle this case as needed
            }

            // Check if the locomotive key exists in the LocomotiveDestination dictionary
            if (!LocoTelem.currentDestination.ContainsKey(locomotive))
            {
                Logger.LogToError($"LocomotiveDestination does not contain key: {locomotive}");
                LocoTelem.currentDestination[locomotive] = StationManager.GetClosestStation(locomotive).Item1;

                if (!LocoTelem.currentDestination.ContainsKey(locomotive))
                {
                    return -6969f; // Or handle this scenario appropriately
                }

            }
            Vector3 locomotivePosition = new Vector3();
            string destination = LocoTelem.currentDestination[locomotive].identifier;

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
