using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UI;
using RollingStock;
using System.Collections.Generic;
using System;
using UI.Builder;
using UI.CarInspector;
using Game.Messages;
using Game.State;
using Model.AI;
using System.Linq;
using Model;
using System.Reflection;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Game;
using Helpers;
using Serilog;
using Track;
using System.Collections;
using static ManagedTrains;
using System.Security.Cryptography;
using Character;

/*
 * I need to create an class containigng an object and method that can be used to pass the car object of controlled locomotives to the already started coroutine
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */













namespace RouteManager
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Dispatcher : BaseUnityPlugin
    {
        private const string modGUID = "Erabior.Dispatcher";
        private const string modName = "Dispatcher";
        private const string modVersion = "1.0.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource mls;

        void Awake()
        {
            harmony.PatchAll();
            mls = Logger;
            mls.LogInfo("Dispatcher Mod Loaded - Awake method called.");
            

        }
        

    }

    // Separate MonoBehaviour class for key press logging
    public class KeyPressLogger : MonoBehaviour
    {
        void Update()
        {
            if (Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(keyCode))
                    {
                        Debug.Log("Key Pressed: " + keyCode);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(FlarePickable), nameof(FlarePickable.Configure))]
    internal static class flarePickable
    {
        private static void Postfix(FlarePickable __instance)
        {
            Log.Information("Flare {FlareId} is at {Position}", __instance.FlareId, WorldTransformer.WorldToGame(__instance.transform.position));
        }
    }




    [HarmonyPatch(typeof(PlayerController), "Update")]
    public class RouteAI : MonoBehaviour
    {
        void Postfix()
        {
            if (LocoTelem.locomotiveCoroutines.Count >= 1)
            {
                Debug.Log("There is data in locomotiveCoroutines");
                var keys = LocoTelem.locomotiveCoroutines.Keys.ToArray();

                for (int i = 0; i < keys.Count(); i++)
                {
                    if (!LocoTelem.locomotiveCoroutines[keys[i]])
                    {
                        LocoTelem.locomotiveCoroutines[keys[i]] = true;
                        StartCoroutine(AutoEngineerControlRoutine(keys[i]));
                    }
                }
            }
            else
            {
                Debug.Log("No key in locomotiveCoroutines: there are no locomotives that require the extended logic");
            }

        }

        private IEnumerator AutoEngineerControlRoutine(Car locomotive)
        {

            ManagedTrains.GetNextDestination(locomotive);

            Debug.Log($"Entered Coroutine for {locomotive.id} - is any station selected {StationManager.IsAnyStationSelectedForLocomotive(locomotive)}");

            bool transitMode = true;
            bool loadingMode = false;
            float RMmaxSpeed = 0;

            while (StationManager.IsAnyStationSelectedForLocomotive(locomotive))
            {
                if (transitMode)
                {
                    Debug.Log("14");
                    while (transitMode)
                    {
                        float distanceToStation = ManagedTrains.GetDistanceToDest(locomotive);
                        Debug.Log($"15 distance to station: {distanceToStation}");
                        if (distanceToStation > 125)
                        {
                            yield return new WaitForSeconds(20);
                            RMmaxSpeed = 45;
                        }
                        else if (distanceToStation <= 125 && distanceToStation > 25)
                        {
                            yield return new WaitForSeconds(2);
                            RMmaxSpeed = distanceToStation / 3f;
                        }
                        else if (distanceToStation <= 25 && distanceToStation > 2.5)
                        {
                            yield return new WaitForSeconds(0.5f);
                            RMmaxSpeed = distanceToStation / 3f;
                        }
                        else if (distanceToStation <= 2.5)
                        {
                            RMmaxSpeed = 0;
                            loadingMode = true;
                            transitMode = false;
                            break; // Exit the while loop
                        }
                        Debug.Log($"16 speed: {RMmaxSpeed}");
                        int RMmaxSpeedint = (int)RMmaxSpeed;
                        StateManager.ApplyLocal(new AutoEngineerCommand(locomotive.id, AutoEngineerMode.Road, LocoTelem.LocomotiveDirections[locomotive], RMmaxSpeedint, null));
                    }
                }

                if (loadingMode)
                {
                    yield return new WaitForSeconds(20);
                }


                yield return null; // This ensures the coroutine yields properly
            }


        }

    }

    public static class TrainControllerPatch
    {
        public static Vector3? GetCenterPointOfCar(TrainController trainController, string carId)
        {
            if (trainController == null) return null;

            var carLookupField = AccessTools.Field(typeof(TrainController), "_carLookup");
            var carLookup = (Dictionary<string, Car>)carLookupField.GetValue(trainController);

            if (carLookup != null && carLookup.TryGetValue(carId, out Car car))
            {
                return car.GetCenterPosition(trainController.graph);
            }
            else
            {
                return null;
            }
        }
    }

}

public class ManagedTrains : MonoBehaviour
{

    // Rest of your ManagedTrains code...

    public class LocoTelem
    {
        public static Dictionary<Car, float> RMMaxSpeed { get; private set; } = new Dictionary<Car, float>();
        public static Dictionary<Car, bool> TransitMode { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, List<PassengerStop>> SelectedStations { get; private set; } = new Dictionary<Car, List<PassengerStop>>();
        public static Dictionary<Car, bool> LocomotiveDirections { get; private set; } = new Dictionary<Car, bool>();
        public static Dictionary<Car, string> LocomotiveDestination { get; private set; } = new Dictionary<Car, string>();
        public static Dictionary<Car, bool> locomotiveCoroutines { get; private set; } = new Dictionary<Car, bool>();

    }

   
    public static Graph graph { get; set; }

    // A dictionary mapping cars to a list of selected stations.
   


    public static void InitializeLocomotive(Car locomotive)
    {
        if (!LocoTelem.LocomotiveDirections.ContainsKey(locomotive))
        {
            // Default direction is true
            LocoTelem.LocomotiveDirections[locomotive] = true;
        }

        if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
        {
            // Get the first selected station for this locomotive, if any
            if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStops) && selectedStops.Any())
            {
                LocoTelem.LocomotiveDestination[locomotive] = selectedStops.First().identifier;
            }
            else
            {
                LocoTelem.LocomotiveDestination[locomotive] = "whittier"; // No stations selected, set to whittier
            }
        }
    }


    public static void UpdateSelectedStations(Car car, List<PassengerStop> selectedStops)
    {
        if (car == null)
        {
            throw new ArgumentNullException(nameof(car));
        }

        LocoTelem.SelectedStations[car] = selectedStops;
    }


    public static void PrintCarInfo(Car car)
    {
        if (car == null)
        {
            Debug.Log("Car is null");
            return;
        }
        Debug.Log("18");
        // Retrieve saved stations for this car from ManagedTrains
        if (ManagedTrains.LocoTelem.SelectedStations.TryGetValue(car, out List<PassengerStop> selectedStations))
        {
            string stationNames = string.Join(", ", selectedStations.Select(s => s.name));
            Vector3? centerPoint = car.GetCenterPosition(graph); // Assuming GetCenterPosition exists

            Debug.Log($"Car ID: {car.id}, Selected Stations: {stationNames}, Center Position: {centerPoint}");
        }
        else
        {
            Debug.Log("No stations selected for this car.");
        }
        Debug.Log("19");

        if (ManagedTrains.LocoTelem.LocomotiveDestination.TryGetValue(car, out string dest))
        {

            Debug.Log($"destination: {dest}");
        }
        else
        {
            Debug.Log("No destination for this car.");
        }
        Debug.Log("20");
        //Debug.Log($"Rotation: {car.GetCenterRotation(graph)}");
    }


    public static bool IsDirectionForward { get; set; } = true;

    private static readonly List<string> orderedStations = new List<string>
    {
        "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
        "almond", "nantahala", "topton", "rhodo", "andrews"
    };

    // Method to get the next destination station
    public static string GetNextDestination(Car locomotive)
    {
        string currentStation = null;

        if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
        {
            Debug.Log($"LocomotiveDestination does not contain key: {locomotive}");
            currentStation = null;
        }
        else
        {
            currentStation = LocoTelem.LocomotiveDestination[locomotive];
        }
        if (!LocoTelem.LocomotiveDirections.ContainsKey(locomotive))
        {
            LocoTelem.LocomotiveDirections[locomotive] = true;
        }
        bool isForward = LocoTelem.LocomotiveDirections[locomotive];




        if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStops) && selectedStops.Count > 0)
        {
            int currentIndex = orderedStations.IndexOf(currentStation);
            if (currentIndex == -1)
            {
                return selectedStops.First().identifier; // If no current station, return the first selected station
            }

            if (isForward)
            {
                for (int i = currentIndex + 1; i < orderedStations.Count; i++)
                {
                    if (selectedStops.Any(stop => stop.identifier == orderedStations[i]))
                    {
                        return orderedStations[i]; // Return next selected station in forward direction
                    }
                }

                // If at the end of the line, reverse direction
                LocoTelem.LocomotiveDirections[locomotive] = !LocoTelem.LocomotiveDirections[locomotive];
                return GetNextDestination(locomotive);
            }
            else
            {
                for (int i = currentIndex - 1; i >= 0; i--)
                {
                    if (selectedStops.Any(stop => stop.identifier == orderedStations[i]))
                    {
                        return orderedStations[i]; // Return next selected station in reverse direction
                    }
                }

                // If at the beginning of the line, reverse direction
                LocoTelem.LocomotiveDirections[locomotive] = !LocoTelem.LocomotiveDirections[locomotive];
                return GetNextDestination(locomotive);
            }
        }

        return null; // No next destination
    }
    public static float GetDistanceToDest(Car locomotive)
    {
        // Check if the locomotive is null
        if (locomotive == null)
        {
            Debug.LogError("Locomotive is null in GetDistanceToDest.");
            return 0f; // Return a default value or handle this case as needed
        }

        // Check if the locomotive key exists in the LocomotiveDestination dictionary
        if (!LocoTelem.LocomotiveDestination.ContainsKey(locomotive))
        {
            Debug.LogError($"LocomotiveDestination does not contain key: {locomotive}");
            return 0f; // Or handle this scenario appropriately
        }

        string destination = LocoTelem.LocomotiveDestination[locomotive];
        if (destination == null)
        {
            Debug.LogError("Destination is null for locomotive.");
            return 0f; // Handle null destination
        }

        Vector3 locomotivePosition = locomotive.GetCenterPosition(graph);
        if (!StationManager.Stations.ContainsKey(destination))
        {
            Debug.LogError($"Station not found for destination: {destination}");
            return 0f; // Handle missing station
        }

        Vector3 destCenter = StationManager.Stations[destination].Center;
        return Vector3.Distance(locomotivePosition, destCenter);
    }


}



public class StationData
{
    public Vector3 Pos0 { get; set; }
    public Vector3 Pos1 { get; set; }
    public Vector3 Center { get; set; }
    public float Length { get; set; }

    public StationData(float x0, float y0, float z0, float x1, float y1, float z1, float xc, float yc, float zc, float len)
    {
        Pos0 = new Vector3(x0, y0, z0);
        Pos1 = new Vector3(x1, y1, z1);
        Center = new Vector3(xc, yc, zc);
        Length = len;
    }
}

public static class StationManager
{
    private static Dictionary<string, bool> stationSelections = new Dictionary<string, bool>();

    public static Dictionary<string, StationData> Stations = new Dictionary<string, StationData>
    {
        { "sylva", new StationData(24634.5f, 620.57f, -941.23f, 24563.24f, 620.57f, -935.94f, 24598.87f, 620.57f, -938.585f, 71.45608232f) },
        { "dillsboro", new StationData(22379.87f, 603.17f, -1410.88f, 22326.76f, 603.17f, -1434.12f, 22353.315f, 603.17f, -1422.5f, 57.9721459f) },
        { "wilmot", new StationData(16511.31f, 569.97f, 2326.23f, 16493.52f, 569.97f, 2329.14f, 16502.415f, 569.97f, 2327.685f, 18.0264306f) },
        { "whittier", new StationData(12267.1f, 561.45f, 5864.33f, 12279.19f, 561.45f, 5893.68f, 12273.145f, 561.45f, 5879.005f, 31.74256763f) },
        { "ela", new StationData(9569.54f, 546.61f, 7404.1f, 9554.41f, 546.61f, 7409.92f, 9561.975f, 546.61f, 7407.01f, 16.21077728f) },
        { "bryson", new StationData(4530.43f, 528.97f, 5428.56f, 4473.52f, 528.97f, 5407.87f, 4501.975f, 528.97f, 5418.215f, 60.55430786f) },
        { "hemingway", new StationData(2820.64f, 578.52f, 3079.64f, 2815.72f, 578.54f, 3055.54f, 2818.18f, 578.53f, 3067.59f, 24.59708926f) },
        { "alarkajct", new StationData(1745.6f, 590.23f, 1503.32f, 1737.93f, 589.78f, 1425.91f, 1741.765f, 590.005f, 1464.615f, 77.79035609f) },
        { "cochran", new StationData(1996.88f, 591.62f, -205.13f, 2007.29f, 591.85f, -218.98f, 2002.085f, 591.735f, -212.055f, 17.32753589f) },
        { "alarka", new StationData(4170.52f, 644.81f, -3113.05f, 4201.17f, 645.24f, -3140.48f, 4185.845f, 645.025f, -3126.765f, 41.13407711f) },
        { "almond", new StationData(-6340.3f, 524.97f, -1291.01f, -6316.44f, 524.97f, -1347.1f, -6328.37f, 524.97f, -1319.055f, 60.95398018f) },
        { "nantahala", new StationData(-15594.29f, 595.2f, -10588.8f, -15642.63f, 595.51f, -10646.29f, -15618.46f, 595.355f, -10617.545f, 75.11292698f) },
        { "topton", new StationData(-18969.52f, 793.22f, -15217.75f, -18977.49f, 792.7f, -15231.27f, -18973.505f, 792.96f, -15224.51f, 15.70292011f) },
        { "rhodo", new StationData(-22993.12f, 653.53f, -18005.08f, -23014.11f, 653.15f, -18030.5f, -23003.615f, 653.34f, -18017.79f, 32.96818011f) },
        { "andrews", new StationData(-29923.78f, 538.97f, -20057.8f, -29990.74f, 538.97f, -20092.33f, -29957.26f, 538.97f, -20075.065f, 75.33898393f) }

    };

    
    public static void InitializeStationSelections()
    {
        var allStops = PassengerStop.FindAll();
        foreach (var stop in allStops)
        {
            stationSelections[stop.identifier] = false;
        }
    }

    public static bool IsStationSelected(PassengerStop stop)
    {
        return stationSelections.TryGetValue(stop.identifier, out bool isSelected) && isSelected;
    }

    public static bool IsAnyStationSelected(List<PassengerStop> stations)
    {
        return stations.Any(stop => stationSelections.TryGetValue(stop.identifier, out bool isSelected) && isSelected);
    }

    public static void SetStationSelected(PassengerStop stop, bool isSelected)
    {
        stationSelections[stop.identifier] = isSelected;
    }
    public static bool IsAnyStationSelectedForLocomotive(Car locomotive)
    {
        // Check if the locomotive exists in the SelectedStations dictionary
        if (LocoTelem.SelectedStations.TryGetValue(locomotive, out List<PassengerStop> selectedStations))
        {
            // Return true if there is at least one selected station
            return selectedStations.Any();
        }

        // Return false if the locomotive is not found or no stations are selected
        return false;
    }
}

namespace RouteManagerUI
{
    
    [HarmonyPatch(typeof(CarInspector), "PopulateAIPanel")]
    public static class CarInspectorPopulateAIPanelPatch 
    {

        static bool Prefix(CarInspector __instance, UIPanelBuilder builder)
        {
            // Access the _car field using reflection
            var carField = typeof(CarInspector).GetField("_car", BindingFlags.NonPublic | BindingFlags.Instance);
            var car = carField.GetValue(__instance) as Car; // Assuming the type of _car is Car
            Debug.Log("1");
            if (car == null)
            {
                // Handle the case where car is not found or is null
                return true; // You might want to let the original method run in this case
            }
            Debug.Log("2");
            builder.FieldLabelWidth = 100f;
            builder.Spacing = 8f;
            AutoEngineerPersistence persistence = new AutoEngineerPersistence(car.KeyValueObject);
            AutoEngineerMode mode2 = Mode();
            builder.AddObserver(persistence.ObserveOrders(delegate
            {
                if (Mode() != mode2)
                {
                    builder.Rebuild();
                }
            }, callInitial: false));
            Debug.Log("3");
            builder.AddField("Mode", builder.ButtonStrip(delegate (UIPanelBuilder builder)
            {
                builder.AddButtonSelectable("Manual", mode2 == AutoEngineerMode.Off, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Off, null, null, null);
                });
                builder.AddButtonSelectable("Road", mode2 == AutoEngineerMode.Road, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Road, null, null, null);
                });
                builder.AddButtonSelectable("Yard", mode2 == AutoEngineerMode.Yard, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Yard, null, null, null);
                });

            }));
            Debug.Log("4");
            if (!persistence.Orders.Enabled)
            {
                builder.AddExpandingVerticalSpacer();
                return false;
            }
            Debug.Log("5");
            if (!StationManager.IsAnyStationSelectedForLocomotive(car))
            {
                builder.AddField("Direction", builder.ButtonStrip(delegate (UIPanelBuilder builder)
            {
                builder.AddObserver(persistence.ObserveOrders(delegate
                {
                    builder.Rebuild();
                }, callInitial: false));
                builder.AddButtonSelectable("Reverse", !persistence.Orders.Forward, delegate
                {
                    bool? forward3 = false;
                    if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                    {
                        SetOrdersValue(null, forward3, null, null);
                    }

                });
                builder.AddButtonSelectable("Forward", persistence.Orders.Forward, delegate
                {
                    bool? forward2 = true;
                    if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                    {
                        SetOrdersValue(null, forward2, null, null);
                    }

                });
            }));
                }
            Debug.Log("6");
            if (mode2 == AutoEngineerMode.Road)
            {
                if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                {
                    int num = MaxSpeedMphForMode(mode2);
                    RectTransform control = builder.AddSlider(() => persistence.Orders.MaxSpeedMph / 5, delegate
                    {
                        int maxSpeedMph4 = persistence.Orders.MaxSpeedMph;
                        return maxSpeedMph4.ToString();
                    }, delegate (float value)
                    {
                        int? maxSpeedMph3 = (int)(value * 5f);
                        if (!StationManager.IsAnyStationSelectedForLocomotive(car))
                        {
                            SetOrdersValue(null, null, maxSpeedMph3, null);
                        }


                    }, 0f, num / 5, wholeNumbers: true);
                    builder.AddField("Max Speed", control);
                }
                
                var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
                var orderedStops = new string[] { "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka", "almond", "nantahala", "topton", "rhodo", "andrews" }
                                   .Select(id => stopsLookup[id])
                                   .Where(ps => !ps.ProgressionDisabled)
                                   .ToList();

                // Create a scrollable view to list the stations
                builder.VScrollView(delegate (UIPanelBuilder builder)
                {
                    foreach (PassengerStop stop in orderedStops)
                    {
                        builder.HStack(delegate (UIPanelBuilder hstack)
                        {
                            // Add a checkbox for each station
                            hstack.AddToggle(() => StationManager.IsStationSelected(stop), isOn =>
                            {
                                StationManager.SetStationSelected(stop, isOn);
                                UpdateManagedTrainsSelectedStations(car); // Update when checkbox state changes
                            });

                            // Add a label next to the checkbox
                            hstack.AddLabel(stop.name);
                        });
                    }
                });

                bool anyStationSelected = StationManager.IsAnyStationSelectedForLocomotive(car);

                 //If any station is selected, add a button to the UI
                if (anyStationSelected)
                {
                    builder.AddButton("Print Car Info", () => ManagedTrains.PrintCarInfo(car));
                }

            }
            Debug.Log("7");

            if (mode2 == AutoEngineerMode.Yard)
            {
                RectTransform control2 = builder.ButtonStrip(delegate (UIPanelBuilder builder)
                {
                    builder.AddButton("Stop", delegate
                    {
                        float? distance8 = 0f;
                        SetOrdersValue(null, null, null, distance8);
                    });
                    builder.AddButton("½", delegate
                    {
                        float? distance7 = 6.1f;
                        SetOrdersValue(null, null, null, distance7);
                    });
                    builder.AddButton("1", delegate
                    {
                        float? distance6 = 12.2f;
                        SetOrdersValue(null, null, null, distance6);
                    });
                    builder.AddButton("2", delegate
                    {
                        float? distance5 = 24.4f;
                        SetOrdersValue(null, null, null, distance5);
                    });
                    builder.AddButton("5", delegate
                    {
                        float? distance4 = 61f;
                        SetOrdersValue(null, null, null, distance4);
                    });
                    builder.AddButton("10", delegate
                    {
                        float? distance3 = 122f;
                        SetOrdersValue(null, null, null, distance3);
                    });
                    builder.AddButton("20", delegate
                    {
                        float? distance2 = 244f;
                        SetOrdersValue(null, null, null, distance2);
                    });
                }, 4);
                builder.AddField("Car Lengths", control2);
            }
            Debug.Log("8");
            builder.AddExpandingVerticalSpacer();
            builder.AddField("Status", () => persistence.PlannerStatus, UIPanelBuilder.Frequency.Periodic);
            static int MaxSpeedMphForMode(AutoEngineerMode mode)
            {
                return mode switch
                {
                    AutoEngineerMode.Off => 0,
                    AutoEngineerMode.Road => 45,
                    AutoEngineerMode.Yard => 15,
                    _ => throw new ArgumentOutOfRangeException("mode", mode, null),
                };
            }
            Debug.Log("9");
            AutoEngineerMode Mode()
            {
                Orders orders2 = persistence.Orders;
                if (!orders2.Enabled)
                {
                    return AutoEngineerMode.Off;
                }
                if (!orders2.Yard)
                {
                    return AutoEngineerMode.Road;
                }
                return AutoEngineerMode.Yard;
            }
            Debug.Log("10");
            void SendAutoEngineerCommand(AutoEngineerMode mode, bool forward, int maxSpeedMph, float? distance)
            {
                StateManager.ApplyLocal(new AutoEngineerCommand(car.id, mode, forward, maxSpeedMph, distance));
            }
            Debug.Log("11");
            void SetOrdersValue(AutoEngineerMode? mode, bool? forward, int? maxSpeedMph, float? distance)
            {
                Orders orders = persistence.Orders;
                if (!orders.Enabled && mode.HasValue && mode.Value != 0 && !maxSpeedMph.HasValue)
                {
                    float num2 = car.velocity * 2.23694f;
                    float num3 = Mathf.Abs(num2);
                    maxSpeedMph = ((num2 > 0.1f) ? (Mathf.CeilToInt(num3 / 5f) * 5) : 0);
                    forward = num2 >= -0.1f;
                }
                if (mode == AutoEngineerMode.Yard)
                {
                    maxSpeedMph = MaxSpeedMphForMode(AutoEngineerMode.Yard);
                }
                AutoEngineerMode mode3 = mode ?? Mode();
                int maxSpeedMph2 = Mathf.Min(maxSpeedMph ?? orders.MaxSpeedMph, MaxSpeedMphForMode(mode3));
                SendAutoEngineerCommand(mode3, forward ?? orders.Forward, maxSpeedMph2, distance);
            }
            Debug.Log("12");

            
            return false; // Prevent the original method from running
        }

        private static void UpdateManagedTrainsSelectedStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();
            var selectedStations = allStops.Where(StationManager.IsStationSelected).ToList();

            // Update the ManagedTrains with the selected stations for this car
            ManagedTrains.UpdateSelectedStations(car, selectedStations);
            LocoTelem.locomotiveCoroutines[car] = false;

            Debug.Log("13");
        }

    }
}