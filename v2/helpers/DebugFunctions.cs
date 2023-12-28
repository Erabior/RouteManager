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
        public static void TestLoadInfo(Car locomotive, string loadIdentifier)
        {
            int slotIndex;


            if (loadIdentifier == "diesel-fuel")
            {

                CarLoadInfo? loadInfo = locomotive.GetLoadInfo(loadIdentifier, out slotIndex);

                if (loadInfo.HasValue)
                {
                    Logger.LogToDebug($"Load Identifier: {loadIdentifier}");
                    Logger.LogToDebug($"Slot Index: {slotIndex}");
                    Logger.LogToDebug($"Value: {loadInfo.Value}");
                    Logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}");
                    // Add more details you wish to log
                    return;
                }
                else
                {
                    Logger.LogToDebug($"No load information found for {loadIdentifier}.");
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
                        Logger.LogToDebug($"Load Identifier: {loadIdentifier}");
                        Logger.LogToDebug($"Slot Index: {slotIndex}");
                        Logger.LogToDebug($"Value: {loadInfo.Value}");
                        Logger.LogToDebug($"Quantity: {loadInfo.Value.Quantity}");
                        // Add more details you wish to log
                    }
                    else
                    {
                        Logger.LogToDebug($"No load information found for {loadIdentifier}.");
                    }
                }
                else
                {
                    Logger.LogToDebug($"No Tender found for {loadIdentifier}.");
                }
            }
        }


        public static void PrintCarInfo(Car car)
        {
            var graph = Graph.Shared;
            if (car == null)
            {
                Logger.LogToDebug("Car is null");
                return;
            }

            // Retrieve saved stations for this car from ManagedTrains
            if (LocoTelem.SelectedStations.TryGetValue(car, out List<PassengerStop> selectedStations))
            {
                string stationNames = string.Join(", ", selectedStations.Select(s => s.name));
                Vector3? centerPoint = car.GetCenterPosition(graph); // Assuming GetCenterPosition exists

                Logger.LogToDebug($"Car ID: {car.id}, Selected Stations: {stationNames}, Center Position: {centerPoint}");
            }
            else
            {
                Logger.LogToDebug("No stations selected for this car.");
            }


            if (LocoTelem.LocomotiveDestination.TryGetValue(car, out string dest))
            {

                Logger.LogToDebug($"destination: {dest}");
            }
            else
            {
                Logger.LogToDebug("No destination for this car.");
            }

            if (graph == null)
            {
                Logger.LogToError("Graph object is null");
                return; // or handle this case as needed
            }

            if (car == null)
            {
                Logger.LogToError("Car object is null");
                return; // or handle this case as needed
            }

            var locationF = car.LocationF;
            var locationR = car.LocationR;
            var direction = car.GetCenterRotation(graph);
            Logger.LogToDebug($"LocationF {locationF} LocationR {locationR} Rotation: {direction}");

            if (LocoTelem.LocomotivePrevDestination.TryGetValue(car, out string prevDest))
            {
                Logger.LogToDebug($"Previous destination: {prevDest}");
            }
            else
            {
                Logger.LogToDebug("No previous destination for this car.");
            }
            if (LocoTelem.TransitMode.TryGetValue(car, out bool inTransitMode))
            {
                Logger.LogToDebug($"Transit Mode: {inTransitMode}");
            }
            else
            {
                Logger.LogToDebug("No Transit Mode recorded for this car.");
            }
            if (LocoTelem.LineDirectionEastWest.TryGetValue(car, out bool isEastWest))
            {
                Logger.LogToDebug($"Line Direction East/West: {isEastWest}");
            }
            else
            {
                Logger.LogToDebug("No Line Direction East/West recorded for this car.");
            }
            if (LocoTelem.DriveForward.TryGetValue(car, out bool driveForward))
            {
                Logger.LogToDebug($"Drive Forward: {driveForward}");
            }
            else
            {
                Logger.LogToDebug("No Drive Forward recorded for this car.");
            }
            if (LocoTelem.locomotiveCoroutines.TryGetValue(car, out bool coroutineExists))
            {
                Logger.LogToDebug($"Locomotive Coroutine Exists: {coroutineExists}");
            }
            else
            {
                Logger.LogToDebug("No Locomotive Coroutine recorded for this car.");
            }
            if (LocoTelem.CenterCar.TryGetValue(car, out Car centerCar))
            {
                Logger.LogToDebug($"Center Car: {centerCar}");
            }
            else
            {
                Logger.LogToDebug("No Center Car recorded for this car.");
            }
            try
            {
                LocoTelem.CenterCar[car] = ManagedTrains.GetCenterCoach(car);
                Logger.LogToDebug($"center car for {car}: {LocoTelem.CenterCar[car]}");
            }
            catch (Exception ex)
            {
                Logger.LogToDebug($"could not get center car: {ex}");
            }
            var Locovelocity = car.velocity;
            Logger.LogToDebug($"Current Speed: {Locovelocity}");

            var cars = car.EnumerateCoupled().ToList();

            foreach (var trainCar in cars)
            {
                Logger.LogToDebug($"{trainCar.Archetype}");
            }

            TestLoadInfo(car, "water");

            TestLoadInfo(car, "coal");

            TestLoadInfo(car, "diesel-fuel");

        }
    }
}
