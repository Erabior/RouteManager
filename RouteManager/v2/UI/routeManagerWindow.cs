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
using UI;
using UI.Builder;
using UI.CarCustomizeWindow;
using UI.CarInspector;
using UI.Common;
using UI.CompanyWindow;
using UI.SwitchList;
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
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindObjectOfType<routeManagerWindow>();
            }

            _instance.Populate(car);
            _instance._window.ShowWindow();
        }

        private void Awake()
        {
            _window = GetComponent<Window>();
        }

        private void OnEnable()
        {
            Messenger.Default.Register(this, delegate (CarIdentChanged evt)
            {
                if (_car != null && _car.id == evt.CarId)
                {
                    Rebuild();
                }
            });
        }

        private void OnDisable()
        {
            Messenger.Default.Unregister(this);
        }

        private void Rebuild()
        {
            if (!(_car == null))
            {
                Populate(_car);
            }
        }

        private void Populate(Car car)
        {
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
            //_window.Title = "Route Manager Station Selection";
            UIPanel.Create(_window.contentRectTransform, UnityEngine.Object.FindObjectOfType<CarInspector>().BuilderAssets, PopulatePanel);
        }

        private void PopulatePanel(UIPanelBuilder builder)
        {
            //builder.AddTitle(TitleForCar(_car), SubtitleForCar(_car));
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
