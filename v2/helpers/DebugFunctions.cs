using Model;
using Model.Definition;
using Model.OpsNew;
using RollingStock;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Track;
using UnityEngine;
using Logger = RouteManager.v2.Logging.Logger;

namespace RouteManager.v2.helpers
{
    internal class DebugFunctions
    {

        public static void logAllWaitingPassengers()
        {
            foreach (PassengerStop stop in PassengerStop.FindAll())
            {
                Logger.LogToDebug(String.Format("Stop: {0} ", stop.DisplayName));
                foreach (KeyValuePair<string, int> pair in stop.Waiting)
                {
                    Logger.LogToDebug(String.Format("\t Has {0} Passengers for {1}", pair.Value, pair.Key));
                }
            }
        }

        public static void TestLoadInfo(Car locomotive, string loadIdentifier)
        {

            //Trace Function
            Logger.LogToDebug("ENTERED FUNCTION: TestLoadInfo", Logger.logLevel.Trace);

            int slotIndex;
            if (loadIdentifier == "diesel-fuel")
            {

                CarLoadInfo? loadInfo = locomotive.GetLoadInfo(loadIdentifier, out slotIndex);

                if (loadInfo.HasValue)
                {
                    Logger.LogToDebug($"Load Identifier: {loadIdentifier}",Logger.logLevel.Debug);
                    Logger.LogToDebug($"Slot Index: {slotIndex}", Logger.logLevel.Debug);
                    Logger.LogToDebug($"Value: {loadInfo.Value}", Logger.logLevel.Debug);
                    Logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}", Logger.logLevel.Debug);
                    // Add more details you wish to log
                    return;
                }
                else
                {
                    Logger.LogToDebug($"No load information found for {loadIdentifier}.", Logger.logLevel.Debug);
                    return;
                }

            }

            var cars = locomotive.EnumerateCoupled().ToList();
            foreach (var trainCar in cars)
            {
                if (trainCar.Archetype == CarArchetype.Tender)
                {
                    Car Tender = trainCar;
                    CarLoadInfo? loadInfo = Tender.GetLoadInfo(loadIdentifier, out slotIndex);

                    if (loadInfo.HasValue)
                    {
                        Logger.LogToDebug($"Load Identifier: {loadIdentifier}", Logger.logLevel.Debug);
                        Logger.LogToDebug($"Slot Index: {slotIndex}", Logger.logLevel.Debug);
                        Logger.LogToDebug($"Value: {loadInfo.Value}", Logger.logLevel.Debug);
                        Logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}", Logger.logLevel.Debug);
                        // Add more details you wish to log
                    }
                    else
                    {
                        Logger.LogToDebug($"No load information found for {loadIdentifier}.", Logger.logLevel.Debug);
                    }
                }
                else
                {
                    Logger.LogToDebug($"No Tender found for {loadIdentifier}.", Logger.logLevel.Debug);
                }
            }

            //Trace Function
            Logger.LogToDebug("EXITING FUNCTION: TestLoadInfo", Logger.logLevel.Trace);
        }
    }
}
