using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight.Messaging;
using KeyValue.Runtime;
using Model;
using RollingStock;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.harmonyPatches;
using RouteManager.v2.Logging;
using UI;
using UI.Builder;
using UI.Common;
using UI.CompanyWindow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static Game.Reputation.PassengerReputationCalculator;
using static UnityEngine.InputSystem.Layouts.InputControlLayout;

namespace RouteManager.v2.UI
{
    [RequireComponent(typeof(Window))]
    public class RouteManagerWindow : MonoBehaviour, IBuilderWindow
    {
        private Window _window;

        private Car _car;

        private static RouteManagerWindow _instance;

        private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

        private readonly UIState<string> _selectedTabState = new UIState<string>(null);

        public UIBuilderAssets BuilderAssets { get; set; }


        public static void Show(Car car)
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Show()");
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindObjectOfType<RouteManagerWindow>();
            }

            if (car == null)
                return;

            _instance._car = car;

            ensureTelemetryObjectsExist();

            BuildPanel();

            //_instance._window.SetContentWidth(700);
            _instance._window.SetPosition(Window.Position.Center);
            _instance._window.ShowWindow();
        }


        private static void ensureTelemetryObjectsExist()
        {
            if (!LocoTelem.UIStationEntries.ContainsKey(_instance._car))
                LocoTelem.UIStationEntries[_instance._car] = new List<PassengerStop>();
            if (!LocoTelem.UIPickupStationSelections.ContainsKey(_instance._car))
                LocoTelem.UIPickupStationSelections[_instance._car] = new Dictionary<string, bool>();
            if (!LocoTelem.UIStopStationSelections.ContainsKey(_instance._car))
                LocoTelem.UIStopStationSelections[_instance._car] = new Dictionary<string, bool>();
            if (!LocoTelem.UITransferStationSelections.ContainsKey(_instance._car))
                LocoTelem.UITransferStationSelections[_instance._car] = new Dictionary<string, PassengerStop>();
        }


        private void Awake()
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Awake()");
            _window = GetComponent<Window>();
            BuilderAssets = ProgrammaticWindowCreatorPatch.builderAssets;
        }

        private void OnEnable()
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.OnEnable()");
            /*
            Messenger.Default.Register(this, delegate (CarIdentChanged evt)
            {
                if (_car != null && _car.id == evt.CarId)
                {
                    Rebuild();
                }
            });
            */
        }

        private void OnDisable()
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.OnDisable()");
            Messenger.Default.Unregister(this);
        }

        
        private void Rebuild()
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Rebuild()");
            BuildPanel();
        }
        

        private static void BuildPanel()
        {
            var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
            List<PassengerStop> orderedStops = new string[] { "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka", "almond", "nantahala", "topton", "rhodo", "andrews" }
                               .Select(id => stopsLookup[id])
                               .Where(ps => !ps.ProgressionDisabled)
                               .ToList();

            List<int> values = orderedStops.Select((PassengerStop st, int i) => i).ToList();

            _instance._window.Title = "Route Manager Station Selection";
            UIPanel.Create(_instance._window.contentRectTransform, _instance.BuilderAssets, delegate (UIPanelBuilder builder)
            {
                builder.AddTitle(TitleForCar(_instance._car), "");

                //builder.AddLabel("Add/remove passenger stations from your train's route.");
                builder.AddLabel("Trains will board all passengers marked for pickup, but will only stop at marked stations.");
                builder.AddLabel("Transfer stations are used when the train does not service the entire route.");

                builder.Spacer().Height(20f);

                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    builder.AddLabel("<b>Station</b>").Width(200f);
                    builder.AddLabel("<b>Pickup</b>").Width(80f);
                    builder.AddLabel("<b>Stop</b>").Width(80f);
                    builder.AddLabel("<b>Transfer Station</b>").FlexibleWidth(200f);
                }, 8f).Height(25f);

                builder.VScrollView(delegate (UIPanelBuilder builder)
                {
                foreach (PassengerStop stop in orderedStops)
                {
                    builder.HStack(delegate (UIPanelBuilder builder)
                    {
                        //Station name
                        builder.AddLabel(stop.DisplayName).Width(200f);

                        //Pickup passengers travelling to this station
                        builder.AddToggle(() => DestinationManager.IsPickupStationSelected(stop, _instance._car), isOn =>
                        {
                            RouteManager.logger.LogToDebug($"Pickup Toggled: {stop?.identifier} State: {isOn}", LogLevel.Verbose);

                            DestinationManager.SetPickupStationSelected(stop, _instance._car, isOn);
                            
                            // Update when checkbox state changes
                            UpdateManagedTrainsPickupStations(_instance._car);

                            if (LocoTelem.RouteMode[_instance._car])
                                TrainManager.CopyStationsFromLocoToCoaches_dev(_instance._car);
                            
                            builder.Rebuild();
                        }).Tooltip("Pickup", $"This train will collect passengers heading to {stop.DisplayName}.<br>If passengers are collected for {stop.DisplayName} but the train does not stop at {stop.DisplayName}, then a Transfer station will need to be set.")
                          .Width(80f);

                        //Train stops at this station
                        builder.AddToggle(() => DestinationManager.IsStopStationSelected(stop, _instance._car), isOn =>
                        {

                            Dictionary<PassengerStop, PassengerStop> transferStations;
                            LocoTelem.transferStations.TryGetValue(_instance._car, out transferStations);

                            RouteManager.logger.LogToDebug($"Stop Toggled: {stop?.identifier} State: {isOn} Is Transfer Station: {transferStations?.ContainsValue(stop)}", LogLevel.Verbose);

                            //Ensure the stop is not also a transfer station when unticking!
                            if (transferStations != null && transferStations.ContainsValue(stop))
                            {
                                isOn = true;
                            }

                            DestinationManager.SetStopStationSelected(stop, _instance._car, isOn);
                            
                            // Update when checkbox state changes
                            UpdateManagedTrainsStopStations(_instance._car);

                            if (LocoTelem.RouteMode[_instance._car])
                            {
                                TrainManager.CopyStationsFromLocoToCoaches_dev(_instance._car);
                                
                                //TODO: If we're already moving, we need to update our next station
                                //(or at least check if this station is between us and the next station or the next station has been removed)
                                
                            }

                            builder.Rebuild();
                        }).Width(80f);


                        //Passengers bound for this station need to get off here
                        PassengerStop selTransfer = DestinationManager.IsTransferStationSelected(stop, _instance._car);
                        int selInt = -1;

                        //IF picking up passengers for this station, but not stopping here a transfer station is required
                        bool write = DestinationManager.IsPickupStationSelected(stop, _instance._car) &&
                                     !DestinationManager.IsStopStationSelected(stop, _instance._car);

                        if (selTransfer != null && write )
                        {
                            selInt = orderedStops.FindIndex(stop => stop == selTransfer);
                        }
                        RouteManager.logger.LogToDebug($"Building: stored transfer: {stop?.identifier} -> {selTransfer?.identifier} SelInt: {selInt}", LogLevel.Verbose);

                        builder.AddDropdownIntPicker(values,
                                                            selInt,
                                                            (int i) => (i >= 0) ? orderedStops[i].DisplayName : "",
                                                            canWrite: write,
                                                            delegate (int i)
                            {

                                RouteManager.logger.LogToDebug($"Transfer Selected: {stop?.identifier} Transfer to: {orderedStops[i]?.identifier}", LogLevel.Verbose);

                                //Ensure the transfer station's stop is ticked (we need to stop there to do a transfer!)
                                DestinationManager.SetStopStationSelected(orderedStops[i], _instance._car, true);
                                DestinationManager.SetTransferStationSelected(stop, _instance._car, (i >= 0) ? orderedStops[i] : null);

                                UpdateManagedTrainsStopStations(_instance._car);
                                UpdateManagedTrainsTransferStations(_instance._car);

                                if (LocoTelem.RouteMode[_instance._car])
                                    TrainManager.CopyStationsFromLocoToCoaches_dev(_instance._car);

                                //really, really rebuild - possibly need to setup nested builders so we only trigger a rebuild on a smaller panel?
                                _instance.Rebuild();

                            }).Tooltip("Transfer", $"Passengers destined for {stop.DisplayName} will transfer trains here.").FlexibleWidth(200f);
                        }, 8f);//.ChildAlignment(TextAnchor.MiddleLeft);
            }
                }, new RectOffset(0, 4, 0, 0));

            });


        }

        private static void BuildPaneltest()
        {
            //Determine valid stations
            Dictionary<PassengerStop,int>   validStations       = validStationStops();
            List<string>                    stationIdentifiers  = validStations.Keys.Select(x => x.identifier).ToList();


            RouteManager.logger.LogToDebug("Valid Station count was " + validStations.Count);

            //Set Visible Window Title
            _instance._window.Title = "Route Manager Orders";

            //Create UI Panel
            UIPanel.Create(_instance._window.contentRectTransform, _instance.BuilderAssets, delegate (UIPanelBuilder builder)
            {
                //UI Design
                builder.AddTitle(TitleForCar(_instance._car), "");
                builder.AddLabel("Trains will board all passengers marked for pickup, but will only stop at marked stations.");
                builder.AddLabel("Transfer stations are used when the train does not service the entire route.");

                //Formatting
                builder.Spacer().Height(20f);

                //Table Headers
                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    builder.AddLabel("<b>Station</b>").Width(200f);
                    builder.AddLabel("<b>Load</b>").Width(80f);
                    builder.AddLabel("<b>Unload</b>").Width(80f);
                    builder.AddLabel("<b>Transfer Station</b>").FlexibleWidth(200f);
                }, 8f).Height(25f);

                //Create Scrollable view
                builder.VScrollView(delegate (UIPanelBuilder builder) 
                {
                    List<PassengerStop> stops = LocoTelem.UIStationEntries[_instance._car];
                    //Foreach stop in the user defined list
                    for (int i=0; i < stops.Count; i++) //(PassengerStop currentOrder in LocoTelem.UIStationEntries[_instance._car])
                    {                       

                        builder.HStack(delegate (UIPanelBuilder builder) 
                        {

                            RouteManager.logger.LogToDebug("Current Stop is: " + stops[i].identifier);

                            //Add Column 1
                            builder.AddDropdownIntPicker(validStations.Values.ToList(), validStations.Keys.ToList().IndexOf(stops[i]) < 0 ? 0 : validStations.Keys.ToList().IndexOf(stops[i]), (int j) => (j >= 0) ? validStations.Keys.ElementAt(j).DisplayName : "", true, delegate (int i)
                            {
                                _instance.Rebuild();
                            }).Tooltip("Station", "Station / Passenger destination to service").Width(200f);

                            //Add Column 2
                            //Pickup passengers travelling to this station
                            builder.AddToggle(() => DestinationManager.IsPickupStationSelected(stops[i], _instance._car), isOn => {
                                RouteManager.logger.LogToDebug($"Pickup Toggled: {stops[i]?.identifier} State: {isOn}", LogLevel.Verbose);

                                DestinationManager.SetPickupStationSelected(stops[i], _instance._car, isOn);

                                // Update when checkbox state changes
                                UpdateManagedTrainsPickupStations(_instance._car);

                                if (LocoTelem.RouteMode[_instance._car])
                                    TrainManager.CopyStationsFromLocoToCoaches_dev(_instance._car);

                                builder.Rebuild();
                            }).Tooltip("Pickup", $"This train will collect passengers heading to {stops[i].DisplayName}.<br>If passengers are collected for {stops[i].DisplayName} but the train does not stop at {stops[i].DisplayName}, then a Transfer station will need to be set.")
                                .Width(80f);

                            ////Add Column 3
                            builder.AddToggle(() => DestinationManager.IsStopStationSelected(stops[i], _instance._car), isOn => {

                                Dictionary<PassengerStop, PassengerStop> transferStations;
                                LocoTelem.transferStations.TryGetValue(_instance._car, out transferStations);

                                RouteManager.logger.LogToDebug($"Stop Toggled: {stops[i]?.identifier} State: {isOn} Is Transfer Station: {transferStations?.ContainsValue(stops[i])}", LogLevel.Verbose);

                                //Ensure the stop is not also a transfer station when unticking!
                                if (transferStations != null && transferStations.ContainsValue(stops[i]))
                                {
                                    isOn = true;
                                }

                                DestinationManager.SetStopStationSelected(stops[i], _instance._car, isOn);

                                // Update when checkbox state changes
                                UpdateManagedTrainsStopStations(_instance._car);

                                if (LocoTelem.RouteMode[_instance._car])
                                {
                                    TrainManager.CopyStationsFromLocoToCoaches_dev(_instance._car);

                                    //TODO: If we're already moving, we need to update our next station
                                    //(or at least check if this station is between us and the next station or the next station has been removed)

                                }

                                builder.Rebuild();
                            }).Width(80f);

                            ////Add Column 4
                            //builder.AddDropdownIntPicker(validStations.Values.ToList(), validStations.Keys.ToList().IndexOf(stops[i]) < 0 ? 0 : validStations.Keys.ToList().IndexOf(stops[i]), (int j) => (j >= 0) ? validStations.Keys.ElementAt(j).DisplayName : "", true, delegate (int i)
                            //{
                            //    _instance.Rebuild();
                            //}).Tooltip("Station", "Station / Passenger destination to service").Width(200f);
                        }, 10f); //.ChildAlignment(TextAnchor.MiddleLeft);
                    }
                }, new RectOffset(0, 4, 0, 0));

                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    //Define a button to add new tuples. 
                    builder.AddButton("Remove Last Stop", delegate
                    {
                        RouteManager.logger.LogToDebug("Remove Stop Clicked!");

                        if(LocoTelem.UIStationEntries[_instance._car].Count >=1)
                            LocoTelem.UIStationEntries[_instance._car].RemoveAt(LocoTelem.UIStationEntries[_instance._car].Count-1);

                        //Since this is an event, rebuild on the event completion.
                        _instance.Rebuild();
                    }).Height(30f);

                    //Define a button to add new tuples. 
                    builder.AddButton("Add New Stop", delegate
                    {
                        RouteManager.logger.LogToDebug("Add Stop Clicked!");
                        LocoTelem.UIStationEntries[_instance._car].Add(validStations.Keys.First());

                        //Since this is an event, rebuild on the event completion.
                        _instance.Rebuild();
                    }).Height(30f);
                }, 10f).ChildAlignment(TextAnchor.MiddleLeft);
            });
        }

        private static Dictionary<PassengerStop,int> validStationStops()
        {
            Dictionary<String,PassengerStop> stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);

            List<PassengerStop> orderedStops = StationInformation.Stations.Keys.Select(id => stopsLookup[id])
                .Where(ps => !ps.ProgressionDisabled)
                .ToList();

            return orderedStops.Zip(orderedStops.Select((PassengerStop st, int i) => i).ToList(),(k,v) => new { k, v }).ToDictionary(x=>x.k, x=> x.v);
        }

        private void PopulatePanel(UIPanelBuilder builder)
        {
            builder.AddTitle(TitleForCar(_car), SubtitleForCar(_car));
        }
        private static string TitleForCar(Car car)
        {
            return car.CarType + " " + car.DisplayName;
        }
        private static string SubtitleForCar(Car car)
        {
            int num = Mathf.CeilToInt(car.Weight / 2000f);
            string arg = (string.IsNullOrEmpty(car.DefinitionInfo.Metadata.Name) ? car.CarType : car.DefinitionInfo.Metadata.Name);
            return $"{num}T {arg}";
        }

        private static void UpdateManagedTrainsStopStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();
            var selectedStations = allStops.Where(stop => DestinationManager.IsStopStationSelected(stop, car)).ToList();

            // Update the ManagedTrains with the selected stations for this car
            DestinationManager.SetStopStations(car, selectedStations);
        }

        private static void UpdateManagedTrainsPickupStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();
            var selectedStations = allStops.Where(stop => DestinationManager.IsPickupStationSelected(stop, car)).ToList();

            // Update the ManagedTrains with the selected stations for this car
            DestinationManager.SetPickupStations(car, selectedStations);
        }

        private static void UpdateManagedTrainsTransferStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();

            //filter to stops that have a transfer
            var stationsWithTransfer = allStops.Where(stop => DestinationManager.IsTransferStationSelected(stop, car) != null);

            Dictionary<PassengerStop, PassengerStop> selectedStations = new Dictionary<PassengerStop, PassengerStop>();

            //build a dictionary of from -> to
            foreach (var stop in stationsWithTransfer)
            {
                selectedStations.Add(stop, DestinationManager.IsTransferStationSelected(stop, car));

                RouteManager.logger.LogToDebug($"UpdateManagedTrainsTransferStations stationsWithTransfer: {stop.identifier} -> {selectedStations[stop].identifier}", LogLevel.Verbose);
            }

            // Update the ManagedTrains with the selected stations for this car
            DestinationManager.SetTransferStations(car, selectedStations);
        }
    }
}
