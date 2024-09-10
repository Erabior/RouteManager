using Game.Messages;
using Game.State;
using HarmonyLib;
using Model.AI;
using RollingStock;
using System;
using System.Linq;
using System.Reflection;
using UI.Builder;
using UI.CarInspector;
using UnityEngine;
using Model;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.Logging;
using RouteManager.v2.UI;
using UI.Common;


namespace RouteManager.v2.harmonyPatches
{

    [HarmonyPatch(typeof(CarInspector), "PopulateAIPanel")]
    public static class CarInspectorPopulateAIPanelPatch
    {
        static Vector2 initialSize = Vector2.zero;
        static bool Prefix(CarInspector __instance, UIPanelBuilder builder)
        {
            /**********************************************************************************
            *
            *
            *        UI HACKS
            *
            *
            **********************************************************************************/

            RectTransform uiPanel = UnityEngine.Object.FindFirstObjectByType<CarInspector>().GetComponent<RectTransform>();

            //capture size before we do anything
            if (initialSize.Equals(Vector2.zero))
            {
                initialSize = uiPanel.sizeDelta;
            }

            if (!RouteManager.Settings.experimentalUI)
            {
                RouteManager.logger.LogToDebug("Ui Panel Size was:" + uiPanel.sizeDelta.ToString(), LogLevel.Verbose);
                uiPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 500);
                RouteManager.logger.LogToDebug("Ui Panel Size is:" + uiPanel.sizeDelta.ToString(), LogLevel.Verbose);
            }
            else
            {
                uiPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, initialSize.y);
            }

            //fix hanging off the screen
            UnityEngine.Object.FindFirstObjectByType<CarInspector>().GetComponent<Window>().SetPosition(Window.Position.LowerRight); 

            bool placeHolder = false;

            /**********************************************************************************
            *
            *
            *        END UI HACKS
            *
            *
            **********************************************************************************/

            // Access the _car field using reflection
            var carField = typeof(CarInspector).GetField("_car", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assuming the type of _car is Car
            var car = carField.GetValue(__instance) as Car;

            //Custom MOD Logic. Generate station selection
            DestinationManager.InitializeStationSelectionForLocomotive(car);

            //Ensure that car is not null
            if (car == null)
            {
                // Let Original Railroader Method Logic to take over.
                return true; 
            }

            /**********************************************************************************
            *
            *
            *        ORIGINAL RailRoader Logic Except otherwise noted
            *
            *
            **********************************************************************************/


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
                    //Disable mod only if not paused
                    if(LocoTelem.RouteModePaused.ContainsKey(car) && LocoTelem.RouteModePaused[car] == false)
                        TrainManager.SetRouteModeEnabled(false, car);

                    SetOrdersValue(AutoEngineerMode.Off, null, null, null);
                });
                builder.AddButtonSelectable("Road", mode2 == AutoEngineerMode.Road, delegate
                {
                    SetOrdersValue(AutoEngineerMode.Road, null, null, null);
                });
                builder.AddButtonSelectable("Yard", mode2 == AutoEngineerMode.Yard, delegate
                {
                    //Disable mod only if not paused
                    if (LocoTelem.RouteModePaused.ContainsKey(car) && LocoTelem.RouteModePaused[car] == false)
                        TrainManager.SetRouteModeEnabled(false, car);

                    SetOrdersValue(AutoEngineerMode.Yard, null, null, null);
                });

            }));

            if (!persistence.Orders.Enabled)
            {
                if(mode2 != AutoEngineerMode.Road)
                    builder.AddExpandingVerticalSpacer();
                //Alteration: Return but do not allow original logic to run.
                return false;
            }

            /**********************************************************************************
            *
            *
            *        END ORIGINAL RailRoader Logic Except otherwise noted
            *
            *
            **********************************************************************************/

            if (!LocoTelem.RouteMode.ContainsKey(car))
            {
                LocoTelem.RouteMode[car] = false;
            }
            if (!LocoTelem.RouteMode[car] || LocoTelem.RouteModePaused[car])
            {
                /**********************************************************************************
                *
                *
                *        ORIGINAL RailRoader Logic Except otherwise noted
                *
                *
                **********************************************************************************/

                    builder.AddField("Direction", builder.ButtonStrip(delegate (UIPanelBuilder builder)
                    {
                        builder.AddObserver(persistence.ObserveOrders(delegate
                        {
                            builder.Rebuild();
                        }, callInitial: false));
                        builder.AddButtonSelectable("Reverse", !persistence.Orders.Forward, delegate
                        {
                            bool? forward3 = false;

                            //IF STATEMENT wrapper for Station Management Logic
                            if (!LocoTelem.RouteMode[car] || LocoTelem.RouteModePaused[car])
                            {
                                //Original Code
                                SetOrdersValue(null, forward3, null, null);
                            }

                        });
                        builder.AddButtonSelectable("Forward", persistence.Orders.Forward, delegate
                        {
                            bool? forward2 = true;

                            //IF STATEMENT wrapper for Station Management Logic
                            if (!LocoTelem.RouteMode[car] || LocoTelem.RouteModePaused[car])
                            {
                                //Original Code
                                SetOrdersValue(null, forward2, null, null);
                            }

                        });
                    }));


                /**********************************************************************************
                *
                *
                *        END ORIGINAL RailRoader Logic Except otherwise noted
                *
                *
                **********************************************************************************/
            }

            if (mode2 == AutoEngineerMode.Road)
            {
                //ImplementFeature enhancement #30
                //Renable the max speed slider. 
                int num = MaxSpeedMphForMode(mode2);
                RectTransform control = builder.AddSlider(() => persistence.Orders.MaxSpeedMph / 5, () => persistence.Orders.MaxSpeedMph.ToString(), delegate (float value)
                {
                    LocoTelem.RMMaxSpeed[car] = (int)(value * 5f);
                    SetOrdersValue(null, null, (int) LocoTelem.RMMaxSpeed[car], null);
                }, 0f, num / 5, wholeNumbers: true);
                builder.AddField("Max Speed", control);

                /**********************************************************************************
                *
                *
                *        Major MOD GUI additions
                *
                *
                **********************************************************************************/

                builder.HStack(delegate (UIPanelBuilder hstack)
                {
                    hstack.AddToggle(() => TrainManager.IsRouteModeEnabled(car), isOn =>
                    {
                        TrainManager.SetRouteModeEnabled(isOn, car);
                    });
                    hstack.AddLabel("Enable Route Mode");

                    hstack.AddToggle(() => TrainManager.IsRouteModePaused(car), isOn =>
                    {
                        TrainManager.PauseRouteMode(isOn, car);
                    });
                    hstack.AddLabel("Pause for Refuel");

                    // Subscribe to the OnRouteModeChanged event
                    TrainManager.OnRouteModeChanged += (changedCar) =>
                    {
                        if (changedCar == car) // Check if the changed car is the one currently displayed in the UI
                        {
                            builder.Rebuild();
                        }
                    };
                });

                if (RouteManager.Settings.experimentalUI)
                {
                    builder.AddButtonSelectable("Define Stations", placeHolder, delegate
                    {
                        RouteManagerWindow.Show(car);
                    });

                    builder.AddExpandingVerticalSpacer();
                }
                else
                {
                    //Define the list to bind to the vertical scroll bar.
                    var stopsLookup = PassengerStop.FindAll().ToDictionary(stop => stop.identifier, stop => stop);
                    var orderedStops = StationInformation.OrderedStations
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
                                hstack.AddToggle(() => DestinationManager.IsStopStationSelected(stop, car), isOn =>
                                {

                                    DestinationManager.SetStopStationSelected(stop, car, isOn);
                                    builder.Rebuild();

                                    // Update when checkbox state changes
                                    UpdateManagedTrainsStopStations(car);

                                    if (LocoTelem.RouteMode[car])
                                    {
                                        if (RouteManager.Settings.experimentalUI)
                                        {
                                            TrainManager.CopyStationsFromLocoToCoaches_dev(car);
                                        }
                                        else
                                        {
                                            TrainManager.CopyStationsFromLocoToCoaches(car);
                                        }
                                    }
                                        
                                });

                                // Add a label next to the checkbox
                                hstack.AddLabel(stop.name);
                            });
                        }
                    });
                }

                if (RouteManager.Settings.waitUntilFull && LocoTelem.RouteMode[car])
                {
                    builder.Spacer(4f);
                    builder.AddButtonCompact("Force Departure", new Action(() => { LocoTelem.clearedForDeparture[car] = true; }));
                }

                bool anyStationSelected = DestinationManager.IsAnyStationSelectedForLocomotive(car);

                /**********************************************************************************
                *
                *
                *        END Major MOD GUI additions
                *
                *
                **********************************************************************************/
            }



            /**********************************************************************************
            *
            *
            *        ORIGINAL RailRoader Logic Except otherwise noted
            *
            *
            **********************************************************************************/

            if (mode2 == AutoEngineerMode.Yard)
            {
                //
                //
                //
                //PUT CODE HERE TO DISABLE ROUTE MODE
                //
                //
                //
                //
                //


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

            if (mode2 == AutoEngineerMode.Road)
            {
                builder.Spacer(4f);
            }
            else
            {
                builder.AddExpandingVerticalSpacer();
            }

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

            /**********************************************************************************
            *
            *
            *        END ORIGINAL RailRoader Logic Except otherwise noted
            *
            *
            **********************************************************************************/

            // Prevent the original method from running
            return false; 
        }

        private static void UpdateManagedTrainsStopStations(Car car)
        {
            // Get the list of all selected stations
            var allStops = PassengerStop.FindAll();
            var selectedStations = allStops.Where(stop => DestinationManager.IsStopStationSelected(stop, car)).ToList();

            // Update the ManagedTrains with the selected stations for this car
            DestinationManager.SetStopStations(car, selectedStations);
        }
    }
}
