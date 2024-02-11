using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Track;

namespace RouteManager.v2.dataStructures
{
    public class RouteSwitchData
    {
        public TrackNode trackSwitch;       //switch we're interested in
        public TrackSegment segmentFrom;    //track we're coming from
        public TrackSegment segmentTo;      //track we're going to
        public bool requiredStateNormal;    //switch state to make the traversal
        public bool isDecision;             //this switch selects between two platforms


        public RouteSwitchData(TrackNode trackSwitch, TrackSegment segmentFrom, TrackSegment segmentTo, bool requiredStateNormal)
        {
            this.trackSwitch = trackSwitch;
            this.segmentFrom = segmentFrom;
            this.segmentTo = segmentTo;
            this.requiredStateNormal = requiredStateNormal;
        }

        public override string ToString()
        {
            return $"Switch ID: {this.trackSwitch.id}, From: {segmentFrom.id}, To: {segmentTo.id}, Is Decision: {this.isDecision}";
        }
    }

    public class RouteSwitchDataComparer : IEqualityComparer<RouteSwitchData>
    {

        #region IEqualityComparer<ThisClass> Members


        public bool Equals(RouteSwitchData x, RouteSwitchData y)
        {
            //no null check here, you might want to do that, or correct that to compare just one part of your object
            return x.trackSwitch == y.trackSwitch;
        }


        public int GetHashCode(RouteSwitchData obj)
        {
            unchecked
            {
                var hash = 17;
                //same here, if you only want to get a hashcode on a, remove the line with b
                hash = hash * 23 + obj.trackSwitch.GetHashCode();
                hash = hash * 23 + obj.trackSwitch.GetHashCode();

                return hash;
            }
        }

        #endregion
    }
}
