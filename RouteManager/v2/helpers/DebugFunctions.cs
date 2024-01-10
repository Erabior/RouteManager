using Model;
using Model.Definition;
using Model.OpsNew;
using RollingStock;
using RouteManager.v2.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RouteManager.v2.helpers
{
    internal class DebugFunctions
    {

        public static void logAllWaitingPassengers()
        {
            foreach (PassengerStop stop in PassengerStop.FindAll())
            {
                RouteManager.logger.LogToDebug(String.Format("Stop: {0} ", stop.DisplayName));
                foreach (KeyValuePair<string, int> pair in stop.Waiting)
                {
                    RouteManager.logger.LogToDebug(String.Format("\t Has {0} Passengers for {1}", pair.Value, pair.Key));
                }
            }
        }

        public static void TestLoadInfo(Car locomotive, string loadIdentifier)
        {

            //Trace Function
            RouteManager.logger.LogToDebug("ENTERED FUNCTION: TestLoadInfo", LogLevel.Trace);

            int slotIndex;
            if (loadIdentifier == "diesel-fuel")
            {

                CarLoadInfo? loadInfo = locomotive.GetLoadInfo(loadIdentifier, out slotIndex);

                if (loadInfo.HasValue)
                {
                    RouteManager.logger.LogToDebug($"Load Identifier: {loadIdentifier}",LogLevel.Debug);
                    RouteManager.logger.LogToDebug($"Slot Index: {slotIndex}", LogLevel.Debug);
                    RouteManager.logger.LogToDebug($"Value: {loadInfo.Value}", LogLevel.Debug);
                    RouteManager.logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}", LogLevel.Debug);
                    // Add more details you wish to log
                    return;
                }
                else
                {
                    RouteManager.logger.LogToDebug($"No load information found for {loadIdentifier}.", LogLevel.Debug);
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
                        RouteManager.logger.LogToDebug($"Load Identifier: {loadIdentifier}", LogLevel.Debug);
                        RouteManager.logger.LogToDebug($"Slot Index: {slotIndex}", LogLevel.Debug);
                        RouteManager.logger.LogToDebug($"Value: {loadInfo.Value}", LogLevel.Debug);
                        RouteManager.logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}", LogLevel.Debug);
                        // Add more details you wish to log
                    }
                    else
                    {
                        RouteManager.logger.LogToDebug($"No load information found for {loadIdentifier}.", LogLevel.Debug);
                    }
                }
                else
                {
                    RouteManager.logger.LogToDebug($"No Tender found for {loadIdentifier}.", LogLevel.Debug);
                }
            }

            //Trace Function
            RouteManager.logger.LogToDebug("EXITING FUNCTION: TestLoadInfo", LogLevel.Trace);
        }
    }
}
