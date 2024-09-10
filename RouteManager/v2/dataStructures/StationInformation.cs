using RollingStock;
using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;

namespace RouteManager.v2.dataStructures
{
    public class StationInformation
    {
        public static Dictionary<string, StationMapData> Stations = new Dictionary<string, StationMapData>()
        /*
        {
            { "sylva",      new StationMapData(24634.5f, 620.57f, -941.23f, 24563.24f, 620.57f, -935.94f, 24598.87f, 620.57f, -938.585f, 71.45608232f) },
            { "dillsboro",  new StationMapData(22379.87f, 603.17f, -1410.88f, 22326.76f, 603.17f, -1434.12f, 22353.315f, 603.17f, -1422.5f, 57.9721459f) },
            { "wilmot",     new StationMapData(16511.31f, 569.97f, 2326.23f, 16493.52f, 569.97f, 2329.14f, 16502.415f, 569.97f, 2327.685f, 18.0264306f) },
            { "whittier",   new StationMapData(12267.1f, 561.45f, 5864.33f, 12279.19f, 561.45f, 5893.68f, 12273.145f, 561.45f, 5879.005f, 31.74256763f) },
            { "ela",        new StationMapData(9569.54f, 546.61f, 7404.1f, 9554.41f, 546.61f, 7409.92f, 9561.975f, 546.61f, 7407.01f, 16.21077728f) },
            { "bryson",     new StationMapData(4530.43f, 528.97f, 5428.56f, 4473.52f, 528.97f, 5407.87f, 4501.975f, 528.97f, 5418.215f, 60.55430786f) },
            { "hemingway",  new StationMapData(2820.64f, 578.52f, 3079.64f, 2815.72f, 578.54f, 3055.54f, 2818.18f, 578.53f, 3067.59f, 24.59708926f) },
            { "alarkajct",  new StationMapData(1745.6f, 590.23f, 1503.32f, 1737.93f, 589.78f, 1425.91f, 1741.765f, 590.005f, 1464.615f, 77.79035609f) },
            { "cochran",    new StationMapData(1996.88f, 591.62f, -205.13f, 2007.29f, 591.85f, -218.98f, 2002.085f, 591.735f, -212.055f, 17.32753589f) },
            { "alarka",     new StationMapData(4170.52f, 644.81f, -3113.05f, 4201.17f, 645.24f, -3140.48f, 4185.845f, 645.025f, -3126.765f, 41.13407711f) },
            //{ "alarkajctn", new StationMapData(1738.56f, 590.22f, 1504.86f, 1713.16f, 589.78f, 1431.7f, 1725.86f, 590f, 1468.28f, 77.44507215f)  },
            { "almond",     new StationMapData(-6340.3f, 524.97f, -1291.01f, -6316.44f, 524.97f, -1347.1f, -6328.37f, 524.97f, -1319.055f, 60.95398018f) },
            { "nantahala",  new StationMapData(-15594.29f, 595.2f, -10588.8f, -15642.63f, 595.51f, -10646.29f, -15618.46f, 595.355f, -10617.545f, 75.11292698f) },
            { "topton",     new StationMapData(-18969.52f, 793.22f, -15217.75f, -18977.49f, 792.7f, -15231.27f, -18973.505f, 792.96f, -15224.51f, 15.70292011f) },
            { "rhodo",      new StationMapData(-22993.12f, 653.53f, -18005.08f, -23014.11f, 653.15f, -18030.5f, -23003.615f, 653.34f, -18017.79f, 32.96818011f) },
            { "andrews",    new StationMapData(-29923.78f, 538.97f, -20057.8f, -29990.74f, 538.97f, -20092.33f, -29957.26f, 538.97f, -20075.065f, 75.33898393f) }
        }*/;

        public static List<string> OrderedStations { get; private set; } = new List<string>();

        /*
        {
            "sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "cochran", "alarka",
            "almond", "nantahala", "topton", "rhodo", "andrews"
        };*/

        public static Dictionary<string, List<PassengerStop>> Branches { get; private set; } = new Dictionary<string, List<PassengerStop>>();

        public static void BuildStationMap()
        {
            Stations.Clear();
            Branches.Clear();

            List<PassengerStop> visited = new List<PassengerStop>();

            //Get fitst station and check if it has neighbours
            PassengerStop start = PassengerStop.FindAll().First();

            //check
            if (start == null)
            {
                RouteManager.logger.LogToDebug("No passenger stops loaded. Nothing to do!");
                return;
            }

            if (start.neighbors.Count() != 1)
            {
                RouteManager.logger.LogToDebug($"First passenger stop has wrong number of neighbours! ({start.neighbors.Count()} neighbours). ");
                return;
            }

            //track that we've visited the station
            visited.Add(start);

            //build a list of stations on each branch, including the main line
            Branches.Add("main", new List<PassengerStop>());
            Branches["main"].Add(start);
            GetStationData(start, "main");

            ExploreNeighbours(start.neighbors.First(), ref visited);

            RouteManager.logger.LogToDebug($"Visited stations: {string.Join(", ", visited.Select(s => s.DisplayName).ToArray())}");

            RouteManager.logger.LogToDebug($"Branches: {string.Join(", ", Branches.Select(b => b.Key).ToArray())}");

            foreach(string branch in Branches.Keys)
            {
                RouteManager.logger.LogToDebug($"Branch {branch}: {string.Join(", ", Branches[branch].Select(s => s.DisplayName).ToArray())}");
            }

        }

        public static void BuildOrderedList()
        {
            OrderedStations.Clear();

            //we'll traverse the main branch and insert sub branches ordered by length of branch
            foreach(PassengerStop ps in Branches["main"])
            {
                //add the current station to the list
                OrderedStations.Add(ps.identifier);

                //does it have any branches?
                if(Stations[ps.identifier].Branches.Count() > 0)
                {
                    Stations[ps.identifier].Branches.Sort();

                    //now lets list the stations in each branch
                    foreach (string branch in Stations[ps.identifier].Branches)
                    {
                        foreach(PassengerStop station in Branches[branch])
                        {
                            OrderedStations.Add(station.identifier);
                        }
                    }
                }
            }

            RouteManager.logger.LogToDebug($"Ordered Stations: {string.Join(", ", OrderedStations.ToArray())}");
        }

        private static void GetStationData(PassengerStop station, string branch)
        {
            if(!Stations.ContainsKey(station.identifier))
            {
                Vector3? pos0 =  station.TrackSpans.First()?.lower?.GetPosition();
                Vector3? pos1 =  station.TrackSpans.First()?.upper?.GetPosition();
                Vector3? centre = station.TrackSpans.First()?.GetCenterPoint();
                float? len = station.TrackSpans.First()?.Length;

                if (pos0 != null && pos1 != null && centre != null && len != null) 
                {
                    bool hardEndOfLine = station.neighbors.Count() <= 1;

                    StationMapData data = new StationMapData((float)pos0?.x, (float)pos0?.y, (float)pos0?.z, (float)pos1?.x, (float)pos1?.y, (float)pos1?.z, (float)centre?.x, (float)centre?.y, (float)centre?.z, (float)len, hardEndOfLine);
                    data.Branch = branch;
                    data.station = station;

                    Stations.Add(station.identifier, data);

                    RouteManager.logger.LogToDebug($"Station Data {station.identifier}: {(float)pos0?.x}, {(float)pos0?.y}, {(float)pos0?.z}, {(float)pos1?.x}, {(float)pos1?.y}, {(float)pos1?.z}, {(float)centre?.x}, {(float)centre?.y}, {(float)centre?.z}, {(float)len}");
                }
                
            }
        }
        

        private static string ExploreNeighbours(PassengerStop current, ref List<PassengerStop> visited)
        {
            string branch = "main";

            //Make sure we don't go here twice
            visited.Add(current);

            //is this station on the mainline?
            bool hasMainline = HasMainLineTrack(current, out string groupId); //no tracks are mainline

            if (!hasMainline)
            { 
                //check if the one and only neighbour is mainline and only has 2 neighbours
                if (current.neighbors.Count() == 1 && (HasMainLineTrack(current.neighbors.First(), out string neighbourGroupId) && current.neighbors.First().neighbors.Count() == 2))
                {
                    //for now we will assume this is the end of the mainline and not a spur to a single station
                    //nothing to do
                }
                else
                {
                    branch = groupId;

                    if (!Branches.ContainsKey(branch))
                    {
                        Branches.Add(branch, new List<PassengerStop>());
                    }
                }
            }

            Branches[branch].Add(current);
            GetStationData(current, branch);


            //check all neighbours to see if it has been visited before
            foreach (PassengerStop neighbour in current.neighbors)
            {
                if (!visited.Contains(neighbour))
                {
                    string neighbourGroupId = ExploreNeighbours(neighbour, ref visited);

                    //check if the neighbour is on the same branch
                    if (neighbourGroupId != branch)
                    {
                        //it's not, so let's check if we are already aware of the conencting branch
                        if (!Stations[current.identifier].Branches.Contains(neighbourGroupId))
                        {
                            //not currently know, add to the list
                            Stations[current.identifier].Branches.Add(neighbourGroupId);
                        }
                    }
                }
            }

            return branch;
        }

        private static bool HasMainLineTrack(PassengerStop current, out string groupId)
        {
            bool hasMainline = false;
            groupId = "";

            foreach (TrackSpan ts in current.TrackSpans)
            {
                RouteManager.logger.LogToDebug($"Station: {current.DisplayName}, Class: {ts.lower?.segment.trackClass}, GroupID: {ts.lower?.segment.groupId}");
                if (ts.lower?.segment.trackClass == TrackClass.Mainline)
                {
                    hasMainline = true;
                }

                //we will jus take the last groupId as THE groupId
                groupId = ts.lower?.segment.groupId;
            }

            RouteManager.logger.LogToDebug($"Station: {current.DisplayName} has a mainline track: {hasMainline}");

            RouteManager.logger.LogToDebug($"Station: {current.DisplayName}, Neighbours: {string.Join(", ", current.neighbors.Select(s => s.DisplayName))}");

            return hasMainline;
        }
    }
}
