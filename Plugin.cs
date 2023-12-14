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
}

public static class StationManager
{
    private static Dictionary<string, bool> stationSelections = new Dictionary<string, bool>();

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

    public static void SetStationSelected(PassengerStop stop, bool isSelected)
    {
        stationSelections[stop.identifier] = isSelected;
    }
}

namespace RouteManager
{
    [HarmonyPatch(typeof(GameInput))] // Specify the class you are patching
    [HarmonyPatch("Update")] // Specify the method you are patching
    public static class GameInputUpdatePatch
    {
        // This is the postfix method. It will be called after GameInput's Update method
        public static void Postfix()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                // Call ProcessPassengerStops method
                ProcessPassengerStops();
            }
        }

        private static void ProcessPassengerStops()
        {
            var allStops = PassengerStop.FindAll();
            var stopMapping = new Dictionary<string, Vector3>();

            foreach (var stop in allStops)
            {
                // Check if the stop is not null, has a valid identifier, and is active
                if (stop != null && !string.IsNullOrEmpty(stop.identifier) && stop.gameObject.activeInHierarchy)
                {
                    try
                    {
                        Vector3 centerPoint = stop.CenterPoint; // Assuming CenterPoint is a property of PassengerStop
                        stopMapping[stop.identifier] = centerPoint;
                        Dispatcher.mls.LogInfo($"Identifier: {stop.identifier}, CenterPoint: {centerPoint}");
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.mls.LogError($"Error processing stop '{stop.identifier}': {ex.Message}");
                    }
                }
            }
        }
    }
}


namespace RouteManagerUI
{
    public static class RouteManagerPlugin
    {
        //likley put control vars here
    }



    [HarmonyPatch(typeof(CarInspector), "PopulateAIPanel")]
    public static class CarInspectorPopulateAIPanelPatch
    {

        static bool Prefix(CarInspector __instance, UIPanelBuilder builder)
        {
            // Access the _car field using reflection
            var carField = typeof(CarInspector).GetField("_car", BindingFlags.NonPublic | BindingFlags.Instance);
            var car = carField.GetValue(__instance) as Car; // Assuming the type of _car is Car

            if (car == null)
            {
                // Handle the case where car is not found or is null
                return true; // You might want to let the original method run in this case
            }

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
            if (!persistence.Orders.Enabled)
            {
                builder.AddExpandingVerticalSpacer();
                return false;
            }
            builder.AddField("Direction", builder.ButtonStrip(delegate (UIPanelBuilder builder)
            {
                builder.AddObserver(persistence.ObserveOrders(delegate
                {
                    builder.Rebuild();
                }, callInitial: false));
                builder.AddButtonSelectable("Reverse", !persistence.Orders.Forward, delegate
                {
                    bool? forward3 = false;
                    SetOrdersValue(null, forward3, null, null);
                });
                builder.AddButtonSelectable("Forward", persistence.Orders.Forward, delegate
                {
                    bool? forward2 = true;
                    SetOrdersValue(null, forward2, null, null);
                });
            }));
            if (mode2 == AutoEngineerMode.Road)
            {
                int num = MaxSpeedMphForMode(mode2);
                RectTransform control = builder.AddSlider(() => persistence.Orders.MaxSpeedMph / 5, delegate
                {
                    int maxSpeedMph4 = persistence.Orders.MaxSpeedMph;
                    return maxSpeedMph4.ToString();
                }, delegate (float value)
                {
                    int? maxSpeedMph3 = (int)(value * 5f);
                    SetOrdersValue(null, null, maxSpeedMph3, null);
                }, 0f, num / 5, wholeNumbers: true);
                builder.AddField("Max Speed", control);

                var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
                var orderedStops = new string[] { "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "almond", "nantahala", "topton", "rhodo", "andrews", "cochran", "alarka" }
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
                            hstack.AddToggle(() => StationManager.IsStationSelected(stop), isOn => StationManager.SetStationSelected(stop, isOn));

                            // Add a label next to the checkbox
                            hstack.AddLabel(stop.name);
                        });
                    }
                });

            }

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
            void SendAutoEngineerCommand(AutoEngineerMode mode, bool forward, int maxSpeedMph, float? distance)
            {
                StateManager.ApplyLocal(new AutoEngineerCommand(car.id, mode, forward, maxSpeedMph, distance));
            }
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
            return false; // Prevent the original method from running
        }

    }
}

/*

if (RouteManagerPlugin.IsRouteModeActive)
            {
                Debug.Log("It was active");
                Debug.Log("before route menu built Checking for Autoengineer: " + mode2 + "\tIs Route Active:" + RouteManagerPlugin.IsRouteModeActive);
                if (mode2 == AutoEngineerMode.Off && RouteManagerPlugin.IsRouteModeActive)
                {
                    int num = MaxSpeedMphForMode(mode2);
                    RectTransform control = builder.AddSlider(() => persistence.Orders.MaxSpeedMph / 5, delegate
                    {
                        int maxSpeedMph4 = persistence.Orders.MaxSpeedMph;
                        return maxSpeedMph4.ToString();
                    }, delegate (float value)
                    {
                        int? maxSpeedMph3 = (int)(value * 5f);
                        SetOrdersValue(null, null, maxSpeedMph3, null);
                    }, 0f, num / 5, wholeNumbers: true);
                    builder.AddField("Max Speed", control);

                    Debug.Log("Building Route Mode Menu");
                    // Retrieve and prepare the list of all available stations
                    var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
                    var orderedStops = new string[] { "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "almond", "nantahala", "topton", "rhodo", "andrews", "cochran", "alarka" }
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
                                hstack.AddToggle(() => StationManager.IsStationSelected(stop), isOn => StationManager.SetStationSelected(stop, isOn));

                                // Add a label next to the checkbox
                                hstack.AddLabel(stop.name);
                            });
                        }
                    });
                }













private void PopulatePassengerCarPanel(UIPanelBuilder builder)
	{
		builder.AddField("Passengers", () => _car.PassengerCountString(_cachedMarker), UIPanelBuilder.Frequency.Fast);
		if (_stopsLookup == null || _stopsLookup.Count == 0)
		{
			_stopsLookup = PassengerStop.FindAll().ToDictionary((PassengerStop stop) => stop.identifier, (PassengerStop stop) => stop);
		}
		List<PassengerStop> orderedStops = (from id in new string[15]
			{
				"sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "almond", "nantahala",
				"topton", "rhodo", "andrews", "cochran", "alarka"
			}
			select _stopsLookup[id] into ps
			where !ps.ProgressionDisabled
			select ps).ToList();
		builder.VScrollView(delegate(UIPanelBuilder builder)
		{
			foreach (PassengerStop item in orderedStops)
			{
				PassengerStop stop2 = item;
				builder.HStack(delegate(UIPanelBuilder hstack)
				{
					hstack.AddToggle(() => IsPassengerStopChecked(stop2), delegate(bool isOn)
					{
						SetPassengerStopChecked(stop2, isOn);
					});
					hstack.AddLocationField(FieldName, stop2, delegate
					{
						JumpTo(stop2);
					});
				});
				string FieldName()
				{
					if (!_cachedMarker.HasValue)
					{
						return stop2.name;
					}
					int num = _cachedMarker.Value.CountPassengersForStop(stop2.identifier);
					if (num != 0)
					{
						return stop2.name + " (" + num + ")";
					}
					return stop2.name;
				}
			}
		});
		builder.Spacer(6f);
		builder.AddButtonCompact("Copy to Coupled", CopyStopsToCoupledCoaches);
		_observers.Add(_car.KeyValueObject.Observe("ops.passengerMarker", delegate
		{
			if (!(_car == null))
			{
				_cachedMarker = _car.GetPassengerMarker();
			}
		}));
	}

	private void SetPassengerStopChecked(PassengerStop passengerStop, bool isOn)
	{
		PassengerMarker passengerMarkerOrEmpty = GetPassengerMarkerOrEmpty();
		string identifier = passengerStop.identifier;
		HashSet<string> destinations = passengerMarkerOrEmpty.Destinations;
		bool flag = destinations.Contains(identifier);
		if (isOn && !flag)
		{
			destinations.Add(identifier);
		}
		else if (!isOn && flag)
		{
			destinations.Remove(identifier);
		}
		StateManager.ApplyLocal(new SetPassengerDestinations(_car.id, destinations.ToList()));
	}

	private PassengerMarker GetPassengerMarkerOrEmpty()
	{
		return _car.GetPassengerMarker() ?? PassengerMarker.Empty();
	}

	private bool IsPassengerStopChecked(PassengerStop passengerStop)
	{
		if (_cachedMarker.HasValue)
		{
			return _cachedMarker.Value.Destinations.Contains(passengerStop.identifier);
		}
		return false;
	}



*/