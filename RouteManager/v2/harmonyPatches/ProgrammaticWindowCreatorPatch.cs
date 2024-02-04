using HarmonyLib;
using UI.Builder;
using UnityEngine;
using RouteManager.v2.core;
using RouteManager.v2.dataStructures;
using RouteManager.v2.Logging;
using RouteManager.v2.UI;
using UI;
using System.Reflection;
using UI.Common;
using UI.Equipment;
using System;



namespace RouteManager.v2.harmonyPatches
{


    [HarmonyPatch(typeof(ProgrammaticWindowCreator))]
    public static class ProgrammaticWindowCreatorPatch
    {
        public static UIBuilderAssets builderAssets;
        static Window windowPrefab;
        static Transform transform;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProgrammaticWindowCreator), "Start")]
        public static void Start(ProgrammaticWindowCreator __instance)
        {
            RouteManager.logger.LogToDebug($"ProgrammaticWindowCreator.Start()", LogLevel.Trace);
            builderAssets = __instance.builderAssets;
            windowPrefab = __instance.windowPrefab;
            transform = __instance.transform;


            CreateWindow<RouteManagerWindow>(600, 500, Window.Position.Center, null);

            RouteManager.logger.LogToDebug($"ProgrammaticWindowCreator.Start() Finished", LogLevel.Trace);
        }


        private static void CreateWindow<TWindow>(int width, int height, Window.Position position, Action<TWindow> configure = null) where TWindow : Component, IBuilderWindow
        {
            Window window = CreateWindow(width, height, position);
            window.name = typeof(TWindow).ToString();
            TWindow twindow = window.gameObject.AddComponent<TWindow>();
            twindow.BuilderAssets = builderAssets;
            window.CloseWindow();
            if (configure != null)
            {
                configure(twindow);
            }
        }

        // Token: 0x06000929 RID: 2345 RVA: 0x0004BFD1 File Offset: 0x0004A1D1
        private static Window CreateWindow(int width, int height, Window.Position position)
        {
            Window window = UnityEngine.Object.Instantiate<Window>(windowPrefab, transform, false);
            window.SetContentSize(new Vector2((float)width, (float)height));
            window.SetPosition(position);
            return window;
        }
    }
}