using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model;
using Model.AI;
using Model.Definition;
using Model.OpsNew;
using RollingStock;
using RouteManager.v2.harmonyPatches;
using TMPro;
using UI;
using UI.Builder;
using UI.CarCustomizeWindow;
using UI.CarInspector;
using UI.Common;
using UI.CompanyWindow;
using UI.SwitchList;
using UnityEngine;
using static UnityEngine.InputSystem.Layouts.InputControlLayout;

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

            if ( car != null)
            {
                _instance._car = car;
            }

            BuildPanel();
            //_instance.Populate(car);
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
            List<string> stations = (new string[] {
                    "sylva",
                    "dillsboro",
                    "wilmot",
                    "whittier",
                    "ela",
                    "bryson",
                    "hemingway",
                    "alarkajct",
                    "almond",
                    "nantahala",
                    "topton",
                    "rhodo",
                    "andrews",
                    "cochran",
                    "alarka"
                }).ToList<string>();

            List<int> values = stations.Select((string st, int i) => i).ToList();

            _instance._window.Title = "Route Manager Station Selection";
            UIPanel.Create(_instance._window.contentRectTransform, _instance.BuilderAssets, delegate (UIPanelBuilder builder) {
                builder.AddTitle(TitleForCar(_instance._car), "");

                //builder.AddLabel("Add/remove passenger stations from your train's route.");
                builder.AddLabel("Trains will board all passengers marked for pickup, but will only stop at marked stations.");
                builder.AddLabel("Transfer stations are used when the train does not service the entire route.");

                builder.Spacer().Height(20f);

                builder.HStack(delegate (UIPanelBuilder builder)
                {
                    builder.AddLabel("<b>Station</b>").Width(150f);
                    builder.AddLabel("<b>Pickup</b>").Width(100f);
                    builder.AddLabel("<b>Stop</b>").Width(100f);
                    builder.AddLabel("<b>Transfer Station</b>").FlexibleWidth(200f);
                }, 8f).Height(25f);

                builder.VScrollView(delegate (UIPanelBuilder builder)
                {
                    foreach (string station in stations)
                    {
                        builder.HStack(delegate (UIPanelBuilder builder)
                        {

                            builder.AddLabel($"<b>{station}</b>").Width(150f);
                            builder.AddToggle(() => true, delegate (bool yes) { }).Tooltip("Pickup", $"This train will collect passengers heading to {station}.<br>If passengers are collected for {station} but the train does not stop at {station}, then a Transfer station will need to be set.").Width(100f);
                            builder.AddToggle(() => true, delegate (bool yes) { }).Tooltip("Stopping", "This train will stop at this station.").Width(100f);
                            builder.AddDropdownIntPicker(values, -1, (int i) => (i >= 0) ? stations[i] : "", canWrite: true, delegate (int i)
                            {

                            }).Tooltip("Transfer", $"Passengers destined for {station} will transfer trains here.").FlexibleWidth(200f);


                        }, 8f);//.ChildAlignment(TextAnchor.MiddleLeft);
                    }
                }, new RectOffset(0, 4, 0, 0));

            });

            
        }
        /*
        private void Populate(Car car)
        {
            RouteManager.logger.LogToDebug("routeManagerWindow.Populate()");
            if (_car != car)
            {
                _selectedTabState.Value = null;
            }

            _car = car;
            foreach (IDisposable observer in _observers)
            {
                observer?.Dispose();
            }

            _observers.Clear();
            _window.Title = "Route Manager Station Selection";
            UIPanel.Create(_window.contentRectTransform, UnityEngine.Object.FindObjectOfType<CarInspector>().BuilderAssets, PopulatePanel);
        }
        */
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

    }
}
