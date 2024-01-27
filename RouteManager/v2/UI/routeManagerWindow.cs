using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Model;
using RollingStock;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.harmonyPatches;
using UI;
using UI.Builder;
using UI.Common;
using UI.CompanyWindow;
using UnityEngine;


namespace RouteManager.v2.UI
{
    [RequireComponent(typeof(Window))]
    public class routeManagerWindow : MonoBehaviour, IBuilderWindow
    {
        private Window _window;

        private Car _car;

        private static routeManagerWindow _instance;

        private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

        private readonly UIState<string> _selectedTabState = new UIState<string>(null);

        public UIBuilderAssets BuilderAssets { get; set; }


        public static void Show(Car car)
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Show()");
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindObjectOfType<routeManagerWindow>();
            }

            if (car == null)
                return;

            _instance._car = car;

            BuildPanel();

            _instance._window.SetPosition(Window.Position.Center);
            _instance._window.ShowWindow();
        }

        private void Awake()
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Awake()");
            _window = GetComponent<Window>();
            BuilderAssets = ProgrammaticWindowCreatorPatch.builderAssets;
            //this.transform.name = "RouteManagerWindow";
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

        /*
        private void Rebuild()
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Rebuild()");
            if (!(_car == null))
            {
                Populate(_car);
            }
        }
        */

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
                            builder.AddToggle(() => DestinationManager.IsStationSelected(stop, _instance._car), isOn =>
                            {

                                DestinationManager.SetStationSelected(stop, _instance._car, isOn);
                                builder.Rebuild();

                                // Update when checkbox state changes
                                UpdateManagedTrainsSelectedStations(_instance._car);

                                if (LocoTelem.RouteMode[_instance._car])
                                    TrainManager.CopyStationsFromLocoToCoaches(_instance._car);
                            }).Tooltip("Pickup", $"This train will collect passengers heading to {stop.DisplayName}.<br>If passengers are collected for {stop.DisplayName} but the train does not stop at {stop.DisplayName}, then a Transfer station will need to be set.")
                              .Width(80f);
                            
                            //Train stops at this station
                            builder.AddToggle(() => DestinationManager.IsStationSelected(stop, _instance._car), isOn =>
                            {

                                DestinationManager.SetStationSelected(stop, _instance._car, isOn);
                                builder.Rebuild();

                                // Update when checkbox state changes
                                UpdateManagedTrainsSelectedStations(_instance._car);

                                if (LocoTelem.RouteMode[_instance._car])
                                    TrainManager.CopyStationsFromLocoToCoaches(_instance._car);
                            }).Tooltip("Stopping", "This train will stop at this station.")
                            .Width(80f);

                            //Passengers bound for this station need to get off here
                            builder.AddDropdownIntPicker(values, -1, (int i) => (i >= 0) ? orderedStops[i].DisplayName : "", canWrite: true, delegate (int i)
                            {

                            }).Tooltip("Transfer", $"Passengers destined for {stop.DisplayName} will transfer trains here.").FlexibleWidth(200f);


                        }, 8f);//.ChildAlignment(TextAnchor.MiddleLeft);
                    }
                }, new RectOffset(0, 4, 0, 0));

            });


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

        private static void UpdateManagedTrainsSelectedStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();
            var selectedStations = allStops.Where(stop => DestinationManager.IsStationSelected(stop, car)).ToList();

            // Update the ManagedTrains with the selected stations for this car
            DestinationManager.SetSelectedStations(car, selectedStations);
        }
    }
}
