using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;
using RouteManager.v2.Logging;
using Network;

namespace RouteManager.v2.core
{
    public static class DestinationManager
    {
        public static readonly List<string> orderedStations = new List<string>
        {
            "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
            "almond", "nantahala", "topton", "rhodo", "andrews"
        };

        public static readonly List<string> orderedStations_dev = new List<string>
        {
            "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka", "cochran",
            "almond", "nantahala", "topton", "rhodo", "andrews"
        };


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

            //Todo: use end of train in direction of travel, rather than locomotive
            Car centerCar = LocoTelem.CenterCar[locomotive];
            //centerCar.GetCenterPosition(trackGraph);

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

        //Returns True if node is a switch
        //Returns False if node is not a switch
        //switchNormal will be true if the traversal is the normal path, false if it's the reverse path and null if not traversable
        //  i.e. moving from normal to reversed branch
        public static bool PathIsNormal(TrackSegment from, TrackSegment to, out TrackNode? trackSwitch, out bool? switchNormal)
        {
            trackSwitch = GetCommonNode(from,to) ?? throw new Exception($"No common node for From: {from.id}, To: {to.id}");


            RouteManager.logger.LogToDebug($"PathIsNormal: From: {from?.id}, {from?.name}, To: {to?.id}, {to?.name}, Node: {trackSwitch?.id}, {trackSwitch?.name}", LogLevel.Verbose);

            bool result = Graph.Shared.DecodeSwitchAt(trackSwitch, out TrackSegment enter, out TrackSegment normal, out TrackSegment reversed);

            if (result)
            {
                //we have switch, determine if we can go between from and to
                if (from == enter && to == normal || from == normal && to == enter)
                {
                    switchNormal = true;
                }
                else if (from == enter && to == reversed || from == reversed && to == enter)
                {
                    switchNormal = false;
                }
                else
                {
                    RouteManager.logger.LogToError($"PathIsNormal: Unable to traverse switch");
                    RouteManager.logger.LogToDebug($"PathIsNormal: Result: {result}, Enter: {enter?.id}, Normal: {normal?.id}, Reversed: {reversed?.id}", LogLevel.Verbose);

                    switchNormal = null;
                }
            }
            else
            {
                //not a switch
                switchNormal = null;
                return false;
            }

            return true;
        }

        public static TrackNode? GetCommonNode(TrackSegment a, TrackSegment b)
        {

            if (a == null || b == null)
                return null;

            if (a.a == b.a || a.a == b.b) {
                return a.a;
            }
            else if (a.b == b.a || a.b == b.b)
            {
                return a.b;
            }

            return null;
        }

        public static bool GetRouteSwitches(Location start, Location destination, out List<RouteSwitchData> switchRequirements)
        {
            switchRequirements = new List<RouteSwitchData>();

            //alarka hack - if Alarka is on our list, we need to use this as our final destination first and plan the route in 2 segments
            //will need to be updated to make a general solution for branch lines

            //TODO: put hack in
            //PassengerStop alarka = PassengerStop.FindAll().Where(stop => stop.identifier == "alarka").First();
            //(Track.Location)alarka.TrackSpans.First().lower
            //RouteManager.logger.LogToDebug($"Finding Route to Alarka (segments) {loco.DisplayName} to {alarka?.name}...");
            //RouteManager.logger.LogToDebug($"Current Location F: {loco.LocationF}, Location A: {loco.LocationA}, Location B: {loco.LocationB}");
            // 

            List<TrackSegment> segmentSteps = Graph.Shared.FindRoute(start, destination);

            RouteManager.logger.LogToDebug($"Route found: {segmentSteps.Count} steps:");

            for (int i = 0; i < segmentSteps.Count - 1; i++)
            {
                TrackSegment seg = segmentSteps[i];
                TrackSegment segNext = segmentSteps[i + 1];

                bool? requiredSwitchState;
                bool isSwitch = DestinationManager.PathIsNormal(seg, segNext, out TrackNode trackSwitch, out requiredSwitchState);

                if (isSwitch && requiredSwitchState != null)
                {
                    //RouteManager.logger.LogToDebug($"\r\nSeg.a: {seg.a.id}, {seg.a.name}\r\nSeg.b: {seg.b.id}, {seg.b.name}\r\nSegNext.a: {segNext.a.id}, {segNext.a.name}\r\nSegNext.b: {segNext.b.id}, {segNext.b.name}", LogLevel.Debug);
                    //RouteManager.logger.LogToDebug($"\t\t\tSegment: {seg?.id}, {seg?.name}, {seg?.trackClass}, Node A: {seg?.a.name}, Node B: {seg?.b.name}, Desired switch pos normal: {requiredSwitchState}", LogLevel.Debug);

                    switchRequirements.Add(new RouteSwitchData(trackSwitch, seg, segNext, (bool)requiredSwitchState));
                }
                else if (isSwitch && requiredSwitchState == null)
                {
                    RouteManager.logger.LogToError("Unable to resolve path!");
                    return false;
                }

            }

            return true;
        }

        public static float GetDistanceToSwitch(Car locomotive, RouteSwitchData trackSwitch)
        {
            RouteManager.logger.LogToDebug($"Loco: {locomotive.DisplayName} getting distance to switch {trackSwitch.trackSwitch?.id}", LogLevel.Trace);

            Graph trackGraph = Graph.Shared;

            //use end of train, rather than centre car
            Car leading = TrainManager.GetLeadingEnd(locomotive);


            Location swLocation = new Location(trackSwitch.segmentFrom, 0f, trackSwitch.segmentFrom.EndForNode(trackSwitch.trackSwitch));

            float distanceA = trackGraph.FindDistance(leading.LocationA, swLocation);
            float distanceB = trackGraph.FindDistance(leading.LocationB, swLocation);
            float closestEndDistance = Math.Min(distanceA, distanceB);

            float straightLineDistance = Vector3.Distance(leading.GetCenterPosition(trackGraph), swLocation.GetPosition());
            Vector3 heading = swLocation.GetPosition() - leading.GetCenterPosition(trackGraph);

            RouteManager.logger.LogToDebug($"Straight Line: {straightLineDistance}, Heading: {heading}, Heading.mag: {heading.magnitude}, Direction: {heading/heading.magnitude}");
            RouteManager.logger.LogToDebug($"Heading sign: {(heading.x >0 && (heading.x > -heading.y && heading.x<heading.y))}");

            //Once close enough, swap to using straight line distance since TrackSpan distance only goes from the edge of the car to the edge of the span
            if (closestEndDistance <= 100 )
            {
                Vector3 direction = heading / heading.magnitude;
                return straightLineDistance * (direction.x <0 ? 1: -1);
            }

            return closestEndDistance;
        }

        public static void PlanNextRoute(Location start,PassengerStop nextStation, ref List<RouteSwitchData> mainRoute)
        {
            TrackSpan[] tracks = nextStation.TrackSpans.ToArray();
            
            if (tracks.Length > 1)
            {
                PlanNextRoute(start, (Location)tracks[1].lower, ref mainRoute, out RouteSwitchData commonPoint);
            }
        }

        public static bool PlanNextRoute(Location start, Location nextPlatform, ref List<RouteSwitchData> mainRoute, out RouteSwitchData commonPoint)
        {
            commonPoint = null;

            List<RouteSwitchData> requirementsP2;
            if (GetRouteSwitches(start, nextPlatform, out requirementsP2))
            {
                //found a route to second platform
                //find last common node
                commonPoint = mainRoute.Intersect(requirementsP2, new RouteSwitchDataComparer()).Last();
                if (commonPoint != null)
                {
                    //common point between leaving the station and the current main route
                    commonPoint.isRoutable = true;
                    //new route from P2
                    mainRoute = requirementsP2;
                }
                else
                {
                    //routes don't intersect
                    RouteManager.logger.LogToDebug("No intersection of routes to second platform!", LogLevel.Debug);
                    return false;
                }
            }
            return true;
        }

        public static bool PlanRouteDeviation(ref List<RouteSwitchData> mainRoute, RouteSwitchData nextSwitch, Location current, Location nextPlatform)
        {

            //can we get to the next platform?
            if (DestinationManager.PlanNextRoute(current, nextPlatform, ref mainRoute, out RouteSwitchData commonPoint1))
            {
                //find the last location on the mainRoute
                Location finalDestination = new Location(mainRoute.Last().segmentTo, 0, mainRoute.Last().segmentTo.EndForNode(mainRoute.Last().trackSwitch));

                List<RouteSwitchData> newRoute = new List<RouteSwitchData>(mainRoute); 
                //can we get from the next platform to the final destination
                if (DestinationManager.PlanNextRoute(nextPlatform, finalDestination, ref newRoute, out RouteSwitchData commonPoint2))
                {
                    //we can get in and out, without breaking our route, lets update the route

                    //flip the current switch requirements at our common switches
                    commonPoint1.requiredStateNormal = !commonPoint1.requiredStateNormal;
                    commonPoint2.requiredStateNormal = !commonPoint2.requiredStateNormal;

                    //merge the two routes
                    int index1 = mainRoute.IndexOf(commonPoint1);
                    int index2 = mainRoute.IndexOf(commonPoint2);

                    int indexNewRoute = newRoute.IndexOf(commonPoint2);

                    index1++;

                    if(index1 != index2) { 
                        //remove all elements between common points
                        mainRoute.RemoveRange(index1, index2 - index1);

                        //insert any new elements
                        mainRoute.InsertRange(index1, newRoute.Take(indexNewRoute));
                    }

                    return true;
                }
            }
            
            
            //can't deviate
            return false;
            
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

            bool result = LocoTelem.UITransferStationSelections[locomotive].TryGetValue(transferFrom.identifier, out PassengerStop transferTo);

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