using HarmonyLib;
using System.Net;
using System;
using System.Reflection;
using Track;
using RouteManager.v2.Logging;
using Microsoft.SqlServer.Server;
using TriangleNet.Geometry;



namespace RouteManager.v2.harmonyPatches
{

    
    [HarmonyPatch(typeof(Graph))]
    public static class GraphPatch
    {
        public delegate void delSegmentsReachableFrom(TrackSegment segment, TrackSegment.End end, out TrackSegment normal, out TrackSegment reversed);
        public delegate void delCheckSwitchAgainstMovement(TrackSegment seg, TrackSegment nextSegment, TrackNode node);

        public static delSegmentsReachableFrom SegmentsReachableFrom;
        public static delCheckSwitchAgainstMovement CheckSwitchAgainstMovement;


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Graph), "Awake")]
        public static void Awake(Graph __instance)
        {

            RouteManager.logger.LogToDebug($"Graph.Awake()", LogLevel.Trace);

            //set up delegates to access private methods
            /*
            SegmentsReachableFrom = (DelSegmentsReachableFrom)Delegate.CreateDelegate(typeof(DelSegmentsReachableFrom),
                                                                                        null,
                                                                                        Graph.Shared.GetType().GetMethod("SegmentsReachableFrom",
                                                                                        BindingFlags.NonPublic | BindingFlags.Instance)
                                                                                     );
            */

            /*
                MethodInfo dynMethod = Graph.Shared.GetType().GetMethod("SegmentsReachableFrom",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(Graph.Shared, new object[] { segment, end, normal, reversed });
            */

            SegmentsReachableFrom = (delSegmentsReachableFrom)Delegate.CreateDelegate(typeof(delSegmentsReachableFrom),
                                                                                        Graph.Shared,
                                                                                        Graph.Shared.GetType().GetMethod("SegmentsReachableFrom",
                                                                                            BindingFlags.NonPublic | BindingFlags.Instance)
                                                                                      );

            CheckSwitchAgainstMovement = (delCheckSwitchAgainstMovement)Delegate.CreateDelegate(typeof(delCheckSwitchAgainstMovement),
                                                                                        Graph.Shared,
                                                                                        Graph.Shared.GetType().GetMethod("CheckSwitchAgainstMovement",
                                                                                        BindingFlags.NonPublic | BindingFlags.Instance)
                                                                                     );

            RouteManager.logger.LogToDebug($"EXITING Graph.Awake()", LogLevel.Trace);
        }

        /*public static void DelSegmentsReachableFrom(TrackSegment segment, TrackSegment.End end, out TrackSegment normal, out TrackSegment reversed)
        {
            SegmentsReachableFrom(segment, end, out normal, out reversed);
        }*/

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Graph), "SegmentsReachableFrom")]
        public static void SegmentsReachableFrom(Graph __instance)
        {
            RouteManager.logger.LogToDebug($"Graph.SegmentsReachableFrom()", LogLevel.Trace);
            
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Graph), "SegmentsReachableFrom")]
        public static void SegmentsReachableFromPost(Graph __instance)
        {
            RouteManager.logger.LogToDebug($"Leaving Graph.SegmentsReachableFrom()", LogLevel.Trace);

        }
        */
    }
    
}