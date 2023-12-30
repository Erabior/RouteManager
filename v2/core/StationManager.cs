using JetBrains.Annotations;
using Model;
using RollingStock;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
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

        public static (PassengerStop,float) GetClosestStation(Car locomotive)
        {
            //Trace Logging
            Logger.LogToDebug("ENTERED FUNCTION: GetClosestStation", Logger.logLevel.Trace);

            //Debugging Output
            Logger.LogToDebug(String.Format("Loco {0} calculating closest station...",locomotive.DisplayName), Logger.logLevel.Debug);

            // Initialize variables;
            PassengerStop closestStation = null;
            float closestDistance = float.MaxValue;
            Graph graph = Graph.Shared;


            //Get Locomotive Center on the map
            Vector3? locoMotivePosition = locomotive.GetCenterPosition(graph);

            // If centerpoint is null then bail
            if (locoMotivePosition == null)
            {
                Logger.LogToError("Could not obtain locomotive's center position.");
                return (null,0);
            }

            //Debugging Output
            Logger.LogToDebug(String.Format("Loco {0} centerpoint {1} has value {2})", locomotive, locoMotivePosition, locoMotivePosition.Value), Logger.logLevel.Verbose);

            // Iterate over each selected station using a for loop
            foreach ( PassengerStop station in UnityEngine.Object.FindObjectsOfType<PassengerStop>())
            {
                Logger.LogToDebug($"Station center was: {station.CenterPoint}",Logger.logLevel.Verbose);
                Logger.LogToDebug($"Loco center was: {locoMotivePosition.Value}", Logger.logLevel.Verbose);

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
            Logger.LogToDebug(String.Format("Loco {0} Closest Station was: {1}", locomotive.DisplayName, closestStation.DisplayName), Logger.logLevel.Debug);

            //Trace Logging
            Logger.LogToDebug("EXITING FUNCTION: GetClosestStation", Logger.logLevel.Trace);

            return (closestStation, closestDistance);
        }



        //Will need adjusting
        public static bool isLocomotiveInStation(Car locomotive)
        {
            if(Vector3.Distance(LocoTelem.closestStation[locomotive].Item1.CenterPoint, locomotive.GetCenterPosition(Graph.Shared)) <= 15f)
            {
                return true;
            }

            return false;
        }

    }
}
